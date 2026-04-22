using UruFacturaSDK.Enums;
using UruFacturaSDK.Exceptions;
using UruFacturaSDK.Models;
using UruFacturaSDK.Xml;
using Xunit;

namespace UruFacturaSDK.Tests;

public class CfeXmlBuilderTests
{
    private readonly CfeXmlBuilder _builder = new();

    private static Cfe CrearCfeCompleto() =>
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
        var cfe = CrearCfeCompleto();
        var xml = _builder.Generar(cfe);

        Assert.NotNull(xml);
        Assert.NotEmpty(xml);
    }

    [Fact]
    public void Generar_CfeValido_XmlContieneNodoCfe()
    {
        var cfe = CrearCfeCompleto();
        var xml = _builder.Generar(cfe);

        Assert.Contains("<CFE", xml);
        Assert.Contains("</CFE>", xml);
    }

    [Fact]
    public void Generar_CfeValido_XmlContieneEncabezado()
    {
        var cfe = CrearCfeCompleto();
        var xml = _builder.Generar(cfe);

        Assert.Contains("<Encabezado", xml);
        Assert.Contains("<IdDoc", xml);
        Assert.Contains("<Emisor", xml);
        Assert.Contains("<Totales", xml);
    }

    [Fact]
    public void Generar_CfeValido_XmlContieneTipoCorrecto()
    {
        var cfe = CrearCfeCompleto();
        var xml = _builder.Generar(cfe);

        Assert.Contains("<TipoCFE>101</TipoCFE>", xml);
    }

    [Fact]
    public void Generar_CfeValido_XmlContieneNumero()
    {
        var cfe = CrearCfeCompleto();
        var xml = _builder.Generar(cfe);

        Assert.Contains("<Nro>42</Nro>", xml);
    }

    [Fact]
    public void Generar_CfeValido_XmlContieneRutEmisor()
    {
        var cfe = CrearCfeCompleto();
        var xml = _builder.Generar(cfe);

        Assert.Contains("<RUCEmisor>210000000012</RUCEmisor>", xml);
    }

    [Fact]
    public void Generar_CfeValido_XmlContieneDetalle()
    {
        var cfe = CrearCfeCompleto();
        var xml = _builder.Generar(cfe);

        Assert.Contains("<Detalle", xml);
        Assert.Contains("<Item", xml);
        Assert.Contains("Servicio de consultoría", xml);
    }

    [Fact]
    public void Generar_CfeValido_XmlContieneTotales()
    {
        var cfe = CrearCfeCompleto();
        var xml = _builder.Generar(cfe);

        // 1000 neto basico, 22% IVA = 220, Total = 1220
        Assert.Contains("<MntNetoIVA>1000.00</MntNetoIVA>", xml);
        Assert.Contains("<MntIVA>220.00</MntIVA>", xml);
        Assert.Contains("<MntTotal>1220.00</MntTotal>", xml);
    }

    [Fact]
    public void Generar_CfeValido_XmlEsXmlValido()
    {
        var cfe = CrearCfeCompleto();
        var xml = _builder.Generar(cfe);

        // Verificar que el XML es parseable
        var doc = new System.Xml.XmlDocument();
        doc.LoadXml(xml); // No debe lanzar excepción
        Assert.NotNull(doc.DocumentElement);
    }

    [Fact]
    public void Generar_CfeConReceptor_XmlContieneReceptor()
    {
        var cfe = CrearCfeCompleto();
        cfe.Receptor = new Models.Receptor
        {
            RazonSocial = "Cliente Test S.R.L.",
            Documento = "219999999018",
            TipoDocumento = UruFacturaSDK.Enums.TipoDocumentoReceptor.Rut,
        };

        var xml = _builder.Generar(cfe);

        Assert.Contains("<Receptor", xml);
        Assert.Contains("Cliente Test S.R.L.", xml);
        Assert.Contains("219999999018", xml);
    }

    [Fact]
    public void Generar_CfeConReferencia_XmlContieneReferencia()
    {
        var cfe = CrearCfeCompleto();
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
    public void Generar_XmlGenerado_EsXmlBienFormado()
    {
        var cfe = CrearCfeCompleto();
        var xml = _builder.Generar(cfe);

        // Verificar que el XML comienza con declaración XML y contiene el elemento raíz
        Assert.StartsWith("<?xml", xml.TrimStart());
        Assert.Contains("<CFE", xml);
        Assert.NotEmpty(xml);
    }

    // --- DGI Compatibility ---

    [Fact]
    public void Generar_MntBruto_EsCero_PreciosNetos()
    {
        var cfe = CrearCfeCompleto();
        var xml = _builder.Generar(cfe);

        // MntBruto=0 significa que los precios en <Detalle> son netos (sin IVA).
        // MntBruto=1 indicaría precios brutos (con IVA incluido), lo cual sería incorrecto.
        Assert.Contains("<MntBruto>0</MntBruto>", xml);
        Assert.DoesNotContain("<MntBruto>1</MntBruto>", xml);
    }

    [Fact]
    public void Generar_MonedaDolar_TipoMonedaUsaCodigoAlfa()
    {
        var cfe = CrearCfeCompleto();
        cfe.Moneda = Moneda.DolarAmericano;
        cfe.TipoCambio = 42.5m;
        var xml = _builder.Generar(cfe);

        Assert.Contains("<TipoMoneda>USD</TipoMoneda>", xml);
        Assert.DoesNotContain("<TipoMoneda>840</TipoMoneda>", xml);
        Assert.Contains("<TpoCambio>42.5000</TpoCambio>", xml);
    }

    [Fact]
    public void Generar_MonedaEuro_TipoMonedaUsaCodigoAlfa()
    {
        var cfe = CrearCfeCompleto();
        cfe.Moneda = Moneda.Euro;
        cfe.TipoCambio = 50.0m;
        var xml = _builder.Generar(cfe);

        Assert.Contains("<TipoMoneda>EUR</TipoMoneda>", xml);
    }

    [Fact]
    public void Generar_MonedaPeso_OmiteTipoMoneda()
    {
        var cfe = CrearCfeCompleto();
        cfe.Moneda = Moneda.PesoUruguayo;
        var xml = _builder.Generar(cfe);

        Assert.DoesNotContain("TipoMoneda", xml);
        Assert.DoesNotContain("TpoCambio", xml);
    }

    [Fact]
    public void Validar_MonedaExtranjeraSinTipoCambio_RetornaError()
    {
        var cfe = CrearCfeCompleto();
        cfe.Moneda = Moneda.DolarAmericano;
        // TipoCambio = null → debe dar error
        var errores = cfe.Validar();
        Assert.Contains(errores, e => e.Contains("TipoCambio"));
    }

    [Fact]
    public void Validar_MonedaExtranjeraConTipoCambio_NoRetornaErrorMoneda()
    {
        var cfe = CrearCfeCompleto();
        cfe.Moneda = Moneda.DolarAmericano;
        cfe.TipoCambio = 42.5m;
        var errores = cfe.Validar();
        Assert.DoesNotContain(errores, e => e.Contains("TipoCambio"));
    }

    [Fact]
    public void Generar_XmlGenerado_DeclaracionContieneEncodingUtf8()
    {
        var cfe = CrearCfeCompleto();
        var xml = _builder.Generar(cfe);

        // La declaración XML debe indicar encoding="utf-8" (no utf-16).
        // Esto es requerido por los validadores de la DGI.
        Assert.Contains("encoding=\"utf-8\"", xml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("utf-16", xml, StringComparison.OrdinalIgnoreCase);
    }

    // --- Nuevos campos DGI ---

    [Fact]
    public void Generar_CfeValido_XmlContieneMntPagar()
    {
        var cfe = CrearCfeCompleto();
        var xml = _builder.Generar(cfe);

        // MntPagar es obligatorio en Totales y debe coincidir con MntTotal
        Assert.Contains("<MntPagar>1220.00</MntPagar>", xml);
    }

    [Fact]
    public void Generar_ConIvaSuspendido_XmlContieneMntSuspenso()
    {
        var cfe = CrearCfeCompleto();
        cfe.Detalle[0].IndFactIva = TipoIva.Suspendido;
        cfe.Detalle[0].Cantidad = 1;
        cfe.Detalle[0].PrecioUnitario = 500m;

        var xml = _builder.Generar(cfe);

        Assert.Contains("<MntSuspenso>500.00</MntSuspenso>", xml);
        Assert.DoesNotContain("<MntExe>", xml);
    }

    [Fact]
    public void Generar_ConIvaSuspendido_NoMezclaMntExe()
    {
        var cfe = CrearCfeCompleto();
        cfe.Detalle[0].IndFactIva = TipoIva.Suspendido;

        var xml = _builder.Generar(cfe);

        // Suspendido debe ir en MntSuspenso, no en MntExe
        Assert.Contains("MntSuspenso", xml);
        Assert.DoesNotContain("<MntExe>", xml);
    }

    [Fact]
    public void Generar_ERemito_XmlContieneIndTraslado()
    {
        var cfe = CrearCfeCompleto();
        cfe.Tipo = TipoCfe.ERemito;
        cfe.IndTraslado = IndTraslado.TrasladoPropio;

        var xml = _builder.Generar(cfe);

        Assert.Contains("<IndTraslado>1</IndTraslado>", xml);
    }

    [Fact]
    public void Generar_ETicket_XmlNoContieneIndTraslado()
    {
        var cfe = CrearCfeCompleto();
        var xml = _builder.Generar(cfe);

        Assert.DoesNotContain("IndTraslado", xml);
    }

    [Fact]
    public void Generar_ConGiro_XmlContieneGiroNegocio()
    {
        var cfe = CrearCfeCompleto();
        cfe.Giro = "Comercio minorista";

        var xml = _builder.Generar(cfe);

        Assert.Contains("<GiroNegocio>Comercio minorista</GiroNegocio>", xml);
    }

    [Fact]
    public void Generar_SinGiro_XmlNoContieneGiroNegocio()
    {
        var cfe = CrearCfeCompleto();
        cfe.Giro = null;

        var xml = _builder.Generar(cfe);

        Assert.DoesNotContain("GiroNegocio", xml);
    }
}
