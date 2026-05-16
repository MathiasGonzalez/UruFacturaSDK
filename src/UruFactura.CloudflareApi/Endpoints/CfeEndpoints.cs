using Microsoft.AspNetCore.Mvc;
using UruFactura.CloudflareApi.Models;
using UruFactura.CloudflareApi.Services;
using UruFacturaSDK;
using UruFacturaSDK.Models;

namespace UruFactura.CloudflareApi.Endpoints;

public static class CfeEndpoints
{
    /// <summary>
    /// Registra todos los endpoints bajo <c>/cfe</c>.
    /// </summary>
    public static IEndpointRouteBuilder MapCfeEndpoints(this IEndpointRouteBuilder app)
    {
        var cfe = app.MapGroup("/cfe").WithTags("CFE");

        // --- Generar XML de e-Ticket (sin enviar a DGI) ---
        cfe.MapPost("/eticket/xml", (
            HttpContext ctx,
            [FromBody] CfeRequest req,
            IUruFacturaClientFactory factory) =>
        {
            var client = factory.GetClient(ctx.TenantId());
            var doc = client.CrearETicket();
            MapRequest(doc, req);
            var xml = client.GenerarYFirmar(doc);
            return Results.Text(xml, "application/xml");
        })
        .WithName("GenerarXmlETicket")
        .WithSummary("Genera y firma el XML de un e-Ticket (sin enviar a DGI)");

        // --- Generar XML de e-Factura (sin enviar a DGI) ---
        cfe.MapPost("/efactura/xml", (
            HttpContext ctx,
            [FromBody] CfeRequest req,
            IUruFacturaClientFactory factory) =>
        {
            var client = factory.GetClient(ctx.TenantId());
            var doc = client.CrearEFactura();
            MapRequest(doc, req);
            var xml = client.GenerarYFirmar(doc);
            return Results.Text(xml, "application/xml");
        })
        .WithName("GenerarXmlEFactura")
        .WithSummary("Genera y firma el XML de una e-Factura (sin enviar a DGI)");

        // --- Enviar e-Ticket a DGI ---
        cfe.MapPost("/eticket/enviar", async (
            HttpContext ctx,
            [FromBody] CfeRequest req,
            IUruFacturaClientFactory factory,
            CancellationToken ct) =>
        {
            var client = factory.GetClient(ctx.TenantId());
            var doc = client.CrearETicket();
            MapRequest(doc, req);
            var resp = await client.EnviarCfeAsync(doc, ct);
            return resp.Exitoso ? Results.Ok(resp) : Results.BadRequest(resp);
        })
        .WithName("EnviarETicket")
        .WithSummary("Firma y envía un e-Ticket a la DGI");

        // --- Enviar e-Factura a DGI ---
        cfe.MapPost("/efactura/enviar", async (
            HttpContext ctx,
            [FromBody] CfeRequest req,
            IUruFacturaClientFactory factory,
            CancellationToken ct) =>
        {
            var client = factory.GetClient(ctx.TenantId());
            var doc = client.CrearEFactura();
            MapRequest(doc, req);
            var resp = await client.EnviarCfeAsync(doc, ct);
            return resp.Exitoso ? Results.Ok(resp) : Results.BadRequest(resp);
        })
        .WithName("EnviarEFactura")
        .WithSummary("Firma y envía una e-Factura a la DGI");

        // --- Consultar estado de un CFE ---
        cfe.MapPost("/consultar", async (
            HttpContext ctx,
            [FromBody] CfeRequest req,
            IUruFacturaClientFactory factory,
            CancellationToken ct) =>
        {
            var client = factory.GetClient(ctx.TenantId());
            var doc = client.CrearETicket();
            MapRequest(doc, req);
            var resp = await client.ConsultarEstadoCfeAsync(doc, ct);
            return resp.Exitoso ? Results.Ok(resp) : Results.BadRequest(resp);
        })
        .WithName("ConsultarEstadoCfe")
        .WithSummary("Consulta el estado de un CFE en la DGI");

        // --- Generar PDF A4 ---
        cfe.MapPost("/eticket/pdf/a4", (
            HttpContext ctx,
            [FromBody] CfeRequest req,
            IUruFacturaClientFactory factory) =>
        {
            var client = factory.GetClient(ctx.TenantId());
            var doc = client.CrearETicket();
            MapRequest(doc, req);
            var pdf = client.GenerarPdfA4(doc);
            return Results.File(pdf, "application/pdf", $"eticket_{doc.Numero}.pdf");
        })
        .WithName("GenerarPdfA4ETicket")
        .WithSummary("Genera el PDF A4 de un e-Ticket");

        // --- Generar PDF Térmico ---
        cfe.MapPost("/eticket/pdf/termico", (
            HttpContext ctx,
            [FromBody] CfeRequest req,
            IUruFacturaClientFactory factory) =>
        {
            var client = factory.GetClient(ctx.TenantId());
            var doc = client.CrearETicket();
            MapRequest(doc, req);
            var pdf = client.GenerarPdfTermico(doc);
            return Results.File(pdf, "application/pdf", $"eticket_{doc.Numero}_termico.pdf");
        })
        .WithName("GenerarPdfTermicoETicket")
        .WithSummary("Genera el PDF térmico (ticket) de un e-Ticket");

        return app;
    }

    // -------------------------------------------------------------------------

    private static void MapRequest(Cfe doc, CfeRequest req)
    {
        doc.Numero    = req.Numero;
        doc.Serie     = req.Serie;
        doc.FormaPago = req.FormaPago;
        doc.Moneda    = req.Moneda;
        doc.Receptor  = req.Receptor;
        doc.Detalle   = req.Detalle;
        doc.CalcularTotales();
    }
}
