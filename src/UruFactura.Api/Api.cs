#:sdk Microsoft.NET.Sdk.Web
#:project ../UruFacturaSDK/UruFacturaSDK.csproj
#:package Microsoft.AspNetCore.OpenApi@10.*-*
#:package Scalar.AspNetCore@2.*

using Microsoft.AspNetCore.Mvc;
using Scalar.AspNetCore;
using System.Text.Json.Serialization.Metadata;
using UruFacturaSDK;
using UruFacturaSDK.Configuration;
using UruFacturaSDK.Enums;
using UruFacturaSDK.Models;

// ---------------------------------------------------------------------------
// Configuración
// ---------------------------------------------------------------------------

var builder = WebApplication.CreateBuilder(args);

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
        RutaCertificado        = cfg["UruFactura:RutaCertificado"]        ?? "cert.p12",
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

    // Totales calculados a partir del detalle
    foreach (var linea in doc.Detalle)
    {
        switch (linea.IndFactIva)
        {
            case TipoIva.Basico:
                doc.MontoNetoBasico += linea.MontoTotal;
                break;
            case TipoIva.Minimo:
                doc.MontoNetoMinimo += linea.MontoTotal;
                break;
            case TipoIva.Exento:
                doc.MontoNetoExento += linea.MontoTotal;
                break;
            case TipoIva.Suspendido:
                doc.MontoNetoSuspendido += linea.MontoTotal;
                break;
        }
    }

    doc.IvaBasico  = Math.Round(doc.MontoNetoBasico * 0.22m, 2);
    doc.IvaMinimo  = Math.Round(doc.MontoNetoMinimo * 0.10m, 2);
    doc.MontoTotal = doc.MontoNetoBasico + doc.IvaBasico
                   + doc.MontoNetoMinimo + doc.IvaMinimo
                   + doc.MontoNetoExento + doc.MontoNetoSuspendido;
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
