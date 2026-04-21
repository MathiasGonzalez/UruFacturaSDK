using UruFacturaSDK.Enums;

namespace UruFacturaSDK.Models;

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

    /// <summary>RUT del emisor (sin puntos ni guión, 12 dígitos).</summary>
    public string RutEmisor { get; set; } = string.Empty;

    /// <summary>Razón social del emisor.</summary>
    public string RazonSocialEmisor { get; set; } = string.Empty;

    /// <summary>Nombre comercial del emisor (opcional).</summary>
    public string? NombreComercialEmisor { get; set; }

    /// <summary>Giro o actividad económica del emisor (opcional).</summary>
    public string? Giro { get; set; }

    /// <summary>Domicilio fiscal del emisor.</summary>
    public string DomicilioFiscalEmisor { get; set; } = string.Empty;

    /// <summary>Ciudad del emisor.</summary>
    public string CiudadEmisor { get; set; } = string.Empty;

    /// <summary>Departamento del emisor.</summary>
    public string DepartamentoEmisor { get; set; } = string.Empty;

    // --- Receptor ---

    /// <summary>Datos del receptor del comprobante.</summary>
    public Receptor? Receptor { get; set; }

    /// <summary>
    /// Indicador del motivo del traslado. Obligatorio para e-Remito (181) y e-Remito Despachante (131).
    /// </summary>
    public IndTraslado? IndTraslado { get; set; }

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

    /// <summary>Monto neto exento de IVA.</summary>
    public decimal MontoNetoExento { get; set; }

    /// <summary>Monto neto con IVA suspendido.</summary>
    public decimal MontoNetoSuspendido { get; set; }

    /// <summary>Monto neto gravado a IVA mínimo (10%).</summary>
    public decimal MontoNetoMinimo { get; set; }

    /// <summary>Monto neto gravado a IVA básico (22%).</summary>
    public decimal MontoNetoBasico { get; set; }

    /// <summary>Monto de IVA mínimo (10% sobre <see cref="MontoNetoMinimo"/>).</summary>
    public decimal IvaMinimo { get; set; }

    /// <summary>Monto de IVA básico (22% sobre <see cref="MontoNetoBasico"/>).</summary>
    public decimal IvaBasico { get; set; }

    /// <summary>Monto total del comprobante (netos + IVA).</summary>
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
        MontoNetoSuspendido = 0;
        MontoNetoMinimo = 0;
        MontoNetoBasico = 0;

        foreach (var linea in Detalle)
        {
            switch (linea.IndFactIva)
            {
                case TipoIva.Exento:
                    MontoNetoExento += linea.MontoTotal;
                    break;
                case TipoIva.Suspendido:
                    MontoNetoSuspendido += linea.MontoTotal;
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
        MontoNetoSuspendido = Math.Round(MontoNetoSuspendido, 2);
        MontoNetoMinimo = Math.Round(MontoNetoMinimo, 2);
        MontoNetoBasico = Math.Round(MontoNetoBasico, 2);

        IvaMinimo = Math.Round(MontoNetoMinimo * 0.10m, 2);
        IvaBasico = Math.Round(MontoNetoBasico * 0.22m, 2);

        MontoTotal = MontoNetoExento + MontoNetoSuspendido
                   + MontoNetoMinimo + IvaMinimo
                   + MontoNetoBasico + IvaBasico;
        MontoTotal = Math.Round(MontoTotal, 2);
    }

    private static readonly TipoCfe[] TiposCorreccion =
    [
        TipoCfe.NotaCreditoETicket, TipoCfe.NotaDebitoETicket,
        TipoCfe.NotaCreditoEFactura, TipoCfe.NotaDebitoEFactura,
        TipoCfe.NotaCreditoEFacturaExportacion, TipoCfe.NotaDebitoEFacturaExportacion,
        TipoCfe.NotaCreditoERemito,
    ];

    private static readonly TipoCfe[] TiposFactura =
    [
        TipoCfe.EFactura, TipoCfe.NotaCreditoEFactura, TipoCfe.NotaDebitoEFactura,
        TipoCfe.EFacturaExportacion, TipoCfe.NotaCreditoEFacturaExportacion, TipoCfe.NotaDebitoEFacturaExportacion,
        TipoCfe.EResguardo,
    ];

    private static readonly TipoCfe[] TiposRemito =
    [
        TipoCfe.ERemito, TipoCfe.ERemitoDespachante,
    ];

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

        if (Detalle.Count == 0)
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

        if (Moneda != Moneda.PesoUruguayo && TipoCambio is null or <= 0)
            errors.Add("TipoCambio es obligatorio y debe ser mayor a cero cuando la moneda no es Peso Uruguayo.");

        // Notas de corrección requieren referencias
        if (Array.Exists(TiposCorreccion, t => t == Tipo) && Referencias.Count == 0)
            errors.Add("Las notas de crédito/débito deben referenciar al menos un CFE.");

        // e-Factura y tipos relacionados requieren datos del receptor
        if (Array.Exists(TiposFactura, t => t == Tipo) && Receptor == null)
            errors.Add("Las e-Facturas y e-Resguardos requieren datos del receptor.");

        // e-Remito y e-Remito Despachante requieren IndTraslado válido
        if (Array.Exists(TiposRemito, t => t == Tipo) && IndTraslado == null)
            errors.Add("Los e-Remitos requieren indicar el motivo de traslado (IndTraslado).");
        else if (Array.Exists(TiposRemito, t => t == Tipo) && IndTraslado.HasValue
                 && !Enum.IsDefined(typeof(IndTraslado), IndTraslado.Value))
            errors.Add("IndTraslado contiene un valor no soportado para e-Remitos.");

        return errors;
    }
}
