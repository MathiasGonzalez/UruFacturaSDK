import { CFE_META } from '../constants/cfeMeta.js'
import { C, sectionTitle, th, td } from '../constants/styles.js'

export default function HistorialTab({ invoices, onReload }) {
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
  const cfeBadge     = (color, bg) => ({ display: 'inline-flex', alignItems: 'center', gap: 4, background: bg ?? color + '18', color, border: `1px solid ${color}44`, borderRadius: 20, padding: '2px 10px', fontSize: 12, fontWeight: 600 })

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
