using UruFacturaSDK.Models;

namespace UruFacturaSDK.Pdf;

/// <summary>
/// Contrato para la generación de la representación impresa (PDF) de un CFE.
/// Implementar esta interfaz permite reemplazar el motor de PDF predeterminado.
/// </summary>
public interface ICfePdfGenerator
{
    /// <summary>
    /// Genera el PDF en formato A4 del CFE.
    /// </summary>
    /// <param name="cfe">El CFE con datos completos.</param>
    /// <returns>Array de bytes del PDF generado.</returns>
    byte[] GenerarA4(Cfe cfe);

    /// <summary>
    /// Genera el PDF en formato térmico (ticket de 80mm) del CFE.
    /// </summary>
    /// <param name="cfe">El CFE con datos completos.</param>
    /// <returns>Array de bytes del PDF generado.</returns>
    byte[] GenerarTermico(Cfe cfe);
}
