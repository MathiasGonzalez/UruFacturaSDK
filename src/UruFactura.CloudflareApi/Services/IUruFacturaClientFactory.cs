using UruFactura;

namespace UruFactura.CloudflareApi.Services;

/// <summary>
/// Resuelve un <see cref="IUruFacturaClient"/> a partir de un identificador de tenant.
/// En modo single-tenant, omitir <paramref name="tenantId"/> (o pasar <c>null</c>)
/// utiliza la sección de configuración <c>UruFactura:*</c>.
/// En modo multi-tenant, proveer el <c>X-Tenant-Id</c> de la solicitud para
/// obtener el cliente correspondiente, configurado desde <c>Tenants:{tenantId}:*</c>.
/// </summary>
public interface IUruFacturaClientFactory
{
    /// <summary>
    /// Obtiene (o crea) el cliente para el tenant indicado.
    /// </summary>
    /// <param name="tenantId">
    /// Identificador del tenant. <c>null</c> = tenant único (sección <c>UruFactura:*</c>).
    /// </param>
    IUruFacturaClient GetClient(string? tenantId = null);
}
