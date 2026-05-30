namespace UruFactura.CloudflareApi.Models;

/// <summary>
/// Payload para enviar el Reporte Diario de CFE a la DGI.
/// </summary>
/// <param name="Fecha">Fecha del reporte (debe coincidir con la fecha de emisión de los CFE).</param>
/// <param name="Cfes">Lista de CFE emitidos en el día. No puede ser nulo ni vacío.</param>
public record ReporteDiarioRequest(
    DateTime         Fecha,
    List<CfeRequest> Cfes);
