namespace UruFactura.CloudflareApi.Models;

/// <summary>
/// Payload para enviar el Reporte Diario de CFE a la DGI.
/// <para>
/// Cada elemento de <c>Cfes</c> debe incluir el campo <c>Tipo</c> además
/// de todos los campos requeridos para el tipo de comprobante correspondiente.
/// </para>
/// </summary>
public record ReporteDiarioRequest(
    /// <summary>Fecha del reporte (debe coincidir con la fecha de emisión de los CFE).</summary>
    DateTime       Fecha,
    /// <summary>Lista de CFE emitidos en el día.</summary>
    List<CfeRequest> Cfes);
