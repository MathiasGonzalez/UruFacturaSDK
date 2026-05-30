# UruFactura Admin

Web de administración de tenants para UruFactura. Desplegable en **Cloudflare Pages**.

## Funcionalidades (v0.1)

- **Login**: conectar a un tenant existente verificando health de la API.
- **Registro**: crear un nuevo tenant (datos básicos, genera guía de configuración).
- **Dashboard**: estado de la API, CAEs activos, advertencias.
- **Emitir CFE**: formulario para emitir cualquiera de los 13 tipos de CFE o generar XML.
- **CAEs**: listar, registrar nuevos CAEs, ver advertencias.
- **Configuración**: ver datos del tenant y variables de entorno requeridas para producción.

## Stack

- React 19 + React Router 7
- Vite 6 (build tool)
- CSS vanilla (sin frameworks)
- Cloudflare Pages (deploy target)

## Desarrollo local

```bash
cd admin
npm install
npm run dev
# → http://localhost:5173

# La API se accede via proxy (/api → localhost:5100)
# Levantar TestApi en otra terminal:
cd ../src/UruFactura.TestApi
dotnet run
```

## Build para producción

```bash
npm run build
# Output: admin/dist/
```

## Deploy en Cloudflare Pages

1. Conectar el repositorio en Cloudflare Pages Dashboard.
2. Configurar:
   - **Build command**: `npm run build`
   - **Build output directory**: `admin/dist`
   - **Root directory**: `admin`
3. Variable de entorno: `VITE_API_URL=https://urufactura-api.<account>.workers.dev`

## Arquitectura

```
admin/                    → Cloudflare Pages (SPA)
  ↕ HTTP (X-Tenant-Id)
cloudflare/worker.js      → Cloudflare Worker (router)
  ↕ Container proxy
src/UruFactura.CloudflareApi → .NET Container (API)
  ↕ SOAP
DGI                       → Servicios SOAP DGI
```

## Roadmap

- [ ] Autenticación real (Cloudflare Access / JWT)
- [ ] Subida de certificado .p12 desde la web
- [ ] Historial de CFEs emitidos
- [ ] Webhooks para notificaciones DGI
- [ ] Panel de métricas y uso
- [ ] Multi-idioma (es/en)
