using Microsoft.EntityFrameworkCore;
using UruFacturaSDK;
using UruFacturaSDK.Configuration;
using UruFacturaSDK.Enums;
using UruFacturaSDK.Models;

var builder = WebApplication.CreateBuilder(args);

builder.AddNpgsqlDbContext<AppDbContext>("erpdb");

builder.Services.AddCors(o =>
    o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();

app.UseCors();

// -----------------------------------------------------------------------
// Endpoints
// -----------------------------------------------------------------------

app.MapGet("/api/invoices", async (AppDbContext db) =>
    await db.Invoices.OrderByDescending(i => i.FechaEmision).ToListAsync());

app.MapPost("/api/invoices", async (CreateInvoiceRequest req, AppDbContext db, IConfiguration config) =>
{
    var ufConfig = config.GetSection("UruFactura").Get<UruFacturaConfig>()!;

    using var client = new UruFacturaClient(ufConfig);

    var cfe = req.TipoCfe == (int)TipoCfe.EFactura
        ? client.CrearEFactura()
        : client.CrearETicket();

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
        XmlFirmado = cfe.XmlFirmado,
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

    using var client = new UruFacturaClient(ufConfig);

    var cfe = new Cfe
    {
        Tipo = (TipoCfe)invoice.TipoCfe,
        Numero = invoice.Numero,
        FechaEmision = invoice.FechaEmision,
        MontoTotal = invoice.MontoTotal,
        RutEmisor = ufConfig.RutEmisor,
        RazonSocialEmisor = ufConfig.RazonSocialEmisor,
        DomicilioFiscalEmisor = ufConfig.DomicilioFiscal,
        CiudadEmisor = ufConfig.Ciudad,
        DepartamentoEmisor = ufConfig.Departamento,
        XmlFirmado = invoice.XmlFirmado,
    };

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
    List<LineaDetalleDto> Detalle);

record LineaDetalleDto(
    string NombreItem,
    decimal Cantidad,
    decimal PrecioUnitario,
    int IndFactIva);
