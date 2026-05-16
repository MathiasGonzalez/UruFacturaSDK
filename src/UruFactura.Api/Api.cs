#:sdk Microsoft.NET.Sdk.Web
#:project ../UruFacturaSDK/UruFacturaSDK.csproj
#:package Microsoft.AspNetCore.OpenApi@10.*-*
#:package Scalar.AspNetCore@2.*

using Microsoft.AspNetCore.Mvc;
using Scalar.AspNetCore;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using UruFacturaSDK;
using UruFacturaSDK.Configuration;
using UruFacturaSDK.Enums;
using UruFacturaSDK.Models;

// ---------------------------------------------------------------------------
// Configuración
// ---------------------------------------------------------------------------

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Certificate resolution
// ---------------------------------------------------------------------------
// Support loading the certificate from a Base64 env var (useful in containers
// where mounting a file is less convenient than an environment variable / secret).
// Set UruFactura:CertificadoBase64 to the Base64-encoded content of the .p12 file.
// If both are set, CertificadoBase64 takes precedence over RutaCertificado.
var certBase64 = builder.Configuration["UruFactura:CertificadoBase64"];
string? tempCertPath = null;

if (!string.IsNullOrEmpty(certBase64))
{
    var certBytes = Convert.FromBase64String(certBase64);
    tempCertPath = Path.Combine(Path.GetTempPath(), "urufactura_cert.p12");
    File.WriteAllBytes(tempCertPath, certBytes);
}

builder.Services.AddSingleton<IUruFacturaClient>(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();

    var config = new UruFacturaConfig
    {
        RutEmisor              = cfg["UruFactura:RutEmisor"]              ?? "000000000000",
        RazonSocialEmisor      = cfg["UruFactura:RazonSocialEmisor"]      ?? "EMPRESA DE PRUEBA SA",
        NombreComercialEmisor  = cfg["UruFactura:NombreComercialEmisor"],
        Giro                   = cfg["UruFactura:Giro"],
        DomicilioFiscal        = cfg["UruFactura:DomicilioFiscal"]        ?? "18 DE JULIO 1234",
        Ciudad                 = cfg["UruFactura:Ciudad"]                 ?? "MONTEVIDEO",
        Departamento           = cfg["UruFactura:Departamento"]           ?? "MONTEVIDEO",
        RutaCertificado        = tempCertPath ?? cfg["UruFactura:RutaCertificado"] ?? "cert.p12",
        PasswordCertificado    = cfg["UruFactura:PasswordCertificado"]    ?? "",
        Ambiente               = Enum.Parse<Ambiente>(cfg["UruFactura:Ambiente"] ?? "Homologacion"),
        OmitirValidacionSsl    = bool.Parse(cfg["UruFactura:OmitirValidacionSsl"] ?? "false"),
    };

    return UruFacturaClientBuilder.WithDefaults(config).Build();
});

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.TypeInfoResolverChain.Insert(0, new DefaultJsonTypeInfoResolver()));

builder.Services.AddOpenApi();

var app = builder.Build();

// Delete the temp cert file when the app stops to avoid leaving sensitive
// certificate material on disk longer than needed.
if (tempCertPath != null)
{
    app.Lifetime.ApplicationStopped.Register(() =>
    {
        try { File.Delete(tempCertPath); } catch { /* best-effort */ }
    });
}

// ---------------------------------------------------------------------------
// Startup: pre-load CAEs from configuration
// ---------------------------------------------------------------------------
// Set UruFactura:Caes to a JSON array to seed CAEs on startup — handy in
// container environments where the manager starts empty after each restart.
//
// Example (env var or appsettings.json):
//   UruFactura__Caes = '[{"NroSerie":"CAE001","Tipo":101,"RangoDesde":1,"RangoHasta":1000,"FechaVencimiento":"2026-12-31"}]'
//
// Tipo values: ETicket=101, EFactura=111, ERemito=124, ...
var caesJson = app.Configuration["UruFactura:Caes"];
if (!string.IsNullOrWhiteSpace(caesJson))
{
    var caeDtos = JsonSerializer.Deserialize<List<CaeConfigRequest>>(caesJson,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

    if (caeDtos?.Count > 0)
    {
        var sdkClient = app.Services.GetRequiredService<IUruFacturaClient>();
        sdkClient.Cae.RegistrarCaes(caeDtos.Select(d => d.ToModel()));
    }
}

if (app.Environment.IsDevelopment())
{
    Console.WriteLine("UruFactura API - Modo Desarrollo");
    app.MapOpenApi();
    app.MapScalarApiReference(o => o.WithTitle("UruFactura API"));

    //Open default route to API reference page
    app.MapGet("/", () => Results.Redirect("/scalar/v1"));

}

// ---------------------------------------------------------------------------
// Endpoints
// ---------------------------------------------------------------------------

var cfe = app.MapGroup("/cfe").WithTags("CFE");

// --- Generar XML de e-Ticket (sin enviar a DGI) ---
cfe.MapPost("/eticket/xml", (
    [FromBody] CfeRequest req,
    IUruFacturaClient client) =>
{
    var doc = client.CrearETicket();
    MapRequest(doc, req);
    var xml = client.GenerarYFirmar(doc);
    return Results.Text(xml, "application/xml");
})
.WithName("GenerarXmlETicket")
.WithSummary("Genera y firma el XML de un e-Ticket (sin enviar a DGI)");

// --- Generar XML de e-Factura (sin enviar a DGI) ---
cfe.MapPost("/efactura/xml", (
    [FromBody] CfeRequest req,
    IUruFacturaClient client) =>
{
    var doc = client.CrearEFactura();
    MapRequest(doc, req);
    var xml = client.GenerarYFirmar(doc);
    return Results.Text(xml, "application/xml");
})
.WithName("GenerarXmlEFactura")
.WithSummary("Genera y firma el XML de una e-Factura (sin enviar a DGI)");

// --- Enviar e-Ticket a DGI ---
cfe.MapPost("/eticket/enviar", async (
    [FromBody] CfeRequest req,
    IUruFacturaClient client,
    CancellationToken ct) =>
{
    var doc = client.CrearETicket();
    MapRequest(doc, req);
    var resp = await client.EnviarCfeAsync(doc, ct);
    return resp.Exitoso ? Results.Ok(resp) : Results.BadRequest(resp);
})
.WithName("EnviarETicket")
.WithSummary("Firma y envía un e-Ticket a la DGI");

// --- Enviar e-Factura a DGI ---
cfe.MapPost("/efactura/enviar", async (
    [FromBody] CfeRequest req,
    IUruFacturaClient client,
    CancellationToken ct) =>
{
    var doc = client.CrearEFactura();
    MapRequest(doc, req);
    var resp = await client.EnviarCfeAsync(doc, ct);
    return resp.Exitoso ? Results.Ok(resp) : Results.BadRequest(resp);
})
.WithName("EnviarEFactura")
.WithSummary("Firma y envía una e-Factura a la DGI");

// --- Consultar estado de un CFE ---
cfe.MapPost("/consultar", async (
    [FromBody] CfeRequest req,
    IUruFacturaClient client,
    CancellationToken ct) =>
{
    var doc = client.CrearETicket();
    MapRequest(doc, req);
    var resp = await client.ConsultarEstadoCfeAsync(doc, ct);
    return resp.Exitoso ? Results.Ok(resp) : Results.BadRequest(resp);
})
.WithName("ConsultarEstadoCfe")
.WithSummary("Consulta el estado de un CFE en la DGI");

// --- Generar PDF A4 ---
cfe.MapPost("/eticket/pdf/a4", (
    [FromBody] CfeRequest req,
    IUruFacturaClient client) =>
{
    var doc = client.CrearETicket();
    MapRequest(doc, req);
    var pdf = client.GenerarPdfA4(doc);
    return Results.File(pdf, "application/pdf", $"eticket_{doc.Numero}.pdf");
})
.WithName("GenerarPdfA4ETicket")
.WithSummary("Genera el PDF A4 de un e-Ticket");

// --- Generar PDF Térmico ---
cfe.MapPost("/eticket/pdf/termico", (
    [FromBody] CfeRequest req,
    IUruFacturaClient client) =>
{
    var doc = client.CrearETicket();
    MapRequest(doc, req);
    var pdf = client.GenerarPdfTermico(doc);
    return Results.File(pdf, "application/pdf", $"eticket_{doc.Numero}_termico.pdf");
})
.WithName("GenerarPdfTermicoETicket")
.WithSummary("Genera el PDF térmico (ticket) de un e-Ticket");

// --- Listar CAEs ---
app.MapGet("/cae", (IUruFacturaClient client) =>
    Results.Ok(client.Cae.ObtenerTodosLosCaes()))
.WithTags("CAE")
.WithName("ListarCaes")
.WithSummary("Lista los CAEs cargados en memoria");

// --- Registrar un CAE en tiempo de ejecución ---
// Útil cuando los CAEs se cargan dinámicamente (p.ej. desde una BD externa o API propia)
// en lugar de desde la variable de entorno UruFactura:Caes al inicio.
app.MapPost("/cae", ([FromBody] CaeConfigRequest req, IUruFacturaClient client) =>
{
    var cae = req.ToModel();
    client.Cae.RegistrarCae(cae);
    return Results.Created($"/cae/{cae.NroSerie}", cae);
})
.WithTags("CAE")
.WithName("RegistrarCae")
.WithSummary("Registra un CAE en memoria en tiempo de ejecución");

// --- Advertencias de CAEs ---
app.MapGet("/cae/advertencias", (IUruFacturaClient client) =>
    Results.Ok(client.Cae.ObtenerAdvertencias()))
.WithTags("CAE")
.WithName("AdvertenciasCaes")
.WithSummary("Devuelve advertencias de CAEs por vencer o con alto porcentaje de uso");

app.Run();

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

static void MapRequest(Cfe doc, CfeRequest req)
{
    doc.Numero     = req.Numero;
    doc.Serie      = req.Serie;
    doc.FormaPago  = req.FormaPago;
    doc.Moneda     = req.Moneda;
    doc.Receptor   = req.Receptor;
    doc.Detalle    = req.Detalle;

    doc.CalcularTotales();
}

// ---------------------------------------------------------------------------
// DTOs
// ---------------------------------------------------------------------------

record CfeRequest(
    long              Numero,
    string?           Serie,
    FormaPago         FormaPago,
    Moneda            Moneda,
    Receptor?         Receptor,
    List<LineaDetalle> Detalle);

// DTO for registering / pre-loading a CAE.
// Tipo must be the integer value of TipoCfe (e.g. ETicket = 101, EFactura = 111).
// FechaVencimiento must be an ISO 8601 date string (yyyy-MM-dd).
record CaeConfigRequest(
    string NroSerie,
    int    Tipo,
    long   RangoDesde,
    long   RangoHasta,
    string FechaVencimiento,
    long   UltimoNroUsado = 0)
{
    public Models.Cae ToModel() => new()
    {
        NroSerie         = NroSerie,
        TipoCfe          = (TipoCfe)Tipo,
        RangoDesde       = RangoDesde,
        RangoHasta       = RangoHasta,
        FechaVencimiento = DateOnly.Parse(FechaVencimiento),
        UltimoNroUsado   = UltimoNroUsado,
    };
}
