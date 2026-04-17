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

    /// <summary>Razón de la referencia.</summary>
    public string? Razon { get; set; }
}
