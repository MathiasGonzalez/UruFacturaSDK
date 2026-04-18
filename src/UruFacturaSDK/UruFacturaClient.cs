using UruFacturaSDK.Cae;
using UruFacturaSDK.Configuration;
using UruFacturaSDK.Enums;
using UruFacturaSDK.Exceptions;
using UruFacturaSDK.Models;
using UruFacturaSDK.Pdf;
using UruFacturaSDK.Signature;
using UruFacturaSDK.Soap;
using UruFacturaSDK.Xml;

namespace UruFacturaSDK;

/// <summary>
/// Punto de entrada principal del SDK de UruFactura.
/// Orquesta la generación, firma, envío y representación impresa de CFE.
/// </summary>
public class UruFacturaClient : IUruFacturaClient
{
    private readonly UruFacturaConfig _config;
    private readonly CfeXmlBuilder _xmlBuilder;
    private readonly CfeFirmante _firmante;
    private readonly CaeManager _caeManager;
    private readonly CfePdfGenerator _pdfGenerator;
    private DgiSoapClient? _soapClient;
    private bool _disposed;

    /// <summary>
    /// Gestión de CAEs del cliente.
    /// </summary>
    public ICaeManager Cae => _caeManager;

    /// <summary>
    /// Inicializa el cliente de UruFactura con la configuración provista.
    /// </summary>
    /// <param name="config">Configuración del SDK.</param>
    public UruFacturaClient(UruFacturaConfig config)
    {
        config.Validate();
        _config = config;
        _xmlBuilder = new CfeXmlBuilder();
        _firmante = new CfeFirmante(config.RutaCertificado, config.PasswordCertificado);
        _caeManager = new CaeManager();
        _pdfGenerator = new CfePdfGenerator(config);
    }

    // -----------------------------------------------------------------------
    // Generación de CFE
    // -----------------------------------------------------------------------

    /// <summary>
    /// Crea un nuevo e-Ticket pre-configurado con los datos del emisor.
    /// </summary>
    public Cfe CrearETicket() => CrearCfe(TipoCfe.ETicket);

    /// <summary>
    /// Crea una nueva e-Factura pre-configurada con los datos del emisor.
    /// </summary>
    public Cfe CrearEFactura() => CrearCfe(TipoCfe.EFactura);

    /// <summary>
    /// Crea un nuevo e-Remito pre-configurado con los datos del emisor.
    /// </summary>
    public Cfe CrearERemito() => CrearCfe(TipoCfe.ERemito);

    /// <summary>
    /// Crea una nota de crédito de e-Ticket.
    /// </summary>
    public Cfe CrearNotaCreditoETicket() => CrearCfe(TipoCfe.NotaCreditoETicket);

    /// <summary>
    /// Crea una nota de débito de e-Ticket.
    /// </summary>
    public Cfe CrearNotaDebitoETicket() => CrearCfe(TipoCfe.NotaDebitoETicket);

    /// <summary>
    /// Crea una nota de crédito de e-Factura.
    /// </summary>
    public Cfe CrearNotaCreditoEFactura() => CrearCfe(TipoCfe.NotaCreditoEFactura);

    /// <summary>
    /// Crea una nota de débito de e-Factura.
    /// </summary>
    public Cfe CrearNotaDebitoEFactura() => CrearCfe(TipoCfe.NotaDebitoEFactura);

    /// <summary>
    /// Crea una e-Factura de exportación.
    /// </summary>
    public Cfe CrearEFacturaExportacion() => CrearCfe(TipoCfe.EFacturaExportacion);

    /// <summary>
    /// Crea una nota de crédito de e-Factura de exportación.
    /// </summary>
    public Cfe CrearNotaCreditoEFacturaExportacion() => CrearCfe(TipoCfe.NotaCreditoEFacturaExportacion);

    /// <summary>
    /// Crea una nota de débito de e-Factura de exportación.
    /// </summary>
    public Cfe CrearNotaDebitoEFacturaExportacion() => CrearCfe(TipoCfe.NotaDebitoEFacturaExportacion);

    /// <summary>
    /// Crea un e-Remito de despachante (131).
    /// </summary>
    public Cfe CrearERemitoDespachante() => CrearCfe(TipoCfe.ERemitoDespachante);

    /// <summary>
    /// Crea un e-Resguardo (151).
    /// </summary>
    public Cfe CrearEResguardo() => CrearCfe(TipoCfe.EResguardo);

    /// <summary>
    /// Crea una nota de crédito de e-Remito (182).
    /// </summary>
    public Cfe CrearNotaCreditoERemito() => CrearCfe(TipoCfe.NotaCreditoERemito);

    private Cfe CrearCfe(TipoCfe tipo)
    {
        return new Cfe
        {
            Tipo = tipo,
            RutEmisor = _config.RutEmisor,
            RazonSocialEmisor = _config.RazonSocialEmisor,
            NombreComercialEmisor = _config.NombreComercialEmisor,
            DomicilioFiscalEmisor = _config.DomicilioFiscal,
            CiudadEmisor = _config.Ciudad,
            DepartamentoEmisor = _config.Departamento,
            FechaEmision = DateTime.Today,
        };
    }

    // -----------------------------------------------------------------------
    // XML y Firma
    // -----------------------------------------------------------------------

    /// <summary>
    /// Genera el XML sin firmar del CFE.
    /// </summary>
    /// <param name="cfe">El CFE a serializar.</param>
    /// <returns>XML sin firmar.</returns>
    public string GenerarXml(Cfe cfe)
    {
        ThrowIfDisposed();
        var xml = _xmlBuilder.Generar(cfe);
        cfe.XmlSinFirmar = xml;
        return xml;
    }

    /// <summary>
    /// Firma digitalmente el CFE con XAdES-BES.
    /// </summary>
    /// <param name="cfe">El CFE (debe haberse generado el XML previamente).</param>
    /// <returns>XML firmado.</returns>
    public string FirmarCfe(Cfe cfe)
    {
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(cfe.XmlSinFirmar))
            GenerarXml(cfe);

        var xmlFirmado = _firmante.Firmar(cfe.XmlSinFirmar!);
        cfe.XmlFirmado = xmlFirmado;
        return xmlFirmado;
    }

    /// <summary>
    /// Genera y firma el CFE en un solo paso.
    /// </summary>
    /// <param name="cfe">El CFE a procesar.</param>
    /// <returns>XML firmado.</returns>
    public string GenerarYFirmar(Cfe cfe)
    {
        ThrowIfDisposed();
        GenerarXml(cfe);
        return FirmarCfe(cfe);
    }

    // -----------------------------------------------------------------------
    // Comunicación con DGI
    // -----------------------------------------------------------------------

    /// <summary>
    /// Envía el CFE firmado a la DGI.
    /// Si el CFE aún no está firmado, lo firma primero.
    /// </summary>
    /// <param name="cfe">El CFE a enviar.</param>
    /// <param name="cancellationToken">Token de cancelación.</param>
    /// <returns>Respuesta de la DGI.</returns>
    public async Task<RespuestaDgi> EnviarCfeAsync(
        Cfe cfe,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(cfe.XmlFirmado))
            GenerarYFirmar(cfe);

        var cliente = ObtenerSoapClient();
        var respuesta = await cliente.EnviarCfeAsync(cfe.XmlFirmado!, cancellationToken);

        cfe.CodigoRespuestaDgi = respuesta.Codigo;
        cfe.MensajeRespuestaDgi = respuesta.Mensaje;
        cfe.AceptadoPorDgi = respuesta.Exitoso;

        return respuesta;
    }

    /// <summary>
    /// Consulta el estado de un CFE en los servidores de la DGI.
    /// </summary>
    public async Task<RespuestaDgi> ConsultarEstadoCfeAsync(
        Cfe cfe,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var cliente = ObtenerSoapClient();
        return await cliente.ConsultarEstadoCfeAsync(
            cfe.RutEmisor, (int)cfe.Tipo,
            cfe.Serie ?? string.Empty, cfe.Numero,
            cancellationToken);
    }

    /// <summary>
    /// Envía el Reporte Diario a la DGI con los CFE del día.
    /// </summary>
    public async Task<RespuestaReporteDiario> EnviarReporteDiarioAsync(
        DateTime fecha,
        IEnumerable<Cfe> cfes,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var xmlsFirmados = cfes
            .Select(c => string.IsNullOrWhiteSpace(c.XmlFirmado)
                ? GenerarYFirmar(c)
                : c.XmlFirmado!)
            .ToList();

        var cliente = ObtenerSoapClient();
        return await cliente.EnviarReporteDiarioAsync(fecha, xmlsFirmados, cancellationToken);
    }

    // -----------------------------------------------------------------------
    // PDF
    // -----------------------------------------------------------------------

    /// <summary>
    /// Genera el PDF A4 del CFE.
    /// </summary>
    /// <param name="cfe">El CFE.</param>
    /// <returns>Bytes del PDF.</returns>
    public byte[] GenerarPdfA4(Cfe cfe)
    {
        ThrowIfDisposed();
        return _pdfGenerator.GenerarA4(cfe);
    }

    /// <summary>
    /// Genera el PDF térmico (ticket 80mm) del CFE.
    /// </summary>
    /// <param name="cfe">El CFE.</param>
    /// <returns>Bytes del PDF.</returns>
    public byte[] GenerarPdfTermico(Cfe cfe)
    {
        ThrowIfDisposed();
        return _pdfGenerator.GenerarTermico(cfe);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private DgiSoapClient ObtenerSoapClient()
    {
        _soapClient ??= new DgiSoapClient(_config);
        return _soapClient;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _soapClient?.Dispose();
        _firmante.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
