# ERP Demo – UruFactura SDK

Ejemplo mínimo de una aplicación ERP con:

- **.NET 10** – Web API (minimal API)
- **PostgreSQL 18+** – base de datos (contenedor Docker vía Aspire)
- **React 19 + Vite** – frontend
- **Aspire 9** – orquestación de servicios

## Estructura

```
ErpApp.AppHost/   ← orquestador Aspire
ErpApp.Api/       ← API REST (.NET 10, EF Core + Npgsql)
erp-web/          ← frontend React + Vite
```

## Pre-requisitos

| Herramienta | Versión mínima |
|------------|---------------|
| .NET SDK   | 10.0          |
| Node.js    | 20 LTS        |
| Docker     | 24+           |
| Aspire workload | `dotnet workload install aspire` |

## Certificado digital

El SDK requiere un certificado `.pfx` válido para firmar los CFE.

1. Coloca tu certificado en `ErpApp.Api/certs/demo.pfx`.
2. Ajusta `RutaCertificado` y `PasswordCertificado` en `ErpApp.Api/appsettings.json`
   (o usa variables de entorno `UruFactura__RutaCertificado` / `UruFactura__PasswordCertificado`).

> Para pruebas rápidas puedes generar un certificado autofirmado con OpenSSL:
> ```bash
> openssl req -x509 -newkey rsa:2048 -keyout key.pem -out cert.pem -days 365 -nodes -subj "/CN=demo"
> openssl pkcs12 -export -out ErpApp.Api/certs/demo.pfx -inkey key.pem -in cert.pem -passout pass:demo123
> ```
> La DGI **no aceptará** un certificado autofirmado; sólo sirve para generar y visualizar XML/PDF localmente.

## Ejecutar

```bash
# Desde la carpeta example/erp
dotnet run --project ErpApp.AppHost
```

Aspire levantará:
- **PostgreSQL** (Docker) + pgAdmin
- **API** en http://localhost:5xxx (asignado por Aspire)
- **Frontend React** en http://localhost:5173

Abre el dashboard de Aspire para ver las URLs exactas.

## API Endpoints

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET    | `/api/invoices` | Lista todos los comprobantes |
| POST   | `/api/invoices` | Crea y firma un nuevo CFE |
| GET    | `/api/invoices/{id}/pdf` | Descarga el PDF A4 del comprobante |

### POST /api/invoices – body de ejemplo

```json
{
  "tipoCfe": 101,
  "numero": 1,
  "rutReceptor": null,
  "nombreReceptor": null,
  "detalle": [
    {
      "nombreItem": "Servicio de consultoría",
      "cantidad": 1,
      "precioUnitario": 5000,
      "indFactIva": 3
    }
  ]
}
```

`tipoCfe`: `101` = e-Ticket · `111` = e-Factura  
`indFactIva`: `1` = Exento · `2` = IVA Mínimo 10% · `3` = IVA Básico 22%

## Notas

- La persistencia usa `EnsureCreated()` por simplicidad. En producción usa migraciones EF Core.
- La configuración `UruFactura` en `appsettings.json` puede sobreescribirse con variables de entorno.
- El envío a la DGI (`EnviarCfeAsync`) no está incluido en este ejemplo; ver `UruFacturaClient` en el SDK.
