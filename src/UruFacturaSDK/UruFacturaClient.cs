using UruFacturaSDK.Configuration;
using UruFacturaSDK.Pdf;

namespace UruFacturaSDK;

/// <summary>
/// Punto de entrada principal del SDK de UruFactura.
/// Extiende <see cref="UruFacturaClientBase"/> añadiendo la generación de representaciones impresas (PDF) del CFE.
/// </summary>
public class UruFacturaClient : UruFacturaClientBase, IUruFacturaClient
{
    private readonly ICfePdfGenerator _pdfGenerator;

    /// <summary>
    /// Inicializa el cliente de UruFactura con la configuración provista.
    /// </summary>
    /// <param name="config">Configuración del SDK.</param>
    public UruFacturaClient(UruFacturaConfig config)
        : this(config, new CfePdfGenerator(config))
    {
    }

    /// <summary>
    /// Inicializa el cliente de UruFactura con la configuración y un generador de PDF personalizado.
    /// </summary>
    /// <param name="config">Configuración del SDK.</param>
    /// <param name="pdfGenerator">Implementación de <see cref="ICfePdfGenerator"/> a utilizar.</param>
    public UruFacturaClient(UruFacturaConfig config, ICfePdfGenerator pdfGenerator)
        : base(config)
    {
        _pdfGenerator = pdfGenerator;
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
}

