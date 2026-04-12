namespace UruFacturaSDK.Enums;

/// <summary>
/// Indicadores de forma de pago según DGI.
/// </summary>
public enum FormaPago
{
    Contado = 1,
    Credito = 2,
}

/// <summary>
/// Indicadores de tipo de moneda según nomenclatura DGI / ISO 4217.
/// El Peso Uruguayo utiliza el valor 0 (código interno DGI para moneda local).
/// Los demás valores siguen los códigos numéricos ISO 4217.
/// </summary>
public enum Moneda
{
    PesoUruguayo = 0,
    DolarAmericano = 840,
    Euro = 978,
}

/// <summary>
/// Ambiente de operación DGI.
/// </summary>
public enum Ambiente
{
    Homologacion,
    Produccion,
}

/// <summary>
/// Indicadores de IVA según DGI.
/// </summary>
public enum TipoIva
{
    /// <summary>Exento de IVA</summary>
    Exento = 1,

    /// <summary>IVA mínimo (10%)</summary>
    Minimo = 2,

    /// <summary>IVA básico (22%)</summary>
    Basico = 3,

    /// <summary>Suspendido</summary>
    Suspendido = 4,
}
