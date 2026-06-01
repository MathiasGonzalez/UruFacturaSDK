using UruFactura.Enums;
using UruFactura.Models;

namespace UruFactura.CloudflareApi.Models;

/// <summary>
/// Payload para operaciones sobre un CFE.
/// </summary>
/// <param name="Numero">Número del CFE.</param>
/// <param name="Serie">Serie del CFE (puede ser null).</param>
/// <param name="FormaPago">Forma de pago del comprobante.</param>
/// <param name="Moneda">Moneda del comprobante.</param>
/// <param name="Receptor">Datos del receptor (puede ser null para consumidor final).</param>
/// <param name="Detalle">Líneas de detalle del CFE.</param>
/// <param name="Tipo">
/// Tipo de CFE. <b>Obligatorio</b> en los endpoints genéricos
/// (<c>/cfe/xml</c>, <c>/cfe/enviar</c>, <c>/cfe/pdf/*</c>).
/// Ignorado en los endpoints con tipo implícito (<c>/cfe/eticket/*</c>, <c>/cfe/efactura/*</c>).
/// </param>
/// <param name="Referencias">Referencias a CFE previos. Obligatorio para notas de crédito/débito.</param>
/// <param name="IndTraslado">Indicador de traslado. Obligatorio para e-Remito y e-Remito Despachante.</param>
/// <param name="TipoCambio">Tipo de cambio. Obligatorio cuando <c>Moneda</c> no es Peso Uruguayo.</param>
/// <param name="FechaEmision">Fecha de emisión. Si es null se usa la fecha del día.</param>
public record CfeRequest(
    long               Numero,
    string?            Serie,
    FormaPago          FormaPago,
    Moneda             Moneda,
    Receptor?          Receptor,
    List<LineaDetalle> Detalle,
    TipoCfe?       Tipo         = null,
    List<RefCfe>?  Referencias  = null,
    IndTraslado?   IndTraslado  = null,
    decimal?       TipoCambio   = null,
    DateOnly?      FechaEmision = null);
