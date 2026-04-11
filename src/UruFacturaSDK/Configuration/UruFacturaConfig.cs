using UruFacturaSDK.Enums;

namespace UruFacturaSDK.Configuration;

/// <summary>
/// Configuración principal del SDK de UruFactura.
/// </summary>
public class UruFacturaConfig
{
    /// <summary>RUT de la empresa emisora (sin puntos ni guión, 12 dígitos).</summary>
    public string RutEmisor { get; set; } = string.Empty;

    /// <summary>Razón social del emisor.</summary>
    public string RazonSocialEmisor { get; set; } = string.Empty;

    /// <summary>Nombre comercial del emisor (opcional).</summary>
    public string? NombreComercialEmisor { get; set; }

    /// <summary>Domicilio fiscal del emisor.</summary>
    public string DomicilioFiscal { get; set; } = string.Empty;

    /// <summary>Ciudad del emisor.</summary>
    public string Ciudad { get; set; } = string.Empty;

    /// <summary>Departamento del emisor.</summary>
    public string Departamento { get; set; } = string.Empty;

    /// <summary>Ambiente de operación (Homologación o Producción).</summary>
    public Ambiente Ambiente { get; set; } = Ambiente.Homologacion;

    /// <summary>Ruta al certificado digital .p12/.pfx.</summary>
    public string RutaCertificado { get; set; } = string.Empty;

    /// <summary>Contraseña del certificado digital.</summary>
    public string PasswordCertificado { get; set; } = string.Empty;

    /// <summary>Número de serie del CAE activo.</summary>
    public string? NroSerieCae { get; set; }

    /// <summary>Rango inicial del CAE.</summary>
    public long CaeRangoDesde { get; set; }

    /// <summary>Rango final del CAE.</summary>
    public long CaeRangoHasta { get; set; }

    /// <summary>Fecha de vencimiento del CAE.</summary>
    public DateTime? CaeFechaVencimiento { get; set; }

    /// <summary>Timeout en segundos para las llamadas SOAP (por defecto 30 s).</summary>
    public int SoapTimeoutSegundos { get; set; } = 30;

    /// <summary>
    /// URL base del servicio SOAP de la DGI según el ambiente configurado.
    /// </summary>
    public string DgiSoapBaseUrl =>
        Ambiente == Ambiente.Produccion
            ? "https://efactura.dgi.gub.uy/ePresentacionSoap/service"
            : "https://efacturahomologacion.dgi.gub.uy/ePresentacionSoap/service";

    /// <summary>
    /// Valida que la configuración obligatoria esté presente.
    /// </summary>
    public void Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(RutEmisor))
            errors.Add("RutEmisor es obligatorio.");

        if (string.IsNullOrWhiteSpace(RazonSocialEmisor))
            errors.Add("RazonSocialEmisor es obligatorio.");

        if (string.IsNullOrWhiteSpace(DomicilioFiscal))
            errors.Add("DomicilioFiscal es obligatorio.");

        if (string.IsNullOrWhiteSpace(RutaCertificado))
            errors.Add("RutaCertificado es obligatorio.");

        if (string.IsNullOrWhiteSpace(PasswordCertificado))
            errors.Add("PasswordCertificado es obligatorio.");

        if (errors.Count > 0)
            throw new Exceptions.UruFacturaException(
                $"Configuración inválida: {string.Join("; ", errors)}");
    }
}
