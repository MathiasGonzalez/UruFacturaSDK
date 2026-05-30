using UruFacturaSDK.Enums;

namespace UruFacturaSDK.Models;

/// <summary>
/// Referencia a otro CFE (para notas de crédito/débito).
/// </summary>
public class RefCfe
{
    /// <summary>Tipo de CFE al que se hace referencia.</summary>
    public TipoCfe TipoCfe { get; set; }

    /// <summary>Número de serie del CFE referenciado.</summary>
    public string Serie { get; set; } = string.Empty;

    /// <summary>Número del CFE referenciado.</summary>
    public long NroCfe { get; set; }

    /// <summary>Fecha del CFE referenciado.</summary>
    public DateTime FechaCfe { get; set; }

    /// <summary>
    /// Monto total del CFE referenciado (F-C8). Obligatorio en notas de crédito/débito
    /// desde formato CFE v25.01. Debe ser ≥ 0 y ≤ al monto total del comprobante original.
    /// </summary>
    public decimal? MontoCfeRef { get; set; }

    /// <summary>
    /// Moneda del CFE referenciado (F-C9). Obligatorio cuando <see cref="MontoCfeRef"/> está presente.
    /// </summary>
    public Moneda? MonedaCfeRef { get; set; }

    /// <summary>
    /// Tipo de cambio del CFE referenciado (F-C10). Requerido cuando la moneda referenciada
    /// es distinta al peso uruguayo. No debe informarse en referencias globales.
    /// </summary>
    public decimal? TipoCambioCfeRef { get; set; }

    /// <summary>Razón de la referencia.</summary>
    public string? Razon { get; set; }
}
