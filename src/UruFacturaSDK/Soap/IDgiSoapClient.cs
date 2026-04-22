using UruFacturaSDK.Models;

namespace UruFacturaSDK.Soap;

/// <summary>
/// Contrato para la comunicación SOAP con los servicios web de la DGI de Uruguay.
/// Implementar esta interfaz permite reemplazar o mockear el transporte hacia DGI.
/// </summary>
public interface IDgiSoapClient : IDisposable
{
    /// <summary>Envía un CFE firmado a la DGI.</summary>
    Task<RespuestaDgi> EnviarCfeAsync(string xmlFirmado, CancellationToken cancellationToken = default);

    /// <summary>Consulta el estado de un CFE previamente enviado.</summary>
    Task<RespuestaDgi> ConsultarEstadoCfeAsync(
        string rutEmisor,
        int tipoCfe,
        string serie,
        long numero,
        CancellationToken cancellationToken = default);

    /// <summary>Envía el Reporte Diario de CFE a la DGI.</summary>
    Task<RespuestaReporteDiario> EnviarReporteDiarioAsync(
        DateTime fecha,
        IEnumerable<string> cfesFirmados,
        CancellationToken cancellationToken = default);

    IDgiSoapClient WithHttpClient(HttpClient httpClient);
}
