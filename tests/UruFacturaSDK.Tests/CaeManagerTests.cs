using UruFacturaSDK.Enums;
using UruFacturaSDK.Exceptions;
using UruFacturaSDK.Models;
using Xunit;

namespace UruFacturaSDK.Tests;

public class CaeManagerTests
{
    private static Models.Cae CriarCaeVigente(
        TipoCfe tipo = TipoCfe.ETicket,
        long desde = 1,
        long hasta = 100,
        long ultimoUsado = 0) =>
        new()
        {
            NroSerie = "CAE-001",
            TipoCfe = tipo,
            RangoDesde = desde,
            RangoHasta = hasta,
            FechaVencimiento = DateTime.Today.AddMonths(3),
            UltimoNroUsado = ultimoUsado,
        };

    private static Models.Cae CriarCaeVencido() =>
        new()
        {
            NroSerie = "CAE-OLD",
            TipoCfe = TipoCfe.ETicket,
            RangoDesde = 1,
            RangoHasta = 100,
            FechaVencimiento = DateTime.Today.AddDays(-1),
            UltimoNroUsado = 0,
        };

    [Fact]
    public void RegistrarCae_YObtenerCaeActivo_RetornaCae()
    {
        var manager = new Cae.CaeManager();
        var cae = CriarCaeVigente();
        manager.RegistrarCae(cae);

        var activo = manager.ObtenerCaeActivo(TipoCfe.ETicket);
        Assert.NotNull(activo);
        Assert.Equal("CAE-001", activo.NroSerie);
    }

    [Fact]
    public void ObtenerCaeActivo_SinCaeRegistrado_RetornaNull()
    {
        var manager = new Cae.CaeManager();
        var activo = manager.ObtenerCaeActivo(TipoCfe.EFactura);
        Assert.Null(activo);
    }

    [Fact]
    public void ObtenerCaeActivo_CaeVencido_RetornaNull()
    {
        var manager = new Cae.CaeManager();
        manager.RegistrarCae(CriarCaeVencido());

        var activo = manager.ObtenerCaeActivo(TipoCfe.ETicket);
        Assert.Null(activo);
    }

    [Fact]
    public void ObtenerProximoNumero_PrimerUso_RetornaRangoDesde()
    {
        var manager = new Cae.CaeManager();
        manager.RegistrarCae(CriarCaeVigente(desde: 500));

        var (_, numero) = manager.ObtenerProximoNumero(TipoCfe.ETicket);
        Assert.Equal(500, numero);
    }

    [Fact]
    public void ObtenerProximoNumero_SegundoUso_Incrementa()
    {
        var manager = new Cae.CaeManager();
        manager.RegistrarCae(CriarCaeVigente(desde: 1, ultimoUsado: 5));

        var (_, numero) = manager.ObtenerProximoNumero(TipoCfe.ETicket);
        Assert.Equal(6, numero);
    }

    [Fact]
    public void ObtenerProximoNumero_SinCae_LanzaCaeException()
    {
        var manager = new Cae.CaeManager();
        Assert.Throws<CaeException>(() => manager.ObtenerProximoNumero(TipoCfe.EFactura));
    }

    [Fact]
    public void ObtenerProximoNumero_RangoAgotado_LanzaCaeException()
    {
        var manager = new Cae.CaeManager();
        // CAE con rango 1-5, ya usados todos
        manager.RegistrarCae(CriarCaeVigente(desde: 1, hasta: 5, ultimoUsado: 5));

        Assert.Throws<CaeException>(() => manager.ObtenerProximoNumero(TipoCfe.ETicket));
    }

    [Fact]
    public void ObtenerAdvertencias_CaePorVencer_RetornaAdvertencia()
    {
        var manager = new Cae.CaeManager();
        var cae = new Models.Cae
        {
            NroSerie = "CAE-VENCE",
            TipoCfe = TipoCfe.ETicket,
            RangoDesde = 1,
            RangoHasta = 100,
            FechaVencimiento = DateTime.Today.AddDays(3), // vence en 3 días
            UltimoNroUsado = 0,
        };
        manager.RegistrarCae(cae);

        var advertencias = manager.ObtenerAdvertencias(diasAlertaVencimiento: 7);
        Assert.NotEmpty(advertencias);
        Assert.Contains(advertencias, a => a.Contains("vence"));
    }

    [Fact]
    public void ObtenerAdvertencias_CaeSinProblemas_RetornaVacio()
    {
        var manager = new Cae.CaeManager();
        manager.RegistrarCae(CriarCaeVigente()); // vence en 3 meses, sin uso

        var advertencias = manager.ObtenerAdvertencias(diasAlertaVencimiento: 7, porcentajeAlertaUso: 80);
        Assert.Empty(advertencias);
    }

    [Fact]
    public void Cae_EsVigente_VigenteRetornaTrue()
    {
        var cae = CriarCaeVigente();
        Assert.True(cae.EsVigente);
    }

    [Fact]
    public void Cae_EsVigente_VencidoRetornaFalse()
    {
        var cae = CriarCaeVencido();
        Assert.False(cae.EsVigente);
    }

    [Fact]
    public void Cae_TieneNumerosDisponibles_RetornaTrue()
    {
        var cae = CriarCaeVigente(desde: 1, hasta: 100, ultimoUsado: 50);
        Assert.True(cae.TieneNumerosDisponibles);
    }

    [Fact]
    public void Cae_TieneNumerosDisponibles_AgotadoRetornaFalse()
    {
        var cae = CriarCaeVigente(desde: 1, hasta: 5, ultimoUsado: 5);
        Assert.False(cae.TieneNumerosDisponibles);
    }

    [Fact]
    public void Cae_PorcentajeUso_CalculaCorrectamente()
    {
        var cae = CriarCaeVigente(desde: 1, hasta: 100, ultimoUsado: 50);
        Assert.Equal(50m, cae.PorcentajeUso);
    }

    [Fact]
    public void Cae_ObtenerProximoNumero_VencidoLanzaExcepcion()
    {
        var cae = CriarCaeVencido();
        Assert.Throws<CaeException>(() => cae.ObtenerProximoNumero());
    }

    [Fact]
    public void ObtenerTodosLosCaes_RetornaTodos()
    {
        var manager = new Cae.CaeManager();
        manager.RegistrarCae(CriarCaeVigente(TipoCfe.ETicket));
        manager.RegistrarCae(CriarCaeVigente(TipoCfe.EFactura));

        var todos = manager.ObtenerTodosLosCaes();
        Assert.Equal(2, todos.Count);
    }

    [Fact]
    public void ResumenEstado_RetornaStringNoVacio()
    {
        var manager = new Cae.CaeManager();
        manager.RegistrarCae(CriarCaeVigente());
        var resumen = manager.ResumenEstado();
        Assert.NotEmpty(resumen);
        Assert.Contains("CAE-001", resumen);
    }
}
