namespace UruFacturaSDK.Enums;

/// <summary>
/// Tipos de Comprobante Fiscal Electrónico (CFE) reconocidos por la DGI de Uruguay.
/// </summary>
public enum TipoCfe
{
    /// <summary>e-Ticket (101)</summary>
    ETicket = 101,

    /// <summary>Nota de crédito de e-Ticket (102)</summary>
    NotaCreditoETicket = 102,

    /// <summary>Nota de débito de e-Ticket (103)</summary>
    NotaDebitoETicket = 103,

    /// <summary>e-Remito (181)</summary>
    ERemito = 181,

    /// <summary>Nota de crédito de e-Remito (182)</summary>
    NotaCreditoERemito = 182,

    /// <summary>e-Factura (111)</summary>
    EFactura = 111,

    /// <summary>Nota de crédito de e-Factura (112)</summary>
    NotaCreditoEFactura = 112,

    /// <summary>Nota de débito de e-Factura (113)</summary>
    NotaDebitoEFactura = 113,

    /// <summary>e-Factura de exportación (121)</summary>
    EFacturaExportacion = 121,

    /// <summary>Nota de crédito de e-Factura de exportación (122)</summary>
    NotaCreditoEFacturaExportacion = 122,

    /// <summary>Nota de débito de e-Factura de exportación (123)</summary>
    NotaDebitoEFacturaExportacion = 123,

    /// <summary>e-Remito de exportación (131)</summary>
    ERemitoDespachante = 131,

    /// <summary>e-Resguardo (151)</summary>
    EResguardo = 151,
}
