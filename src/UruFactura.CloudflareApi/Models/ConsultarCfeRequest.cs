using UruFacturaSDK.Enums;

namespace UruFactura.CloudflareApi.Models;

/// <summary>Payload para consultar el estado de un CFE en la DGI.</summary>
public record ConsultarCfeRequest(
    /// <summary>Tipo de CFE a consultar.</summary>
    TipoCfe Tipo,
    /// <summary>Serie del CFE (puede ser null).</summary>
    string? Serie,
    /// <summary>Número del CFE.</summary>
    long    Numero);
