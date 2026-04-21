using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;
using UruFacturaSDK.Configuration;
using UruFacturaSDK.Exceptions;
using UruFacturaSDK.Formatting;
using UruFacturaSDK.Models;

namespace UruFacturaSDK.Soap;

/// <summary>
/// Cliente SOAP para comunicarse con los servicios web de la DGI de Uruguay
/// (envío de CFE, consulta de estado y envío de reporte diario).
/// </summary>
public class DgiSoapClient : IDgiSoapClient
{
    private readonly UruFacturaConfig _config;
    private readonly HttpClient _httpClient;
    private bool _disposed;

    private const string SoapAction = "\"\"";
    private const string SoapNs = "http://schemas.xmlsoap.org/soap/envelope/";

    /// <summary>
    /// Inicializa el cliente SOAP con la configuración del SDK.
    /// </summary>
    public DgiSoapClient(UruFacturaConfig config, HttpClient? httpClient = null)
    {
        _config = config;
        _httpClient = httpClient ?? CrearHttpClient();
    }

    /// <summary>
    /// Envía un CFE firmado a la DGI.
    /// </summary>
    /// <param name="xmlFirmado">XML del CFE firmado digitalmente.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Respuesta de la DGI.</returns>
    public async Task<RespuestaDgi> EnviarCfeAsync(
        string xmlFirmado,
        CancellationToken cancellationToken = default)
    {
        var soapBody = ConstruirSoapEnvio(xmlFirmado);
        return await EnviarSolicitudAsync(soapBody, "enviarCFE", cancellationToken);
    }

    /// <summary>
    /// Consulta el estado de un CFE previamente enviado.
    /// </summary>
    /// <param name="rutEmisor">RUT del emisor.</param>
    /// <param name="tipoCfe">Tipo de CFE (número entero de la DGI).</param>
    /// <param name="serie">Serie del CFE.</param>
    /// <param name="numero">Número del CFE.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    public async Task<RespuestaDgi> ConsultarEstadoCfeAsync(
        string rutEmisor,
        int tipoCfe,
        string serie,
        long numero,
        CancellationToken cancellationToken = default)
    {
        var soapBody = ConstruirSoapConsulta(rutEmisor, tipoCfe, serie, numero);
        return await EnviarSolicitudAsync(soapBody, "consultarEstadoCFE", cancellationToken);
    }

    /// <summary>
    /// Envía el Reporte Diario de CFE a la DGI.
    /// </summary>
    /// <param name="fecha">Fecha del reporte.</param>
    /// <param name="cfesFirmados">Lista de XMLs firmados de los CFE del día.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    public async Task<RespuestaReporteDiario> EnviarReporteDiarioAsync(
        DateTime fecha,
        IEnumerable<string> cfesFirmados,
        CancellationToken cancellationToken = default)
    {
        var listaXmls = cfesFirmados.ToList();
        var soapBody = ConstruirSoapReporteDiario(fecha, listaXmls);
        var respuesta = await EnviarSolicitudAsync(soapBody, "enviarReporteDiario", cancellationToken);

        return new RespuestaReporteDiario
        {
            FechaReporte = fecha,
            CantidadCfe = listaXmls.Count,
            Respuesta = respuesta,
        };
    }

    private async Task<RespuestaDgi> EnviarSolicitudAsync(
        string soapEnvelope,
        string operacion,
        CancellationToken cancellationToken)
    {
        try
        {
            var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", SoapAction);

            var response = await _httpClient.PostAsync(
                _config.DgiSoapBaseUrl, content, cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            return ProcesarRespuesta(responseBody, operacion);
        }
        catch (HttpRequestException ex)
        {
            throw new DgiCommunicationException(
                $"Error de red al invocar '{operacion}': {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new DgiCommunicationException(
                $"Timeout al invocar '{operacion}'. Verifique el SoapTimeoutSegundos.", ex);
        }
        catch (DgiCommunicationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new DgiCommunicationException(
                $"Error inesperado al invocar '{operacion}'.", ex);
        }
    }

    private static RespuestaDgi ProcesarRespuesta(string xmlRespuesta, string operacion)
    {
        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(xmlRespuesta);

            var nsManager = new XmlNamespaceManager(doc.NameTable);
            nsManager.AddNamespace("soap", SoapNs);

            // Detectar SOAP Fault
            var fault = doc.SelectSingleNode("//soap:Fault", nsManager)
                      ?? doc.SelectSingleNode("//Fault");

            if (fault != null)
            {
                var faultString = fault.SelectSingleNode("faultstring")?.InnerText
                               ?? "Error SOAP sin detalle.";
                return RespuestaDgi.Error("SOAP_FAULT", faultString, xmlRespuesta);
            }

            // Intentar extraer codigo y mensaje de la respuesta DGI
            var codigo = doc.SelectSingleNode("//*[local-name()='Codigo']")?.InnerText
                      ?? doc.SelectSingleNode("//*[local-name()='Estado']")?.InnerText
                      ?? "OK";

            var mensaje = doc.SelectSingleNode("//*[local-name()='Mensaje']")?.InnerText
                       ?? doc.SelectSingleNode("//*[local-name()='Descripcion']")?.InnerText
                       ?? $"Respuesta de '{operacion}' procesada correctamente.";

            bool exitoso = codigo == "00" || codigo == "01";

            return exitoso
                ? RespuestaDgi.Exito(codigo, mensaje, xmlRespuesta)
                : RespuestaDgi.Error(codigo, mensaje, xmlRespuesta);
        }
        catch (Exception ex)
        {
            throw new DgiCommunicationException(
                $"Error al procesar la respuesta de '{operacion}'.", ex);
        }
    }

    private static string ConstruirSoapEnvio(string xmlFirmado)
    {
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""{SoapNs}"">
  <soap:Body>
    <enviarCFE xmlns=""http://dgi.gub.uy/efactura"">
      <cfe><![CDATA[{xmlFirmado}]]></cfe>
    </enviarCFE>
  </soap:Body>
</soap:Envelope>";
    }

    private static string ConstruirSoapConsulta(
        string rut, int tipo, string serie, long numero)
    {
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""{SoapNs}"">
  <soap:Body>
    <consultarEstadoCFE xmlns=""http://dgi.gub.uy/efactura"">
      <rut>{EscapeXml(rut)}</rut>
      <tipoCFE>{tipo}</tipoCFE>
      <serie>{EscapeXml(serie)}</serie>
      <numero>{numero}</numero>
    </consultarEstadoCFE>
  </soap:Body>
</soap:Envelope>";
    }

    private static string ConstruirSoapReporteDiario(
        DateTime fecha, IList<string> cfes)
    {
        var cfesXml = new StringBuilder();
        foreach (var xml in cfes)
            cfesXml.Append($"<cfeFirmado><![CDATA[{xml}]]></cfeFirmado>");

        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:soap=""{SoapNs}"">
  <soap:Body>
    <enviarReporteDiario xmlns=""http://dgi.gub.uy/efactura"">
      <fecha>{CfeFormat.DateIso(fecha)}</fecha>
      <cfes>{cfesXml}</cfes>
    </enviarReporteDiario>
  </soap:Body>
</soap:Envelope>";
    }

    private HttpClient CrearHttpClient()
    {
        var handler = new HttpClientHandler();

        // Disable TLS certificate validation only when the caller explicitly opts in.
        // Previously this was enabled automatically for the Homologación environment, but
        // doing so silently is a security risk. Callers must now set OmitirValidacionSsl = true.
        if (_config.OmitirValidacionSsl)
        {
            handler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }

        if (!string.IsNullOrWhiteSpace(_config.RutaCertificado)
            && File.Exists(_config.RutaCertificado))
        {
            var cert = X509CertificateLoader.LoadPkcs12FromFile(
                _config.RutaCertificado, _config.PasswordCertificado);
            handler.ClientCertificates.Add(cert);
        }

        var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(_config.SoapTimeoutSegundos),
            BaseAddress = new Uri(_config.DgiSoapBaseUrl),
        };

        client.DefaultRequestHeaders.Add("Accept", "text/xml");
        return client;
    }

    private static string EscapeXml(string value) =>
        System.Security.SecurityElement.Escape(value) ?? string.Empty;

    public void Dispose()
    {
        if (_disposed) return;
        _httpClient.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
