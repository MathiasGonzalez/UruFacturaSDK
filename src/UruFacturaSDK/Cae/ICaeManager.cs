using UruFacturaSDK.Enums;
using UruFacturaSDK.Exceptions;
using UruFacturaSDK.Models;

namespace UruFacturaSDK.Cae;

/// <summary>
/// Contrato para la gestión de CAE (Constancias de Autorización de Emisión).
/// Permite mockear la gestión de CAEs en tests de aplicaciones consumidoras.
/// </summary>
public interface ICaeManager
{
    /// <summary>Registra un CAE en el gestor.</summary>
    void RegistrarCae(Models.Cae cae);

    /// <summary>Registra múltiples CAEs a la vez.</summary>
    void RegistrarCaes(IEnumerable<Models.Cae> caes);

    /// <summary>
    /// Obtiene el próximo número de comprobante para el tipo de CFE indicado.
    /// </summary>
    /// <exception cref="CaeException">Si no hay CAE vigente o disponible.</exception>
    (Models.Cae Cae, long Numero) ObtenerProximoNumero(TipoCfe tipoCfe);

    /// <summary>Obtiene el CAE activo para el tipo de CFE indicado, o null si no existe.</summary>
    Models.Cae? ObtenerCaeActivo(TipoCfe tipoCfe);

    /// <summary>Retorna todas las advertencias activas de los CAEs registrados.</summary>
    IReadOnlyList<string> ObtenerAdvertencias(
        int diasAlertaVencimiento = 7,
        decimal porcentajeAlertaUso = 80m);

    /// <summary>Retorna todos los CAEs registrados.</summary>
    IReadOnlyList<Models.Cae> ObtenerTodosLosCaes();

    /// <summary>Retorna todos los CAEs vigentes con números disponibles.</summary>
    IReadOnlyList<Models.Cae> ObtenerCaesActivos();

    /// <summary>Retorna un resumen de estado de los CAEs.</summary>
    string ResumenEstado();
}
