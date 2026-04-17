namespace UruFacturaSDK.Exceptions;

/// <summary>
/// Excepción base del SDK de UruFactura.
/// </summary>
public class UruFacturaException : Exception
{
    public UruFacturaException(string message) : base(message) { }
    public UruFacturaException(string message, Exception innerException) : base(message, innerException) { }
}
