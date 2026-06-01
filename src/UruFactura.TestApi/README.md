# UruFactura.TestApi

Entorno de desarrollo y testing local que reutiliza **UruFactura.CloudflareApi** como base y agrega [Scalar](https://scalar.com) como explorador interactivo de la API.

## ¿Para qué sirve?

- Probar endpoints localmente con UI interactiva (Scalar en `/scalar/v1`).
- Validar flujos CFE sin necesidad de desplegar en Cloudflare.
- Simular escenarios multi-tenant localmente.

## Ejecución

```bash
cd src/UruFactura.TestApi
dotnet run
# Abrir http://localhost:5100 → redirige a Scalar UI
```

## Configuración

Usa la misma configuración que CloudflareApi (ver `appsettings.json`). Soporta:

- **Single-tenant**: sección `UruFactura:*`
- **Multi-tenant**: secciones `Tenants:{tenantId}:*` + header `X-Tenant-Id`

## Relación con CloudflareApi

```
UruFactura.TestApi
  └── referencia → UruFactura.CloudflareApi
                      └── referencia → UruFactura
```

TestApi no duplica lógica: importa endpoints, servicios y modelos de CloudflareApi directamente.
