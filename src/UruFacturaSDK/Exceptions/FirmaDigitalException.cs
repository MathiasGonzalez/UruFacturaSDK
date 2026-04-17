namespace UruFacturaSDK.Exceptions;

/// <summary>
/// Error al firmar digitalmente el documento CFE.
/// </summary>
public class FirmaDigitalException : UruFacturaException
{
    public FirmaDigitalException(string message) : base(message) { }
    public FirmaDigitalException(string message, Exception innerException) : base(message, innerException) { }
}
