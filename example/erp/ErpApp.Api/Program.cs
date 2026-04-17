using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using ErpApp.Api;
using UruFacturaSDK;
using UruFacturaSDK.Configuration;
using UruFacturaSDK.Enums;
using UruFacturaSDK.Models;

var builder = WebApplication.CreateBuilder(args);

builder.AddNpgsqlDbContext<AppDbContext>("erpdb");

builder.Services.AddCors(o =>
    o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// NOTE: EnsureCreatedAsync creates the schema on first run but does NOT apply
// incremental schema changes to an existing database. If the Invoice table
// already exists and new columns were added (e.g. MontoNetoExento, DetalleJson),
// drop the database or run a migration before starting the app.
using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();

app.UseCors();

// -----------------------------------------------------------------------
// Endpoints
// -----------------------------------------------------------------------

// Returns all supported CFE types.
app.MapGet("/api/cfe-types", () =>
    Enum.GetValues<TipoCfe>()
        .Select(t => new { value = (int)t, label = t.ToString() })
        .OrderBy(t => t.value));

// Returns the current UruFactura configuration status so the frontend can
// show whether the environment is properly set up before attempting to sign.
app.MapGet("/api/config/status", (IConfiguration config) =>
{
    var section = config.GetSection("UruFactura");
    var ufConfig = section.Get<UruFacturaConfig>() ?? new UruFacturaConfig();

    var issues = new List<string>();

    if (string.IsNullOrWhiteSpace(ufConfig.RutEmisor))
        issues.Add("RutEmisor no configurado.");
    if (string.IsNullOrWhiteSpace(ufConfig.RazonSocialEmisor))
        issues.Add("RazonSocialEmisor no configurado.");
    if (string.IsNullOrWhiteSpace(ufConfig.DomicilioFiscal))
        issues.Add("DomicilioFiscal no configurado.");
    if (string.IsNullOrWhiteSpace(ufConfig.RutaCertificado))
        issues.Add("RutaCertificado no configurado.");
    else if (!File.Exists(ufConfig.RutaCertificado))
        issues.Add($"Certificado no encontrado: {ufConfig.RutaCertificado}");
    if (string.IsNullOrWhiteSpace(ufConfig.PasswordCertificado))
        issues.Add("PasswordCertificado no configurado.");

    return new
    {
        ok = issues.Count == 0,
        ambiente = ufConfig.Ambiente.ToString(),
        rutEmisor = ufConfig.RutEmisor,
        razonSocial = ufConfig.RazonSocialEmisor,
        certificado = ufConfig.RutaCertificado,
        certificadoExiste = !string.IsNullOrWhiteSpace(ufConfig.RutaCertificado) && File.Exists(ufConfig.RutaCertificado),
        issues,
    };
});

app.MapGet("/api/invoices", async (AppDbContext db) =>
    await db.Invoices.OrderByDescending(i => i.FechaEmision).ToListAsync());

app.MapPost("/api/invoices", async (CreateInvoiceRequest req, AppDbContext db, IConfiguration config) =>
{
    var ufConfig = config.GetSection("UruFactura").Get<UruFacturaConfig>()!;

    // NOTE: Instantiated per request for demo simplicity.
    // In production, keep UruFacturaClient per request (scoped/transient) because it may hold mutable state
    // and should not be shared as a singleton across concurrent requests. If certificate loading becomes a
    // performance concern, cache/load the certificate or other immutable configuration once instead.
    using var client = new UruFacturaClient(ufConfig);

    var tipo = (TipoCfe)req.TipoCfe;

    var cfe = tipo switch
    {
        TipoCfe.ETicket                        => client.CrearETicket(),
        TipoCfe.NotaCreditoETicket             => client.CrearNotaCreditoETicket(),
        TipoCfe.NotaDebitoETicket              => client.CrearNotaDebitoETicket(),
        TipoCfe.EFactura                       => client.CrearEFactura(),
        TipoCfe.NotaCreditoEFactura            => client.CrearNotaCreditoEFactura(),
        TipoCfe.NotaDebitoEFactura             => client.CrearNotaDebitoEFactura(),
        TipoCfe.EFacturaExportacion            => client.CrearEFacturaExportacion(),
        TipoCfe.ERemito                        => client.CrearERemito(),
        _                                      => throw new ArgumentException($"Tipo de CFE no soportado: {tipo}"),
    };

    cfe.Numero = req.Numero;

    if (!string.IsNullOrWhiteSpace(req.RutReceptor))
        cfe.Receptor = new Receptor { Documento = req.RutReceptor, RazonSocial = req.NombreReceptor };

    for (int i = 0; i < req.Detalle.Count; i++)
    {
        var l = req.Detalle[i];
        cfe.Detalle.Add(new LineaDetalle
        {
            NroLinea = i + 1,
            NombreItem = l.NombreItem,
            Cantidad = l.Cantidad,
            PrecioUnitario = l.PrecioUnitario,
            IndFactIva = (TipoIva)l.IndFactIva,
        });
    }

    foreach (var r in req.Referencias ?? [])
    {
        if (!Enum.IsDefined(typeof(TipoCfe), r.TipoCfe))
            return Results.BadRequest(new { detail = $"TipoCfe de referencia inválido: {r.TipoCfe}" });

        cfe.Referencias.Add(new RefCfe
        {
            TipoCfe = (TipoCfe)r.TipoCfe,
            Serie = r.Serie ?? string.Empty,
            NroCfe = r.NroCfe,
            FechaCfe = r.FechaCfe,
            Razon = r.Razon,
        });
    }

    cfe.CalcularTotales();
    client.GenerarYFirmar(cfe);

    var invoice = new Invoice
    {
        TipoCfe = (int)cfe.Tipo,
        Numero = cfe.Numero,
        FechaEmision = cfe.FechaEmision,
        RutReceptor = cfe.Receptor?.Documento,
        NombreReceptor = cfe.Receptor?.RazonSocial,
        MontoTotal = cfe.MontoTotal,
        MontoNetoExento = cfe.MontoNetoExento,
        MontoNetoMinimo = cfe.MontoNetoMinimo,
        MontoNetoBasico = cfe.MontoNetoBasico,
        IvaMinimo = cfe.IvaMinimo,
        IvaBasico = cfe.IvaBasico,
        XmlFirmado = cfe.XmlFirmado,
        DetalleJson = JsonSerializer.Serialize(cfe.Detalle),
    };

    db.Invoices.Add(invoice);
    await db.SaveChangesAsync();

    return Results.Created($"/api/invoices/{invoice.Id}", invoice);
});

app.MapGet("/api/invoices/{id:int}/pdf", async (int id, AppDbContext db, IConfiguration config) =>
{
    var invoice = await db.Invoices.FindAsync(id);
    if (invoice is null) return Results.NotFound();

    var ufConfig = config.GetSection("UruFactura").Get<UruFacturaConfig>()!;

    // NOTE: See note in POST /api/invoices about per-request instantiation.
    using var client = new UruFacturaClient(ufConfig);

    var cfe = new Cfe
    {
        Tipo = (TipoCfe)invoice.TipoCfe,
        Numero = invoice.Numero,
        FechaEmision = invoice.FechaEmision,
        MontoTotal = invoice.MontoTotal,
        MontoNetoExento = invoice.MontoNetoExento,
        MontoNetoMinimo = invoice.MontoNetoMinimo,
        MontoNetoBasico = invoice.MontoNetoBasico,
        IvaMinimo = invoice.IvaMinimo,
        IvaBasico = invoice.IvaBasico,
        RutEmisor = ufConfig.RutEmisor,
        RazonSocialEmisor = ufConfig.RazonSocialEmisor,
        DomicilioFiscalEmisor = ufConfig.DomicilioFiscal,
        CiudadEmisor = ufConfig.Ciudad,
        DepartamentoEmisor = ufConfig.Departamento,
        XmlFirmado = invoice.XmlFirmado,
    };

    if (!string.IsNullOrWhiteSpace(invoice.DetalleJson))
    {
        try
        {
            var detalle = JsonSerializer.Deserialize<List<LineaDetalle>>(invoice.DetalleJson);
            if (detalle is not null)
                cfe.Detalle.AddRange(detalle);
        }
        catch (JsonException)
        {
            return Results.Problem(
                detail: "La factura no puede generar el PDF porque su detalle almacenado es inválido o incompatible.",
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Detalle de factura inválido");
        }
    }

    if (!string.IsNullOrWhiteSpace(invoice.RutReceptor))
        cfe.Receptor = new Receptor { Documento = invoice.RutReceptor, RazonSocial = invoice.NombreReceptor };

    var pdf = client.GenerarPdfA4(cfe);
    return Results.File(pdf, "application/pdf", $"factura-{invoice.Numero}.pdf");
});

app.Run();

// -----------------------------------------------------------------------
// Request / Response DTOs
// -----------------------------------------------------------------------

record CreateInvoiceRequest(
    int TipoCfe,
    long Numero,
    string? RutReceptor,
    string? NombreReceptor,
    List<LineaDetalleDto> Detalle,
    List<RefCfeDto>? Referencias = null);

record LineaDetalleDto(
    string NombreItem,
    decimal Cantidad,
    decimal PrecioUnitario,
    int IndFactIva);

record RefCfeDto(
    int TipoCfe,
    string? Serie,
    long NroCfe,
    DateTime FechaCfe,
    string? Razon);
