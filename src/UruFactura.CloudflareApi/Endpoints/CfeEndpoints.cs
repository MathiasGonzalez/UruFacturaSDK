using Microsoft.AspNetCore.Mvc;
using UruFactura.CloudflareApi.Models;
using UruFactura.CloudflareApi.Services;
using UruFacturaSDK;
using UruFacturaSDK.Enums;
using UruFacturaSDK.Models;

namespace UruFactura.CloudflareApi.Endpoints;

public static class CfeEndpoints
{
    /// <summary>
    /// Registra todos los endpoints bajo <c>/cfe</c> y el <c>POST /reporte-diario</c>.
    /// </summary>
    public static IEndpointRouteBuilder MapCfeEndpoints(this IEndpointRouteBuilder app)
    {
        var cfe = app.MapGroup("/cfe").WithTags("CFE");

        // ── Generic endpoints (all 13 CFE types via Tipo in the request body) ──────

        cfe.MapPost("/xml", (
            HttpContext ctx,
            [FromBody] CfeRequest req,
            IUruFacturaClientFactory factory) =>
        {
            if (req.Tipo is null)
                return Results.BadRequest("El campo 'Tipo' es obligatorio para este endpoint.");
            var client = factory.GetClient(ctx.TenantId());
            var doc    = CrearCfe(client, req.Tipo.Value);
            MapRequest(doc, req);
            return Results.Text(client.GenerarYFirmar(doc), "application/xml");
        })
        .WithName("GenerarXmlCfe")
        .WithSummary("Genera y firma el XML de cualquier tipo de CFE. Incluir 'Tipo' en el body.");

        cfe.MapPost("/enviar", async (
            HttpContext ctx,
            [FromBody] CfeRequest req,
            IUruFacturaClientFactory factory,
            CancellationToken ct) =>
        {
            if (req.Tipo is null)
                return Results.BadRequest("El campo 'Tipo' es obligatorio para este endpoint.");
            var client = factory.GetClient(ctx.TenantId());
            var doc    = CrearCfe(client, req.Tipo.Value);
            MapRequest(doc, req);
            var resp = await client.EnviarCfeAsync(doc, ct);
            return resp.Exitoso ? Results.Ok(resp) : Results.BadRequest(resp);
        })
        .WithName("EnviarCfe")
        .WithSummary("Firma y envía cualquier tipo de CFE a la DGI. Incluir 'Tipo' en el body.");

        cfe.MapPost("/pdf/a4", (
            HttpContext ctx,
            [FromBody] CfeRequest req,
            IUruFacturaClientFactory factory) =>
        {
            if (req.Tipo is null)
                return Results.BadRequest("El campo 'Tipo' es obligatorio para este endpoint.");
            var client = factory.GetClient(ctx.TenantId());
            var doc    = CrearCfe(client, req.Tipo.Value);
            MapRequest(doc, req);
            var pdf = client.GenerarPdfA4(doc);
            return Results.File(pdf, "application/pdf", $"cfe_{doc.Tipo}_{doc.Numero}.pdf");
        })
        .WithName("GenerarPdfA4Cfe")
        .WithSummary("Genera el PDF A4 de cualquier tipo de CFE. Incluir 'Tipo' en el body.");

        cfe.MapPost("/pdf/termico", (
            HttpContext ctx,
            [FromBody] CfeRequest req,
            IUruFacturaClientFactory factory) =>
        {
            if (req.Tipo is null)
                return Results.BadRequest("El campo 'Tipo' es obligatorio para este endpoint.");
            var client = factory.GetClient(ctx.TenantId());
            var doc    = CrearCfe(client, req.Tipo.Value);
            MapRequest(doc, req);
            var pdf = client.GenerarPdfTermico(doc);
            return Results.File(pdf, "application/pdf", $"cfe_{doc.Tipo}_{doc.Numero}_termico.pdf");
        })
        .WithName("GenerarPdfTermicoCfe")
        .WithSummary("Genera el PDF térmico de cualquier tipo de CFE. Incluir 'Tipo' en el body.");

        // ── Consultar estado ──────────────────────────────────────────────────────

        cfe.MapPost("/consultar", async (
            HttpContext ctx,
            [FromBody] ConsultarCfeRequest req,
            IUruFacturaClientFactory factory,
            CancellationToken ct) =>
        {
            var client = factory.GetClient(ctx.TenantId());
            var doc    = CrearCfe(client, req.Tipo);
            doc.Serie  = req.Serie;
            doc.Numero = req.Numero;
            var resp = await client.ConsultarEstadoCfeAsync(doc, ct);
            return resp.Exitoso ? Results.Ok(resp) : Results.BadRequest(resp);
        })
        .WithName("ConsultarEstadoCfe")
        .WithSummary("Consulta el estado de un CFE en la DGI");

        // ── Named shortcuts — e-Ticket ────────────────────────────────────────────

        cfe.MapPost("/eticket/xml", (
            HttpContext ctx, [FromBody] CfeRequest req, IUruFacturaClientFactory factory) =>
        {
            var client = factory.GetClient(ctx.TenantId());
            var doc    = client.CrearETicket();
            MapRequest(doc, req);
            return Results.Text(client.GenerarYFirmar(doc), "application/xml");
        })
        .WithName("GenerarXmlETicket")
        .WithSummary("Genera y firma el XML de un e-Ticket (sin enviar a DGI)");

        cfe.MapPost("/eticket/enviar", async (
            HttpContext ctx, [FromBody] CfeRequest req, IUruFacturaClientFactory factory,
            CancellationToken ct) =>
        {
            var client = factory.GetClient(ctx.TenantId());
            var doc    = client.CrearETicket();
            MapRequest(doc, req);
            var resp = await client.EnviarCfeAsync(doc, ct);
            return resp.Exitoso ? Results.Ok(resp) : Results.BadRequest(resp);
        })
        .WithName("EnviarETicket")
        .WithSummary("Firma y envía un e-Ticket a la DGI");

        cfe.MapPost("/eticket/pdf/a4", (
            HttpContext ctx, [FromBody] CfeRequest req, IUruFacturaClientFactory factory) =>
        {
            var client = factory.GetClient(ctx.TenantId());
            var doc    = client.CrearETicket();
            MapRequest(doc, req);
            var pdf = client.GenerarPdfA4(doc);
            return Results.File(pdf, "application/pdf", $"eticket_{doc.Numero}.pdf");
        })
        .WithName("GenerarPdfA4ETicket")
        .WithSummary("Genera el PDF A4 de un e-Ticket");

        cfe.MapPost("/eticket/pdf/termico", (
            HttpContext ctx, [FromBody] CfeRequest req, IUruFacturaClientFactory factory) =>
        {
            var client = factory.GetClient(ctx.TenantId());
            var doc    = client.CrearETicket();
            MapRequest(doc, req);
            var pdf = client.GenerarPdfTermico(doc);
            return Results.File(pdf, "application/pdf", $"eticket_{doc.Numero}_termico.pdf");
        })
        .WithName("GenerarPdfTermicoETicket")
        .WithSummary("Genera el PDF térmico de un e-Ticket");

        // ── Named shortcuts — e-Factura ───────────────────────────────────────────

        cfe.MapPost("/efactura/xml", (
            HttpContext ctx, [FromBody] CfeRequest req, IUruFacturaClientFactory factory) =>
        {
            var client = factory.GetClient(ctx.TenantId());
            var doc    = client.CrearEFactura();
            MapRequest(doc, req);
            return Results.Text(client.GenerarYFirmar(doc), "application/xml");
        })
        .WithName("GenerarXmlEFactura")
        .WithSummary("Genera y firma el XML de una e-Factura (sin enviar a DGI)");

        cfe.MapPost("/efactura/enviar", async (
            HttpContext ctx, [FromBody] CfeRequest req, IUruFacturaClientFactory factory,
            CancellationToken ct) =>
        {
            var client = factory.GetClient(ctx.TenantId());
            var doc    = client.CrearEFactura();
            MapRequest(doc, req);
            var resp = await client.EnviarCfeAsync(doc, ct);
            return resp.Exitoso ? Results.Ok(resp) : Results.BadRequest(resp);
        })
        .WithName("EnviarEFactura")
        .WithSummary("Firma y envía una e-Factura a la DGI");

        cfe.MapPost("/efactura/pdf/a4", (
            HttpContext ctx, [FromBody] CfeRequest req, IUruFacturaClientFactory factory) =>
        {
            var client = factory.GetClient(ctx.TenantId());
            var doc    = client.CrearEFactura();
            MapRequest(doc, req);
            var pdf = client.GenerarPdfA4(doc);
            return Results.File(pdf, "application/pdf", $"efactura_{doc.Numero}.pdf");
        })
        .WithName("GenerarPdfA4EFactura")
        .WithSummary("Genera el PDF A4 de una e-Factura");

        cfe.MapPost("/efactura/pdf/termico", (
            HttpContext ctx, [FromBody] CfeRequest req, IUruFacturaClientFactory factory) =>
        {
            var client = factory.GetClient(ctx.TenantId());
            var doc    = client.CrearEFactura();
            MapRequest(doc, req);
            var pdf = client.GenerarPdfTermico(doc);
            return Results.File(pdf, "application/pdf", $"efactura_{doc.Numero}_termico.pdf");
        })
        .WithName("GenerarPdfTermicoEFactura")
        .WithSummary("Genera el PDF térmico de una e-Factura");

        return app;
    }

    /// <summary>
    /// Registra el endpoint <c>POST /reporte-diario</c>.
    /// </summary>
    public static IEndpointRouteBuilder MapReporteDiarioEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/reporte-diario", async (
            HttpContext ctx,
            [FromBody] ReporteDiarioRequest req,
            IUruFacturaClientFactory factory,
            CancellationToken ct) =>
        {
            if (req.Fecha == default)
                return Results.BadRequest("El campo 'Fecha' es obligatorio.");
            if (req.Cfes is null || req.Cfes.Count == 0)
                return Results.BadRequest("El campo 'Cfes' es obligatorio y no puede estar vacío.");

            var client = factory.GetClient(ctx.TenantId());

            var cfes = req.Cfes.Select(r =>
            {
                var tipo = r.Tipo
                    ?? throw new InvalidOperationException(
                        "Cada elemento de 'Cfes' debe incluir el campo 'Tipo'.");
                var doc = CrearCfe(client, tipo);
                MapRequest(doc, r);
                return doc;
            });

            var resp = await client.EnviarReporteDiarioAsync(req.Fecha, cfes, ct);
            return resp.Respuesta.Exitoso ? Results.Ok(resp) : Results.BadRequest(resp);
        })
        .WithTags("Reporte Diario")
        .WithName("EnviarReporteDiario")
        .WithSummary("Envía el Reporte Diario de CFE a la DGI");

        return app;
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Crea el objeto <see cref="Cfe"/> correcto según el <see cref="TipoCfe"/> indicado.
    /// </summary>
    private static Cfe CrearCfe(IUruFacturaClient client, TipoCfe tipo) => tipo switch
    {
        TipoCfe.ETicket                        => client.CrearETicket(),
        TipoCfe.NotaCreditoETicket             => client.CrearNotaCreditoETicket(),
        TipoCfe.NotaDebitoETicket              => client.CrearNotaDebitoETicket(),
        TipoCfe.EFactura                       => client.CrearEFactura(),
        TipoCfe.NotaCreditoEFactura            => client.CrearNotaCreditoEFactura(),
        TipoCfe.NotaDebitoEFactura             => client.CrearNotaDebitoEFactura(),
        TipoCfe.EFacturaExportacion            => client.CrearEFacturaExportacion(),
        TipoCfe.NotaCreditoEFacturaExportacion => client.CrearNotaCreditoEFacturaExportacion(),
        TipoCfe.NotaDebitoEFacturaExportacion  => client.CrearNotaDebitoEFacturaExportacion(),
        TipoCfe.ERemito                        => client.CrearERemito(),
        TipoCfe.ERemitoDespachante             => client.CrearERemitoDespachante(),
        TipoCfe.NotaCreditoERemito             => client.CrearNotaCreditoERemito(),
        TipoCfe.EResguardo                     => client.CrearEResguardo(),
        _ => throw new ArgumentOutOfRangeException(nameof(tipo), tipo,
                 $"Tipo de CFE no soportado: {tipo}"),
    };

    private static void MapRequest(Cfe doc, CfeRequest req)
    {
        doc.Numero    = req.Numero;
        doc.Serie     = req.Serie;
        doc.FormaPago = req.FormaPago;
        doc.Moneda    = req.Moneda;
        doc.Receptor  = req.Receptor;
        doc.Detalle   = req.Detalle;

        if (req.Referencias?.Count > 0)
            doc.Referencias = req.Referencias;

        if (req.IndTraslado.HasValue)
            doc.IndTraslado = req.IndTraslado;

        if (req.TipoCambio.HasValue)
            doc.TipoCambio = req.TipoCambio;

        if (req.FechaEmision.HasValue)
            doc.FechaEmision = req.FechaEmision.Value;

        doc.CalcularTotales();
    }
}
