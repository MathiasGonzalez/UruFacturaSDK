using UruFacturaSDK.Configuration;
using UruFacturaSDK.Pdf;

namespace UruFacturaSDK;

public partial class UruFacturaClient
{
    /// <summary>
    /// Inicializa el cliente de UruFactura (versión Lite, sin generador de PDF predeterminado).
    /// Para habilitar la generación de PDF, use el constructor
    /// <see cref="UruFacturaClient(UruFacturaConfig, ICfePdfGenerator?)"/> con su propia implementación.
    /// </summary>
    /// <param name="config">Configuración del SDK.</param>
    public UruFacturaClient(UruFacturaConfig config)
        : this(config, null)
    {
    }
}


