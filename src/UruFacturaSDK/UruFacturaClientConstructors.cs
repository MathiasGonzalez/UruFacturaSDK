using UruFacturaSDK.Configuration;
using UruFacturaSDK.Pdf;

namespace UruFacturaSDK;

public partial class UruFacturaClient
{
    /// <summary>
    /// Inicializa el cliente de UruFactura con la configuración provista.
    /// Usa el generador de PDF predeterminado (QuestPDF + SkiaSharp + ZXing).
    /// </summary>
    /// <param name="config">Configuración del SDK.</param>
    public UruFacturaClient(UruFacturaConfig config)
        : this(config, new CfePdfGenerator(config))
    {
    }
}
