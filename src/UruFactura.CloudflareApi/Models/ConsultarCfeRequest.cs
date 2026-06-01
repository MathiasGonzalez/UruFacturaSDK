using UruFactura.Enums;

namespace UruFactura.CloudflareApi.Models;

/// <summary>Payload para consultar el estado de un CFE en la DGI.</summary>
/// <param name="Tipo">Tipo de CFE a consultar.</param>
/// <param name="Serie">Serie del CFE (puede ser null).</param>
/// <param name="Numero">Número del CFE.</param>
public record ConsultarCfeRequest(TipoCfe Tipo, string? Serie, long Numero);
