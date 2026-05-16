using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using UruFacturaSDK;
using UruFacturaSDK.Configuration;
using UruFacturaSDK.Enums;
using UruFactura.CloudflareApi.Models;

namespace UruFactura.CloudflareApi.Services;

/// <summary>
/// Fábrica de clientes UruFactura con soporte multi-tenant.
/// <para>
/// <b>Single-tenant</b>: configure las variables de entorno bajo la sección
/// <c>UruFactura:*</c> y omita el header <c>X-Tenant-Id</c>.
/// </para>
/// <para>
/// <b>Multi-tenant</b>: configure cada tenant bajo
/// <c>Tenants:{tenantId}:*</c> y envíe el header <c>X-Tenant-Id</c> en
/// cada solicitud.
/// </para>
/// Los clientes se crean de forma diferida (lazy) y se almacenan en caché
/// durante toda la vida útil del contenedor.
/// </summary>
public sealed class TenantClientFactory : IUruFacturaClientFactory, IDisposable
{
    private const string DefaultKey = "__default__";

    private readonly IConfiguration _configuration;
    private readonly ILogger<TenantClientFactory> _logger;
    private readonly ConcurrentDictionary<string, IUruFacturaClient> _clients = new();
    private readonly ConcurrentBag<string> _tempCertPaths = new();

    public TenantClientFactory(IConfiguration configuration, ILogger<TenantClientFactory> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public IUruFacturaClient GetClient(string? tenantId = null)
    {
        var key = string.IsNullOrWhiteSpace(tenantId) ? DefaultKey : tenantId;
        return _clients.GetOrAdd(key, _ => CreateClient(tenantId));
    }

    // -------------------------------------------------------------------------

    private IUruFacturaClient CreateClient(string? tenantId)
    {
        var section = string.IsNullOrWhiteSpace(tenantId)
            ? _configuration.GetSection("UruFactura")
            : _configuration.GetSection($"Tenants:{tenantId}");

        var certPath = ResolveCertificate(section, tenantId);

        var config = new UruFacturaConfig
        {
            RutEmisor             = Require(section, "RutEmisor",            tenantId),
            RazonSocialEmisor     = Require(section, "RazonSocialEmisor",    tenantId),
            NombreComercialEmisor = section["NombreComercialEmisor"],
            Giro                  = section["Giro"],
            DomicilioFiscal       = Require(section, "DomicilioFiscal",      tenantId),
            Ciudad                = section["Ciudad"]       ?? "MONTEVIDEO",
            Departamento          = section["Departamento"] ?? "MONTEVIDEO",
            RutaCertificado       = certPath,
            PasswordCertificado   = section["PasswordCertificado"] ?? "",
            Ambiente              = Enum.Parse<Ambiente>(section["Ambiente"] ?? "Homologacion"),
            OmitirValidacionSsl   = bool.Parse(section["OmitirValidacionSsl"] ?? "false"),
        };

        var client = UruFacturaClientBuilder.WithDefaults(config).Build();

        SeedCaes(client, section, tenantId);

        return client;
    }

    /// <summary>
    /// Resuelve la ruta del certificado para el tenant.
    /// Si <c>CertificadoBase64</c> está presente, escribe un archivo temporal
    /// y devuelve su ruta. De lo contrario usa <c>RutaCertificado</c>.
    /// </summary>
    private string ResolveCertificate(IConfigurationSection section, string? tenantId)
    {
        var base64 = section["CertificadoBase64"];
        if (!string.IsNullOrEmpty(base64))
        {
            var suffix = string.IsNullOrWhiteSpace(tenantId) ? "" : $"_{SafePath(tenantId)}";
            var path = Path.Combine(Path.GetTempPath(), $"urufactura_cert{suffix}.p12");
            File.WriteAllBytes(path, Convert.FromBase64String(base64));
            _tempCertPaths.Add(path);
            _logger.LogDebug("Certificado temporal escrito en {Path} para tenant '{Tenant}'.",
                path, SafeLog(tenantId));
            return path;
        }

        return section["RutaCertificado"]
            ?? throw new InvalidOperationException(
                $"Se requiere 'RutaCertificado' o 'CertificadoBase64' " +
                $"para el tenant '{SafeLog(tenantId)}'.");
    }

    /// <summary>
    /// Carga CAEs desde la configuración <c>Caes</c> (JSON array) del tenant.
    /// </summary>
    private void SeedCaes(IUruFacturaClient client, IConfigurationSection section, string? tenantId)
    {
        var caesJson = section["Caes"];
        if (string.IsNullOrWhiteSpace(caesJson))
            return;

        try
        {
            var caeDtos = JsonSerializer.Deserialize<List<CaeConfigRequest>>(
                caesJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (caeDtos?.Count > 0)
            {
                client.Cae.RegistrarCaes(caeDtos.Select(d => d.ToModel()));
                _logger.LogInformation(
                    "Se registraron {Count} CAE(s) para tenant '{Tenant}'.",
                    caeDtos.Count, SafeLog(tenantId));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "No se pudieron cargar los CAEs de configuración para tenant '{Tenant}'.",
                SafeLog(tenantId));
        }
    }

    // -------------------------------------------------------------------------

    private static string Require(IConfigurationSection section, string key, string? tenantId)
        => section[key]
           ?? throw new InvalidOperationException(
               $"Falta la configuración requerida '{section.Path}:{key}' " +
               $"para el tenant '{SafeLog(tenantId)}'.");

    /// <summary>
    /// Devuelve una representación segura del tenant ID para incluir en logs y mensajes:
    /// elimina control characters para prevenir log forging.
    /// </summary>
    private static string SafeLog(string? tenantId)
        => tenantId is null ? "(default)"
            : Regex.Replace(tenantId, @"[\x00-\x1F\x7F]", "_");

    /// <summary>
    /// Devuelve un sufijo seguro para usar en nombres de archivo:
    /// permite sólo caracteres alfanuméricos, guión y guión bajo, y trunca a 64 caracteres.
    /// Previene path traversal al eliminar separadores de directorio y otros caracteres especiales.
    /// </summary>
    private static string SafePath(string tenantId)
    {
        var sanitized = Regex.Replace(tenantId, @"[^a-zA-Z0-9\-_]", "_");
        return sanitized[..Math.Min(sanitized.Length, 64)];
    }

    public void Dispose()
    {
        foreach (var (_, client) in _clients)
            client.Dispose();

        _clients.Clear();

        foreach (var path in _tempCertPaths)
        {
            try { File.Delete(path); }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "No se pudo eliminar el certificado temporal {Path}.", path);
            }
        }
    }
}
