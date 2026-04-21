using UruFacturaSDK.Models;

namespace UruFacturaSDK.Xml;

/// <summary>
/// Contrato para la generación del XML de un CFE según el esquema de la DGI.
/// Implementar esta interfaz permite reemplazar el motor de serialización XML predeterminado.
/// </summary>
public interface ICfeXmlBuilder
{
    /// <summary>
    /// Genera el XML sin firmar del CFE.
    /// </summary>
    /// <param name="cfe">El CFE a serializar.</param>
    /// <returns>Cadena XML UTF-8 sin firmar.</returns>
    string Generar(Cfe cfe);
}
