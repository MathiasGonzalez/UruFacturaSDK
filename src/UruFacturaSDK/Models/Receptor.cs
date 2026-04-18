using UruFacturaSDK.Enums;

namespace UruFacturaSDK.Models;

/// <summary>
/// Información del receptor del CFE.
/// </summary>
public class Receptor
{
    /// <summary>RUT o documento de identidad del receptor (opcional para e-Ticket al consumidor final).</summary>
    public string? Documento { get; set; }

    /// <summary>Tipo de documento del receptor.</summary>
    public TipoDocumentoReceptor TipoDocumento { get; set; } = TipoDocumentoReceptor.Rut;

    /// <summary>Razón social o nombre del receptor.</summary>
    public string? RazonSocial { get; set; }

    /// <summary>Dirección fiscal del receptor.</summary>
    public string? Direccion { get; set; }

    /// <summary>Ciudad del receptor.</summary>
    public string? Ciudad { get; set; }

    /// <summary>Departamento del receptor.</summary>
    public string? Departamento { get; set; }

    /// <summary>País del receptor (para exportación).</summary>
    public string? Pais { get; set; }

    /// <summary>Email del receptor (para envío electrónico).</summary>
    public string? Email { get; set; }
}
