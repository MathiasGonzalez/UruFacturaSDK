# GitHub Actions – Secrets & Variables Setup

Esta guía cubre **todos** los secrets y variables que deben configurarse en GitHub Actions para que los workflows de UruErp funcionen. Se indica a qué workflow pertenece cada uno y cómo obtener el valor desde la web de Railway o Cloudflare.

---

## Índice

1. [Cómo agregar un Secret o Variable en GitHub](#1-cómo-agregar-un-secret-o-variable-en-github)
2. [Railway – Cómo obtener credenciales](#2-railway--cómo-obtener-credenciales)
3. [Cloudflare – Cómo obtener credenciales](#3-cloudflare--cómo-obtener-credenciales)
4. [Docker Hub – Cómo obtener credenciales](#4-docker-hub--cómo-obtener-credenciales)
5. [Secrets requeridos por workflow](#5-secrets-requeridos-por-workflow)
6. [Tabla resumen completa](#6-tabla-resumen-completa)

---

## 1. Cómo agregar un Secret o Variable en GitHub

### Secret (valor oculto, ej. tokens y contraseñas)

1. Ir al repositorio en GitHub.
2. Clic en **Settings** → **Secrets and variables** → **Actions**.
3. En la pestaña **Secrets**, clic en **New repository secret**.
4. Completar **Name** (exactamente como aparece en esta guía) y **Secret**.
5. Clic en **Add secret**.

### Variable (valor visible, ej. nombres de proyecto)

1. Mismo camino: **Settings** → **Secrets and variables** → **Actions**.
2. En la pestaña **Variables**, clic en **New repository variable**.
3. Completar **Name** y **Value**.
4. Clic en **Add variable**.

> **Diferencia clave:** los *secrets* están encriptados y nunca se muestran en logs; las *variables* son texto plano visible en los logs del workflow.

---

## 2. Railway – Cómo obtener credenciales

### `RAILWAY_TOKEN`

Es el token personal de la CLI de Railway. Sirve para autenticar todos los comandos `railway` en los workflows.

1. Iniciar sesión en [railway.app](https://railway.app).
2. Clic en tu avatar (esquina superior derecha) → **Account Settings**.
3. Ir a la sección **Tokens**.
4. Clic en **New Token**, darle un nombre (ej. `github-actions`) y copiar el valor generado.
5. Guardar en GitHub como secret `RAILWAY_TOKEN`.

---

### `RAILWAY_PROJECT_ID`

Identifica el proyecto Railway donde viven los servicios.

1. En Railway, abrir el proyecto correspondiente.
2. Clic en **Settings** (ícono de engranaje en la barra lateral).
3. En la sección **General**, copiar el valor de **Project ID**.
4. Guardar en GitHub como secret `RAILWAY_PROJECT_ID`.

---

### `RAILWAY_SERVICE_ID`

Identifica el servicio Railway de la API (necesario para el redeploy).

1. En Railway, abrir el proyecto.
2. Clic sobre el servicio **UruErp API**.
3. Ir a **Settings** → **Service** → copiar el valor de **Service ID**.
4. Guardar en GitHub como secret `RAILWAY_SERVICE_ID`.

---

### `POSTGRES_PASSWORD`

Contraseña para la base de datos PostgreSQL que se crea con el workflow `provision-railway-db.yml`.

1. Generar una contraseña segura (ej. con `openssl rand -hex 32` en tu terminal).
2. Guardar en GitHub como secret `POSTGRES_PASSWORD`.
3. Este valor se inyectará como variable `POSTGRES_PASSWORD` en el servicio de PostgreSQL al momento del provisionamiento.

---

## 3. Cloudflare – Cómo obtener credenciales

### `CF_ACCOUNT_ID`

Identificador único de tu cuenta Cloudflare. Se usa en todos los workflows que interactúan con Cloudflare.

1. Iniciar sesión en [dash.cloudflare.com](https://dash.cloudflare.com).
2. En el panel lateral derecho (o en la URL al seleccionar tu cuenta), verás el **Account ID**.
3. Alternativamente: ir a cualquier dominio → el **Account ID** aparece en la barra lateral derecha bajo "API".
4. Guardar en GitHub como secret `CF_ACCOUNT_ID`.

---

### `CF_API_TOKEN`

Token de API de Cloudflare con permisos para Pages y Workers. **Un solo token alcanza para ambos** si se configuran los permisos correctamente.

1. En el dashboard de Cloudflare, clic en el avatar (esquina superior derecha) → **My Profile**.
2. Ir a **API Tokens** → clic en **Create Token**.
3. Elegir la plantilla **Edit Cloudflare Workers** (cubre Workers y Pages).
   - Si preferís granularidad: seleccioná permisos manuales:
     - `Account` → `Cloudflare Pages` → **Edit**
     - `Account` → `Workers Scripts` → **Edit**
     - `Account` → `Workers Tail` → **Read** (opcional, para `wrangler tail`)
4. En **Account Resources**, seleccioná tu cuenta.
5. Clic en **Continue to summary** → **Create Token** → copiar el token.
6. Guardar en GitHub como secret `CF_API_TOKEN`.

> ⚠️ El token solo se muestra **una vez**. Si lo perdés, debés crear uno nuevo.

---

### `CF_PAGES_PROJECT` (Variable, no secret)

Nombre del proyecto en Cloudflare Pages donde se desplegará el frontend.

1. En el dashboard de Cloudflare, ir a **Workers & Pages** → **Pages**.
2. Si el proyecto no existe aún: clic en **Create a project** → elegir **Direct Upload** → asignar un nombre (ej. `uruerp-web`).
3. Copiar el nombre exacto del proyecto.
4. Guardar en GitHub como **variable** (no secret) `CF_PAGES_PROJECT`.

---

### `VITE_API_URL`

URL del Worker `api-proxy` ya desplegado. El frontend usa esta URL para todas las llamadas `/api/*`.

1. Primero deployar el Worker `api-proxy` (ejecutar el workflow `deploy-workers.yml` manualmente una vez).
2. En el dashboard de Cloudflare → **Workers & Pages** → seleccionar `api-proxy`.
3. Copiar la URL que aparece en la sección **Routes** (ej. `https://api-proxy.tu-cuenta.workers.dev`).
4. Guardar en GitHub como secret `VITE_API_URL`.

---

### `UPSTREAM_API_URL`

URL pública del servicio API en Railway. El Worker `api-proxy` reenvía todas las solicitudes a esta URL.

1. En Railway, abrir el proyecto → abrir el servicio API.
2. En **Settings** → **Networking** → copiar la **Public URL** (ej. `https://uruerp-api.up.railway.app`).
3. Guardar en GitHub como secret `UPSTREAM_API_URL`.

---

### `CORS_ORIGINS` (Variable, no secret)

Orígenes permitidos por el Worker `api-proxy` en el header CORS. Normalmente es la URL de Cloudflare Pages.

1. Definir la URL del frontend en Pages (ej. `https://uruerp-web.pages.dev`).
2. Guardar en GitHub como **variable** (no secret) `CORS_ORIGINS`.
3. Para múltiples orígenes, separar con comas: `https://uruerp-web.pages.dev,https://mi-dominio.com`.

---

## 4. Docker Hub – Cómo obtener credenciales

Los secrets de Docker Hub son necesarios para el workflow `deploy-api-railway.yml`, que construye y publica la imagen Docker de la API.

### `DOCKER_USERNAME`

1. Iniciar sesión en [hub.docker.com](https://hub.docker.com).
2. El **username** es el que aparece en la esquina superior derecha.
3. Guardar en GitHub como secret `DOCKER_USERNAME`.

### `DOCKER_PASSWORD`

Se recomienda usar un **Access Token** en lugar de la contraseña de la cuenta.

1. En Docker Hub → clic en tu avatar → **Account Settings**.
2. Ir a **Security** → **New Access Token**.
3. Asignar un nombre (ej. `github-actions`) y permisos **Read & Write**.
4. Copiar el token generado.
5. Guardar en GitHub como secret `DOCKER_PASSWORD`.

---

## 5. Secrets requeridos por workflow

### `uerp-ci.yml` – CI (build y tests)

Este workflow no requiere secrets. Solo construye y testea el código.

---

### `deploy-api-railway.yml` – Deploy API → Railway

| Nombre | Tipo | Descripción |
|--------|------|-------------|
| `RAILWAY_TOKEN` | Secret | Token de Railway CLI |
| `RAILWAY_SERVICE_ID` | Secret | ID del servicio API en Railway |
| `DOCKER_USERNAME` | Secret | Usuario de Docker Hub |
| `DOCKER_PASSWORD` | Secret | Access Token de Docker Hub |

**Variables de entorno que deben estar configuradas directamente en el servicio Railway** (no en GitHub Actions):

| Variable | Descripción |
|----------|-------------|
| `DATABASE_URL` | Provista automáticamente por Railway si linkeas el servicio de PostgreSQL |
| `Jwt__Secret` | String aleatorio de al menos 64 caracteres (generá con `openssl rand -hex 32`) |
| `AllowedOrigins` | URL de Cloudflare Pages, ej. `https://uruerp-web.pages.dev` |
| `UruFactura__RutEmisor` | RUT de la empresa emisora (DGI) |
| `UruFactura__RazonSocialEmisor` | Razón social del emisor |
| `UruFactura__DomicilioFiscal` | Domicilio fiscal del emisor |
| `UruFactura__Ambiente` | `Homologacion` o `Produccion` |
| `UruFactura__RutaCertificado` | Ruta del certificado `.pfx` dentro del contenedor, ej. `/app/certs/cert.pfx` |
| `UruFactura__PasswordCertificado` | Contraseña del certificado `.pfx` |
| `CloudflareR2__AccountId` | Account ID de Cloudflare (para R2) |
| `CloudflareR2__BucketName` | Nombre del bucket R2 (default: `uruerp-invoices`) |
| `CloudflareR2__AccessKeyId` | Access Key ID del token R2 |
| `CloudflareR2__SecretAccessKey` | Secret Access Key del token R2 |
| `CloudflareR2__PublicBaseUrl` | URL pública del bucket (opcional; vacío = presigned URLs) |
| `InvoiceMailer__WorkerUrl` | URL del Worker `invoice-mailer` |
| `InvoiceMailer__ApiSecret` | Secret compartido con el Worker `invoice-mailer` |

> **Cómo configurar variables en Railway:** Dashboard → Proyecto → Servicio API → **Variables** → agregar cada una.

> **Cómo crear un token R2:** Cloudflare Dashboard → **R2** → **Manage R2 API Tokens** → **Create API Token** → permisos **Object Read & Write** → copiar `Access Key ID` y `Secret Access Key`.

---

### `deploy-web-cloudflare.yml` – Deploy Web → Cloudflare Pages

| Nombre | Tipo | Descripción |
|--------|------|-------------|
| `CF_API_TOKEN` | Secret | Token de API de Cloudflare con permisos Pages Edit |
| `CF_ACCOUNT_ID` | Secret | Account ID de Cloudflare |
| `VITE_API_URL` | Secret | URL del Worker `api-proxy` |
| `CF_PAGES_PROJECT` | **Variable** | Nombre del proyecto en Cloudflare Pages |

---

### `deploy-workers.yml` – Deploy Cloudflare Workers (api-proxy + invoice-mailer)

| Nombre | Tipo | Descripción |
|--------|------|-------------|
| `CF_API_TOKEN` | Secret | Token de API de Cloudflare con permisos Workers Edit |
| `CF_ACCOUNT_ID` | Secret | Account ID de Cloudflare |
| `UPSTREAM_API_URL` | Secret | URL pública de la API en Railway |
| `CORS_ORIGINS` | **Variable** | Orígenes CORS permitidos (URL de Cloudflare Pages) |

**Secrets adicionales del Worker `invoice-mailer`** (configurar una vez con `wrangler secret put` o desde el dashboard de Cloudflare):

| Nombre | Descripción |
|--------|-------------|
| `SENDER_EMAIL` | Dirección de email verificada para enviar correos vía MailChannels |
| `SENDER_NAME` | Nombre del remitente, ej. `UruErp Facturación` |
| `ALLOWED_API_SECRET` | Secret compartido con la variable `InvoiceMailer__ApiSecret` del API |

> **Cómo configurar secrets de un Worker desde Cloudflare Dashboard:** Workers & Pages → seleccionar el Worker → **Settings** → **Variables and Secrets** → **Add** → marcar como **Secret**.

> **Cómo configurar con Wrangler CLI:**
> ```bash
> cd uerp/cf-workers/invoice-mailer
> npx wrangler secret put SENDER_EMAIL
> npx wrangler secret put SENDER_NAME
> npx wrangler secret put ALLOWED_API_SECRET
> ```

---

### `provision-railway-db.yml` – Provision PostgreSQL 18 (manual, one-shot)

| Nombre | Tipo | Descripción |
|--------|------|-------------|
| `RAILWAY_TOKEN` | Secret | Token de Railway CLI |
| `RAILWAY_PROJECT_ID` | Secret | ID del proyecto en Railway |
| `POSTGRES_PASSWORD` | Secret | Contraseña para el usuario de PostgreSQL |

---

## 6. Tabla resumen completa

| Secret / Variable | Tipo | Dónde se obtiene | Usado en workflow(s) |
|-------------------|------|-----------------|---------------------|
| `RAILWAY_TOKEN` | Secret | Railway → Account Settings → Tokens | `deploy-api-railway.yml`, `provision-railway-db.yml` |
| `RAILWAY_SERVICE_ID` | Secret | Railway → Proyecto → Servicio → Settings → Service ID | `deploy-api-railway.yml` |
| `RAILWAY_PROJECT_ID` | Secret | Railway → Proyecto → Settings → General → Project ID | `provision-railway-db.yml` |
| `POSTGRES_PASSWORD` | Secret | Generado localmente (ej. `openssl rand -hex 32`) | `provision-railway-db.yml` |
| `DOCKER_USERNAME` | Secret | Docker Hub → perfil → username | `deploy-api-railway.yml` |
| `DOCKER_PASSWORD` | Secret | Docker Hub → Account Settings → Security → Access Token | `deploy-api-railway.yml` |
| `CF_API_TOKEN` | Secret | Cloudflare → My Profile → API Tokens → Create Token | `deploy-web-cloudflare.yml`, `deploy-workers.yml` |
| `CF_ACCOUNT_ID` | Secret | Cloudflare Dashboard → barra lateral derecha → Account ID | `deploy-web-cloudflare.yml`, `deploy-workers.yml` |
| `VITE_API_URL` | Secret | URL del Worker `api-proxy` desplegado en Cloudflare | `deploy-web-cloudflare.yml` |
| `UPSTREAM_API_URL` | Secret | Railway → Proyecto → Servicio API → Settings → Public URL | `deploy-workers.yml` |
| `CF_PAGES_PROJECT` | **Variable** | Nombre del proyecto creado en Cloudflare Pages | `deploy-web-cloudflare.yml` |
| `CORS_ORIGINS` | **Variable** | URL(s) del frontend en Cloudflare Pages | `deploy-workers.yml` |

---

## Orden de configuración recomendado

1. **Railway:** crear proyecto, provisionar PostgreSQL 18 (`provision-railway-db.yml`), configurar el servicio API con todas sus variables de entorno.
2. **Docker Hub:** crear token de acceso.
3. **Cloudflare:** crear API token, obtener Account ID, crear proyecto en Pages.
4. **GitHub Actions:** agregar todos los secrets y variables de la tabla anterior.
5. **Deploy Workers** (`deploy-workers.yml`): ejecutar manualmente una vez para deployar `api-proxy` e `invoice-mailer`.
6. **Copiar la URL del Worker `api-proxy`** → agregarla como secret `VITE_API_URL`.
7. **Deploy Web** (`deploy-web-cloudflare.yml`): ejecutar manualmente o esperar el próximo push a `main` en `uerp/uerp-web/`.
8. **Deploy API** (`deploy-api-railway.yml`): ejecutar manualmente o esperar el próximo push a `main` en `uerp/UruErpApp.Api/`.
