using UruFacturaSDK.Enums;

namespace UruFacturaSDK.Models;

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
