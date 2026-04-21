using UruFacturaSDK.Enums;
using UruFacturaSDK.Models;

namespace UruFacturaSDK.Pdf;

/// <summary>
/// Contrato para la generación del código QR de un CFE según normativa DGI.
/// Implementar esta interfaz permite reemplazar el motor de QR predeterminado.
/// </summary>
public interface ICfeQrGenerator
{
    /// <summary>
    /// Genera el código QR del CFE usando el ambiente de <b>Producción</b>.
    /// </summary>
    /// <param name="cfe">El CFE cuyos datos se codifican en el QR.</param>
    /// <returns>Imagen PNG del código QR como array de bytes.</returns>
    byte[] GenerarQrCode(Cfe cfe);

    /// <summary>
    /// Genera el código QR del CFE para el ambiente especificado.
    /// </summary>
    /// <param name="cfe">El CFE cuyos datos se codifican en el QR.</param>
    /// <param name="ambiente">Ambiente de operación (Homologación o Producción).</param>
    /// <returns>Imagen PNG del código QR como array de bytes.</returns>
    byte[] GenerarQrCode(Cfe cfe, Ambiente ambiente);
}
