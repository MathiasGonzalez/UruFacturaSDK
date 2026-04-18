namespace UruFacturaSDK.Models;

/// <summary>
/// Resultado de una operación de envío a la DGI.
/// </summary>
public class RespuestaDgi
{
    /// <summary>Código de respuesta devuelto por la DGI.</summary>
    public string Codigo { get; set; } = string.Empty;

    /// <summary>Mensaje descriptivo de la respuesta.</summary>
    public string Mensaje { get; set; } = string.Empty;

    /// <summary>Indica si la operación fue exitosa.</summary>
    public bool Exitoso { get; set; }

    /// <summary>Timestamp de la respuesta.</summary>
    public DateTime FechaRespuesta { get; set; } = DateTime.UtcNow;

    /// <summary>XML de respuesta en bruto (opcional).</summary>
    public string? XmlRespuesta { get; set; }

    public static RespuestaDgi Exito(string codigo, string mensaje, string? xmlRespuesta = null) =>
        new() { Codigo = codigo, Mensaje = mensaje, Exitoso = true, XmlRespuesta = xmlRespuesta };

    public static RespuestaDgi Error(string codigo, string mensaje, string? xmlRespuesta = null) =>
        new() { Codigo = codigo, Mensaje = mensaje, Exitoso = false, XmlRespuesta = xmlRespuesta };
}

