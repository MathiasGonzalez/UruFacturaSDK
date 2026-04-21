using UruFacturaSDK.Pdf;

namespace UruFacturaSDK;

public sealed partial class UruFacturaClientBuilder
{
    /// <summary>
    /// Agrega el generador de PDF predeterminado del paquete completo
    /// (QuestPDF + SkiaSharp + ZXing).
    /// </summary>
    public UruFacturaClientBuilder ConPdfPorDefecto() =>
        ConGeneradorPdf(new CfePdfGenerator(_config));
}
