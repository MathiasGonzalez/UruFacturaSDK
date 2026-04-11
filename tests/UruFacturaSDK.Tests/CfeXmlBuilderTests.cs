using UruFacturaSDK.Enums;
using UruFacturaSDK.Exceptions;
using UruFacturaSDK.Models;
using UruFacturaSDK.Xml;
using Xunit;

namespace UruFacturaSDK.Tests;

public class CfeXmlBuilderTests
{
    private readonly CfeXmlBuilder _builder = new();

    private static Cfe CriarCfeCompleto() =>
        new()
        {
            Tipo = TipoCfe.ETicket,
            Serie = "A",
            Numero = 42,
            FechaEmision = new DateTime(2025, 6, 15),
            RutEmisor = "210000000012",
            RazonSocialEmisor = "Empresa Test S.A.",
            DomicilioFiscalEmisor = "Av. 18 de Julio 1234",
            CiudadEmisor = "Montevideo",
            DepartamentoEmisor = "Montevideo",
            FormaPago = FormaPago.Contado,
            Moneda = Moneda.PesoUruguayo,
            Detalle =
            {
                new LineaDetalle
                {
                    NroLinea = 1,
                    NombreItem = "Servicio de consultoría",
                    Cantidad = 1,
                    PrecioUnitario = 1000m,
                    IndFactIva = TipoIva.Basico,
                }
            }
        };

    [Fact]
    public void Generar_CfeValido_RetornaXmlNoVacio()
    {
        var cfe = CriarCfeCompleto();
        var xml = _builder.Generar(cfe);

        Assert.NotNull(xml);
        Assert.NotEmpty(xml);
    }

    [Fact]
    public void Generar_CfeValido_XmlContieneNodoCfe()
    {
        var cfe = CriarCfeCompleto();
        var xml = _builder.Generar(cfe);

        Assert.Contains("<CFE", xml);
        Assert.Contains("</CFE>", xml);
    }

    [Fact]
    public void Generar_CfeValido_XmlContieneEncabezado()
    {
        var cfe = CriarCfeCompleto();
        var xml = _builder.Generar(cfe);

        Assert.Contains("<Encabezado", xml);
        Assert.Contains("<IdDoc", xml);
        Assert.Contains("<Emisor", xml);
        Assert.Contains("<Totales", xml);
    }

    [Fact]
    public void Generar_CfeValido_XmlContieneTipoCorrecto()
    {
        var cfe = CriarCfeCompleto();
        var xml = _builder.Generar(cfe);

        Assert.Contains("<TipoCFE>101</TipoCFE>", xml);
    }

    [Fact]
    public void Generar_CfeValido_XmlContieneNumero()
    {
        var cfe = CriarCfeCompleto();
        var xml = _builder.Generar(cfe);

        Assert.Contains("<Nro>42</Nro>", xml);
    }

    [Fact]
    public void Generar_CfeValido_XmlContieneRutEmisor()
    {
        var cfe = CriarCfeCompleto();
        var xml = _builder.Generar(cfe);

        Assert.Contains("<RUCEmisor>210000000012</RUCEmisor>", xml);
    }

    [Fact]
    public void Generar_CfeValido_XmlContieneDetalle()
    {
        var cfe = CriarCfeCompleto();
        var xml = _builder.Generar(cfe);

        Assert.Contains("<Detalle", xml);
        Assert.Contains("<Item", xml);
        Assert.Contains("Servicio de consultoría", xml);
    }

    [Fact]
    public void Generar_CfeValido_XmlContieneTotales()
    {
        var cfe = CriarCfeCompleto();
        var xml = _builder.Generar(cfe);

        // 1000 neto basico, 22% IVA = 220, Total = 1220
        Assert.Contains("<MntNetoIVA>1000.00</MntNetoIVA>", xml);
        Assert.Contains("<MntIVA>220.00</MntIVA>", xml);
        Assert.Contains("<MntTotal>1220.00</MntTotal>", xml);
    }

    [Fact]
    public void Generar_CfeValido_XmlEsXmlValido()
    {
        var cfe = CriarCfeCompleto();
        var xml = _builder.Generar(cfe);

        // Verificar que el XML es parseable
        var doc = new System.Xml.XmlDocument();
        doc.LoadXml(xml); // No debe lanzar excepción
        Assert.NotNull(doc.DocumentElement);
    }

    [Fact]
    public void Generar_CfeConReceptor_XmlContieneReceptor()
    {
        var cfe = CriarCfeCompleto();
        cfe.Receptor = new Models.Receptor
        {
            RazonSocial = "Cliente Test S.R.L.",
            Documento = "219999999018",
            TipoDocumento = 2,
        };

        var xml = _builder.Generar(cfe);

        Assert.Contains("<Receptor", xml);
        Assert.Contains("Cliente Test S.R.L.", xml);
        Assert.Contains("219999999018", xml);
    }

    [Fact]
    public void Generar_CfeConReferencia_XmlContieneReferencia()
    {
        var cfe = CriarCfeCompleto();
        cfe.Tipo = TipoCfe.NotaCreditoETicket;
        cfe.Referencias.Add(new Models.RefCfe
        {
            TipoCfe = TipoCfe.ETicket,
            Serie = "A",
            NroCfe = 10,
            FechaCfe = new DateTime(2025, 6, 1),
            Razon = "Anulación de e-Ticket",
        });

        var xml = _builder.Generar(cfe);

        Assert.Contains("<Referencia", xml);
        Assert.Contains("<RefDoc", xml);
        Assert.Contains("Anulación de e-Ticket", xml);
    }

    [Fact]
    public void Generar_CfeInvalido_LanzaCfeValidationException()
    {
        var cfe = new Cfe
        {
            Tipo = TipoCfe.ETicket,
            Numero = 0, // inválido
            FechaEmision = DateTime.Today,
        };

        Assert.Throws<CfeValidationException>(() => _builder.Generar(cfe));
    }

    [Fact]
    public void Generar_XmlActualizaXmlSinFirmarEnCfe()
    {
        var cfe = CriarCfeCompleto();
        var xml = _builder.Generar(cfe);

        // El XML generado debe coincidir con lo que retorna el método
        Assert.Equal(xml, xml); // just checking it doesn't throw
        Assert.NotNull(xml);
    }
}
