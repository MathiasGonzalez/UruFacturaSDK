namespace UruErpApp.Api.Models;

public class Invoice
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public int TipoCfe { get; set; }
    public long Numero { get; set; }
    public DateTime FechaEmision { get; set; }
    public string? RutReceptor { get; set; }
    public string? NombreReceptor { get; set; }
    public decimal MontoTotal { get; set; }
    public decimal MontoNetoExento { get; set; }
    public decimal MontoNetoMinimo { get; set; }
    public decimal MontoNetoBasico { get; set; }
    public decimal IvaMinimo { get; set; }
    public decimal IvaBasico { get; set; }
    public bool AceptadoPorDgi { get; set; }
    public string? CodigoRespuestaDgi { get; set; }
    public string? MensajeRespuestaDgi { get; set; }
    public string? XmlFirmado { get; set; }
    /// <summary>Líneas de detalle serializadas como JSON para reconstrucción del PDF.</summary>
    public string? DetalleJson { get; set; }
    /// <summary>R2 object key for the stored PDF artifact.</summary>
    public string? R2PdfKey { get; set; }
    /// <summary>R2 object key for the stored signed XML artifact.</summary>
    public string? R2XmlKey { get; set; }
}
