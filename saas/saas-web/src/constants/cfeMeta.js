import { C } from './styles.js'

// ─── CFE Metadata ─────────────────────────────────────────────────────────────
export const CFE_META = {
  101: {
    label: 'e-Ticket', short: 'e-Ticket (101)', emoji: '🧾',
    color: C.blue, colorL: C.blueL, colorBorder: C.blueBorder, grupo: 'Tickets',
    desc: 'Comprobante al consumidor final. No requiere identificación del receptor (sin RUT).',
    sdkMethod: 'client.CrearETicket()',
    sdkCode: `var cfe = client.CrearETicket();
cfe.Numero = 1;
cfe.Detalle.Add(new LineaDetalle {
    NombreItem = "Consultoría",
    Cantidad = 1,
    PrecioUnitario = 5000,
    IndFactIva = TipoIva.Basico,
});
cfe.CalcularTotales();
client.GenerarYFirmar(cfe); // firma XML con .pfx`,
    dgiNote: 'Art. 4 Res. 798/2012 – ventas sin identificar al comprador.',
    requiresReceptor: false, requiresRef: false,
  },
  102: {
    label: 'Nota de Crédito e-Ticket', short: 'NC e-Ticket (102)', emoji: '↩️',
    color: C.violet, colorL: C.violetL, colorBorder: C.violetBorder, grupo: 'Tickets',
    desc: 'Anula o corrige un e-Ticket ya emitido. Debe referenciar el comprobante original.',
    sdkMethod: 'client.CrearNotaCreditoETicket()',
    sdkCode: `var nc = client.CrearNotaCreditoETicket();
nc.Numero = 2;
nc.Referencias.Add(new RefCfe {
    TipoCfe = TipoCfe.ETicket,
    NroCfe  = 1,
    FechaCfe = originalDate,
    Razon   = "Anulación de comprobante",
});
nc.Detalle.Add(new LineaDetalle { ... });
nc.CalcularTotales();
client.GenerarYFirmar(nc);`,
    dgiNote: 'La NC debe contener la referencia al CFE original (tipo, número y fecha).',
    requiresReceptor: false, requiresRef: true,
  },
  103: {
    label: 'Nota de Débito e-Ticket', short: 'ND e-Ticket (103)', emoji: '📈',
    color: C.violet, colorL: C.violetL, colorBorder: C.violetBorder, grupo: 'Tickets',
    desc: 'Ajuste de precio (mayor valor) sobre un e-Ticket ya emitido.',
    sdkMethod: 'client.CrearNotaDebitoETicket()',
    sdkCode: `var nd = client.CrearNotaDebitoETicket();
nd.Numero = 3;
nd.Referencias.Add(new RefCfe {
    TipoCfe = TipoCfe.ETicket, NroCfe = 1,
    FechaCfe = originalDate,
    Razon = "Ajuste de precio",
});
nd.CalcularTotales();
client.GenerarYFirmar(nd);`,
    dgiNote: 'La ND debe contener la referencia al CFE original (tipo, número y fecha).',
    requiresReceptor: false, requiresRef: true,
  },
  111: {
    label: 'e-Factura', short: 'e-Factura (111)', emoji: '📄',
    color: C.green, colorL: C.greenL, colorBorder: C.greenBorder, grupo: 'Facturas',
    desc: 'Factura electrónica para empresas o personas jurídicas. Requiere RUT del receptor.',
    sdkMethod: 'client.CrearEFactura()',
    sdkCode: `var cfe = client.CrearEFactura();
cfe.Numero = 1;
cfe.Receptor = new Receptor {
    Documento   = "211234560010",
    RazonSocial = "Empresa Ejemplo S.A.",
};
cfe.Detalle.Add(new LineaDetalle { ... });
cfe.CalcularTotales();
client.GenerarYFirmar(cfe);`,
    dgiNote: 'Art. 4 Res. 798/2012 – ventas a contribuyentes identificados con RUT.',
    requiresReceptor: true, requiresRef: false,
  },
  112: {
    label: 'Nota de Crédito e-Factura', short: 'NC e-Factura (112)', emoji: '↩️',
    color: C.violet, colorL: C.violetL, colorBorder: C.violetBorder, grupo: 'Facturas',
    desc: 'Anula o corrige una e-Factura ya emitida. Requiere receptor y referencia al original.',
    sdkMethod: 'client.CrearNotaCreditoEFactura()',
    sdkCode: `var nc = client.CrearNotaCreditoEFactura();
nc.Receptor = new Receptor { ... };
nc.Referencias.Add(new RefCfe {
    TipoCfe = TipoCfe.EFactura,
    NroCfe  = 1,
    FechaCfe = originalDate,
    Razon   = "Anulación de comprobante",
});
nc.CalcularTotales();
client.GenerarYFirmar(nc);`,
    dgiNote: 'La NC de e-Factura requiere receptor identificado y referencia al CFE original.',
    requiresReceptor: true, requiresRef: true,
  },
  113: {
    label: 'Nota de Débito e-Factura', short: 'ND e-Factura (113)', emoji: '📈',
    color: C.violet, colorL: C.violetL, colorBorder: C.violetBorder, grupo: 'Facturas',
    desc: 'Ajuste de precio (mayor valor) sobre una e-Factura ya emitida.',
    sdkMethod: 'client.CrearNotaDebitoEFactura()',
    sdkCode: `var nd = client.CrearNotaDebitoEFactura();
nd.Receptor = new Receptor { ... };
nd.Referencias.Add(new RefCfe {
    TipoCfe = TipoCfe.EFactura, NroCfe = 1,
    FechaCfe = originalDate,
    Razon = "Ajuste de precio",
});
nd.CalcularTotales();
client.GenerarYFirmar(nd);`,
    dgiNote: 'La ND de e-Factura requiere receptor identificado y referencia al CFE original.',
    requiresReceptor: true, requiresRef: true,
  },
  121: {
    label: 'e-Factura Exportación', short: 'e-Fact. Export. (121)', emoji: '🌍',
    color: C.amber, colorL: C.amberL, colorBorder: C.amberBorder, grupo: 'Exportación',
    desc: 'Factura electrónica para exportaciones al exterior. Las líneas van con IVA Exento.',
    sdkMethod: 'client.CrearEFacturaExportacion()',
    sdkCode: `var cfe = client.CrearEFacturaExportacion();
cfe.Receptor = new Receptor {
    Documento   = "NIF-ES-12345678A",
    RazonSocial = "Cliente Exterior S.L.",
};
cfe.Detalle.Add(new LineaDetalle {
    IndFactIva = TipoIva.Exento, // exportaciones
    ...
});
cfe.CalcularTotales();
client.GenerarYFirmar(cfe);`,
    dgiNote: 'Res. DGI 1080/2021 – exportaciones de bienes y servicios al exterior.',
    requiresReceptor: true, requiresRef: false,
  },
  181: {
    label: 'e-Remito', short: 'e-Remito (181)', emoji: '📦',
    color: C.slate, colorL: C.slateL, colorBorder: C.slateBorder, grupo: 'Remitos',
    desc: 'Comprobante de traslado de mercadería. No genera obligación fiscal de IVA.',
    sdkMethod: 'client.CrearERemito()',
    sdkCode: `var remito = client.CrearERemito();
remito.Numero = 1;
remito.Detalle.Add(new LineaDetalle {
    NombreItem    = "Mercadería en tránsito",
    Cantidad      = 10,
    PrecioUnitario = 0,
    IndFactIva    = TipoIva.Exento,
});
remito.CalcularTotales();
client.GenerarYFirmar(remito);`,
    dgiNote: 'Art. 28 Res. 798/2012 – movimientos de stock sin transferencia de dominio.',
    requiresReceptor: false, requiresRef: false,
  },
}

// ─── IVA Labels ───────────────────────────────────────────────────────────────
export const TIPO_IVA = { 1: 'Exento', 2: 'IVA Mínimo 10%', 3: 'IVA Básico 22%' }

// ─── Form Defaults ────────────────────────────────────────────────────────────
export const defaultLine = () => ({ nombreItem: '', cantidad: 1, precioUnitario: 0, indFactIva: 3 })
export const defaultForm = (tipoCfe = 101) => ({ tipoCfe, numero: 1, rutReceptor: '', nombreReceptor: '', detalle: [defaultLine()] })

// ─── Maps a numeric TipoCfe value to its C# enum member name ─────────────────
export function tipoCfeEnumName(value) {
  const labels = {
    101: 'ETicket', 102: 'NotaCreditoETicket', 103: 'NotaDebitoETicket',
    111: 'EFactura', 112: 'NotaCreditoEFactura', 113: 'NotaDebitoEFactura',
    121: 'EFacturaExportacion', 181: 'ERemito',
  }
  return labels[value] ?? 'ETicket'
}
