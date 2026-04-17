# UruErp

Aplicación ERP multi-tenant construida sobre el **UruFactura SDK**.
Permite a múltiples empresas emitir, firmar y gestionar sus CFE (Comprobantes Fiscales Electrónicos) desde un único portal web.

## Stack

| Capa | Tecnología |
|------|-----------|
| Orquestación (dev) | .NET Aspire 9 |
| API | .NET 10 · Minimal API · EF Core 9 · JWT |
| Base de datos | PostgreSQL 17+ |
| Frontend | React 19 · Vite 6 |
| Deploy API | [Railway](https://railway.app) (Docker) |
| Deploy Web | [Cloudflare Pages](https://pages.cloudflare.com) (estático) |

## Estructura

```
uerp/
├── UruErpApp.AppHost/   ← orquestador Aspire (solo para desarrollo local)
├── UruErpApp.Api/       ← API REST multi-tenant con auth JWT
│   └── Dockerfile     ← imagen para Railway
├── uerp-web/          ← SPA React + Vite
└── README.md          ← este archivo
```

## Funcionalidades

- **Registro / Login** – JWT, 30 días de expiración.
- **Multi-tenant** – cada empresa tiene sus propios comprobantes aislados.
- **Dashboard** – KPIs: total de comprobantes, ingresos, últimos 7 días.
- **Crear CFE** – e-Ticket, e-Factura, Notas de Crédito/Débito, Exportación, Remito.
- **Modo Demo** – ciclo de vida completo (emisión + anulación con NC).
- **Historial** – tabla con descarga de PDF A4.
- **Config Status** – valida en tiempo real si el certificado y los datos del emisor están correctos.

---

## Desarrollo local con Aspire

### Pre-requisitos

| Herramienta | Versión mínima |
|-------------|----------------|
| .NET SDK    | 10.0 |
| Node.js     | 20 LTS |
| Docker      | 24+ |
| Aspire workload | `dotnet workload install aspire` |

### Certificado digital

```bash
# Generar certificado autofirmado (solo para pruebas locales)
openssl req -x509 -newkey rsa:2048 -keyout key.pem -out cert.pem -days 365 -nodes -subj "/CN=demo"
openssl pkcs12 -export -out uerp/UruErpApp.Api/certs/demo.pfx -inkey key.pem -in cert.pem -passout pass:demo123
```

Luego ajusta `UruFactura:PasswordCertificado` en `appsettings.json`.

### Iniciar

```bash
cd uerp
dotnet run --project UruErpApp.AppHost
```

Aspire levantará PostgreSQL (Docker), la API y el frontend.
Abre el Dashboard de Aspire para ver las URLs asignadas.

---

## Despliegue en producción

### API → Railway

1. Crea un nuevo proyecto en [railway.app](https://railway.app).
2. Agrega un servicio **PostgreSQL** (el plugin oficial de Railway).
3. Agrega un servicio **Docker** apuntando a este repositorio con:
   - **Root Directory**: `/` (raíz del repo)
   - **Dockerfile Path**: `uerp/UruErpApp.Api/Dockerfile`
4. Configura las variables de entorno del servicio (ver tabla abajo).
5. El workflow `.github/workflows/deploy-api-railway.yml` automatiza los despliegues
   con cada push a `main`.

#### Variables de entorno – Railway API

| Variable | Descripción |
|----------|-------------|
| `DATABASE_URL` | Proporcionada automáticamente por el plugin PostgreSQL de Railway |
| `Jwt__Secret` | Cadena aleatoria ≥ 32 caracteres (usa `openssl rand -hex 32`) |
| `AllowedOrigins` | URL de Cloudflare Pages, ej. `https://app.pages.dev` |
| `UruFactura__RutEmisor` | RUT de la empresa emisora |
| `UruFactura__RazonSocialEmisor` | Razón social |
| `UruFactura__DomicilioFiscal` | Domicilio fiscal |
| `UruFactura__Ciudad` | Ciudad |
| `UruFactura__Departamento` | Departamento |
| `UruFactura__Ambiente` | `Homologacion` o `Produccion` |
| `UruFactura__RutaCertificado` | Ruta al `.pfx` dentro del contenedor (ej. `/app/certs/cert.pfx`) |
| `UruFactura__PasswordCertificado` | Password del certificado |

> **Certificado en Railway:** montá el `.pfx` como un *volume* o incluyelo en la
> imagen (solo en ambientes privados). Para producción se recomienda Railway Volumes.

---

### Frontend → Cloudflare Pages

1. En el dashboard de Cloudflare, crea un proyecto **Pages** con **Direct Upload**
   (o conecta el repositorio de GitHub para deploys automáticos).
2. Configura la variable de entorno de build:
   - `VITE_API_URL` = URL pública de tu API en Railway (ej. `https://api.xxx.railway.app`)
3. El workflow `.github/workflows/deploy-web-cloudflare.yml` automatiza el build
   y deploy con cada push a `main`.

#### Build settings (Cloudflare Pages UI)

| Campo | Valor |
|-------|-------|
| Framework preset | None (Vite) |
| Build command | `cd uerp/uerp-web && npm ci && npm run build` |
| Build output directory | `uerp/uerp-web/dist` |
| Node.js version | `20` |

---

## Workflows de GitHub Actions

| Workflow | Archivo | Trigger | Descripción |
|----------|---------|---------|-------------|
| UruErp CI | `uerp-ci.yml` | push/PR a `main` en `uerp/**` o `src/**` | Build del API .NET y del frontend Vite. No despliega. |
| Deploy API → Railway | `deploy-api-railway.yml` | push a `main` en `uerp/UruErpApp.Api/**` | Construye imagen Docker, la sube a Docker Hub y hace `railway redeploy`. |
| Deploy Web → Cloudflare | `deploy-web-cloudflare.yml` | push a `main` en `uerp/uerp-web/**` | `npm run build` + `cloudflare/pages-action` para subir `dist/`. |

### Secrets necesarios en GitHub

| Secret | Usado en |
|--------|---------|
| `RAILWAY_TOKEN` | deploy-api-railway.yml |
| `RAILWAY_SERVICE_ID` | deploy-api-railway.yml |
| `DOCKER_USERNAME` | deploy-api-railway.yml |
| `DOCKER_PASSWORD` | deploy-api-railway.yml |
| `CF_API_TOKEN` | deploy-web-cloudflare.yml |
| `CF_ACCOUNT_ID` | deploy-web-cloudflare.yml |
| `VITE_API_URL` | deploy-web-cloudflare.yml |

**Variable** (no secret): `CF_PAGES_PROJECT` – nombre del proyecto en Cloudflare Pages.

---

## API Endpoints

| Método | Ruta | Auth | Descripción |
|--------|------|------|-------------|
| POST | `/api/auth/register` | ✗ | Crea tenant + usuario admin, retorna JWT |
| POST | `/api/auth/login` | ✗ | Login, retorna JWT |
| GET | `/api/dashboard` | ✓ | KPIs del tenant |
| GET | `/api/cfe-types` | ✗ | Lista tipos de CFE |
| GET | `/api/config/status` | ✓ | Estado de la config del emisor |
| GET | `/api/invoices` | ✓ | Lista comprobantes del tenant |
| POST | `/api/invoices` | ✓ | Crea y firma un CFE |
| GET | `/api/invoices/{id}/pdf` | ✓ | Descarga PDF A4 |

Todos los endpoints protegidos requieren el header `Authorization: Bearer <token>`.

---

## Notas

- El esquema se crea con `EnsureCreated()` en la primera ejecución. Para producción usá migraciones EF Core.
- El envío a DGI (`EnviarCfeAsync`) no está incluido; ver `UruFacturaClient` en el SDK.
- La DGI **no acepta** certificados autofirmados; para homologación obtené el certificado oficial en [DGI](https://www.dgi.gub.uy).
