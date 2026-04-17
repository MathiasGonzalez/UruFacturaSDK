namespace UruFacturaSDK.Exceptions;

/// <summary>
/// Error al comunicarse con los servicios SOAP de la DGI.
/// </summary>
public class DgiCommunicationException : UruFacturaException
{
    public string? CodigoRespuesta { get; }

    public DgiCommunicationException(string message, string? codigoRespuesta = null)
        : base(message)
    {
        CodigoRespuesta = codigoRespuesta;
    }

    public DgiCommunicationException(string message, Exception innerException)
        : base(message, innerException) { }
}
