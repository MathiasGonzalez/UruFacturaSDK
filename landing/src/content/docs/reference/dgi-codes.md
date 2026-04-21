---
title: Códigos DGI
description: Referencia de códigos de respuesta de la DGI
---

## Códigos de respuesta DGI

Cuando enviás un CFE, DGI responde con un código numérico y un mensaje. Estos son los códigos más comunes:

| Código | Estado | Descripción |
|--------|--------|-------------|
| `00` | ✅ Aceptado | El comprobante fue aceptado sin observaciones |
| `01` | ⚠️ Aceptado con obs. | Aceptado pero con observaciones informativas — el CFE es válido |
| `05` | ❌ Rechazado | El comprobante fue rechazado — revisar el campo `Mensaje` |
| `06` | ❌ Sin habilitación | RUT emisor no habilitado como emisor electrónico en DGI |
| `09` | ❌ CAE inválido | CAE inválido, vencido, o número de comprobante fuera del rango |
| `11` | ❌ Error de firma | La firma digital es inválida o el certificado no está reconocido |
| `12` | ❌ XML inválido | El XML del CFE no cumple el esquema DGI |
| `99` | ❌ Error servidor | Error interno del servidor DGI — reintentar después |

---

## Cómo manejar los códigos en el SDK

```csharp
var respuesta = await client.EnviarCfeAsync(cfe);

// Verificación simple
if (respuesta.Exitoso) // true si código es "00" (aceptado) o "01" (aceptado con observaciones)
{
    Console.WriteLine("CFE aceptado ✅");
}

// Manejo detallado por código
switch (respuesta.Codigo)
{
    case "00":
        // Aceptado limpio — continuar normalmente
        break;

    case "01":
        // Aceptado con observaciones — el CFE es válido pero loguear
        logger.LogWarning("CFE aceptado con obs: {Mensaje}", respuesta.Mensaje);
        break;

    case "05":
        // Rechazado — investigar el motivo en respuesta.Mensaje
        throw new DgiRechazoException(respuesta.Mensaje);

    case "09":
        // CAE inválido — verificar CAE y numeración
        throw new CaeInvalidoException($"CAE inválido: {respuesta.Mensaje}");

    case "11":
        // Error de firma — verificar certificado
        throw new FirmaException($"Firma inválida: {respuesta.Mensaje}");

    case "99":
        // Error servidor DGI — reintentar con backoff
        await Task.Delay(TimeSpan.FromSeconds(30));
        respuesta = await client.EnviarCfeAsync(cfe);
        break;

    default:
        logger.LogError("Código DGI desconocido [{Codigo}]: {Mensaje}",
            respuesta.Codigo, respuesta.Mensaje);
        break;
}
```

---

## Códigos de estado de consulta

Al consultar el estado de un CFE previamente enviado:

| Código | Significado |
|--------|-------------|
| `00` | CFE aceptado y registrado |
| `01` | CFE aceptado con observaciones |
| `05` | CFE rechazado |
| `10` | CFE no encontrado en los sistemas DGI |

---

## Recursos oficiales

Para la lista completa y actualizada de códigos de error, consultar:
- [Portal DGI — Gestión CFE](https://www.dgi.gub.uy/)
- Instructivo técnico de integración (disponible en el portal DGI en línea)
