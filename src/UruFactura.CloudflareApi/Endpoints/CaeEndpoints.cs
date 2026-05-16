using Microsoft.AspNetCore.Mvc;
using UruFactura.CloudflareApi.Models;
using UruFactura.CloudflareApi.Services;

namespace UruFactura.CloudflareApi.Endpoints;

public static class CaeEndpoints
{
    /// <summary>
    /// Registra todos los endpoints bajo <c>/cae</c>.
    /// </summary>
    public static IEndpointRouteBuilder MapCaeEndpoints(this IEndpointRouteBuilder app)
    {
        // --- Listar CAEs ---
        app.MapGet("/cae", (HttpContext ctx, IUruFacturaClientFactory factory) =>
            Results.Ok(factory.GetClient(ctx.TenantId()).Cae.ObtenerTodosLosCaes()))
        .WithTags("CAE")
        .WithName("ListarCaes")
        .WithSummary("Lista los CAEs del tenant en memoria");

        // --- Registrar un CAE en tiempo de ejecución ---
        app.MapPost("/cae", (
            HttpContext ctx,
            [FromBody] CaeConfigRequest req,
            IUruFacturaClientFactory factory) =>
        {
            var client = factory.GetClient(ctx.TenantId());
            var cae = req.ToModel();
            client.Cae.RegistrarCae(cae);
            return Results.Created($"/cae/{cae.NroSerie}", cae);
        })
        .WithTags("CAE")
        .WithName("RegistrarCae")
        .WithSummary("Registra un CAE en memoria en tiempo de ejecución");

        // --- Advertencias de CAEs ---
        app.MapGet("/cae/advertencias", (HttpContext ctx, IUruFacturaClientFactory factory) =>
            Results.Ok(factory.GetClient(ctx.TenantId()).Cae.ObtenerAdvertencias()))
        .WithTags("CAE")
        .WithName("AdvertenciasCaes")
        .WithSummary("Devuelve advertencias de CAEs por vencer o con alto uso");

        return app;
    }
}
