import { useState } from 'react'
import { CFE_META, tipoCfeEnumName } from '../constants/cfeMeta.js'
import { C, sectionTitle, lbl, inp, errorBox, successBox, cfeBadge, primaryBtn } from '../constants/styles.js'
import Modal from '../components/Modal.jsx'
import CodeBlock from '../components/CodeBlock.jsx'
import ExplainStep from '../components/ExplainStep.jsx'
import InfoTile from '../components/InfoTile.jsx'
import { useApi } from '../context/ApiContext.jsx'

// ─── Demo Items ────────────────────────────────────────────────────────────────
const DEMO_ITEMS = [
  { nombreItem: 'Consultoría de software', cantidad: 8, precioUnitario: 2500, indFactIva: 3 },
  { nombreItem: 'Soporte técnico',         cantidad: 2, precioUnitario: 1200, indFactIva: 3 },
]

// ─── Timeline ──────────────────────────────────────────────────────────────────
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

// ─── Explanation Panel ─────────────────────────────────────────────────────────
function DemoExplanation({ state, meta, cancelMeta, cancelTipoCfe, invoice, cancelInvoice }) {
  const card = { background: '#fff', border: `1px solid ${C.border}`, borderRadius: 12, padding: 20 }
  const codeInlineStyle = { background: C.slateL, border: `1px solid ${C.slateBorder}`, borderRadius: 4, padding: '1px 5px', fontSize: 12, fontFamily: 'monospace' }

  if (state === 'idle' || state === 'confirming-create') return (
    <div style={card}>
      <h3 style={{ margin: '0 0 16px', fontSize: 15 }}>¿Cómo funciona el ciclo?</h3>
      <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
        <ExplainStep n={1} title={`Emitir: ${meta.label}`} color={meta.color}>
          <p style={{ margin: 0, fontSize: 13, lineHeight: 1.5 }}>
            Se instancia el CFE con <code style={codeInlineStyle}>{meta.sdkMethod}</code>,
            se agregan las líneas de detalle y se llama a <code style={codeInlineStyle}>GenerarYFirmar()</code>
            para producir el XML firmado con el certificado <code style={codeInlineStyle}>.pfx</code>.
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
      <p style={{ color: C.textMuted, fontSize: 14 }}>Ejecutando <code style={codeInlineStyle}>GenerarYFirmar()</code>…</p>
    </div>
  )

  const codeInlineStyle2 = { background: C.slateL, border: `1px solid ${C.slateBorder}`, borderRadius: 4, padding: '1px 5px', fontSize: 12, fontFamily: 'monospace' }

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

// ─── Demo Tab ──────────────────────────────────────────────────────────────────
// orderState machine: idle → confirming-create → creating → created → confirming-cancel → cancelling → cancelled
export default function DemoTab({ onCreated }) {
  const apiFetch = useApi()
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
        rutReceptor:    clientType === 'business' ? bizRut  : null,
        nombreReceptor: clientType === 'business' ? bizName : null,
        detalle: DEMO_ITEMS,
      }
      const res = await apiFetch('/api/invoices', {
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
        rutReceptor:    clientType === 'business' ? bizRut  : null,
        nombreReceptor: clientType === 'business' ? bizName : null,
        detalle: DEMO_ITEMS.map(l => ({ ...l })),
        referencias: [{
          tipoCfe,
          serie: 'A',
          nroCfe: createdInvoice.numero,
          fechaCfe: createdInvoice.fechaEmision,
          razon: 'Anulación de comprobante',
        }],
      }
      const res = await apiFetch('/api/invoices', {
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
              <th style={{ padding: '8px 10px', textAlign: 'left', fontWeight: 600, fontSize: 12, color: C.textMuted }}>Ítem</th>
              <th style={{ padding: '8px 10px', textAlign: 'left', fontWeight: 600, fontSize: 12, color: C.textMuted }}>Cant.</th>
              <th style={{ padding: '8px 10px', textAlign: 'right', fontWeight: 600, fontSize: 12, color: C.textMuted }}>Subtotal</th>
            </tr></thead>
            <tbody>
              {DEMO_ITEMS.map((l, i) => (
                <tr key={i} style={{ borderBottom: `1px solid ${C.border}` }}>
                  <td style={{ padding: '8px 10px', fontSize: 13 }}>{l.nombreItem}</td>
                  <td style={{ padding: '8px 10px', fontSize: 13, textAlign: 'center' }}>{l.cantidad}</td>
                  <td style={{ padding: '8px 10px', fontSize: 13, textAlign: 'right', fontWeight: 500 }}>${(l.cantidad * l.precioUnitario).toLocaleString()}</td>
                </tr>
              ))}
            </tbody>
            <tfoot><tr>
              <td colSpan={2} style={{ padding: '8px 10px', fontSize: 13, fontWeight: 600, textAlign: 'right', paddingTop: 8 }}>Total:</td>
              <td style={{ padding: '8px 10px', fontSize: 13, fontWeight: 700, textAlign: 'right', paddingTop: 8 }}>${total.toLocaleString()}</td>
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
