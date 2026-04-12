---
title: Instalación
description: Cómo instalar UruFactura SDK en tu proyecto .NET
sidebar:
  order: 1
---

import { Tabs, TabItem, Aside, Steps } from '@astrojs/starlight/components';

## Requisitos previos

Antes de instalar el SDK, asegurate de tener:

- **.NET 10 SDK** o superior ([descargar](https://dotnet.microsoft.com/download))
- Un **certificado digital** en formato `.p12` / `.pfx` emitido por una CA reconocida por AGESIC
- Un **CAE vigente** otorgado por DGI

<Aside type="note">
  ¿Todavía no tenés el certificado o CAE? Podés usar el ambiente de **Homologación** de DGI para hacer pruebas sin documentos reales. Consultá la [Guía de Certificación DGI](/guides/dgi-certification/).
</Aside>

---

## Instalar el paquete NuGet

<Tabs>
  <TabItem label=".NET CLI">
  ```bash
  dotnet add package UruFacturaSDK
  ```
  </TabItem>
  <TabItem label="Package Manager Console">
  ```powershell
  Install-Package UruFacturaSDK
  ```
  </TabItem>
  <TabItem label="PackageReference (csproj)">
  ```xml
  <PackageReference Include="UruFacturaSDK" Version="*" />
  ```
  </TabItem>
  <TabItem label="Paket">
  ```
  paket add UruFacturaSDK
  ```
  </TabItem>
</Tabs>

---

## Verificar la instalación

Después de instalar, verificá que el paquete esté disponible:

```bash
dotnet list package
```

Deberías ver algo como:

```
> UruFacturaSDK    1.0.0
```

---

## Configuración básica

<Steps>

1. **Importar los namespaces necesarios**

   ```csharp
   using UruFacturaSDK;
   using UruFacturaSDK.Configuration;
   using UruFacturaSDK.Models;
   using UruFacturaSDK.Enums;
   ```

2. **Crear la configuración del emisor**

   ```csharp
   var config = new UruFacturaConfig
   {
       RutEmisor           = "210000000012",      // 12 dígitos, sin puntos ni guión
       RazonSocialEmisor   = "Mi Empresa S.A.",
       DomicilioFiscal     = "Av. 18 de Julio 1234",
       Ciudad              = "Montevideo",
       Departamento        = "Montevideo",
       Ambiente            = Ambiente.Homologacion, // ← Usar Homologacion para pruebas
       RutaCertificado     = "/ruta/al/certificado.p12",
       PasswordCertificado = Environment.GetEnvironmentVariable("CERT_PASSWORD")!,
   };
   ```

   <Aside type="caution">
     Nunca almacenés `PasswordCertificado` en texto plano en el código fuente. Usá variables de entorno, user secrets de .NET, o un gestor de secretos como Azure Key Vault.
   </Aside>

3. **Instanciar el cliente**

   ```csharp
   using var client = new UruFacturaClient(config);
   ```

   El cliente implementa `IDisposable`, por lo que se recomienda usarlo dentro de un bloque `using`.

4. **Registrar un CAE**

   ```csharp
   client.Cae.RegistrarCae(new Cae
   {
       NroSerie         = "CAE2025001",
       TipoCfe          = TipoCfe.ETicket,
       RangoDesde       = 1,
       RangoHasta       = 1000,
       FechaVencimiento = new DateTime(2026, 12, 31),
   });
   ```

</Steps>

---

## Integración con ASP.NET Core / Dependency Injection

Si estás usando ASP.NET Core, podés registrar el cliente en el contenedor de DI:

```csharp
// Program.cs
builder.Services.AddSingleton<UruFacturaConfig>(sp =>
{
    return new UruFacturaConfig
    {
        RutEmisor           = builder.Configuration["UruFactura:RutEmisor"]!,
        RazonSocialEmisor   = builder.Configuration["UruFactura:RazonSocial"]!,
        DomicilioFiscal     = builder.Configuration["UruFactura:Domicilio"]!,
        Ciudad              = builder.Configuration["UruFactura:Ciudad"]!,
        Departamento        = builder.Configuration["UruFactura:Departamento"]!,
        Ambiente            = Enum.Parse<Ambiente>(builder.Configuration["UruFactura:Ambiente"]!),
        RutaCertificado     = builder.Configuration["UruFactura:RutaCertificado"]!,
        PasswordCertificado = builder.Configuration["UruFactura:PasswordCertificado"]!,
    };
});

builder.Services.AddSingleton<UruFacturaClient>();
```

```json
// appsettings.json
{
  "UruFactura": {
    "RutEmisor": "210000000012",
    "RazonSocial": "Mi Empresa S.A.",
    "Domicilio": "Av. 18 de Julio 1234",
    "Ciudad": "Montevideo",
    "Departamento": "Montevideo",
    "Ambiente": "Homologacion",
    "RutaCertificado": "/ruta/al/certificado.p12"
  }
}
```

```json
// appsettings.Production.json (o secrets.json)
{
  "UruFactura": {
    "PasswordCertificado": "tu_contraseña_segura"
  }
}
```

---

## Próximos pasos

- [Uso Rápido →](/getting-started/quick-start/) — Emitir tu primer CFE
- [Configuración →](/getting-started/configuration/) — Todas las opciones de configuración
