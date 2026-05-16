using UruFacturaSDK.Enums;
using UruFacturaSDK.Models;

namespace UruFactura.CloudflareApi.Models;

/// <summary>Payload para operaciones sobre un CFE.</summary>
public record CfeRequest(
    long               Numero,
    string?            Serie,
    FormaPago          FormaPago,
    Moneda             Moneda,
    Receptor?          Receptor,
    List<LineaDetalle> Detalle);
