namespace UruFacturaSDK.Enums;

/// <summary>
/// Tipos de documento del receptor según la normativa DGI de Uruguay.
/// </summary>
public enum TipoDocumentoReceptor
{
    /// <summary>RUT (Registro Único Tributario) — 2</summary>
    Rut = 2,

    /// <summary>Cédula de Identidad — 3</summary>
    CedulaIdentidad = 3,

    /// <summary>Pasaporte — 4</summary>
    Pasaporte = 4,

    /// <summary>Documento de identidad del exterior — 5</summary>
    DocumentoExterior = 5,
}
