namespace UruFacturaSDK.Models;

/// <summary>
/// Resultado del reporte diario.
/// </summary>
public class RespuestaReporteDiario
{
    /// <summary>Fecha del reporte.</summary>
    public DateTime FechaReporte { get; set; }

    /// <summary>Cantidad de CFE incluidos en el reporte.</summary>
    public int CantidadCfe { get; set; }

    /// <summary>Resultado de la operación.</summary>
    public RespuestaDgi Respuesta { get; set; } = new();
}
