using UruFacturaSDK.Configuration;
using UruFacturaSDK.Enums;
using UruFacturaSDK.Exceptions;
using UruFacturaSDK.Models;
using UruFacturaSDK.Pdf;
using Xunit;

namespace UruFacturaSDK.Tests;

/// <summary>
/// Tests de integración para <see cref="CfePdfGenerator"/>.
/// Ejercitan el generador real (FluentReport + SkiaSharp) sin stubs.
/// </summary>
public class CfePdfGeneratorTests
{
    private static UruFacturaConfig ConfigValida() => new()
    {
        RutEmisor = "210000000012",
        RazonSocialEmisor = "Empresa Test S.A.",
        NombreComercialEmisor = "Comercio Test",
        DomicilioFiscal = "Av. 18 de Julio 1234",
        Ciudad = "Montevideo",
        Departamento = "Montevideo",
    };

    private static Cfe CfeBase() => new()
    {
        Tipo = TipoCfe.ETicket,
        Numero = 1,
        Serie = "A",
        FechaEmision = new DateTime(2025, 6, 15),
        RutEmisor = "210000000012",
        RazonSocialEmisor = "Empresa Test S.A.",
        MontoTotal = 1220m,
        MontoNetoBasico = 1000m,
        IvaBasico = 220m,
        Detalle =
        {
            new LineaDetalle
            {
                NroLinea = 1,
                NombreItem = "Producto A",
                Cantidad = 2,
                PrecioUnitario = 610m,
                IndFactIva = TipoIva.Basico,
            },
        },
    };

    // -----------------------------------------------------------------------
    // Constructor
    // -----------------------------------------------------------------------

    [Fact]
    public void Constructor_QrGeneratorNull_LanzaArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new CfePdfGenerator(ConfigValida(), null!));
    }

    // -----------------------------------------------------------------------
    // GenerarA4 — output válido
    // -----------------------------------------------------------------------

    [Fact]
    public void GenerarA4_CfeCompleto_RetornaBytesNoVacios()
    {
        var generator = new CfePdfGenerator(ConfigValida());
        var bytes = generator.GenerarA4(CfeBase());

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public void GenerarA4_CfeCompleto_RetornaPdf()
    {
        var generator = new CfePdfGenerator(ConfigValida());
        var bytes = generator.GenerarA4(CfeBase());

        // Los PDF comienzan con la firma "%PDF"
        Assert.Equal(0x25, bytes[0]); // '%'
        Assert.Equal(0x50, bytes[1]); // 'P'
        Assert.Equal(0x44, bytes[2]); // 'D'
        Assert.Equal(0x46, bytes[3]); // 'F'
    }

    [Fact]
    public void GenerarA4_CfeConReceptor_NoLanzaExcepcion()
    {
        var cfe = CfeBase();
        cfe.Receptor = new Receptor
        {
            RazonSocial = "Cliente S.A.",
            Documento = "210000000099",
            Direccion = "Calle Falsa 123",
        };

        var generator = new CfePdfGenerator(ConfigValida());
        var ex = Record.Exception(() => generator.GenerarA4(cfe));

        Assert.Null(ex);
    }

    [Fact]
    public void GenerarA4_CfeAceptadoPorDgi_NoLanzaExcepcion()
    {
        var cfe = CfeBase();
        cfe.AceptadoPorDgi = true;

        var generator = new CfePdfGenerator(ConfigValida());
        var ex = Record.Exception(() => generator.GenerarA4(cfe));

        Assert.Null(ex);
    }

    // -----------------------------------------------------------------------
    // GenerarTermico — output válido
    // -----------------------------------------------------------------------

    [Fact]
    public void GenerarTermico_CfeCompleto_RetornaBytesNoVacios()
    {
        var generator = new CfePdfGenerator(ConfigValida());
        var bytes = generator.GenerarTermico(CfeBase());

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public void GenerarTermico_CfeCompleto_RetornaPdf()
    {
        var generator = new CfePdfGenerator(ConfigValida());
        var bytes = generator.GenerarTermico(CfeBase());

        Assert.Equal(0x25, bytes[0]); // '%'
        Assert.Equal(0x50, bytes[1]); // 'P'
        Assert.Equal(0x44, bytes[2]); // 'D'
        Assert.Equal(0x46, bytes[3]); // 'F'
    }

    [Fact]
    public void GenerarTermico_CfeConReceptor_NoLanzaExcepcion()
    {
        var cfe = CfeBase();
        cfe.Receptor = new Receptor { RazonSocial = "Cliente S.A." };

        var generator = new CfePdfGenerator(ConfigValida());
        var ex = Record.Exception(() => generator.GenerarTermico(cfe));

        Assert.Null(ex);
    }

    // -----------------------------------------------------------------------
    // PdfGenerationException wrapping
    // -----------------------------------------------------------------------

    [Fact]
    public void GenerarA4_QrGeneratorLanza_EnvuelveEnPdfGenerationException()
    {
        var generator = new CfePdfGenerator(ConfigValida(), new QrGeneratorQueLanza());

        var ex = Assert.Throws<PdfGenerationException>(() => generator.GenerarA4(CfeBase()));

        Assert.NotNull(ex.InnerException);
        Assert.IsType<InvalidOperationException>(ex.InnerException);
    }

    [Fact]
    public void GenerarTermico_QrGeneratorLanza_EnvuelveEnPdfGenerationException()
    {
        var generator = new CfePdfGenerator(ConfigValida(), new QrGeneratorQueLanza());

        var ex = Assert.Throws<PdfGenerationException>(() => generator.GenerarTermico(CfeBase()));

        Assert.NotNull(ex.InnerException);
        Assert.IsType<InvalidOperationException>(ex.InnerException);
    }

    // -----------------------------------------------------------------------
    // Stub auxiliar
    // -----------------------------------------------------------------------

    private sealed class QrGeneratorQueLanza : ICfeQrGenerator
    {
        public byte[] GenerarQrCode(Cfe cfe) =>
            throw new InvalidOperationException("QR generator simulado falló");

        public byte[] GenerarQrCode(Cfe cfe, Enums.Ambiente ambiente) =>
            throw new InvalidOperationException("QR generator simulado falló");
    }
}
