using UruFactura.Pdf;

namespace UruFactura;

public sealed partial class UruFacturaClientBuilder
{
    /// <summary>
    /// Agrega el generador de PDF predeterminado del paquete completo
    /// (FluentReport + SkiaSharp + ZXing).
    /// </summary>
    public UruFacturaClientBuilder WithDefaultPdf() =>
        WithPdfGenerator(new CfePdfGenerator(_config));
}
