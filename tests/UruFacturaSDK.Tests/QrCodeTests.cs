using UruFacturaSDK.Enums;
using UruFacturaSDK.Exceptions;
using UruFacturaSDK.Models;
using UruFacturaSDK.Pdf;
using Xunit;

namespace UruFacturaSDK.Tests;

public class QrCodeTests
{
    private static Cfe CrearCfeBase() =>
        new()
        {
            Tipo = TipoCfe.ETicket,
            Numero = 100,
            Serie = "A",
            FechaEmision = new DateTime(2025, 6, 15),
            RutEmisor = "210000000012",
            RazonSocialEmisor = "Empresa Test S.A.",
            MontoTotal = 1220m,
        };

    [Fact]
    public void GenerarQrCode_CfeValido_RetornaBytesNoVacios()
    {
        var cfe = CrearCfeBase();
        var bytes = CfePdfGenerator.GenerarQrCode(cfe);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public void GenerarQrCode_RetornaPng()
    {
        var cfe = CrearCfeBase();
        var bytes = CfePdfGenerator.GenerarQrCode(cfe);

        // Los PNG comienzan con la firma \x89PNG
        Assert.Equal(0x89, bytes[0]);
        Assert.Equal(0x50, bytes[1]); // 'P'
        Assert.Equal(0x4E, bytes[2]); // 'N'
        Assert.Equal(0x47, bytes[3]); // 'G'
    }
}
