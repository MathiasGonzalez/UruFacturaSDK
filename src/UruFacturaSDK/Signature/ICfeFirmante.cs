namespace UruFacturaSDK.Signature;

/// <summary>
/// Contrato para la firma digital XAdES-BES de documentos CFE.
/// Implementar esta interfaz permite reemplazar el motor de firma predeterminado.
/// </summary>
public interface ICfeFirmante : IDisposable
{
    /// <summary>
    /// Firma digitalmente el XML del CFE con esquema XAdES-BES.
    /// </summary>
    /// <param name="xmlSinFirmar">XML sin firmar del CFE (UTF-8).</param>
    /// <returns>XML firmado.</returns>
    string Firmar(string xmlSinFirmar);
}
