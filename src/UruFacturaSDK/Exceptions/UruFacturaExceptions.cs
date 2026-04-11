namespace UruFacturaSDK.Exceptions;

/// <summary>
/// Excepción base del SDK de UruFactura.
/// </summary>
public class UruFacturaException : Exception
{
    public UruFacturaException(string message) : base(message) { }
    public UruFacturaException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Error de validación del CFE antes de enviarlo a la DGI.
/// </summary>
public class CfeValidationException : UruFacturaException
{
    public IReadOnlyList<string> Errors { get; }

    public CfeValidationException(IReadOnlyList<string> errors)
        : base($"El CFE contiene {errors.Count} error(es) de validación: {string.Join("; ", errors)}")
    {
        Errors = errors;
    }
}

/// <summary>
/// Error al firmar digitalmente el documento CFE.
/// </summary>
public class FirmaDigitalException : UruFacturaException
{
    public FirmaDigitalException(string message) : base(message) { }
    public FirmaDigitalException(string message, Exception innerException) : base(message, innerException) { }
}

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

/// <summary>
/// Error relacionado con el CAE (Constancia de Autorización de Emisión).
/// </summary>
public class CaeException : UruFacturaException
{
    public CaeException(string message) : base(message) { }
    public CaeException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Error al generar el PDF del comprobante.
/// </summary>
public class PdfGenerationException : UruFacturaException
{
    public PdfGenerationException(string message) : base(message) { }
    public PdfGenerationException(string message, Exception innerException) : base(message, innerException) { }
}
