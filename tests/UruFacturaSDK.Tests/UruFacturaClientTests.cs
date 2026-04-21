using UruFacturaSDK.Cae;
using UruFacturaSDK.Configuration;
using UruFacturaSDK.Enums;
using UruFacturaSDK.Models;
using UruFacturaSDK.Pdf;
using UruFacturaSDK.Signature;
using UruFacturaSDK.Soap;
using UruFacturaSDK.Xml;
using Xunit;

namespace UruFacturaSDK.Tests;

/// <summary>
/// Tests para <see cref="UruFacturaClient"/>.
/// Verifica la creación de CFE, la delegación a cada dependencia,
/// el comportamiento de Dispose y las guardas post-dispose.
/// Las dependencias externas (firma, SOAP, PDF) se sustituyen por stubs
/// para evitar I/O real de red o disco.
/// </summary>
public class UruFacturaClientTests
{
    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static UruFacturaConfig ConfigValida() => new()
    {
        RutEmisor = "210000000012",
        RazonSocialEmisor = "Empresa Test S.A.",
        DomicilioFiscal = "Av. 18 de Julio 1234",
        Ciudad = "Montevideo",
        Departamento = "Montevideo",
        Ambiente = Ambiente.Homologacion,
        RutaCertificado = "/tmp/fake.p12",
        PasswordCertificado = "secret",
    };

    private static UruFacturaClient CrearCliente(
        XmlBuilderStub? xmlBuilder = null,
        FirmanteStub? firmante = null,
        ICaeManager? caeManager = null,
        SoapClientStub? soapClient = null,
        ICfePdfGenerator? pdfGenerator = null) =>
        new(
            ConfigValida(),
            xmlBuilder ?? new XmlBuilderStub(),
            firmante ?? new FirmanteStub(),
            caeManager ?? new CaeManager(),
            soapClient ?? new SoapClientStub(),
            pdfGenerator);

    // -----------------------------------------------------------------------
    // Fábrica de CFE — tipo correcto
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(TipoCfe.ETicket)]
    [InlineData(TipoCfe.NotaCreditoETicket)]
    [InlineData(TipoCfe.NotaDebitoETicket)]
    [InlineData(TipoCfe.EFactura)]
    [InlineData(TipoCfe.NotaCreditoEFactura)]
    [InlineData(TipoCfe.NotaDebitoEFactura)]
    [InlineData(TipoCfe.EFacturaExportacion)]
    [InlineData(TipoCfe.NotaCreditoEFacturaExportacion)]
    [InlineData(TipoCfe.NotaDebitoEFacturaExportacion)]
    [InlineData(TipoCfe.ERemitoDespachante)]
    [InlineData(TipoCfe.EResguardo)]
    [InlineData(TipoCfe.ERemito)]
    [InlineData(TipoCfe.NotaCreditoERemito)]
    public void CrearXxx_RetornaCfeConTipoCorrecto(TipoCfe tipo)
    {
        using var client = CrearCliente();

        Cfe cfe = tipo switch
        {
            TipoCfe.ETicket                        => client.CrearETicket(),
            TipoCfe.NotaCreditoETicket             => client.CrearNotaCreditoETicket(),
            TipoCfe.NotaDebitoETicket              => client.CrearNotaDebitoETicket(),
            TipoCfe.EFactura                       => client.CrearEFactura(),
            TipoCfe.NotaCreditoEFactura            => client.CrearNotaCreditoEFactura(),
            TipoCfe.NotaDebitoEFactura             => client.CrearNotaDebitoEFactura(),
            TipoCfe.EFacturaExportacion            => client.CrearEFacturaExportacion(),
            TipoCfe.NotaCreditoEFacturaExportacion => client.CrearNotaCreditoEFacturaExportacion(),
            TipoCfe.NotaDebitoEFacturaExportacion  => client.CrearNotaDebitoEFacturaExportacion(),
            TipoCfe.ERemitoDespachante             => client.CrearERemitoDespachante(),
            TipoCfe.EResguardo                     => client.CrearEResguardo(),
            TipoCfe.ERemito                        => client.CrearERemito(),
            TipoCfe.NotaCreditoERemito             => client.CrearNotaCreditoERemito(),
            _ => throw new InvalidOperationException($"Tipo no manejado: {tipo}"),
        };

        Assert.Equal(tipo, cfe.Tipo);
    }

    [Fact]
    public void CrearETicket_PropagaDatosEmisorDesdeConfig()
    {
        var config = ConfigValida();
        config.NombreComercialEmisor = "Comercio X";
        config.Giro = "Venta de servicios";
        using var client = new UruFacturaClient(
            config,
            new XmlBuilderStub(),
            new FirmanteStub(),
            new CaeManager(),
            new SoapClientStub(),
            null);

        var cfe = client.CrearETicket();

        Assert.Equal("210000000012", cfe.RutEmisor);
        Assert.Equal("Empresa Test S.A.", cfe.RazonSocialEmisor);
        Assert.Equal("Comercio X", cfe.NombreComercialEmisor);
        Assert.Equal("Venta de servicios", cfe.Giro);
        Assert.Equal("Av. 18 de Julio 1234", cfe.DomicilioFiscalEmisor);
        Assert.Equal("Montevideo", cfe.CiudadEmisor);
        Assert.Equal("Montevideo", cfe.DepartamentoEmisor);
        Assert.Equal(DateTime.Today, cfe.FechaEmision);
    }

    [Fact]
    public void CrearETicket_FechaEmisionEsHoy()
    {
        using var client = CrearCliente();
        var cfe = client.CrearETicket();
        Assert.Equal(DateTime.Today, cfe.FechaEmision);
    }

    // -----------------------------------------------------------------------
    // GenerarXml
    // -----------------------------------------------------------------------

    [Fact]
    public void GenerarXml_LlamaXmlBuilder()
    {
        var xmlStub = new XmlBuilderStub("<CFE/>");
        using var client = CrearCliente(xmlBuilder: xmlStub);

        client.GenerarXml(client.CrearETicket());

        Assert.True(xmlStub.LlamadoGenerar);
    }

    [Fact]
    public void GenerarXml_RetornaXmlDelBuilder()
    {
        var xmlStub = new XmlBuilderStub("<CFE/>");
        using var client = CrearCliente(xmlBuilder: xmlStub);

        var xml = client.GenerarXml(client.CrearETicket());

        Assert.Equal("<CFE/>", xml);
    }

    [Fact]
    public void GenerarXml_GuardaXmlSinFirmarEnCfe()
    {
        var xmlStub = new XmlBuilderStub("<CFE/>");
        using var client = CrearCliente(xmlBuilder: xmlStub);

        var cfe = client.CrearETicket();
        client.GenerarXml(cfe);

        Assert.Equal("<CFE/>", cfe.XmlSinFirmar);
    }

    // -----------------------------------------------------------------------
    // FirmarCfe
    // -----------------------------------------------------------------------

    [Fact]
    public void FirmarCfe_LlamaFirmante()
    {
        var firmanteStub = new FirmanteStub();
        using var client = CrearCliente(firmante: firmanteStub);

        var cfe = client.CrearETicket();
        client.GenerarXml(cfe);
        client.FirmarCfe(cfe);

        Assert.True(firmanteStub.LlamadoFirmar);
    }

    [Fact]
    public void FirmarCfe_GuardaXmlFirmadoEnCfe()
    {
        var xmlStub = new XmlBuilderStub("<CFE/>");
        using var client = CrearCliente(xmlBuilder: xmlStub);

        var cfe = client.CrearETicket();
        var xmlFirmado = client.FirmarCfe(cfe);

        Assert.Equal("<CFE/>-firmado", xmlFirmado);
        Assert.Equal("<CFE/>-firmado", cfe.XmlFirmado);
    }

    [Fact]
    public void FirmarCfe_SinXmlPrevio_GeneraXmlPrimeroLuegoFirma()
    {
        var xmlStub = new XmlBuilderStub("<CFE/>");
        using var client = CrearCliente(xmlBuilder: xmlStub);

        var cfe = client.CrearETicket();
        Assert.Null(cfe.XmlSinFirmar);

        client.FirmarCfe(cfe);

        Assert.True(xmlStub.LlamadoGenerar);
        Assert.NotNull(cfe.XmlSinFirmar);
    }

    // -----------------------------------------------------------------------
    // GenerarYFirmar
    // -----------------------------------------------------------------------

    [Fact]
    public void GenerarYFirmar_GeneraYFirmaEnUnPaso()
    {
        var xmlStub = new XmlBuilderStub("<CFE/>");
        var firmanteStub = new FirmanteStub();
        using var client = CrearCliente(xmlBuilder: xmlStub, firmante: firmanteStub);

        var cfe = client.CrearETicket();
        var resultado = client.GenerarYFirmar(cfe);

        Assert.True(xmlStub.LlamadoGenerar);
        Assert.True(firmanteStub.LlamadoFirmar);
        Assert.NotEmpty(resultado);
        Assert.Equal(cfe.XmlFirmado, resultado);
    }

    // -----------------------------------------------------------------------
    // EnviarCfeAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task EnviarCfeAsync_LlamaSoapClient()
    {
        var soapStub = new SoapClientStub();
        using var client = CrearCliente(soapClient: soapStub);

        await client.EnviarCfeAsync(client.CrearETicket());

        Assert.True(soapStub.LlamadoEnviarCfe);
    }

    [Fact]
    public async Task EnviarCfeAsync_SinXmlFirmado_FirmaAutomaticamente()
    {
        var firmanteStub = new FirmanteStub();
        using var client = CrearCliente(firmante: firmanteStub);

        var cfe = client.CrearETicket();
        Assert.Null(cfe.XmlFirmado);

        await client.EnviarCfeAsync(cfe);

        Assert.True(firmanteStub.LlamadoFirmar);
        Assert.NotNull(cfe.XmlFirmado);
    }

    [Fact]
    public async Task EnviarCfeAsync_RespuestaExitosa_GuardaEstadoEnCfe()
    {
        var soapStub = new SoapClientStub(codigo: "00", mensaje: "Aceptado", exitoso: true);
        using var client = CrearCliente(soapClient: soapStub);

        var cfe = client.CrearETicket();
        var respuesta = await client.EnviarCfeAsync(cfe);

        Assert.True(respuesta.Exitoso);
        Assert.Equal("00", cfe.CodigoRespuestaDgi);
        Assert.Equal("Aceptado", cfe.MensajeRespuestaDgi);
        Assert.True(cfe.AceptadoPorDgi);
    }

    [Fact]
    public async Task EnviarCfeAsync_RespuestaError_GuardaEstadoNegativoEnCfe()
    {
        var soapStub = new SoapClientStub(codigo: "99", mensaje: "Rechazado", exitoso: false);
        using var client = CrearCliente(soapClient: soapStub);

        var cfe = client.CrearETicket();
        var respuesta = await client.EnviarCfeAsync(cfe);

        Assert.False(respuesta.Exitoso);
        Assert.Equal("99", cfe.CodigoRespuestaDgi);
        Assert.False(cfe.AceptadoPorDgi);
    }

    // -----------------------------------------------------------------------
    // ConsultarEstadoCfeAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ConsultarEstadoCfeAsync_LlamaSoapClient()
    {
        var soapStub = new SoapClientStub();
        using var client = CrearCliente(soapClient: soapStub);

        var cfe = client.CrearETicket();
        cfe.Numero = 1;
        await client.ConsultarEstadoCfeAsync(cfe);

        Assert.True(soapStub.LlamadoConsultarEstado);
    }

    [Fact]
    public async Task ConsultarEstadoCfeAsync_RetornaRespuestaDelSoap()
    {
        var soapStub = new SoapClientStub(codigo: "01", mensaje: "Con observaciones", exitoso: true);
        using var client = CrearCliente(soapClient: soapStub);

        var cfe = client.CrearETicket();
        var respuesta = await client.ConsultarEstadoCfeAsync(cfe);

        Assert.Equal("01", respuesta.Codigo);
        Assert.True(respuesta.Exitoso);
    }

    // -----------------------------------------------------------------------
    // EnviarReporteDiarioAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task EnviarReporteDiarioAsync_LlamaSoapClient()
    {
        var soapStub = new SoapClientStub();
        using var client = CrearCliente(soapClient: soapStub);

        await client.EnviarReporteDiarioAsync(DateTime.Today, [client.CrearETicket()]);

        Assert.True(soapStub.LlamadoEnviarReporte);
    }

    [Fact]
    public async Task EnviarReporteDiarioAsync_FirmaCfesNoFirmados()
    {
        var firmanteStub = new FirmanteStub();
        using var client = CrearCliente(firmante: firmanteStub);

        var cfe = client.CrearETicket();
        Assert.Null(cfe.XmlFirmado);

        await client.EnviarReporteDiarioAsync(DateTime.Today, [cfe]);

        Assert.True(firmanteStub.LlamadoFirmar);
    }

    [Fact]
    public async Task EnviarReporteDiarioAsync_NoRefirmaXmlYaFirmado()
    {
        var firmanteStub = new FirmanteStub();
        var xmlStub = new XmlBuilderStub("<CFE/>");
        using var client = CrearCliente(xmlBuilder: xmlStub, firmante: firmanteStub);

        var cfe = client.CrearETicket();
        client.GenerarYFirmar(cfe);

        // Resetear el contador después de la firma inicial
        firmanteStub.ContadorFirmar = 0;

        await client.EnviarReporteDiarioAsync(DateTime.Today, [cfe]);

        Assert.Equal(0, firmanteStub.ContadorFirmar);
    }

    // -----------------------------------------------------------------------
    // GenerarPdfA4 / GenerarPdfTermico
    // -----------------------------------------------------------------------

    [Fact]
    public void GenerarPdfA4_SinGenerador_LanzaInvalidOperationException()
    {
        using var client = CrearCliente(pdfGenerator: null);
        Assert.Throws<InvalidOperationException>(() => client.GenerarPdfA4(client.CrearETicket()));
    }

    [Fact]
    public void GenerarPdfTermico_SinGenerador_LanzaInvalidOperationException()
    {
        using var client = CrearCliente(pdfGenerator: null);
        Assert.Throws<InvalidOperationException>(() => client.GenerarPdfTermico(client.CrearETicket()));
    }

    [Fact]
    public void GenerarPdfA4_ConGenerador_LlamaGenerarA4()
    {
        var pdfStub = new PdfGeneratorStub();
        using var client = CrearCliente(pdfGenerator: pdfStub);

        var bytes = client.GenerarPdfA4(client.CrearETicket());

        Assert.True(pdfStub.LlamadoGenerarA4);
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void GenerarPdfTermico_ConGenerador_LlamaGenerarTermico()
    {
        var pdfStub = new PdfGeneratorStub();
        using var client = CrearCliente(pdfGenerator: pdfStub);

        var bytes = client.GenerarPdfTermico(client.CrearETicket());

        Assert.True(pdfStub.LlamadoGenerarTermico);
        Assert.NotEmpty(bytes);
    }

    // -----------------------------------------------------------------------
    // Dispose
    // -----------------------------------------------------------------------

    [Fact]
    public void Dispose_LlamaDisposeEnSoapClient()
    {
        var soapStub = new SoapClientStub();
        var client = CrearCliente(soapClient: soapStub);

        client.Dispose();

        Assert.True(soapStub.Disposed);
    }

    [Fact]
    public void Dispose_LlamaDisposeEnFirmante()
    {
        var firmanteStub = new FirmanteStub();
        var client = CrearCliente(firmante: firmanteStub);

        client.Dispose();

        Assert.True(firmanteStub.Disposed);
    }

    [Fact]
    public void Dispose_SegundoDispose_NoLanzaExcepcion()
    {
        var client = CrearCliente();
        client.Dispose();
        var ex = Record.Exception(() => client.Dispose());
        Assert.Null(ex);
    }

    [Fact]
    public void GenerarXml_PostDispose_LanzaObjectDisposedException()
    {
        var client = CrearCliente();
        client.Dispose();
        Assert.Throws<ObjectDisposedException>(() => client.GenerarXml(new Cfe { Tipo = TipoCfe.ETicket }));
    }

    [Fact]
    public async Task EnviarCfeAsync_PostDispose_LanzaObjectDisposedException()
    {
        var client = CrearCliente();
        client.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => client.EnviarCfeAsync(new Cfe { Tipo = TipoCfe.ETicket }));
    }

    [Fact]
    public void GenerarPdfA4_PostDispose_LanzaObjectDisposedException()
    {
        var pdfStub = new PdfGeneratorStub();
        var client = CrearCliente(pdfGenerator: pdfStub);
        client.Dispose();
        Assert.Throws<ObjectDisposedException>(() => client.GenerarPdfA4(new Cfe { Tipo = TipoCfe.ETicket }));
    }

    // -----------------------------------------------------------------------
    // Stubs internos
    // -----------------------------------------------------------------------

    private sealed class XmlBuilderStub(string xml = "<CFE/>") : ICfeXmlBuilder
    {
        public bool LlamadoGenerar { get; private set; }
        public string Generar(Cfe cfe) { LlamadoGenerar = true; return xml; }
    }

    private sealed class FirmanteStub : ICfeFirmante
    {
        public bool LlamadoFirmar => ContadorFirmar > 0;
        public int ContadorFirmar { get; set; }
        public bool Disposed { get; private set; }

        public string Firmar(string xmlSinFirmar)
        {
            ContadorFirmar++;
            return xmlSinFirmar + "-firmado";
        }

        public void Dispose() { Disposed = true; }
    }

    private sealed class SoapClientStub(
        string codigo = "00",
        string mensaje = "OK",
        bool exitoso = true) : IDgiSoapClient
    {
        public bool LlamadoEnviarCfe { get; private set; }
        public bool LlamadoConsultarEstado { get; private set; }
        public bool LlamadoEnviarReporte { get; private set; }
        public bool Disposed { get; private set; }

        public Task<RespuestaDgi> EnviarCfeAsync(string xmlFirmado, CancellationToken ct = default)
        {
            LlamadoEnviarCfe = true;
            return Task.FromResult(exitoso
                ? RespuestaDgi.Exito(codigo, mensaje)
                : RespuestaDgi.Error(codigo, mensaje));
        }

        public Task<RespuestaDgi> ConsultarEstadoCfeAsync(
            string rut, int tipo, string serie, long numero, CancellationToken ct = default)
        {
            LlamadoConsultarEstado = true;
            return Task.FromResult(exitoso
                ? RespuestaDgi.Exito(codigo, mensaje)
                : RespuestaDgi.Error(codigo, mensaje));
        }

        public Task<RespuestaReporteDiario> EnviarReporteDiarioAsync(
            DateTime fecha, IEnumerable<string> cfes, CancellationToken ct = default)
        {
            LlamadoEnviarReporte = true;
            return Task.FromResult(new RespuestaReporteDiario
            {
                Respuesta = exitoso
                    ? RespuestaDgi.Exito(codigo, mensaje)
                    : RespuestaDgi.Error(codigo, mensaje),
            });
        }

        public void Dispose() { Disposed = true; }
    }

    private sealed class PdfGeneratorStub : ICfePdfGenerator
    {
        public bool LlamadoGenerarA4 { get; private set; }
        public bool LlamadoGenerarTermico { get; private set; }
        public byte[] GenerarA4(Cfe cfe) { LlamadoGenerarA4 = true; return [1, 2, 3]; }
        public byte[] GenerarTermico(Cfe cfe) { LlamadoGenerarTermico = true; return [4, 5, 6]; }
    }
}
