using UruFacturaSDK.Enums;
using UruFacturaSDK.Exceptions;
using UruFacturaSDK.Models;
using Xunit;

namespace UruFacturaSDK.Tests;

public class CfeModelTests
{
    private static Cfe CriarCfeValido() =>
        new()
        {
            Tipo = TipoCfe.ETicket,
            Numero = 1,
            FechaEmision = DateTime.Today,
            RutEmisor = "210000000012",
            RazonSocialEmisor = "Empresa Test S.A.",
            DomicilioFiscalEmisor = "Av. 18 de Julio 1234",
            CiudadEmisor = "Montevideo",
            DepartamentoEmisor = "Montevideo",
            Detalle =
            {
                new LineaDetalle
                {
                    NroLinea = 1,
                    NombreItem = "Producto A",
                    Cantidad = 2,
                    PrecioUnitario = 100m,
                    IndFactIva = TipoIva.Basico,
                }
            }
        };

    [Fact]
    public void Validar_CfeValido_NoRetornaErrores()
    {
        var cfe = CriarCfeValido();
        var errores = cfe.Validar();
        Assert.Empty(errores);
    }

    [Fact]
    public void Validar_SinDetalle_RetornaError()
    {
        var cfe = CriarCfeValido();
        cfe.Detalle.Clear();
        var errores = cfe.Validar();
        Assert.Contains(errores, e => e.Contains("línea de detalle"));
    }

    [Fact]
    public void Validar_NumeroInvalido_RetornaError()
    {
        var cfe = CriarCfeValido();
        cfe.Numero = 0;
        var errores = cfe.Validar();
        Assert.Contains(errores, e => e.Contains("número"));
    }

    [Fact]
    public void Validar_LineaSinNombre_RetornaError()
    {
        var cfe = CriarCfeValido();
        cfe.Detalle[0].NombreItem = "";
        var errores = cfe.Validar();
        Assert.Contains(errores, e => e.Contains("NombreItem"));
    }

    [Fact]
    public void Validar_LineaCantidadCero_RetornaError()
    {
        var cfe = CriarCfeValido();
        cfe.Detalle[0].Cantidad = 0;
        var errores = cfe.Validar();
        Assert.Contains(errores, e => e.Contains("Cantidad"));
    }

    [Fact]
    public void Validar_NotaCreditoSinReferencias_RetornaError()
    {
        var cfe = CriarCfeValido();
        cfe.Tipo = TipoCfe.NotaCreditoETicket;
        // No añadimos referencias
        var errores = cfe.Validar();
        Assert.Contains(errores, e => e.Contains("referenci"));
    }

    [Fact]
    public void CalcularTotales_IvaBasico_CalculaCorrectamente()
    {
        var cfe = CriarCfeValido();
        // 2 unidades × $100 = $200 neto
        cfe.CalcularTotales();

        Assert.Equal(200m, cfe.MontoNetoBasico);
        Assert.Equal(44m, cfe.IvaBasico);         // 22% de 200
        Assert.Equal(244m, cfe.MontoTotal);
    }

    [Fact]
    public void CalcularTotales_IvaMinimo_CalculaCorrectamente()
    {
        var cfe = CriarCfeValido();
        cfe.Detalle[0].IndFactIva = TipoIva.Minimo;
        cfe.Detalle[0].Cantidad = 1;
        cfe.Detalle[0].PrecioUnitario = 1000m;
        cfe.CalcularTotales();

        Assert.Equal(1000m, cfe.MontoNetoMinimo);
        Assert.Equal(100m, cfe.IvaMinimo);        // 10% de 1000
        Assert.Equal(1100m, cfe.MontoTotal);
    }

    [Fact]
    public void CalcularTotales_Exento_NoAplicaIva()
    {
        var cfe = CriarCfeValido();
        cfe.Detalle[0].IndFactIva = TipoIva.Exento;
        cfe.CalcularTotales();

        Assert.Equal(200m, cfe.MontoNetoExento);
        Assert.Equal(0m, cfe.IvaBasico);
        Assert.Equal(200m, cfe.MontoTotal);
    }

    [Fact]
    public void CalcularTotales_MixtoIvas_TotalesCorrectos()
    {
        var cfe = CriarCfeValido();
        cfe.Detalle.Add(new LineaDetalle
        {
            NroLinea = 2,
            NombreItem = "Producto Exento",
            Cantidad = 1,
            PrecioUnitario = 50m,
            IndFactIva = TipoIva.Exento,
        });
        cfe.CalcularTotales();

        // Basico: 200 neto + 44 IVA = 244
        // Exento: 50
        // Total = 294
        Assert.Equal(200m, cfe.MontoNetoBasico);
        Assert.Equal(50m, cfe.MontoNetoExento);
        Assert.Equal(294m, cfe.MontoTotal);
    }

    [Fact]
    public void LineaDetalle_MontoTotal_CalculaCorrectamente()
    {
        var linea = new LineaDetalle
        {
            Cantidad = 3,
            PrecioUnitario = 150m,
            DescuentoMonto = 10m,
        };
        Assert.Equal(440m, linea.MontoTotal); // 3×150 - 10 = 440
    }

    // --- IVA Suspendido ---

    [Fact]
    public void CalcularTotales_Suspendido_SeParaDeExento()
    {
        var cfe = CriarCfeValido();
        cfe.Detalle[0].IndFactIva = TipoIva.Suspendido;
        cfe.Detalle[0].Cantidad = 1;
        cfe.Detalle[0].PrecioUnitario = 500m;
        cfe.CalcularTotales();

        Assert.Equal(500m, cfe.MontoNetoSuspendido);
        Assert.Equal(0m, cfe.MontoNetoExento);  // no se mezclan
        Assert.Equal(0m, cfe.IvaBasico);
        Assert.Equal(500m, cfe.MontoTotal);
    }

    [Fact]
    public void CalcularTotales_ExentoYSuspendido_SonCamposSeparados()
    {
        var cfe = CriarCfeValido();
        cfe.Detalle[0].IndFactIva = TipoIva.Exento;
        cfe.Detalle[0].PrecioUnitario = 300m;
        cfe.Detalle[0].Cantidad = 1;
        cfe.Detalle.Add(new LineaDetalle
        {
            NroLinea = 2,
            NombreItem = "Item Suspendido",
            Cantidad = 1,
            PrecioUnitario = 200m,
            IndFactIva = TipoIva.Suspendido,
        });
        cfe.CalcularTotales();

        Assert.Equal(300m, cfe.MontoNetoExento);
        Assert.Equal(200m, cfe.MontoNetoSuspendido);
        Assert.Equal(500m, cfe.MontoTotal);
    }

    // --- Validación de Receptor en e-Factura ---

    [Fact]
    public void Validar_EFacturaSinReceptor_RetornaError()
    {
        var cfe = CriarCfeValido();
        cfe.Tipo = TipoCfe.EFactura;
        cfe.Receptor = null;
        var errores = cfe.Validar();
        Assert.Contains(errores, e => e.Contains("receptor"));
    }

    [Fact]
    public void Validar_EFacturaConReceptor_NoRetornaErrorReceptor()
    {
        var cfe = CriarCfeValido();
        cfe.Tipo = TipoCfe.EFactura;
        cfe.Receptor = new Receptor { Documento = "210000000013", TipoDocumento = TipoDocumentoReceptor.Rut };
        var errores = cfe.Validar();
        Assert.DoesNotContain(errores, e => e.Contains("receptor"));
    }

    [Fact]
    public void Validar_EFacturaExportacionSinReceptor_RetornaError()
    {
        var cfe = CriarCfeValido();
        cfe.Tipo = TipoCfe.EFacturaExportacion;
        cfe.Receptor = null;
        var errores = cfe.Validar();
        Assert.Contains(errores, e => e.Contains("receptor"));
    }

    // --- Validación de IndTraslado en e-Remito ---

    [Fact]
    public void Validar_ERemitoConIndTraslado_NoRetornaError()
    {
        var cfe = CriarCfeValido();
        cfe.Tipo = TipoCfe.ERemito;
        cfe.IndTraslado = IndTraslado.TrasladoPropio;
        var errores = cfe.Validar();
        Assert.DoesNotContain(errores, e => e.Contains("IndTraslado") || e.Contains("traslado"));
    }

    [Fact]
    public void Validar_ERemito_SinIndTraslado_RetornaError()
    {
        var cfe = CriarCfeValido();
        cfe.Tipo = TipoCfe.ERemito;
        cfe.IndTraslado = null;
        var errores = cfe.Validar();
        Assert.Contains(errores, e => e.Contains("traslado") || e.Contains("IndTraslado"));
    }

    [Fact]
    public void Validar_ERemitoDespachante_SinIndTraslado_RetornaError()
    {
        var cfe = CriarCfeValido();
        cfe.Tipo = TipoCfe.ERemitoDespachante;
        cfe.IndTraslado = null;
        var errores = cfe.Validar();
        Assert.Contains(errores, e => e.Contains("traslado") || e.Contains("IndTraslado"));
    }

    [Fact]
    public void Validar_ERemitoConIndTrasladoFueraDeRango_RetornaError()
    {
        var cfe = CriarCfeValido();
        cfe.Tipo = TipoCfe.ERemito;
        cfe.IndTraslado = (IndTraslado)99; // valor fuera del enum
        var errores = cfe.Validar();
        Assert.Contains(errores, e => e.Contains("IndTraslado") || e.Contains("no soportado"));
    }
}
