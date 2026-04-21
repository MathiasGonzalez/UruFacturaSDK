using UruFacturaSDK.Configuration;

namespace UruFacturaSDK;

/// <summary>
/// Punto de entrada principal del SDK de UruFactura (versión Lite, sin generación de PDF).
/// Extiende <see cref="UruFacturaClientBase"/> sin añadir dependencias de terceros para PDF.
/// </summary>
public class UruFacturaClient : UruFacturaClientBase, IUruFacturaClient
{
    /// <summary>
    /// Inicializa el cliente de UruFactura con la configuración provista.
    /// </summary>
    /// <param name="config">Configuración del SDK.</param>
    public UruFacturaClient(UruFacturaConfig config) : base(config)
    {
    }
}

