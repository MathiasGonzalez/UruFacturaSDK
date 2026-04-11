using UruFacturaSDK.Enums;

namespace UruFacturaSDK.Models;

/// <summary>
/// Información del receptor del CFE.
/// </summary>
public class Receptor
{
    /// <summary>RUT o documento de identidad del receptor (opcional para e-Ticket al consumidor final).</summary>
    public string? Documento { get; set; }

    /// <summary>Tipo de documento: 2=RUT, 3=Cédula, etc.</summary>
    public int TipoDocumento { get; set; } = 2;

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

/// <summary>
/// Línea de detalle del CFE.
/// </summary>
public class LineaDetalle
{
    /// <summary>Número de línea (correlativo a partir de 1).</summary>
    public int NroLinea { get; set; }

    /// <summary>Cantidad del ítem.</summary>
    public decimal Cantidad { get; set; }

    /// <summary>Unidad de medida (p. ej. "UN", "KG").</summary>
    public string? UnidadMedida { get; set; }

    /// <summary>Descripción del ítem.</summary>
    public string NombreItem { get; set; } = string.Empty;

    /// <summary>Precio unitario sin IVA.</summary>
    public decimal PrecioUnitario { get; set; }

    /// <summary>Indicador de IVA aplicable.</summary>
    public TipoIva IndFactIva { get; set; } = TipoIva.Basico;

    /// <summary>Monto del descuento por línea (sin IVA, opcional).</summary>
    public decimal DescuentoMonto { get; set; }

    /// <summary>Porcentaje de descuento (opcional).</summary>
    public decimal? DescuentoPorcentaje { get; set; }

    /// <summary>Monto del recargo por línea (sin IVA, opcional).</summary>
    public decimal RecargoMonto { get; set; }

    /// <summary>Monto total neto de la línea (Cantidad × PrecioUnitario − Descuento + Recargo).</summary>
    public decimal MontoTotal => Math.Round(Cantidad * PrecioUnitario - DescuentoMonto + RecargoMonto, 2);
}

/// <summary>
/// Totales del CFE por tipo de IVA.
/// </summary>
public class TotalesIva
{
    public TipoIva CodigoIva { get; set; }
    public decimal TasaIva { get; set; }
    public decimal BaseImponible { get; set; }
    public decimal MontoIva { get; set; }
}

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

/// <summary>
/// Modelo principal que representa un CFE (Comprobante Fiscal Electrónico).
/// </summary>
public class Cfe
{
    // --- Identificación ---

    /// <summary>Tipo de comprobante.</summary>
    public TipoCfe Tipo { get; set; }

    /// <summary>Serie del comprobante (letras, opcional según normativa).</summary>
    public string? Serie { get; set; }

    /// <summary>Número del comprobante.</summary>
    public long Numero { get; set; }

    /// <summary>Fecha de emisión del comprobante.</summary>
    public DateTime FechaEmision { get; set; } = DateTime.Today;

    // --- Emisor (completado por SDK desde configuración) ---
    public string RutEmisor { get; set; } = string.Empty;
    public string RazonSocialEmisor { get; set; } = string.Empty;
    public string? NombreComercialEmisor { get; set; }
    public string DomicilioFiscalEmisor { get; set; } = string.Empty;
    public string CiudadEmisor { get; set; } = string.Empty;
    public string DepartamentoEmisor { get; set; } = string.Empty;

    // --- Receptor ---

    /// <summary>Datos del receptor del comprobante.</summary>
    public Receptor? Receptor { get; set; }

    // --- Condiciones ---

    /// <summary>Forma de pago.</summary>
    public FormaPago FormaPago { get; set; } = FormaPago.Contado;

    /// <summary>Moneda del comprobante.</summary>
    public Moneda Moneda { get; set; } = Moneda.PesoUruguayo;

    /// <summary>Tipo de cambio (si la moneda no es peso uruguayo).</summary>
    public decimal? TipoCambio { get; set; }

    // --- Detalle ---

    /// <summary>Líneas de detalle del comprobante.</summary>
    public List<LineaDetalle> Detalle { get; set; } = new();

    // --- Referencias ---

    /// <summary>Referencias a otros CFE (para notas correctivas).</summary>
    public List<RefCfe> Referencias { get; set; } = new();

    // --- Totales calculados ---

    public decimal MontoNetoExento { get; set; }
    public decimal MontoNetoMinimo { get; set; }
    public decimal MontoNetoBasico { get; set; }
    public decimal IvaMinimo { get; set; }
    public decimal IvaBasico { get; set; }
    public decimal MontoTotal { get; set; }

    // --- Estado / firma ---

    /// <summary>XML del CFE sin firmar.</summary>
    public string? XmlSinFirmar { get; set; }

    /// <summary>XML del CFE firmado digitalmente.</summary>
    public string? XmlFirmado { get; set; }

    /// <summary>Código de respuesta de la DGI.</summary>
    public string? CodigoRespuestaDgi { get; set; }

    /// <summary>Mensaje de respuesta de la DGI.</summary>
    public string? MensajeRespuestaDgi { get; set; }

    /// <summary>Indica si el CFE fue aceptado por la DGI.</summary>
    public bool AceptadoPorDgi { get; set; }

    /// <summary>
    /// Calcula los totales del CFE a partir del detalle.
    /// Tasas: IVA mínimo 10%, IVA básico 22%.
    /// </summary>
    public void CalcularTotales()
    {
        MontoNetoExento = 0;
        MontoNetoMinimo = 0;
        MontoNetoBasico = 0;

        foreach (var linea in Detalle)
        {
            switch (linea.IndFactIva)
            {
                case TipoIva.Exento:
                case TipoIva.Suspendido:
                    MontoNetoExento += linea.MontoTotal;
                    break;
                case TipoIva.Minimo:
                    MontoNetoMinimo += linea.MontoTotal;
                    break;
                case TipoIva.Basico:
                    MontoNetoBasico += linea.MontoTotal;
                    break;
            }
        }

        MontoNetoExento = Math.Round(MontoNetoExento, 2);
        MontoNetoMinimo = Math.Round(MontoNetoMinimo, 2);
        MontoNetoBasico = Math.Round(MontoNetoBasico, 2);

        IvaMinimo = Math.Round(MontoNetoMinimo * 0.10m, 2);
        IvaBasico = Math.Round(MontoNetoBasico * 0.22m, 2);

        MontoTotal = MontoNetoExento + MontoNetoMinimo + IvaMinimo
                   + MontoNetoBasico + IvaBasico;
        MontoTotal = Math.Round(MontoTotal, 2);
    }

    /// <summary>
    /// Valida el CFE y retorna la lista de errores encontrados.
    /// </summary>
    public IReadOnlyList<string> Validar()
    {
        var errors = new List<string>();

        if (Numero <= 0)
            errors.Add("El número de comprobante debe ser mayor a cero.");

        if (FechaEmision == default)
            errors.Add("FechaEmision es obligatoria.");

        if (!Detalle.Any())
            errors.Add("El CFE debe tener al menos una línea de detalle.");

        for (int i = 0; i < Detalle.Count; i++)
        {
            var linea = Detalle[i];
            if (string.IsNullOrWhiteSpace(linea.NombreItem))
                errors.Add($"Línea {i + 1}: NombreItem es obligatorio.");
            if (linea.Cantidad <= 0)
                errors.Add($"Línea {i + 1}: Cantidad debe ser mayor a cero.");
            if (linea.PrecioUnitario < 0)
                errors.Add($"Línea {i + 1}: PrecioUnitario no puede ser negativo.");
        }

        // Notas de corrección requieren referencias
        var tiposCorreccion = new[]
        {
            TipoCfe.NotaCreditoETicket, TipoCfe.NotaDebitoETicket,
            TipoCfe.NotaCreditoEFactura, TipoCfe.NotaDebitoEFactura,
            TipoCfe.NotaCreditoEFacturaExportacion, TipoCfe.NotaDebitoEFacturaExportacion,
            TipoCfe.NotaCreditoERemito,
        };

        if (Array.Exists(tiposCorreccion, t => t == Tipo) && !Referencias.Any())
            errors.Add("Las notas de crédito/débito deben referenciar al menos un CFE.");

        return errors;
    }
}
