---
title: API Reference
description: Documentación completa de clases, métodos y modelos de UruFactura SDK
---

import { Aside } from '@astrojs/starlight/components';

## `UruFacturaClient`

El facade principal del SDK. Punto de entrada para todas las operaciones.

```csharp
using var client = new UruFacturaClient(config);
```

### Constructor

| Parámetro | Tipo | Descripción |
|-----------|------|-------------|
| `config` | `UruFacturaConfig` | Configuración del emisor y ambiente |

### Métodos de creación de CFE

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `CrearETicket()` | `ETicket` | e-Ticket (CFE 101) |
| `CrearNotaCreditoETicket()` | `NotaCreditoETicket` | Nota de Crédito e-Ticket (102) |
| `CrearNotaDebitoETicket()` | `NotaDebitoETicket` | Nota de Débito e-Ticket (103) |
| `CrearEFactura()` | `EFactura` | e-Factura (111) |
| `CrearNotaCreditoEFactura()` | `NotaCreditoEFactura` | Nota de Crédito e-Factura (112) |
| `CrearNotaDebitoEFactura()` | `NotaDebitoEFactura` | Nota de Débito e-Factura (113) |
| `CrearEFacturaExportacion()` | `EFacturaExportacion` | e-Factura Exportación (121) |
| `CrearNotaCreditoExportacion()` | `NotaCreditoExportacion` | NC Exportación (122) |
| `CrearNotaDebitoExportacion()` | `NotaDebitoExportacion` | ND Exportación (123) |
| `CrearERemito()` | `ERemito` | e-Remito (181) |
| `CrearNotaCreditoERemito()` | `NotaCreditoERemito` | NC e-Remito (182) |
| `CrearEResguardo()` | `EResguardo` | e-Resguardo (151) |
| `CrearERemitoDespachante()` | `ERemitoDespachante` | e-Remito Despachante (131) |

### Métodos de operación

| Método | Firma | Descripción |
|--------|-------|-------------|
| `GenerarYFirmar` | `string GenerarYFirmar(Cfe cfe)` | Genera XML y lo firma con XAdES-BES. Retorna el XML firmado. |
| `EnviarCfeAsync` | `Task<RespuestaDgi> EnviarCfeAsync(Cfe cfe)` | Genera, firma y envía el CFE a DGI. |
| `ConsultarEstadoCfeAsync` | `Task<RespuestaDgi> ConsultarEstadoCfeAsync(Cfe cfe)` | Consulta el estado de un CFE ya enviado. |
| `EnviarReporteDiarioAsync` | `Task<ResultadoReporte> EnviarReporteDiarioAsync(DateTime fecha, IEnumerable<Cfe> cfes)` | Envía el reporte diario a DGI. |
| `GenerarPdfA4` | `byte[] GenerarPdfA4(Cfe cfe)` | Genera representación impresa A4 en PDF. |
| `GenerarPdfTermico` | `byte[] GenerarPdfTermico(Cfe cfe)` | Genera representación térmica 80mm en PDF. |

### Propiedades

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `Cae` | `CaeManager` | Gestor de CAEs del emisor |

---

## `UruFacturaConfig`

Configuración del cliente SDK.

```csharp
public class UruFacturaConfig
{
    public string RutEmisor { get; set; }
    public string RazonSocialEmisor { get; set; }
    public string DomicilioFiscal { get; set; }
    public string Ciudad { get; set; }
    public string Departamento { get; set; }
    public string RutaCertificado { get; set; }
    public string PasswordCertificado { get; set; }
    public Ambiente Ambiente { get; set; }
}
```

Ver [Configuración](../getting-started/configuration/) para detalles de cada propiedad.

---

## Modelos de datos

### `Cfe` (clase base)

Clase base de todos los comprobantes. No se instancia directamente — usá los métodos de fábrica de `UruFacturaClient`.

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `Numero` | `int` | Número del comprobante (dentro del rango CAE) |
| `FechaEmision` | `DateTime` | Fecha de emisión (default: `DateTime.Now`) |
| `Detalle` | `List<LineaDetalle>` | Líneas de detalle del comprobante |
| `Referencias` | `List<RefCfe>` | Referencias a comprobantes (para NC/ND) |

### `LineaDetalle`

Representa una línea de ítem en el comprobante.

| Propiedad | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `NroLinea` | `int` | ✅ | Número de línea (secuencial desde 1) |
| `NombreItem` | `string` | ✅ | Descripción del producto o servicio |
| `Cantidad` | `decimal` | ✅ | Cantidad |
| `PrecioUnitario` | `decimal` | ✅ | Precio unitario sin IVA |
| `IndFactIva` | `TipoIva` | ✅ | Indicador de IVA (Basico, Minimo, Exento) |
| `Descuento` | `decimal?` | — | Porcentaje de descuento |

### `Receptor`

Datos del receptor del comprobante (requerido para e-Factura).

| Propiedad | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `RutReceptor` | `string` | ✅ | RUT del receptor (12 dígitos) |
| `RazonSocial` | `string` | ✅ | Razón social del receptor |
| `Domicilio` | `string` | ✅ | Domicilio del receptor |
| `Ciudad` | `string` | ✅ | Ciudad del receptor |

### `Cae`

Constancia de Autorización de Emisión.

| Propiedad | Tipo | Requerido | Descripción |
|-----------|------|-----------|-------------|
| `NroSerie` | `string` | ✅ | Número de serie del CAE |
| `TipoCfe` | `TipoCfe` | ✅ | Tipo de CFE al que aplica |
| `RangoDesde` | `int` | ✅ | Primer número autorizado |
| `RangoHasta` | `int` | ✅ | Último número autorizado |
| `FechaVencimiento` | `DateTime` | ✅ | Fecha de vencimiento del CAE |

### `RefCfe`

Referencia a otro comprobante (usado en notas de crédito/débito).

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `TipoCfe` | `TipoCfe` | Tipo del CFE referenciado |
| `Serie` | `string` | Serie del CFE referenciado |
| `NroCfe` | `int` | Número del CFE referenciado |
| `FechaCfe` | `DateTime` | Fecha del CFE referenciado |
| `Razon` | `string` | Motivo de la nota de crédito/débito |

### `RespuestaDgi`

Respuesta del servidor DGI tras el envío de un CFE.

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `Exitoso` | `bool` | `true` si el código es `00` o `01` |
| `Codigo` | `string` | Código de respuesta DGI |
| `Mensaje` | `string` | Descripción de la respuesta |

---

## Enums

### `Ambiente`

```csharp
public enum Ambiente
{
    Homologacion, // Pruebas — endpoint DGI de homologación
    Produccion,   // Real — endpoint DGI de producción
}
```

### `TipoCfe`

```csharp
public enum TipoCfe
{
    ETicket                   = 101,
    NotaCreditoETicket        = 102,
    NotaDebitoETicket         = 103,
    EFactura                  = 111,
    NotaCreditoEFactura       = 112,
    NotaDebitoEFactura        = 113,
    EFacturaExportacion       = 121,
    NotaCreditoExportacion    = 122,
    NotaDebitoExportacion     = 123,
    ERemitoDespachante        = 131,
    EResguardo                = 151,
    ERemito                   = 181,
    NotaCreditoERemito        = 182,
}
```

### `TipoIva`

```csharp
public enum TipoIva
{
    Basico, // 22%
    Minimo, // 10%
    Exento, // 0%
}
```

---

## `CaeManager`

Gestiona los CAEs registrados en el cliente. Accedido a través de `client.Cae`.

| Método | Firma | Descripción |
|--------|-------|-------------|
| `RegistrarCae` | `void RegistrarCae(Cae cae)` | Registra un nuevo CAE |
| `ObtenerAdvertencias` | `IEnumerable<string> ObtenerAdvertencias()` | Retorna advertencias de vencimiento/agotamiento |
| `ResumenEstado` | `string ResumenEstado()` | Texto con el estado de todos los CAEs |

---

## Excepciones

| Excepción | Descripción |
|-----------|-------------|
| `UruFacturaException` | Excepción base del SDK |
| `CaeVencidoException` | El CAE configurado está vencido |
| `CaeAgotadoException` | El rango del CAE está agotado |
| `FirmaException` | Error al firmar el XML (certificado inválido o inaccesible) |
| `DgiException` | Error en la comunicación con DGI |
