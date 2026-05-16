using UruFacturaSDK.Enums;
using UruFacturaSDK.Models;

namespace UruFactura.CloudflareApi.Models;

/// <summary>
/// DTO para registrar o pre-cargar un CAE.
/// <para>
/// <c>Tipo</c> es el valor entero de <see cref="TipoCfe"/>
/// (ej: ETicket = 101, EFactura = 111).
/// </para>
/// <para><c>FechaVencimiento</c> debe ser una fecha ISO 8601 (yyyy-MM-dd).</para>
/// </summary>
public record CaeConfigRequest(
    string NroSerie,
    int    Tipo,
    long   RangoDesde,
    long   RangoHasta,
    string FechaVencimiento,
    long   UltimoNroUsado = 0)
{
    public Cae ToModel() => new()
    {
        NroSerie         = NroSerie,
        TipoCfe          = (TipoCfe)Tipo,
        RangoDesde       = RangoDesde,
        RangoHasta       = RangoHasta,
        FechaVencimiento = DateOnly.Parse(FechaVencimiento),
        UltimoNroUsado   = UltimoNroUsado,
    };
}
