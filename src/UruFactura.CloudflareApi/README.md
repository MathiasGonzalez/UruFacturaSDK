# UruFactura.CloudflareApi

API HTTP para facturación electrónica DGI Uruguay, diseñada para correr en [Cloudflare Containers](https://developers.cloudflare.com/containers/).  
Construida sobre **UruFacturaSDK** y ASP.NET Core 10 Minimal API.

---

## Características

- **Single-tenant** y **multi-tenant (SaaS)** desde la misma imagen.
- Certificado digital via archivo o variable de entorno Base64 (ideal para contenedores).
- CAEs pre-cargados al inicio desde configuración — sobrevive reinicios del contenedor.
- Sin dependencias de UI (sin Swagger UI, sin Scalar): ideal para producción.

---

## Requisitos

| Herramienta | Versión mínima |
|-------------|---------------|
| .NET SDK    | 10.0          |
| Docker      | 24+           |
| Wrangler CLI | 3+           |
| Cuenta Cloudflare | Workers Paid + Containers beta |

---

## Variables de entorno / Configuración

### Single-tenant (`UruFactura:*`)

| Variable | Obligatoria | Descripción |
|----------|:-----------:|-------------|
| `UruFactura__RutEmisor` | ✅ | RUT del emisor (12 dígitos, sin puntos ni guión) |
| `UruFactura__RazonSocialEmisor` | ✅ | Razón social del emisor |
| `UruFactura__DomicilioFiscal` | ✅ | Domicilio fiscal |
| `UruFactura__RutaCertificado` | ✅* | Ruta al `.p12` montado en el contenedor |
| `UruFactura__CertificadoBase64` | ✅* | Contenido del `.p12` en Base64 (alternativa a `RutaCertificado`) |
| `UruFactura__PasswordCertificado` | ✅ | Contraseña del certificado |
| `UruFactura__Ambiente` | – | `Homologacion` (defecto) o `Produccion` |
| `UruFactura__NombreComercialEmisor` | – | Nombre comercial (opcional) |
| `UruFactura__Giro` | – | Giro / actividad económica (opcional) |
| `UruFactura__Ciudad` | – | Ciudad del emisor (defecto: `MONTEVIDEO`) |
| `UruFactura__Departamento` | – | Departamento del emisor (defecto: `MONTEVIDEO`) |
| `UruFactura__OmitirValidacionSsl` | – | `false` (defecto). Solo `true` en homologación con CA no confiable |
| `UruFactura__Caes` | – | JSON array de CAEs a pre-cargar (ver [CAEs](#pre-carga-de-caes)) |

> \* Se requiere exactamente uno de los dos (`RutaCertificado` o `CertificadoBase64`).

### Multi-tenant (`Tenants:{tenantId}:*`)

Para multi-tenant, cada empresa tiene su propia sección de configuración con las mismas claves reemplazando el prefijo `UruFactura` por `Tenants:{tenantId}`:

```
Tenants__empresa-abc__RutEmisor=210000000001
Tenants__empresa-abc__RazonSocialEmisor=EMPRESA ABC SA
Tenants__empresa-abc__CertificadoBase64=<base64>
Tenants__empresa-abc__PasswordCertificado=...
Tenants__empresa-abc__Ambiente=Produccion
Tenants__empresa-abc__Caes=[...]

Tenants__empresa-xyz__RutEmisor=210000000002
...
```

Cada solicitud debe incluir el header **`X-Tenant-Id: {tenantId}`**. Sin ese header se usa la sección `UruFactura:*`.

---

## Pre-carga de CAEs

Los CAEs se guardan en memoria. Al reiniciar el contenedor se pierden si no están en config.  
Provea `UruFactura__Caes` (o `Tenants:{id}__Caes`) con un JSON array:

```json
[
  {
    "NroSerie": "CAE001",
    "Tipo": 101,
    "RangoDesde": 1,
    "RangoHasta": 1000,
    "FechaVencimiento": "2026-12-31"
  }
]
```

Valores de `Tipo`: `ETicket=101`, `EFactura=111`, `ERemito=124`, `EFacturaExportacion=121`.

---

## Endpoints

### CFE (`/cfe`)

| Método | Ruta | Descripción |
|--------|------|-------------|
| POST | `/cfe/eticket/xml` | Genera y firma XML de e-Ticket (sin enviar a DGI) |
| POST | `/cfe/efactura/xml` | Genera y firma XML de e-Factura (sin enviar a DGI) |
| POST | `/cfe/eticket/enviar` | Firma y envía e-Ticket a la DGI |
| POST | `/cfe/efactura/enviar` | Firma y envía e-Factura a la DGI |
| POST | `/cfe/consultar` | Consulta estado de un CFE en la DGI |
| POST | `/cfe/eticket/pdf/a4` | Genera PDF A4 de un e-Ticket |
| POST | `/cfe/eticket/pdf/termico` | Genera PDF térmico de un e-Ticket |

### CAE (`/cae`)

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/cae` | Lista los CAEs en memoria |
| POST | `/cae` | Registra un CAE en tiempo de ejecución |
| GET | `/cae/advertencias` | CAEs por vencer o con alto uso |

---

## Build y ejecución local

```bash
# Desde la raíz del repositorio

# Build de la imagen
docker build -f src/UruFactura.CloudflareApi/Dockerfile -t urufactura-api .

# Run single-tenant (cert como archivo)
docker run -p 8080:8080 \
  -e UruFactura__RutEmisor=210000000001 \
  -e UruFactura__RazonSocialEmisor="MI EMPRESA SA" \
  -e UruFactura__DomicilioFiscal="18 DE JULIO 1234" \
  -e UruFactura__RutaCertificado=/certs/cert.p12 \
  -e UruFactura__PasswordCertificado=miclave \
  -e UruFactura__Ambiente=Homologacion \
  -v /ruta/local/cert.p12:/certs/cert.p12:ro \
  urufactura-api

# Run single-tenant (cert como Base64)
CERT_B64=$(base64 -w0 /ruta/local/cert.p12)
docker run -p 8080:8080 \
  -e UruFactura__RutEmisor=210000000001 \
  -e UruFactura__RazonSocialEmisor="MI EMPRESA SA" \
  -e UruFactura__DomicilioFiscal="18 DE JULIO 1234" \
  -e UruFactura__CertificadoBase64="$CERT_B64" \
  -e UruFactura__PasswordCertificado=miclave \
  urufactura-api
```

---

## Despliegue en Cloudflare Containers

### 1. Publicar la imagen en GHCR

El workflow `.github/workflows/docker.yml` lo hace automáticamente al hacer push a `main`.  
También se puede hacer manual:

```bash
docker build -f src/UruFactura.CloudflareApi/Dockerfile -t ghcr.io/<owner>/urufacturasdk-api:latest .
docker push ghcr.io/<owner>/urufacturasdk-api:latest
```

### 2. Configurar `cloudflare/wrangler.toml`

```toml
name = "urufactura-api"
main = "worker.js"
compatibility_date = "2025-05-01"

[containers]
image = "ghcr.io/<owner>/urufacturasdk-api:latest"

[[containers.bindings]]
name = "CONTAINER"
```

### 3. Configurar secretos

```bash
cd cloudflare

# Certificado como Base64 (recomendado en Cloudflare)
wrangler secret put UruFactura__CertificadoBase64
wrangler secret put UruFactura__PasswordCertificado

# Config del emisor
wrangler secret put UruFactura__RutEmisor
wrangler secret put UruFactura__RazonSocialEmisor
wrangler secret put UruFactura__DomicilioFiscal

# CAEs iniciales (opcional, JSON array como string)
wrangler secret put UruFactura__Caes
```

### 4. Desplegar

```bash
cd cloudflare
wrangler deploy
```

---

## Multi-tenant en Cloudflare

En modo multi-tenant, configure los secretos por tenant:

```bash
wrangler secret put Tenants__empresa-abc__CertificadoBase64
wrangler secret put Tenants__empresa-abc__PasswordCertificado
wrangler secret put Tenants__empresa-abc__RutEmisor
# ... etc
```

Y en cada llamada a la API agregue el header:

```
X-Tenant-Id: empresa-abc
```

Los clientes SDK se crean de forma diferida (lazy) y se cachean en memoria durante la vida del contenedor.

---

## Estructura del proyecto

```
src/UruFactura.CloudflareApi/
├── Endpoints/
│   ├── CfeEndpoints.cs          # Endpoints /cfe/*
│   └── CaeEndpoints.cs          # Endpoints /cae/*
├── Models/
│   ├── CfeRequest.cs            # DTO de solicitud CFE
│   └── CaeConfigRequest.cs      # DTO de CAE
├── Services/
│   ├── IUruFacturaClientFactory.cs
│   └── TenantClientFactory.cs   # Fábrica multi-tenant con caché
├── HttpContextExtensions.cs     # Helper para leer X-Tenant-Id
├── Program.cs                   # Bootstrap de la aplicación
├── Dockerfile
└── README.md
```
