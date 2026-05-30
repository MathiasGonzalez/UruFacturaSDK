namespace UruFactura.CloudflareApi;

internal static class HttpContextExtensions
{
    /// <summary>
    /// Lee el header <c>X-Tenant-Id</c> de la solicitud.
    /// Devuelve <c>null</c> cuando el header está ausente (modo single-tenant).
    /// </summary>
    public static string? TenantId(this HttpContext ctx)
        => ctx.Request.Headers["X-Tenant-Id"].FirstOrDefault();
}
