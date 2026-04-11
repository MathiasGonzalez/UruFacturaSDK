using UruFacturaSDK.Configuration;
using UruFacturaSDK.Enums;
using UruFacturaSDK.Exceptions;
using Xunit;

namespace UruFacturaSDK.Tests;

public class UruFacturaConfigTests
{
    private static UruFacturaConfig ConfigValida() => new()
    {
        RutEmisor = "210000000012",
        RazonSocialEmisor = "Empresa Test S.A.",
        DomicilioFiscal = "Av. 18 de Julio 1234",
        Ciudad = "Montevideo",
        Departamento = "Montevideo",
        Ambiente = Ambiente.Homologacion,
        RutaCertificado = "/tmp/cert.p12",
        PasswordCertificado = "secret",
    };

    [Fact]
    public void Validate_ConfigValida_NoLanzaExcepcion()
    {
        var config = ConfigValida();
        var ex = Record.Exception(() => config.Validate());
        Assert.Null(ex);
    }

    [Fact]
    public void Validate_SinRut_LanzaExcepcion()
    {
        var config = ConfigValida();
        config.RutEmisor = "";
        Assert.Throws<UruFacturaException>(() => config.Validate());
    }

    [Fact]
    public void Validate_SinRazonSocial_LanzaExcepcion()
    {
        var config = ConfigValida();
        config.RazonSocialEmisor = "";
        Assert.Throws<UruFacturaException>(() => config.Validate());
    }

    [Fact]
    public void Validate_SinCertificado_LanzaExcepcion()
    {
        var config = ConfigValida();
        config.RutaCertificado = "";
        Assert.Throws<UruFacturaException>(() => config.Validate());
    }

    [Fact]
    public void DgiSoapBaseUrl_Homologacion_UrlCorrecta()
    {
        var config = ConfigValida();
        config.Ambiente = Ambiente.Homologacion;
        Assert.Contains("homologacion", config.DgiSoapBaseUrl, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DgiSoapBaseUrl_Produccion_UrlCorrecta()
    {
        var config = ConfigValida();
        config.Ambiente = Ambiente.Produccion;
        Assert.DoesNotContain("homologacion", config.DgiSoapBaseUrl, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("efactura.dgi.gub.uy", config.DgiSoapBaseUrl);
    }
}
