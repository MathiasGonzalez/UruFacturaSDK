using UruFacturaSDK.Enums;
using UruFacturaSDK.Exceptions;
using UruFacturaSDK.Models;

namespace UruFacturaSDK.Cae;

/// <summary>
/// Gestiona los CAE (Constancias de Autorización de Emisión) de la DGI.
/// Controla vencimientos, rangos disponibles y alertas.
/// Esta clase es thread-safe.
/// </summary>
public class CaeManager : ICaeManager
{
    private readonly Dictionary<TipoCfe, List<Models.Cae>> _caesPorTipo = new();
    private readonly object _lock = new();

    /// <summary>
    /// Registra un CAE en el gestor.
    /// </summary>
    /// <param name="cae">CAE a registrar.</param>
    public void RegistrarCae(Models.Cae cae)
    {
        ArgumentNullException.ThrowIfNull(cae);

        lock (_lock)
        {
            if (!_caesPorTipo.TryGetValue(cae.TipoCfe, out var lista))
                _caesPorTipo[cae.TipoCfe] = lista = [];

            lista.Add(cae);
        }
    }

    /// <summary>
    /// Registra múltiples CAEs a la vez.
    /// </summary>
    public void RegistrarCaes(IEnumerable<Models.Cae> caes)
    {
        foreach (var cae in caes)
            RegistrarCae(cae);
    }

    /// <summary>
    /// Obtiene el próximo número de comprobante para el tipo de CFE indicado
    /// y el CAE activo correspondiente.
    /// </summary>
    /// <param name="tipoCfe">Tipo de CFE para el que se necesita un número.</param>
    /// <returns>Tupla (cae, número).</returns>
    /// <exception cref="CaeException">Si no hay CAE vigente o disponible.</exception>
    public (Models.Cae Cae, long Numero) ObtenerProximoNumero(TipoCfe tipoCfe)
    {
        Models.Cae? caeActivo;
        lock (_lock)
        {
            caeActivo = ObtenerCaeActivoInterno(tipoCfe);
        }

        if (caeActivo is null)
            throw new CaeException(
                $"No hay CAE vigente y con números disponibles para el tipo {tipoCfe}.");

        var numero = caeActivo.ObtenerProximoNumero();
        return (caeActivo, numero);
    }

    /// <summary>
    /// Obtiene el CAE activo para el tipo de CFE indicado.
    /// Prioriza el CAE con más números disponibles y que no esté por vencer.
    /// </summary>
    /// <param name="tipoCfe">Tipo de CFE.</param>
    /// <returns>CAE activo, o null si no existe.</returns>
    public Models.Cae? ObtenerCaeActivo(TipoCfe tipoCfe)
    {
        lock (_lock)
        {
            return ObtenerCaeActivoInterno(tipoCfe);
        }
    }

    /// <summary>
    /// Retorna todas las advertencias activas de los CAEs registrados.
    /// </summary>
    /// <param name="diasAlertaVencimiento">Días de anticipación para alertar vencimiento.</param>
    /// <param name="porcentajeAlertaUso">Porcentaje de uso a partir del cual alertar.</param>
    public IReadOnlyList<string> ObtenerAdvertencias(
        int diasAlertaVencimiento = 7,
        decimal porcentajeAlertaUso = 80m)
    {
        List<Models.Cae> snapshot;
        lock (_lock)
        {
            snapshot = _caesPorTipo.Values.SelectMany(l => l).ToList();
        }

        var advertencias = new List<string>();
        foreach (var cae in snapshot)
        {
            var advertencia = cae.ObtenerAdvertencia(diasAlertaVencimiento, porcentajeAlertaUso);
            if (advertencia != null)
                advertencias.Add(advertencia);
        }

        return advertencias;
    }

    /// <summary>
    /// Retorna todos los CAEs registrados.
    /// </summary>
    public IReadOnlyList<Models.Cae> ObtenerTodosLosCaes()
    {
        lock (_lock)
        {
            return _caesPorTipo.Values.SelectMany(l => l).ToList();
        }
    }

    /// <summary>
    /// Retorna todos los CAEs vigentes con números disponibles.
    /// </summary>
    public IReadOnlyList<Models.Cae> ObtenerCaesActivos()
    {
        lock (_lock)
        {
            return _caesPorTipo.Values
                .SelectMany(l => l)
                .Where(c => c.EsVigente && c.TieneNumerosDisponibles)
                .ToList();
        }
    }

    /// <summary>
    /// Retorna un resumen de estado de los CAEs.
    /// </summary>
    public string ResumenEstado()
    {
        Dictionary<TipoCfe, List<Models.Cae>> snapshot;
        lock (_lock)
        {
            snapshot = _caesPorTipo.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToList());
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== Estado de CAEs ===");

        foreach (var (tipo, lista) in snapshot)
        {
            sb.AppendLine($"\nTipo CFE: {tipo} ({(int)tipo})");
            foreach (var cae in lista)
            {
                var estado = cae.EsVigente
                    ? (cae.TieneNumerosDisponibles ? "✅ Activo" : "⚠️ Sin números")
                    : "❌ Vencido";

                sb.AppendLine($"  Serie: {cae.NroSerie} | {estado}");
                sb.AppendLine($"  Rango: {cae.RangoDesde}-{cae.RangoHasta} | Usado hasta: {cae.UltimoNroUsado}");
                sb.AppendLine($"  Vence: {cae.FechaVencimiento:dd/MM/yyyy} | Uso: {cae.PorcentajeUso}%");
            }
        }

        return sb.ToString();
    }

    // Thread-safety: This method must be called while holding _lock.
    // Do not call from public methods without first acquiring the lock.
    private Models.Cae? ObtenerCaeActivoInterno(TipoCfe tipoCfe)
    {
        if (!_caesPorTipo.TryGetValue(tipoCfe, out var lista))
            return null;

        return lista
            .Where(c => c.EsVigente && c.TieneNumerosDisponibles)
            .OrderByDescending(c => c.FechaVencimiento)
            .ThenByDescending(c => c.RangoHasta - c.UltimoNroUsado)
            .FirstOrDefault();
    }
}
