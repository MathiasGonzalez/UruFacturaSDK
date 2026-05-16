using UruFacturaSDK.Enums;
using UruFacturaSDK.Models;

namespace UruFactura.CloudflareApi.Models;

/// <summary>
/// Payload para operaciones sobre un CFE.
/// <para>
/// Los campos opcionales al final son compatibles hacia atrás con peticiones previas
/// que no los incluían (se usan los valores por defecto cuando están ausentes en el JSON).
/// </para>
/// </summary>
public record CfeRequest(
    long               Numero,
    string?            Serie,
    FormaPago          FormaPago,
    Moneda             Moneda,
    Receptor?          Receptor,
    List<LineaDetalle> Detalle,
    /// <summary>
    /// Tipo de CFE. Obligatorio en los endpoints genéricos (<c>/cfe/xml</c>, <c>/cfe/enviar</c>, etc.).
    /// Ignorado en los endpoints con tipo implícito (<c>/cfe/eticket/xml</c>, etc.).
    /// </summary>
    TipoCfe        Tipo         = TipoCfe.ETicket,
    /// <summary>Referencias a CFE previos. Obligatorio para notas de crédito/débito.</summary>
    List<RefCfe>?  Referencias  = null,
    /// <summary>Indicador de traslado. Obligatorio para e-Remito y e-Remito Despachante.</summary>
    IndTraslado?   IndTraslado  = null,
    /// <summary>Tipo de cambio. Obligatorio cuando <c>Moneda</c> no es Peso Uruguayo.</summary>
    decimal?       TipoCambio   = null,
    /// <summary>Fecha de emisión. Si es null se usa la fecha del día.</summary>
    DateOnly?      FechaEmision = null);
