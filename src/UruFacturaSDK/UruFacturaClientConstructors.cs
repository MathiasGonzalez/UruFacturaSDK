using UruFacturaSDK.Cae;
using UruFacturaSDK.Configuration;
using UruFacturaSDK.Pdf;
using UruFacturaSDK.Signature;
using UruFacturaSDK.Soap;
using UruFacturaSDK.Xml;

namespace UruFacturaSDK;

public partial class UruFacturaClient
{
    /// <summary>
    /// Inicializa el cliente con la configuración provista y el generador de PDF predeterminado
    /// (QuestPDF + SkiaSharp + ZXing).
    /// </summary>
    /// <param name="config">Configuración del SDK.</param>
    public UruFacturaClient(UruFacturaConfig config)
        : this(config, new CfePdfGenerator(config))
    {
    }

    /// <summary>
    /// Inicializa el cliente con un generador de PDF personalizado.
    /// </summary>
    /// <param name="config">Configuración del SDK.</param>
    /// <param name="pdfGenerator">Implementación de <see cref="ICfePdfGenerator"/> a utilizar.</param>
    public UruFacturaClient(UruFacturaConfig config, ICfePdfGenerator? pdfGenerator)
        : this(config,
               new CfeXmlBuilder(),
               new CfeFirmante(config.RutaCertificado, config.PasswordCertificado),
               new CaeManager(),
               new DgiSoapClient(config),
               pdfGenerator)
    {
    }
}
