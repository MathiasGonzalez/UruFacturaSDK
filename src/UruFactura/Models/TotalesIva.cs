using UruFactura.Enums;

namespace UruFactura.Models;

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
