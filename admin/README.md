# UruFactura Admin

Web de administración de tenants para UruFactura. Desplegable en **Cloudflare Pages** con **Pages Functions** como backend de autenticación.

## Funcionalidades

- **Autenticación por email**: token de 6 dígitos enviado por email (MailChannels / API configurable)
- **Registro de tenants**: crear un nuevo tenant con validación completa (email verificado requerido)
- **Login**: verificar email → recibir código → acceder al panel
- **Selección de tenant**: un usuario puede administrar múltiples tenants
- **Dashboard**: estado de la API, CAEs activos, advertencias
- **Emitir CFE**: formulario para emitir cualquiera de los 13 tipos de CFE o generar XML
- **CAEs**: listar, registrar nuevos CAEs, ver advertencias
- **Configuración**: ver datos del tenant y variables de entorno requeridas para producción

## Stack

- React 19 + React Router 7
- Vite 6 (build tool)
- CSS vanilla (sin frameworks)
- Cloudflare Pages (hosting SPA)
- Cloudflare Pages Functions (backend auth)
- Cloudflare KV (sesiones, códigos, tenants)
- MailChannels / API email configurable (envío de códigos)

## Arquitectura

```
┌─────────────────────────────────────────────────────────────────┐
│                    Cloudflare Pages                               │
│                                                                   │
│  admin/dist/          → SPA (React)                              │
│  admin/functions/     → Pages Functions (auth backend)           │
│                                                                   │
│  KV Namespaces:                                                  │
│    AUTH_CODES     → códigos temporales (TTL 10 min)              │
│    AUTH_SESSIONS  → tokens de sesión (TTL 24h)                   │
│    TENANTS        → registro de tenants                          │
└───────────────────────────────┬──────────────────────────────────┘
                                │ HTTP (X-Tenant-Id + ******
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│               Cloudflare Worker (urufactura-api)                  │
│                                                                   │
│  cloudflare/worker.js → router multi-tenant                      │
│  Durable Objects      → contenedores por tenant                  │
└───────────────────────────────┬──────────────────────────────────┘
                                │ Container proxy (port 8080)
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│           .NET Container (UruFactura.CloudflareApi)               │
│                                                                   │
│  /health, /cfe/xml, /cfe/enviar, /cfe/pdf/*, /cae/*             │
└───────────────────────────────┬──────────────────────────────────┘
                                │ SOAP
                                ▼
                         DGI (Servicios SOAP)
```

## Flujo de autenticación

```
1. Usuario ingresa email
2. POST /auth/send-code → genera código 6 dígitos, guarda en KV, envía email
3. Usuario ingresa código recibido
4. POST /auth/verify-code → valida código, genera token JWT (HMAC-SHA256)
5. Token se almacena en localStorage (24h de vida)
6. Todas las requests posteriores incluyen Authorization: ******
```

## Desarrollo local

### Prerequisitos

- Node.js 18+
- .NET 9 SDK
- Wrangler CLI: `npm install -g wrangler`

### Opción 1: Solo frontend (mock auth)

```bash
cd admin
npm install
npm run dev
# → http://localhost:5173
# Auth requests van a localhost:8788 (ver Opción 2)
# API requests van a localhost:5100 via proxy
```

### Opción 2: Frontend + Pages Functions (auth completa)

Terminal 1 — API .NET:
```bash
cd src/UruFactura.TestApi
dotnet run
# → http://localhost:5100
```

Terminal 2 — Pages Functions (auth backend con KV local):
```bash
cd admin
npm install
npx wrangler pages dev --port 8788 --kv AUTH_CODES --kv AUTH_SESSIONS --kv TENANTS -- npm run dev
# → http://localhost:8788 (proxy a Vite + functions)
```

> **Nota**: En dev local, `wrangler pages dev` crea namespaces KV in-memory.
> Los emails se loguean en consola en vez de enviarse (MailChannels requiere dominio verificado).
> Para probar sin envío real, el código aparece en los logs del worker.

### Opción 3: Todo en Cloudflare (staging)

```bash
# Build
cd admin
npm run build

# Deploy a preview
npx wrangler pages deploy dist --project-name urufactura-admin --branch staging
```

## Deploy en producción (Cloudflare Pages)

### 1. Crear KV Namespaces

```bash
wrangler kv namespace create AUTH_CODES
wrangler kv namespace create AUTH_SESSIONS
wrangler kv namespace create TENANTS
```

Copiar los IDs generados al `wrangler.toml`.

### 2. Configurar Secrets

```bash
wrangler pages secret put JWT_SECRET --project-name urufactura-admin
# → ingresar un string aleatorio largo (min 32 chars)

# Si se usa una API de email externa (Resend, SendGrid):
wrangler pages secret put EMAIL_API_KEY --project-name urufactura-admin
```

### 3. Variables de entorno (Pages Dashboard o wrangler.toml)

| Variable | Descripción | Ejemplo |
|----------|-------------|---------|
| `VITE_API_URL` | URL del Worker API | `https://urufactura-api.account.workers.dev` |
| `VITE_AUTH_URL` | URL auth (vacío si same-origin) | _(vacío)_ |
| `MAIL_FROM` | Email remitente | `noreply@urufactura.dev` |
| `APP_NAME` | Nombre en emails | `UruFactura Admin` |
| `ALLOWED_ORIGIN` | CORS origin | `https://admin.urufactura.dev` |
| `JWT_SECRET` | **Secret** - HMAC key | _(secret)_ |
| `EMAIL_API_URL` | (Opcional) URL API email | `https://api.resend.com/emails` |
| `EMAIL_API_KEY` | **Secret** - API key email | _(secret)_ |

### 4. Deploy

**Via Git (recomendado)**:
1. Conectar repositorio en Cloudflare Pages Dashboard
2. Configurar:
   - **Build command**: `npm run build`
   - **Build output directory**: `dist`
   - **Root directory**: `admin`
3. Cada push a `main` deploya automáticamente

**Via CLI**:
```bash
cd admin
npm run build
npx wrangler pages deploy dist --project-name urufactura-admin
```

### 5. DNS (opcional)

Para usar dominio custom (`admin.urufactura.dev`):
1. En Cloudflare Dashboard → Pages → Custom Domains
2. Agregar `admin.urufactura.dev`
3. Se configura CNAME automáticamente

## Configuración de email

### MailChannels (default, gratis para Workers)

Requiere verificación SPF del dominio. Agregar registro DNS:
```
TXT  _mailchannels  v=mc1 cfid=<pages-project>.pages.dev
```

### API externa (Resend, SendGrid, etc.)

Configurar en secrets:
```bash
wrangler pages secret put EMAIL_API_URL --project-name urufactura-admin
# → https://api.resend.com/emails

wrangler pages secret put EMAIL_API_KEY --project-name urufactura-admin
# → re_xxxxx...
```

## Integración con UruFactura API

La admin web se comunica con dos backends:

1. **Pages Functions** (`/auth/*`): autenticación, registro de tenants
2. **UruFactura Worker** (`/api/*`): operaciones CFE, CAEs, reportes

### Headers requeridos por la API

```
X-Tenant-Id: <tenantId>     ← identifica el tenant en la API
Authorization: ****** ← token de sesión (auth)
Content-Type: application/json
```

### Endpoints API principales

| Endpoint | Método | Descripción |
|----------|--------|-------------|
| `/health` | GET | Estado de la API |
| `/cae` | GET | Listar CAEs del tenant |
| `/cae` | POST | Registrar nuevo CAE |
| `/cae/advertencias` | GET | Advertencias de CAEs |
| `/cfe/xml` | POST | Generar XML de CFE |
| `/cfe/enviar` | POST | Emitir CFE a DGI |
| `/cfe/pdf/a4` | POST | Generar PDF A4 |
| `/cfe/consultar` | POST | Consultar estado CFE en DGI |
| `/reporte-diario` | POST | Generar reporte diario |

### Endpoints Auth (Pages Functions)

| Endpoint | Método | Descripción |
|----------|--------|-------------|
| `/auth/send-code` | POST | Enviar código de verificación |
| `/auth/verify-code` | POST | Verificar código → obtener token |
| `/auth/register` | POST | Registrar tenant (requiere token) |
| `/auth/session` | GET | Info de sesión actual |
| `/auth/tenants` | GET | Listar tenants del usuario |

## Roadmap

- [x] Autenticación por email (token vía código)
- [x] Registro de tenants con email verificado
- [x] Pages Functions como backend auth
- [x] Despliegue Cloudflare Pages + KV
- [ ] Subida de certificado .p12 desde la web
- [ ] Historial de CFEs emitidos
- [ ] Webhooks para notificaciones DGI
- [ ] Panel de métricas y uso
- [ ] Multi-idioma (es/en)
- [ ] Cloudflare Access como opción de SSO empresarial
