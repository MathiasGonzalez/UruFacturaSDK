public class Invoice
{
    public int Id { get; set; }
    public int TipoCfe { get; set; }
    public long Numero { get; set; }
    public DateTime FechaEmision { get; set; }
    public string? RutReceptor { get; set; }
    public string? NombreReceptor { get; set; }
    public decimal MontoTotal { get; set; }
    public bool AceptadoPorDgi { get; set; }
    public string? CodigoRespuestaDgi { get; set; }
    public string? MensajeRespuestaDgi { get; set; }
    public string? XmlFirmado { get; set; }
}
