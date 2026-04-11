using UruFacturaSDK.Enums;

namespace UruFacturaSDK.Models;

/// <summary>
/// Constancia de Autorización de Emisión (CAE) emitida por la DGI.
/// </summary>
public class Cae
{
    /// <summary>Número de serie del CAE.</summary>
    public string NroSerie { get; set; } = string.Empty;

    /// <summary>Tipo de CFE al que aplica este CAE.</summary>
    public TipoCfe TipoCfe { get; set; }

    /// <summary>Número inicial del rango autorizado.</summary>
    public long RangoDesde { get; set; }

    /// <summary>Número final del rango autorizado.</summary>
    public long RangoHasta { get; set; }

    /// <summary>Fecha de vencimiento del CAE.</summary>
    public DateTime FechaVencimiento { get; set; }

    /// <summary>Último número utilizado dentro del rango.</summary>
    public long UltimoNroUsado { get; set; }

    /// <summary>
    /// Indica si el CAE está vigente (no vencido).
    /// </summary>
    public bool EsVigente => DateTime.Today <= FechaVencimiento;

    /// <summary>
    /// Indica si el CAE tiene números disponibles.
    /// </summary>
    public bool TieneNumerosDisponibles => UltimoNroUsado < RangoHasta;

    /// <summary>
    /// Porcentaje de uso del rango del CAE (0-100).
    /// </summary>
    public decimal PorcentajeUso
    {
        get
        {
            long total = RangoHasta - RangoDesde + 1;
            if (total <= 0) return 100m;
            long usados = UltimoNroUsado - RangoDesde + 1;
            if (usados <= 0) return 0m;
            return Math.Round((decimal)usados / total * 100, 2);
        }
    }

    /// <summary>
    /// Obtiene el próximo número disponible e incrementa el contador.
    /// </summary>
    /// <exception cref="Exceptions.CaeException">Si no hay números disponibles o el CAE está vencido.</exception>
    public long ObtenerProximoNumero()
    {
        if (!EsVigente)
            throw new Exceptions.CaeException(
                $"El CAE {NroSerie} venció el {FechaVencimiento:dd/MM/yyyy}.");

        if (!TieneNumerosDisponibles)
            throw new Exceptions.CaeException(
                $"El CAE {NroSerie} no tiene números disponibles (rango hasta {RangoHasta}).");

        UltimoNroUsado = UltimoNroUsado == 0 ? RangoDesde : UltimoNroUsado + 1;
        return UltimoNroUsado;
    }

    /// <summary>
    /// Devuelve una advertencia si el CAE está por vencer o por agotar su rango.
    /// </summary>
    public string? ObtenerAdvertencia(int diasAlertaVencimiento = 7, decimal porcentajeAlertaUso = 80m)
    {
        if (!EsVigente)
            return $"⚠️ CAE {NroSerie} VENCIDO el {FechaVencimiento:dd/MM/yyyy}.";

        var diasRestantes = (FechaVencimiento - DateTime.Today).Days;
        if (diasRestantes <= diasAlertaVencimiento)
            return $"⚠️ CAE {NroSerie} vence en {diasRestantes} día(s) ({FechaVencimiento:dd/MM/yyyy}).";

        if (PorcentajeUso >= porcentajeAlertaUso)
            return $"⚠️ CAE {NroSerie} ha utilizado el {PorcentajeUso}% de su rango autorizado.";

        return null;
    }
}
