# Instrucciones para GitHub Copilot — UruFacturaSDK

## Contexto del proyecto

UruFacturaSDK es una librería .NET 10 C# para integrar sistemas con la facturación electrónica de la DGI (Dirección General Impositiva) de Uruguay. Implementa el formato CFE (Comprobante Fiscal Electrónico) v25.01.

---

## Cuando te pidan revisar normativa DGI

Sigue el runbook en [`docs/NORMATIVA_DGI_UPDATE.md`](docs/NORMATIVA_DGI_UPDATE.md).

**Pasos mínimos:**
1. Busca en la web cambios recientes en el formato CFE o normativas DGI Uruguay.
2. Compara con la versión implementada (ver `src/UruFacturaSDK/Xml/CfeXmlBuilder.cs` — atributo `version`).
3. Si no hay cambios: responde "Sin cambios detectados" y no modifiques código.
4. Si hay cambios: implementa según el runbook, actualiza los docs, y corre `dotnet test UruFacturaSDK.slnx`.

---

## Convenciones de este proyecto

- **Idioma del código:** español para nombres de propiedades, modelos y métodos de dominio. Inglés para nombres técnicos estándar (.NET, SOAP, XML).
- **Modelo CFE:** el modelo principal es `Cfe` en `src/UruFacturaSDK/Models/Cfe.cs`. Siempre agregar validaciones en `Cfe.Validar()` cuando un campo se vuelve obligatorio.
- **XML builder:** `CfeXmlBuilder.cs` usa `XmlWriter` (no interpolación de strings) para evitar vulnerabilidades XSS.
- **Serialización XML:** usar `MemoryStream` + `UTF8Encoding(false)` (sin BOM). No usar `StringBuilder` para la salida XML.
- **Tests:** cada nueva funcionalidad debe tener tests en `tests/UruFacturaSDK.Tests/`. Usar xUnit.
- **Build y tests:** `dotnet test UruFacturaSDK.slnx` corre todo.

---

## Archivos clave

| Archivo | Propósito |
|---------|-----------|
| `src/UruFacturaSDK/Xml/CfeXmlBuilder.cs` | Generación del XML del CFE |
| `src/UruFacturaSDK/Models/Cfe.cs` | Modelo principal + validaciones |
| `src/UruFacturaSDK/Models/RefCfe.cs` | Modelo de referencias (notas de crédito/débito) |
| `src/UruFacturaSDK/Configuration/UruFacturaConfig.cs` | Config del emisor + validaciones |
| `src/UruFacturaSDK/Enums/TipoCfe.cs` | Tipos de comprobante |
| `docs/NORMATIVA_DGI_UPDATE.md` | Runbook de actualización normativa |
| `docs/FACTURACION_URUGUAY.md` | Guía de facturación con ejemplos |
