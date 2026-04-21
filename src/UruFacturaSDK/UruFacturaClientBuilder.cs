using UruFacturaSDK.Cae;
using UruFacturaSDK.Configuration;
using UruFacturaSDK.Pdf;
using UruFacturaSDK.Signature;
using UruFacturaSDK.Soap;
using UruFacturaSDK.Xml;

namespace UruFacturaSDK;

/// <summary>
/// Builder fluido para construir un <see cref="UruFacturaClient"/> con pleno control sobre
/// cada dependencia. Use <see cref="WithDefaults"/> para obtener un builder ya configurado
/// con las implementaciones predeterminadas y reemplace sólo lo que necesite.
/// </summary>
/// <example>
/// <code>
/// // Caso habitual — todas las implementaciones predeterminadas:
/// var client = UruFacturaClientBuilder.WithDefaults(config).Build();
///
/// // Con un gestor de CAE y cliente SOAP personalizados (p.ej. para tests):
/// var client = UruFacturaClientBuilder.WithDefaults(config)
///     .ConGestorCae(miCaeManager)
///     .ConClienteSoap(miSoapMock)
///     .Build();
/// </code>
/// </example>
public sealed partial class UruFacturaClientBuilder
{
    private readonly UruFacturaConfig _config;
    private ICfeXmlBuilder _xmlBuilder;
    private ICfeFirmante _firmante;
    private ICaeManager _caeManager;
    private IDgiSoapClient _soapClient;
    private ICfePdfGenerator? _pdfGenerator;

    private UruFacturaClientBuilder(UruFacturaConfig config)
    {
        config.Validate();
        _config = config;
        _xmlBuilder = new CfeXmlBuilder();
        _firmante   = new CfeFirmante(config.RutaCertificado, config.PasswordCertificado);
        _caeManager = new CaeManager();
        _soapClient = new DgiSoapClient(config);
    }

    /// <summary>
    /// Crea un builder con todas las dependencias en sus implementaciones predeterminadas.
    /// </summary>
    /// <param name="config">Configuración del SDK.</param>
    public static UruFacturaClientBuilder WithDefaults(UruFacturaConfig config) => new(config);

    /// <summary>Reemplaza el generador de XML predeterminado.</summary>
    public UruFacturaClientBuilder ConGeneradorXml(ICfeXmlBuilder xmlBuilder)
    {
        ArgumentNullException.ThrowIfNull(xmlBuilder);
        _xmlBuilder = xmlBuilder;
        return this;
    }

    /// <summary>Reemplaza el firmante digital predeterminado.</summary>
    public UruFacturaClientBuilder ConFirmante(ICfeFirmante firmante)
    {
        ArgumentNullException.ThrowIfNull(firmante);
        _firmante = firmante;
        return this;
    }

    /// <summary>Reemplaza el gestor de CAEs predeterminado.</summary>
    public UruFacturaClientBuilder ConGestorCae(ICaeManager caeManager)
    {
        ArgumentNullException.ThrowIfNull(caeManager);
        _caeManager = caeManager;
        return this;
    }

    /// <summary>Reemplaza el cliente SOAP predeterminado.</summary>
    public UruFacturaClientBuilder ConClienteSoap(IDgiSoapClient soapClient)
    {
        ArgumentNullException.ThrowIfNull(soapClient);
        _soapClient = soapClient;
        return this;
    }

    /// <summary>Establece el generador de PDF a utilizar.</summary>
    public UruFacturaClientBuilder ConGeneradorPdf(ICfePdfGenerator? pdfGenerator)
    {
        _pdfGenerator = pdfGenerator;
        return this;
    }

    /// <summary>
    /// Construye el <see cref="UruFacturaClient"/> con las dependencias configuradas.
    /// </summary>
    public UruFacturaClient Build() =>
        new(_config, _xmlBuilder, _firmante, _caeManager, _soapClient, _pdfGenerator);
}
