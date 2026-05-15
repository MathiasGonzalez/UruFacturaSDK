using System.Net;
using System.Net.Http;
using UruFacturaSDK.Configuration;
using UruFacturaSDK.Enums;
using UruFacturaSDK.Soap;
using Xunit;

namespace UruFacturaSDK.Tests;

public class DgiSoapClientTests
{
    private static UruFacturaConfig ConfigValida() => new()
    {
        RutEmisor = "210000000012",
        RazonSocialEmisor = "Empresa Test S.A.",
        DomicilioFiscal = "Av. 18 de Julio 1234",
        Ciudad = "Montevideo",
        Departamento = "Montevideo",
        Ambiente = Ambiente.Homologacion,
        RutaCertificado = "",
        PasswordCertificado = "",
    };

    [Fact]
    public void Constructor_HttpClientExternoConTimeoutInfinito_LanzaArgumentException()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler())
        {
            Timeout = Timeout.InfiniteTimeSpan,
        };

        Assert.Throws<ArgumentException>(() => new DgiSoapClient(ConfigValida(), httpClient));
    }

    [Fact]
    public void Constructor_OmitirValidacionSslTrueConHttpClientExterno_NoLanzaExcepcion()
    {
        using var httpClient = new HttpClient(new StubHttpMessageHandler())
        {
            Timeout = TimeSpan.FromSeconds(30),
        };

        var ex = Record.Exception(() =>
        {
            using var _ = new DgiSoapClient(
                ConfigValida() with { OmitirValidacionSsl = true },
                httpClient);
        });

        Assert.Null(ex);
    }

    [Fact]
    public void Dispose_CuandoHttpClientEsExterno_NoLoDispone()
    {
        var handler = new TrackingHttpMessageHandler();
        using var httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30),
        };

        var soapClient = new DgiSoapClient(ConfigValida(), httpClient);
        soapClient.Dispose();

        Assert.False(handler.Disposed);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }

    private sealed class TrackingHttpMessageHandler : HttpMessageHandler
    {
        public bool Disposed { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }
}
