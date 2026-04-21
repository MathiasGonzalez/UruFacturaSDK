# UruFacturaSDK 🇺🇾

**La vía rápida hacia la Facturación Electrónica en Uruguay**

[![CI](https://github.com/MathiasGonzalez/UruFacturaSDK/actions/workflows/ci.yml/badge.svg)](https://github.com/MathiasGonzalez/UruFacturaSDK/actions/workflows/ci.yml)
[![Publish NuGet](https://github.com/MathiasGonzalez/UruFacturaSDK/actions/workflows/publish.yml/badge.svg)](https://github.com/MathiasGonzalez/UruFacturaSDK/actions/workflows/publish.yml)
[![Deploy Landing](https://github.com/MathiasGonzalez/UruFacturaSDK/actions/workflows/deploy-pages.yml/badge.svg)](https://github.com/MathiasGonzalez/UruFacturaSDK/actions/workflows/deploy-pages.yml)
[![NuGet](https://img.shields.io/nuget/v/UruFacturaSDK.svg)](https://www.nuget.org/packages/UruFacturaSDK/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/UruFacturaSDK.svg)](https://www.nuget.org/packages/UruFacturaSDK/)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

UruFactura SDK es una librería open-source **.NET 10 C#** de alto nivel diseñada para simplificar la integración de sistemas locales con el ecosistema de la DGI. Olvídate de lidiar con la complejidad manual de los sobres SOAP o la estructura rígida de los esquemas XML; esta herramienta actúa como un puente amigable entre tu lógica de negocio y los requisitos impositivos uruguayos.

---

## 🚀 Características Principales

| Característica | Detalle |
|---|---|
| **Generación de CFE** | Crea e-Tickets, e-Facturas, e-Remitos y sus notas de corrección con objetos simples, cumpliendo el formato XML vigente de la DGI. |
| **Firma Digital XAdES-BES** | Módulo integrado para la firma electrónica usando certificados estándar `.p12` / `.pfx`. |
| **Comunicación SOAP** | Cliente SOAP optimizado para envío de comprobantes, consulta de estados y envío del Reporte Diario. |
| **Gestión de CAE** | Control inteligente de Constancias de Autorización de Emisión con alertas de vencimiento y rango. |
| **Representación Impresa** | Generador de PDFs (A4 y térmico 80mm) con código QR y sellos de seguridad según normativa. |

---

## 📦 Estructura del Proyecto

```
UruFacturaSDK/
├── src/
│   └── UruFacturaSDK/
│       ├── Configuration/      # UruFacturaConfig
│       ├── Enums/              # TipoCfe, TipoIva, FormaPago, Moneda, Ambiente, TipoDocumentoReceptor
│       ├── Exceptions/         # Excepciones tipadas del SDK
│       ├── Models/             # Cfe, Cae, Receptor, LineaDetalle, RespuestaDgi
│       ├── Xml/                # CfeXmlBuilder (generación XML DGI)
│       ├── Signature/          # CfeFirmante (XAdES-BES)
│       ├── Soap/               # DgiSoapClient (comunicación con DGI)
│       ├── Cae/                # CaeManager / ICaeManager (gestión de CAE)
│       ├── Pdf/                # CfePdfGenerator (PDF A4 y térmico)
│       └── UruFacturaClient.cs # Fachada principal del SDK
└── tests/
    └── UruFacturaSDK.Tests/    # Tests unitarios xUnit
```

---

## 🛠️ Uso Rápido

### 1. Configurar el cliente

```csharp
var config = new UruFacturaConfig
{
    RutEmisor           = "210000000012",
    RazonSocialEmisor   = "Mi Empresa S.A.",
    DomicilioFiscal     = "Av. 18 de Julio 1234",
    Ciudad              = "Montevideo",
    Departamento        = "Montevideo",
    Giro                = "Comercio al por mayor", // Actividad económica (opcional)
    Ambiente            = Ambiente.Homologacion,
    RutaCertificado     = "/ruta/al/certificado.p12",
    PasswordCertificado = "mi_password",
};

using var client = new UruFacturaClient(config);
```

### 2. Crear y enviar un e-Ticket

```csharp
using UruFacturaSDK.Models; // Cae, LineaDetalle, Receptor, RefCfe

// Registrar CAE
client.Cae.RegistrarCae(new Cae
{
    NroSerie        = "CAE2025001",
    TipoCfe         = TipoCfe.ETicket,
    RangoDesde      = 1,
    RangoHasta      = 1000,
    FechaVencimiento = new DateTime(2026, 12, 31),
});

// Crear comprobante
var eticket = client.CrearETicket();
eticket.Numero = 1;
eticket.Detalle.Add(new LineaDetalle
{
    NroLinea      = 1,
    NombreItem    = "Servicio de consultoría",
    Cantidad      = 1,
    PrecioUnitario = 1000m,
    IndFactIva    = TipoIva.Basico,
});

// Generar XML, firmar y enviar
var xmlFirmado = client.GenerarYFirmar(eticket);
var respuesta  = await client.EnviarCfeAsync(eticket);

if (respuesta.Exitoso)
    Console.WriteLine($"✅ Aceptado: {respuesta.Mensaje}");
```

### 3. Generar PDF

```csharp
// PDF A4
byte[] pdfA4 = client.GenerarPdfA4(eticket);
await File.WriteAllBytesAsync("eticket.pdf", pdfA4);

// PDF Térmico (ticket 80mm)
byte[] pdfTermico = client.GenerarPdfTermico(eticket);
await File.WriteAllBytesAsync("eticket_termico.pdf", pdfTermico);
```

### 4. Nota de crédito con referencia

```csharp
var nc = client.CrearNotaCreditoETicket();
nc.Numero = 1;
nc.Referencias.Add(new RefCfe
{
    TipoCfe  = TipoCfe.ETicket,
    Serie    = "A",
    NroCfe   = 42,
    FechaCfe = new DateTime(2025, 6, 15),
    Razon    = "Devolución de mercadería",
});
nc.Detalle.Add(new LineaDetalle
{
    NroLinea       = 1,
    NombreItem     = "Devolución producto",
    Cantidad       = 1,
    PrecioUnitario = 500m,
    IndFactIva     = TipoIva.Basico,
});
```

### 5. Verificar estado de CAEs

```csharp
var advertencias = client.Cae.ObtenerAdvertencias();
foreach (var advertencia in advertencias)
    Console.WriteLine(advertencia);

Console.WriteLine(client.Cae.ResumenEstado());
```

---

## 📄 Tipos de CFE soportados

| Código | Tipo |
|--------|------|
| 101 | e-Ticket |
| 102 | Nota de Crédito e-Ticket |
| 103 | Nota de Débito e-Ticket |
| 111 | e-Factura |
| 112 | Nota de Crédito e-Factura |
| 113 | Nota de Débito e-Factura |
| 121 | e-Factura de Exportación |
| 122 | Nota de Crédito e-Factura Exportación |
| 123 | Nota de Débito e-Factura Exportación |
| 131 | e-Remito Despachante |
| 151 | e-Resguardo |
| 181 | e-Remito |
| 182 | Nota de Crédito e-Remito |

---

## 🔧 Requisitos

- .NET 10 SDK
- Certificado digital DGI (`.p12` / `.pfx`)
- CAE vigente emitido por la DGI

---

## 🧪 Tests

```bash
dotnet test tests/UruFacturaSDK.Tests/
```

---

## 📝 Licencia

MIT — ver [LICENSE](LICENSE)
