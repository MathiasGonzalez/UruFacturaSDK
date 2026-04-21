using UruFacturaSDK.Enums;
using UruFacturaSDK.Models;

namespace UruFacturaSDK.Pdf;

/// <summary>
/// Implementación predeterminada de <see cref="ICfeQrGenerator"/>.
/// Delega en los métodos estáticos de <see cref="CfePdfGenerator"/>.
/// </summary>
public class CfeQrGenerator : ICfeQrGenerator
{
    /// <inheritdoc />
    public byte[] GenerarQrCode(Cfe cfe) =>
        CfePdfGenerator.GenerarQrCode(cfe);

    /// <inheritdoc />
    public byte[] GenerarQrCode(Cfe cfe, Ambiente ambiente) =>
        CfePdfGenerator.GenerarQrCode(cfe, ambiente);
}
