import { useState, useEffect } from 'react'

// ─── Design Tokens ────────────────────────────────────────────────────────────
const C = {
  blue: '#2563eb', blueL: '#eff6ff', blueBorder: '#bfdbfe',
  green: '#16a34a', greenL: '#f0fdf4', greenBorder: '#bbf7d0',
  red: '#dc2626', redL: '#fef2f2', redBorder: '#fecaca',
  violet: '#7c3aed', violetL: '#f5f3ff', violetBorder: '#ddd6fe',
  amber: '#d97706', amberL: '#fffbeb', amberBorder: '#fde68a',
  slate: '#475569', slateL: '#f8fafc', slateBorder: '#e2e8f0',
  border: '#e2e8f0', bg: '#f8fafc', card: '#fff',
  text: '#0f172a', textMuted: '#64748b',
}

// ─── CFE Metadata ─────────────────────────────────────────────────────────────
const CFE_META = {
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
    IndFactIva = TipoIva.IvaBasico,
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

const TIPO_IVA = { 1: 'Exento', 2: 'IVA Mínimo 10%', 3: 'IVA Básico 22%' }
const defaultLine = () => ({ nombreItem: '', cantidad: 1, precioUnitario: 0, indFactIva: 3 })
const defaultForm = (tipoCfe = 101) => ({ tipoCfe, numero: 1, rutReceptor: '', nombreReceptor: '', detalle: [defaultLine()] })

// ─── App ──────────────────────────────────────────────────────────────────────
export default function App() {
  const [tab, setTab] = useState('crear')
  const [cfeTypes, setCfeTypes] = useState([])
  const [configStatus, setConfigStatus] = useState(null)
  const [invoices, setInvoices] = useState([])

  useEffect(() => {
    injectStyles()
    loadInvoices()
    fetchCfeTypes()
    fetchConfigStatus()
  }, [])

  async function loadInvoices() {
    try {
      const res = await fetch('/api/invoices')
      if (res.ok) setInvoices(await res.json())
    } catch { /* API not ready */ }
  }

  async function fetchCfeTypes() {
    try {
      const res = await fetch('/api/cfe-types')
      if (res.ok) setCfeTypes(await res.json())
    } catch { /* API not ready */ }
  }

  async function fetchConfigStatus() {
    try {
      const res = await fetch('/api/config/status')
      if (res.ok) setConfigStatus(await res.json())
      else setConfigStatus({ ok: false, ambiente: '?', issues: [`Error HTTP ${res.status}`] })
    } catch (e) {
      setConfigStatus({ ok: false, ambiente: '?', issues: [`No se pudo conectar: ${e.message}`] })
    }
  }

  return (
    <div style={{ fontFamily: '"Inter", system-ui, sans-serif', background: C.bg, minHeight: '100vh' }}>
      {/* ── Header ── */}
      <header style={{ background: '#fff', borderBottom: `1px solid ${C.border}`, position: 'sticky', top: 0, zIndex: 50 }}>
        <div style={{ maxWidth: 1240, margin: '0 auto', padding: '0 24px', display: 'flex', alignItems: 'center', gap: 16, height: 56 }}>
          <span style={{ fontWeight: 700, fontSize: 17, color: C.text, whiteSpace: 'nowrap' }}>🧾 UruFactura ERP Demo</span>
          <nav style={{ display: 'flex', gap: 2, marginLeft: 16 }}>
            {[['crear', '✏️ Crear CFE'], ['demo', '🎬 Modo Demo'], ['historial', '📋 Historial']].map(([id, label]) => (
              <button key={id} onClick={() => setTab(id)}
                style={{
                  padding: '5px 14px', border: 'none', borderRadius: 6, cursor: 'pointer', fontSize: 13,
                  fontWeight: tab === id ? 600 : 400,
                  background: tab === id ? C.blueL : 'transparent',
                  color: tab === id ? C.blue : C.textMuted,
                }}>
                {label}
              </button>
            ))}
          </nav>
          <div style={{ marginLeft: 'auto' }}>
            <ConfigStatusPill status={configStatus} onRefresh={fetchConfigStatus} />
          </div>
        </div>
      </header>

      {/* ── Body ── */}
      <main style={{ maxWidth: 1240, margin: '0 auto', padding: '28px 24px' }}>
        {tab === 'crear'    && <CreateCfeTab cfeTypes={cfeTypes} onCreated={loadInvoices} />}
        {tab === 'demo'     && <DemoTab onCreated={loadInvoices} />}
        {tab === 'historial'&& <HistorialTab invoices={invoices} onReload={loadInvoices} />}
      </main>
    </div>
  )
}

// ─── Config Status Pill ────────────────────────────────────────────────────────
function ConfigStatusPill({ status, onRefresh }) {
  const [open, setOpen] = useState(false)
  if (!status) return <span style={{ fontSize: 12, color: C.textMuted }}>Verificando…</span>
  const ok = status.ok
  return (
    <div style={{ position: 'relative' }}>
      <button onClick={() => setOpen(o => !o)}
        style={{
          display: 'flex', alignItems: 'center', gap: 6, padding: '4px 12px',
          border: `1px solid ${ok ? C.greenBorder : C.redBorder}`,
          borderRadius: 20, cursor: 'pointer', fontSize: 12, fontWeight: 500,
          background: ok ? C.greenL : C.redL, color: ok ? C.green : C.red,
        }}>
        <span style={{ fontSize: 8 }}>●</span>
        {ok ? 'Ambiente OK' : 'Config. incompleta'}
        <span style={{ fontSize: 9 }}>▾</span>
      </button>
      {open && (
        <div className="uf-dropdown" style={{
          position: 'absolute', right: 0, top: 38, width: 340, zIndex: 200,
          background: '#fff', border: `1px solid ${C.border}`, borderRadius: 12,
          boxShadow: '0 8px 30px rgba(0,0,0,0.12)', padding: 16,
        }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 12 }}>
            <strong style={{ fontSize: 13 }}>Estado del Ambiente</strong>
            <div style={{ display: 'flex', gap: 8 }}>
              <button onClick={onRefresh}
                style={{ border: 'none', background: 'none', cursor: 'pointer', fontSize: 12, color: C.blue }}>
                ↻ Verificar
              </button>
              <button onClick={() => setOpen(false)}
                style={{ border: 'none', background: 'none', cursor: 'pointer', fontSize: 16, color: C.textMuted, lineHeight: 1 }}>
                ×
              </button>
            </div>
          </div>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 6, fontSize: 13 }}>
            <PillRow label="Ambiente"    value={status.ambiente} />
            {status.rutEmisor  && <PillRow label="RUT Emisor"   value={status.rutEmisor} />}
            {status.razonSocial&& <PillRow label="Razón Social" value={status.razonSocial} />}
            {status.certificado && (
              <PillRow label="Certificado"
                value={status.certificadoExiste ? '✓ Encontrado' : '✗ No encontrado'}
                valueColor={status.certificadoExiste ? C.green : C.red} />
            )}
          </div>
          {status.issues?.length > 0 && (
            <ul style={{ margin: '10px 0 0', padding: '0 0 0 16px', fontSize: 12, color: C.red }}>
              {status.issues.map((issue, i) => <li key={i}>{issue}</li>)}
            </ul>
          )}
        </div>
      )}
    </div>
  )
}

// ─── Create CFE Tab ────────────────────────────────────────────────────────────
function CreateCfeTab({ cfeTypes, onCreated }) {
  const [selectedType, setSelectedType] = useState(null)
  const [form, setForm] = useState(defaultForm())
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)
  const [success, setSuccess] = useState(null)
  const [showSdkModal, setShowSdkModal] = useState(false)

  // Build groups from CFE_META (augment with API list if available)
  const allTypes = cfeTypes.length > 0
    ? cfeTypes.filter(t => CFE_META[t.value])
    : Object.keys(CFE_META).map(v => ({ value: Number(v), label: CFE_META[v].label }))

  const byGroup = allTypes.reduce((acc, t) => {
    const g = CFE_META[t.value].grupo
    ;(acc[g] = acc[g] || []).push(t)
    return acc
  }, {})

  function selectType(value) {
    setSelectedType(value)
    setForm(defaultForm(value))
    setError(null)
    setSuccess(null)
  }

  const setField = (key, val) => setForm(f => ({ ...f, [key]: val }))
  const setLine  = (i, key, val) => setForm(f => {
    const d = [...f.detalle]
    d[i] = { ...d[i], [key]: val }
    return { ...f, detalle: d }
  })
  const addLine    = () => setForm(f => ({ ...f, detalle: [...f.detalle, defaultLine()] }))
  const removeLine = i  => setForm(f => ({ ...f, detalle: f.detalle.filter((_, x) => x !== i) }))

  async function submit(e) {
    e.preventDefault()
    setLoading(true); setError(null); setSuccess(null)
    try {
      const body = {
        tipoCfe: Number(form.tipoCfe), numero: Number(form.numero),
        rutReceptor: form.rutReceptor || null, nombreReceptor: form.nombreReceptor || null,
        detalle: form.detalle.map(l => ({
          nombreItem: l.nombreItem, cantidad: Number(l.cantidad),
          precioUnitario: Number(l.precioUnitario), indFactIva: Number(l.indFactIva),
        })),
      }
      const res = await fetch('/api/invoices', {
        method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body),
      })
      if (!res.ok) {
        const err = await res.json().catch(() => ({ title: res.statusText }))
        throw new Error(err.detail ?? err.title ?? 'Error desconocido')
      }
      const created = await res.json()
      setSuccess(`CFE Nº ${created.numero} creado y firmado correctamente. ✓`)
      setForm(defaultForm(Number(form.tipoCfe)))
      onCreated()
    } catch (err) { setError(err.message) }
    finally { setLoading(false) }
  }

  const meta = CFE_META[selectedType]

  return (
    <div>
      <h2 style={sectionTitle}>Crear nuevo CFE</h2>
      <p style={{ color: C.textMuted, marginBottom: 20 }}>
        Seleccioná el tipo de comprobante para ver su descripción, requisitos y el código SDK correspondiente.
      </p>

      {/* Type selector */}
      {Object.entries(byGroup).map(([grupo, types]) => (
        <div key={grupo} style={{ marginBottom: 16 }}>
          <div style={{ fontSize: 11, fontWeight: 700, color: C.textMuted, textTransform: 'uppercase', letterSpacing: '0.08em', marginBottom: 8 }}>
            {grupo}
          </div>
          <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
            {types.map(t => {
              const m = CFE_META[t.value]; const sel = selectedType === t.value
              return (
                <button key={t.value} onClick={() => selectType(t.value)}
                  style={{
                    display: 'flex', alignItems: 'center', gap: 8, padding: '9px 14px',
                    border: `2px solid ${sel ? m.color : C.border}`, borderRadius: 10,
                    cursor: 'pointer', fontSize: 13, fontWeight: sel ? 600 : 400,
                    background: sel ? m.colorL : '#fff', color: sel ? m.color : C.text,
                    transition: 'border-color 0.15s, background 0.15s', minWidth: 150,
                  }}>
                  <span style={{ fontSize: 20 }}>{m.emoji}</span>
                  <div style={{ textAlign: 'left' }}>
                    <div>{m.label}</div>
                    <div style={{ fontSize: 11, color: sel ? m.color : C.textMuted, fontWeight: 400, opacity: 0.8 }}>Tipo {t.value}</div>
                  </div>
                </button>
              )
            })}
          </div>
        </div>
      ))}

      {!selectedType && (
        <div style={{ marginTop: 32, textAlign: 'center', color: C.textMuted, fontSize: 14 }}>
          ↑ Seleccioná un tipo de CFE para comenzar
        </div>
      )}

      {selectedType && meta && (
        <div style={{ marginTop: 24, display: 'flex', gap: 20, flexWrap: 'wrap', alignItems: 'flex-start' }}>
          {/* Info panel */}
          <div style={{ flex: '0 0 268px', background: meta.colorL, border: `1px solid ${meta.colorBorder}`, borderRadius: 12, padding: 18 }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 12 }}>
              <span style={{ fontSize: 28 }}>{meta.emoji}</span>
              <div>
                <div style={{ fontWeight: 700, color: meta.color, fontSize: 14 }}>{meta.label}</div>
                <div style={{ fontSize: 12, color: C.textMuted }}>Tipo {selectedType}</div>
              </div>
            </div>
            <p style={{ fontSize: 13, color: C.text, margin: '0 0 12px', lineHeight: 1.5 }}>{meta.desc}</p>
            <div style={{ fontSize: 11, color: C.textMuted, background: 'rgba(255,255,255,0.6)', borderRadius: 6, padding: '6px 9px', lineHeight: 1.5 }}>
              📋 {meta.dgiNote}
            </div>
            {meta.requiresReceptor && (
              <div style={{ marginTop: 8, fontSize: 11, color: meta.color, background: meta.colorL, border: `1px solid ${meta.colorBorder}`, borderRadius: 6, padding: '5px 9px' }}>
                ⚠️ Requiere RUT y razón social del receptor
              </div>
            )}
            {meta.requiresRef && (
              <div style={{ marginTop: 8, fontSize: 11, color: C.amber, background: C.amberL, border: `1px solid ${C.amberBorder}`, borderRadius: 6, padding: '5px 9px' }}>
                🔗 Requiere referencia al CFE original
              </div>
            )}
            <button onClick={() => setShowSdkModal(true)}
              style={{
                marginTop: 14, width: '100%', padding: '8px 0',
                background: meta.color, color: '#fff', border: 'none',
                borderRadius: 8, cursor: 'pointer', fontSize: 13, fontWeight: 600,
              }}>
              {'</>'} Ver código SDK
            </button>
          </div>

          {/* Form */}
          <div style={{ flex: 1, minWidth: 320, background: '#fff', border: `1px solid ${C.border}`, borderRadius: 12, padding: 22 }}>
            <form onSubmit={submit}>
              <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap' }}>
                <div style={{ flex: '0 0 110px' }}>
                  <label style={lbl}>Número CFE</label>
                  <input type="number" min="1" required value={form.numero}
                    onChange={e => setField('numero', e.target.value)} style={inp} />
                </div>
                <div style={{ flex: '1 1 160px' }}>
                  <label style={lbl}>RUT Receptor {meta.requiresReceptor ? '*' : '(opcional)'}</label>
                  <input required={meta.requiresReceptor} value={form.rutReceptor}
                    onChange={e => setField('rutReceptor', e.target.value)}
                    placeholder={meta.requiresReceptor ? 'Ej: 211234560010' : ''} style={inp} />
                </div>
                <div style={{ flex: '1 1 160px' }}>
                  <label style={lbl}>Nombre/Razón Social {meta.requiresReceptor ? '*' : '(opcional)'}</label>
                  <input required={meta.requiresReceptor} value={form.nombreReceptor}
                    onChange={e => setField('nombreReceptor', e.target.value)}
                    placeholder={meta.requiresReceptor ? 'Empresa S.A.' : ''} style={inp} />
                </div>
              </div>

              <div style={{ marginTop: 16 }}>
                <div style={{ fontSize: 13, fontWeight: 600, marginBottom: 8, color: C.text }}>Detalle</div>
                {form.detalle.map((line, i) => (
                  <div key={i} style={{ display: 'flex', gap: 6, marginBottom: 6, flexWrap: 'wrap', alignItems: 'center' }}>
                    <input placeholder="Descripción del ítem" required value={line.nombreItem}
                      onChange={e => setLine(i, 'nombreItem', e.target.value)}
                      style={{ ...inp, flex: '2 1 140px' }} />
                    <input type="number" placeholder="Cant." min="0.01" step="0.01" required value={line.cantidad}
                      onChange={e => setLine(i, 'cantidad', e.target.value)}
                      style={{ ...inp, flex: '0 1 68px' }} />
                    <input type="number" placeholder="Precio unit." min="0" step="0.01" required value={line.precioUnitario}
                      onChange={e => setLine(i, 'precioUnitario', e.target.value)}
                      style={{ ...inp, flex: '0 1 90px' }} />
                    <select value={line.indFactIva} onChange={e => setLine(i, 'indFactIva', e.target.value)}
                      style={{ ...inp, flex: '0 1 130px' }}>
                      {Object.entries(TIPO_IVA).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
                    </select>
                    {form.detalle.length > 1 && (
                      <button type="button" onClick={() => removeLine(i)}
                        style={{ padding: '5px 9px', cursor: 'pointer', border: `1px solid ${C.redBorder}`, borderRadius: 6, color: C.red, background: C.redL, flexShrink: 0 }}>
                        ✕
                      </button>
                    )}
                  </div>
                ))}
                <button type="button" onClick={addLine}
                  style={{ fontSize: 12, color: C.blue, border: `1px dashed ${C.blueBorder}`, borderRadius: 6, padding: '4px 12px', cursor: 'pointer', background: C.blueL, marginTop: 4 }}>
                  + Agregar línea
                </button>
              </div>

              {error   && <div style={errorBox}>{error}</div>}
              {success && <div style={successBox}>{success}</div>}

              <div style={{ marginTop: 18 }}>
                <button type="submit" disabled={loading}
                  style={{ padding: '9px 24px', background: meta.color, color: '#fff', border: 'none', borderRadius: 8, cursor: loading ? 'not-allowed' : 'pointer', fontWeight: 600, fontSize: 14, opacity: loading ? 0.7 : 1 }}>
                  {loading ? '⏳ Procesando…' : `✓ Crear & Firmar ${meta.short}`}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {showSdkModal && meta && (
        <Modal title={`Código SDK – ${meta.label}`} onClose={() => setShowSdkModal(false)} accentColor={meta.color}>
          <div style={{ marginBottom: 14 }}>
            <span style={cfeBadge(meta.color, meta.colorL)}>{meta.emoji} {meta.label}</span>
          </div>
          <p style={{ fontSize: 14, color: C.text, marginBottom: 14, lineHeight: 1.5 }}>{meta.desc}</p>
          <CodeBlock code={meta.sdkCode} />
          <div style={{ marginTop: 12, fontSize: 12, color: C.textMuted, background: C.slateL, borderRadius: 6, padding: 10, lineHeight: 1.5 }}>
            📋 <strong>DGI:</strong> {meta.dgiNote}
          </div>
        </Modal>
      )}
    </div>
  )
}

// ─── Demo Tab ──────────────────────────────────────────────────────────────────
const DEMO_ITEMS = [
  { nombreItem: 'Consultoría de software', cantidad: 8, precioUnitario: 2500, indFactIva: 3 },
  { nombreItem: 'Soporte técnico',         cantidad: 2, precioUnitario: 1200, indFactIva: 3 },
]

// orderState machine: idle → confirming-create → creating → created → confirming-cancel → cancelling → cancelled
function DemoTab({ onCreated }) {
  const [clientType, setClientType] = useState('consumer')
  const [bizRut,  setBizRut]  = useState('211234560010')
  const [bizName, setBizName] = useState('Empresa Ejemplo S.A.')
  const [orderState, setOrderState] = useState('idle')
  const [createdInvoice, setCreatedInvoice] = useState(null)
  const [cancelInvoice,  setCancelInvoice]  = useState(null)
  const [error, setError] = useState(null)

  const tipoCfe       = clientType === 'consumer' ? 101 : 111
  const cancelTipoCfe = clientType === 'consumer' ? 102 : 112
  const meta       = CFE_META[tipoCfe]
  const cancelMeta = CFE_META[cancelTipoCfe]

  async function createOrder() {
    setOrderState('creating'); setError(null)
    try {
      const body = {
        tipoCfe, numero: Math.floor(Math.random() * 9000) + 1000,
        rutReceptor:   clientType === 'business' ? bizRut  : null,
        nombreReceptor:clientType === 'business' ? bizName : null,
        detalle: DEMO_ITEMS,
      }
      const res = await fetch('/api/invoices', {
        method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body),
      })
      if (!res.ok) {
        const err = await res.json().catch(() => ({ title: res.statusText }))
        throw new Error(err.detail ?? err.title ?? 'Error al crear el comprobante')
      }
      setCreatedInvoice(await res.json())
      setOrderState('created')
      onCreated()
    } catch (e) { setError(e.message); setOrderState('idle') }
  }

  async function cancelOrder() {
    setOrderState('cancelling'); setError(null)
    try {
      const body = {
        tipoCfe: cancelTipoCfe,
        numero: Math.floor(Math.random() * 9000) + 1000,
        rutReceptor:   clientType === 'business' ? bizRut  : null,
        nombreReceptor:clientType === 'business' ? bizName : null,
        detalle: DEMO_ITEMS.map(l => ({ ...l })),
        referencias: [{
          tipoCfe,
          serie: 'A',
          nroCfe: createdInvoice.numero,
          fechaCfe: createdInvoice.fechaEmision,
          razon: 'Anulación de comprobante',
        }],
      }
      const res = await fetch('/api/invoices', {
        method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body),
      })
      if (!res.ok) {
        const err = await res.json().catch(() => ({ title: res.statusText }))
        throw new Error(err.detail ?? err.title ?? 'Error al cancelar')
      }
      setCancelInvoice(await res.json())
      setOrderState('cancelled')
      onCreated()
    } catch (e) { setError(e.message); setOrderState('created') }
  }

  function reset() { setOrderState('idle'); setCreatedInvoice(null); setCancelInvoice(null); setError(null) }

  const total = DEMO_ITEMS.reduce((s, l) => s + l.cantidad * l.precioUnitario, 0)
  const busy  = orderState === 'creating' || orderState === 'cancelling'

  return (
    <div>
      <h2 style={sectionTitle}>🎬 Modo Demo: Ciclo de vida de un Pedido</h2>
      <p style={{ color: C.textMuted, marginBottom: 20, maxWidth: 760, lineHeight: 1.6 }}>
        Simulá el flujo completo: emisión de un comprobante y su posterior cancelación.
        En cada paso verás exactamente qué tipo de CFE se emite, por qué lo exige DGI y el código SDK que se ejecuta.
      </p>

      {/* Timeline */}
      <DemoTimeline state={orderState} />

      <div style={{ display: 'flex', gap: 20, flexWrap: 'wrap', marginTop: 20, alignItems: 'flex-start' }}>
        {/* Left – order config */}
        <div style={{ flex: '0 0 310px', background: '#fff', border: `1px solid ${C.border}`, borderRadius: 12, padding: 20 }}>
          <h3 style={{ margin: '0 0 14px', fontSize: 15 }}>🛒 Pedido de ejemplo</h3>

          {/* Client type */}
          <div style={{ display: 'flex', gap: 8, marginBottom: 14 }}>
            {[['consumer', '👤 Consumidor', 101], ['business', '🏢 Empresa', 111]].map(([v, label, tipo]) => {
              const m = CFE_META[tipo]; const sel = clientType === v
              return (
                <button key={v} disabled={orderState !== 'idle'} onClick={() => setClientType(v)}
                  style={{
                    flex: 1, padding: '8px', border: `2px solid ${sel ? m.color : C.border}`,
                    borderRadius: 8, cursor: orderState === 'idle' ? 'pointer' : 'default',
                    background: sel ? m.colorL : '#fff', color: sel ? m.color : C.text,
                    fontSize: 12, fontWeight: sel ? 700 : 400, transition: 'all 0.15s',
                    opacity: orderState !== 'idle' ? 0.7 : 1,
                  }}>
                  {label}
                </button>
              )
            })}
          </div>

          {clientType === 'business' && orderState === 'idle' && (
            <div style={{ marginBottom: 12 }}>
              <label style={lbl}>RUT Receptor</label>
              <input value={bizRut}  onChange={e => setBizRut(e.target.value)}  style={inp} />
              <label style={lbl}>Razón Social</label>
              <input value={bizName} onChange={e => setBizName(e.target.value)} style={inp} />
            </div>
          )}

          {/* Items */}
          <table style={{ width: '100%', fontSize: 13, borderCollapse: 'collapse', marginBottom: 12 }}>
            <thead><tr style={{ background: C.bg }}>
              <th style={th}>Ítem</th><th style={th}>Cant.</th><th style={{ ...th, textAlign: 'right' }}>Subtotal</th>
            </tr></thead>
            <tbody>
              {DEMO_ITEMS.map((l, i) => (
                <tr key={i} style={{ borderBottom: `1px solid ${C.border}` }}>
                  <td style={td}>{l.nombreItem}</td>
                  <td style={{ ...td, textAlign: 'center' }}>{l.cantidad}</td>
                  <td style={{ ...td, textAlign: 'right', fontWeight: 500 }}>${(l.cantidad * l.precioUnitario).toLocaleString()}</td>
                </tr>
              ))}
            </tbody>
            <tfoot><tr>
              <td colSpan={2} style={{ ...td, fontWeight: 600, textAlign: 'right', paddingTop: 8 }}>Total:</td>
              <td style={{ ...td, fontWeight: 700, textAlign: 'right', paddingTop: 8 }}>${total.toLocaleString()}</td>
            </tr></tfoot>
          </table>

          <div style={{ fontSize: 12, color: C.textMuted, marginBottom: 12 }}>
            Se emitirá: <span style={cfeBadge(meta.color, meta.colorL)}>{meta.emoji} {meta.short}</span>
          </div>

          {error && <div style={errorBox}>{error}</div>}

          {orderState === 'idle' && (
            <button onClick={() => setOrderState('confirming-create')} style={{ ...primaryBtn(meta.color), width: '100%' }}>
              📤 Emitir comprobante
            </button>
          )}
          {busy && (
            <div style={{ textAlign: 'center', padding: 10, color: C.textMuted, fontSize: 13 }}>⏳ Procesando…</div>
          )}
          {orderState === 'created' && (
            <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
              <div style={successBox}>✅ Nº {createdInvoice?.numero} emitido correctamente</div>
              <button onClick={() => setOrderState('confirming-cancel')}
                style={{ padding: '8px', border: `1px solid ${C.redBorder}`, borderRadius: 8, cursor: 'pointer', background: C.redL, color: C.red, fontWeight: 600, fontSize: 13 }}>
                🗑️ Cancelar pedido
              </button>
              <button onClick={reset}
                style={{ padding: '7px', border: `1px solid ${C.border}`, borderRadius: 8, cursor: 'pointer', background: '#fff', fontSize: 12, color: C.textMuted }}>
                ↺ Nuevo pedido
              </button>
            </div>
          )}
          {orderState === 'cancelled' && (
            <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
              <div style={{ ...errorBox, background: '#fff7ed', borderColor: '#fed7aa', color: '#c2410c' }}>
                ↩️ Pedido cancelado – NC Nº {cancelInvoice?.numero}
              </div>
              <button onClick={reset}
                style={{ padding: '7px', border: `1px solid ${C.border}`, borderRadius: 8, cursor: 'pointer', background: '#fff', fontSize: 12, color: C.textMuted }}>
                ↺ Nuevo pedido
              </button>
            </div>
          )}
        </div>

        {/* Right – explanation */}
        <div style={{ flex: 1, minWidth: 280 }}>
          <DemoExplanation
            state={orderState} meta={meta} cancelMeta={cancelMeta}
            cancelTipoCfe={cancelTipoCfe}
            invoice={createdInvoice} cancelInvoice={cancelInvoice}
          />
        </div>
      </div>

      {/* Confirm-create modal */}
      {orderState === 'confirming-create' && (
        <Modal title="📤 Emitir Comprobante" onClose={() => setOrderState('idle')} accentColor={meta.color}>
          <p style={{ fontSize: 14, color: C.textMuted, margin: '0 0 12px' }}>Se va a crear y firmar digitalmente:</p>
          <span style={{ ...cfeBadge(meta.color, meta.colorL), fontSize: 15, padding: '6px 14px' }}>
            {meta.emoji} {meta.label} (Tipo {tipoCfe})
          </span>
          <p style={{ marginTop: 14, fontSize: 14, lineHeight: 1.6 }}>{meta.desc}</p>
          <div style={{ fontSize: 12, fontWeight: 600, color: C.textMuted, margin: '14px 0 6px' }}>Código SDK que se ejecutará:</div>
          <CodeBlock code={meta.sdkCode} />
          <div style={{ marginTop: 12, fontSize: 12, color: C.textMuted, background: C.slateL, borderRadius: 6, padding: 10, lineHeight: 1.5 }}>
            📋 <strong>DGI:</strong> {meta.dgiNote}
          </div>
          <div style={{ marginTop: 18, display: 'flex', gap: 10, justifyContent: 'flex-end' }}>
            <button onClick={() => setOrderState('idle')}
              style={{ padding: '8px 20px', border: `1px solid ${C.border}`, borderRadius: 8, cursor: 'pointer', background: '#fff', fontSize: 13 }}>
              Cancelar
            </button>
            <button onClick={createOrder} style={primaryBtn(meta.color)}>
              ✓ Confirmar y firmar
            </button>
          </div>
        </Modal>
      )}

      {/* Confirm-cancel modal */}
      {orderState === 'confirming-cancel' && (
        <Modal title="🗑️ Cancelar Pedido" onClose={() => setOrderState('created')} accentColor={C.red}>
          <p style={{ fontSize: 14, color: C.textMuted, margin: '0 0 12px' }}>
            Para anular el comprobante Nº {createdInvoice?.numero} se emitirá:
          </p>
          <span style={{ ...cfeBadge(cancelMeta.color, cancelMeta.colorL), fontSize: 15, padding: '6px 14px' }}>
            {cancelMeta.emoji} {cancelMeta.label} (Tipo {cancelTipoCfe})
          </span>
          <p style={{ marginTop: 14, fontSize: 14, lineHeight: 1.6 }}>{cancelMeta.desc}</p>
          <div style={{ fontSize: 12, fontWeight: 600, color: C.textMuted, margin: '14px 0 6px' }}>Código SDK que se ejecutará:</div>
          <CodeBlock code={cancelMeta.sdkCode} />
          <div style={{ marginTop: 12, fontSize: 12, color: C.textMuted, background: C.slateL, borderRadius: 6, padding: 10, lineHeight: 1.5 }}>
            📋 <strong>DGI:</strong> {cancelMeta.dgiNote}
          </div>
          <div style={{ marginTop: 18, display: 'flex', gap: 10, justifyContent: 'flex-end' }}>
            <button onClick={() => setOrderState('created')}
              style={{ padding: '8px 20px', border: `1px solid ${C.border}`, borderRadius: 8, cursor: 'pointer', background: '#fff', fontSize: 13 }}>
              Volver
            </button>
            <button onClick={cancelOrder} style={{ ...primaryBtn(C.red), background: C.red }}>
              🗑️ Confirmar cancelación
            </button>
          </div>
        </Modal>
      )}
    </div>
  )
}

function DemoTimeline({ state }) {
  const steps = [
    { label: 'Pedido listo',          icon: '🛒' },
    { label: 'Comprobante emitido',   icon: '📄' },
    { label: 'Pedido cancelado',      icon: '↩️' },
  ]
  const idx = ['idle', 'confirming-create', 'creating'].includes(state) ? 0
    : ['created', 'confirming-cancel', 'cancelling'].includes(state) ? 1
    : 2

  return (
    <div style={{ display: 'flex', alignItems: 'center' }}>
      {steps.map((s, i) => (
        <div key={i} style={{ display: 'flex', alignItems: 'center', flex: i < 2 ? 1 : 'initial' }}>
          <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 4 }}>
            <div style={{
              width: 38, height: 38, borderRadius: '50%', display: 'flex', alignItems: 'center', justifyContent: 'center',
              fontSize: 18, transition: 'all 0.3s',
              background: idx >= i ? (i === 2 ? C.redL   : C.greenL)  : C.bg,
              border:     `2px solid ${idx >= i ? (i === 2 ? C.red    : C.green)   : C.border}`,
            }}>{s.icon}</div>
            <div style={{ fontSize: 11, whiteSpace: 'nowrap', fontWeight: idx >= i ? 600 : 400, color: idx >= i ? C.text : C.textMuted }}>
              {s.label}
            </div>
          </div>
          {i < 2 && (
            <div style={{ flex: 1, height: 2, margin: '0 6px', marginBottom: 18, background: idx > i ? C.green : C.border, transition: 'background 0.3s' }} />
          )}
        </div>
      ))}
    </div>
  )
}

// Maps a numeric TipoCfe value to its C# enum member name for use in code snippets.
function tipoCfeEnumName(value) {
  const labels = {
    101: 'ETicket', 102: 'NotaCreditoETicket', 103: 'NotaDebitoETicket',
    111: 'EFactura', 112: 'NotaCreditoEFactura', 113: 'NotaDebitoEFactura',
    121: 'EFacturaExportacion', 181: 'ERemito',
  }
  return labels[value] ?? 'ETicket'
}

function DemoExplanation({ state, meta, cancelMeta, cancelTipoCfe, invoice, cancelInvoice }) {
  const card = { background: '#fff', border: `1px solid ${C.border}`, borderRadius: 12, padding: 20 }

  if (state === 'idle' || state === 'confirming-create') return (
    <div style={card}>
      <h3 style={{ margin: '0 0 16px', fontSize: 15 }}>¿Cómo funciona el ciclo?</h3>
      <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
        <ExplainStep n={1} title={`Emitir: ${meta.label}`} color={meta.color}>
          <p style={{ margin: 0, fontSize: 13, lineHeight: 1.5 }}>
            Se instancia el CFE con <code style={codeInline}>{meta.sdkMethod}</code>,
            se agregan las líneas de detalle y se llama a <code style={codeInline}>GenerarYFirmar()</code>
            para producir el XML firmado con el certificado <code style={codeInline}>.pfx</code>.
          </p>
        </ExplainStep>
        <ExplainStep n={2} title="Almacenar el XML" color={C.blue}>
          <p style={{ margin: 0, fontSize: 13, lineHeight: 1.5 }}>
            El XML firmado se persiste en la base de datos junto con los metadatos del comprobante.
            Una vez firmado, <strong>no se puede modificar</strong> sin invalidar la firma.
          </p>
        </ExplainStep>
        <ExplainStep n={3} title={`Cancelar: ${cancelMeta.label}`} color={C.red}>
          <p style={{ margin: 0, fontSize: 13, lineHeight: 1.5 }}>
            Para anular se emite una <strong>{cancelMeta.label} (Tipo {cancelTipoCfe})</strong>
            &nbsp;referenciando el número y fecha del CFE original.
            DGI no permite "borrar" comprobantes ya emitidos.
          </p>
        </ExplainStep>
      </div>
    </div>
  )

  if (state === 'creating') return (
    <div style={{ ...card, textAlign: 'center', padding: 40 }}>
      <div style={{ fontSize: 48, marginBottom: 8 }}>⏳</div>
      <p style={{ color: C.textMuted, fontSize: 14 }}>Ejecutando <code style={codeInline}>GenerarYFirmar()</code>…</p>
    </div>
  )

  if (state === 'created' || state === 'confirming-cancel') return (
    <div style={card}>
      <h3 style={{ margin: '0 0 14px', fontSize: 15, color: C.green }}>✅ CFE emitido correctamente</h3>
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 8, marginBottom: 16 }}>
        <InfoTile label="Tipo"  value={<span style={cfeBadge(meta.color, meta.colorL)}>{meta.short}</span>} />
        <InfoTile label="Nº"    value={invoice?.numero} />
        <InfoTile label="Fecha" value={invoice?.fechaEmision?.slice(0, 10)} />
        <InfoTile label="Total" value={`$${invoice?.montoTotal?.toFixed(2)}`} />
      </div>
      <div style={{ fontSize: 12, color: C.textMuted, background: C.slateL, borderRadius: 6, padding: 10, lineHeight: 1.5, marginBottom: 12 }}>
        El XML firmado quedó almacenado. Para anularlo DGI exige emitir una
        &nbsp;<strong>{cancelMeta.label}</strong> referenciando este CFE.
      </div>
      <CodeBlock code={`// Anulación:\nvar nc = ${cancelMeta.sdkMethod};\nnc.Referencias.Add(new RefCfe {\n    TipoCfe = TipoCfe.${tipoCfeEnumName(invoice?.tipoCfe)},\n    NroCfe  = ${invoice?.numero},\n    Razon   = "Anulación de comprobante",\n});\nclient.GenerarYFirmar(nc);`} />
    </div>
  )

  if (state === 'cancelling') return (
    <div style={{ ...card, textAlign: 'center', padding: 40 }}>
      <div style={{ fontSize: 48, marginBottom: 8 }}>⏳</div>
      <p style={{ color: C.textMuted, fontSize: 14 }}>Emitiendo {cancelMeta.label}…</p>
    </div>
  )

  if (state === 'cancelled') return (
    <div style={card}>
      <h3 style={{ margin: '0 0 14px', fontSize: 15, color: '#c2410c' }}>↩️ Pedido cancelado con {cancelMeta.label}</h3>
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 8, marginBottom: 14 }}>
        <InfoTile label="CFE original" value={<span style={cfeBadge(meta.color, meta.colorL)}>{meta.short}</span>} />
        <InfoTile label="Nº original"  value={invoice?.numero} />
        <InfoTile label="NC emitida"   value={<span style={cfeBadge(cancelMeta.color, cancelMeta.colorL)}>{cancelMeta.short}</span>} />
        <InfoTile label="Nº NC"        value={cancelInvoice?.numero} />
      </div>
      <div style={{ fontSize: 12, color: '#92400e', background: '#fff7ed', border: '1px solid #fed7aa', borderRadius: 6, padding: 10, lineHeight: 1.5 }}>
        📋 Ambos comprobantes quedan registrados. La NC compensa fiscalmente al CFE original ante DGI.
        Podés verlos en el <strong>Historial</strong>.
      </div>
    </div>
  )

  return null
}

// ─── Historial Tab ─────────────────────────────────────────────────────────────
function HistorialTab({ invoices, onReload }) {
  async function downloadPdf(invoice) {
    const res = await fetch(`/api/invoices/${invoice.id}/pdf`)
    if (!res.ok) { alert('No se pudo generar el PDF'); return }
    const blob = await res.blob()
    const url  = URL.createObjectURL(blob)
    const a    = document.createElement('a')
    a.href = url; a.download = `cfe-${invoice.numero}.pdf`; a.click()
    URL.revokeObjectURL(url)
  }

  const tipoCfeLabel = v => CFE_META[v]?.label ?? String(v)
  const tipoCfeMeta  = v => CFE_META[v] ?? { color: C.slate, colorL: C.slateL, emoji: '📄' }

  return (
    <div>
      <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 20 }}>
        <h2 style={{ ...sectionTitle, marginBottom: 0 }}>Historial de comprobantes</h2>
        <span style={{ fontSize: 13, color: C.textMuted, background: C.bg, border: `1px solid ${C.border}`, borderRadius: 20, padding: '2px 10px' }}>
          {invoices.length}
        </span>
        <button onClick={onReload}
          style={{ marginLeft: 'auto', fontSize: 12, color: C.blue, border: `1px solid ${C.blueBorder}`, borderRadius: 6, padding: '4px 12px', cursor: 'pointer', background: C.blueL }}>
          ↻ Actualizar
        </button>
      </div>

      {invoices.length === 0
        ? <div style={{ textAlign: 'center', padding: 60, color: C.textMuted, background: '#fff', borderRadius: 12, border: `1px solid ${C.border}` }}>
            Sin comprobantes aún.
          </div>
        : (
          <div style={{ background: '#fff', border: `1px solid ${C.border}`, borderRadius: 12, overflow: 'hidden' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 13 }}>
              <thead>
                <tr style={{ background: C.bg, borderBottom: `1px solid ${C.border}` }}>
                  {['#', 'Tipo', 'Nº', 'Fecha', 'Receptor', 'Total', 'DGI', ''].map(h =>
                    <th key={h} style={{ ...th, padding: '10px 12px' }}>{h}</th>)}
                </tr>
              </thead>
              <tbody>
                {invoices.map(inv => {
                  const m = tipoCfeMeta(inv.tipoCfe)
                  return (
                    <tr key={inv.id} style={{ borderBottom: `1px solid ${C.border}` }}>
                      <td style={{ ...td, color: C.textMuted, padding: '10px 12px' }}>{inv.id}</td>
                      <td style={{ ...td, padding: '10px 12px' }}>
                        <span style={cfeBadge(m.color, m.colorL)}>{m.emoji} {tipoCfeLabel(inv.tipoCfe)}</span>
                      </td>
                      <td style={{ ...td, fontWeight: 600, padding: '10px 12px' }}>{inv.numero}</td>
                      <td style={{ ...td, color: C.textMuted, padding: '10px 12px' }}>{inv.fechaEmision?.slice(0, 10)}</td>
                      <td style={{ ...td, padding: '10px 12px' }}>{inv.nombreReceptor ?? <span style={{ color: C.textMuted }}>Consumidor</span>}</td>
                      <td style={{ ...td, fontWeight: 600, padding: '10px 12px' }}>${inv.montoTotal.toFixed(2)}</td>
                      <td style={{ ...td, textAlign: 'center', padding: '10px 12px' }}>
                        {inv.aceptadoPorDgi ? '✅' : <span style={{ color: C.textMuted }}>—</span>}
                      </td>
                      <td style={{ ...td, padding: '10px 12px' }}>
                        <button onClick={() => downloadPdf(inv)}
                          style={{ fontSize: 12, padding: '3px 10px', border: `1px solid ${C.blueBorder}`, borderRadius: 6, cursor: 'pointer', background: C.blueL, color: C.blue }}>
                          📄 PDF
                        </button>
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        )}
    </div>
  )
}

// ─── Shared Components ─────────────────────────────────────────────────────────
function Modal({ title, onClose, accentColor, children }) {
  return (
    <div className="uf-overlay"
      style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.45)', zIndex: 1000, display: 'flex', alignItems: 'center', justifyContent: 'center', padding: 24, backdropFilter: 'blur(3px)' }}
      onClick={e => { if (e.target === e.currentTarget) onClose() }}>
      <div className="uf-modal"
        style={{ background: '#fff', borderRadius: 16, maxWidth: 600, width: '100%', boxShadow: '0 20px 60px rgba(0,0,0,0.25)', maxHeight: '90vh', overflowY: 'auto' }}>
        <div style={{ padding: '16px 20px', borderBottom: `1px solid ${C.border}`, display: 'flex', alignItems: 'center', justifyContent: 'space-between', background: `${accentColor}11`, borderRadius: '16px 16px 0 0', position: 'sticky', top: 0, zIndex: 1 }}>
          <strong style={{ fontSize: 15, color: accentColor }}>{title}</strong>
          <button onClick={onClose}
            style={{ border: 'none', background: 'none', cursor: 'pointer', fontSize: 22, color: C.textMuted, lineHeight: 1, padding: '0 4px' }}>
            ×
          </button>
        </div>
        <div style={{ padding: 20 }}>{children}</div>
      </div>
    </div>
  )
}

function CodeBlock({ code }) {
  return (
    <pre style={{
      background: '#0f172a', color: '#e2e8f0', borderRadius: 8,
      padding: '12px 16px', fontSize: 12, overflowX: 'auto', margin: 0,
      fontFamily: '"Fira Code","Cascadia Code","Consolas",monospace', lineHeight: 1.65,
    }}>{code}</pre>
  )
}

function ExplainStep({ n, title, color, children }) {
  return (
    <div style={{ display: 'flex', gap: 12 }}>
      <div style={{
        width: 28, height: 28, borderRadius: '50%', background: color + '22',
        border: `2px solid ${color}`, color, fontWeight: 700, fontSize: 13,
        display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0, marginTop: 1,
      }}>{n}</div>
      <div style={{ flex: 1 }}>
        <div style={{ fontSize: 13, fontWeight: 600, color, marginBottom: 5 }}>{title}</div>
        {children}
      </div>
    </div>
  )
}

function InfoTile({ label, value }) {
  return (
    <div style={{ background: C.slateL, borderRadius: 6, padding: '7px 10px' }}>
      <div style={{ fontSize: 11, color: C.textMuted, marginBottom: 2 }}>{label}</div>
      <div style={{ fontSize: 13, fontWeight: 600 }}>{value}</div>
    </div>
  )
}

function PillRow({ label, value, valueColor }) {
  return (
    <div style={{ display: 'flex', justifyContent: 'space-between', gap: 8, padding: '4px 0', borderBottom: `1px solid ${C.border}` }}>
      <span style={{ color: C.textMuted }}>{label}</span>
      <span style={{ fontWeight: 600, color: valueColor }}>{value}</span>
    </div>
  )
}

// ─── Style Helpers ─────────────────────────────────────────────────────────────
const sectionTitle  = { fontSize: 20, fontWeight: 700, marginBottom: 8, color: C.text, marginTop: 0 }
const lbl           = { display: 'block', fontSize: 12, fontWeight: 600, marginTop: 8, marginBottom: 2, color: C.textMuted }
const inp           = { display: 'block', padding: '7px 10px', border: `1px solid ${C.border}`, borderRadius: 8, fontSize: 14, width: '100%', boxSizing: 'border-box' }
const th            = { padding: '8px 10px', textAlign: 'left', fontWeight: 600, fontSize: 12, color: C.textMuted }
const td            = { padding: '8px 10px', fontSize: 13 }
const errorBox      = { marginTop: 10, padding: '8px 12px', background: C.redL,   border: `1px solid ${C.redBorder}`,   borderRadius: 8, fontSize: 13, color: C.red }
const successBox    = { marginTop: 10, padding: '8px 12px', background: C.greenL, border: `1px solid ${C.greenBorder}`, borderRadius: 8, fontSize: 13, color: C.green }
const codeInline    = { background: C.slateL, border: `1px solid ${C.slateBorder}`, borderRadius: 4, padding: '1px 5px', fontSize: 12, fontFamily: 'monospace' }
const cfeBadge      = (color, bg) => ({ display: 'inline-flex', alignItems: 'center', gap: 4, background: bg ?? color + '18', color, border: `1px solid ${color}44`, borderRadius: 20, padding: '2px 10px', fontSize: 12, fontWeight: 600 })
const primaryBtn    = color => ({ padding: '9px 20px', background: color, color: '#fff', border: 'none', borderRadius: 8, cursor: 'pointer', fontWeight: 600, fontSize: 14 })

function injectStyles() {
  if (document.getElementById('uf-styles')) return
  const s = document.createElement('style')
  s.id = 'uf-styles'
  s.textContent = `
    * { box-sizing: border-box; }
    body { margin: 0; }
    .uf-modal    { animation: uf-up   0.2s  cubic-bezier(0.16,1,0.3,1); }
    .uf-overlay  { animation: uf-fade 0.15s ease; }
    .uf-dropdown { animation: uf-up   0.15s cubic-bezier(0.16,1,0.3,1); }
    @keyframes uf-up   { from { opacity:0; transform:translateY(14px); } to { opacity:1; transform:translateY(0); } }
    @keyframes uf-fade { from { opacity:0; } to { opacity:1; } }
    input:focus, select:focus { border-color: #2563eb !important; box-shadow: 0 0 0 3px #2563eb22; outline: none; }
    button:not(:disabled):hover { filter: brightness(0.94); }
  `
  document.head.appendChild(s)
}
