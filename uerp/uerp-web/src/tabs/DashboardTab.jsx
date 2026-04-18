import { C, sectionTitle } from '../constants/styles.js'
import { CFE_META } from '../constants/cfeMeta.js'

function StatCard({ label, value, sub, color }) {
  return (
    <div style={{
      background: '#fff', border: `1px solid ${C.border}`, borderRadius: 12,
      padding: '20px 22px', flex: '1 1 160px',
    }}>
      <div style={{ fontSize: 12, fontWeight: 600, color: C.textMuted, textTransform: 'uppercase', letterSpacing: '0.06em', marginBottom: 8 }}>
        {label}
      </div>
      <div style={{ fontSize: 26, fontWeight: 700, color: color ?? C.text, lineHeight: 1 }}>
        {value ?? '—'}
      </div>
      {sub && <div style={{ fontSize: 12, color: C.textMuted, marginTop: 5 }}>{sub}</div>}
    </div>
  )
}

export default function DashboardTab({ dashboard }) {
  if (!dashboard) {
    return (
      <div style={{ textAlign: 'center', padding: 60, color: C.textMuted }}>
        ⏳ Cargando dashboard…
      </div>
    )
  }

  const fmt = n => n?.toLocaleString('es-UY', { minimumFractionDigits: 2, maximumFractionDigits: 2 })

  return (
    <div>
      <h2 style={sectionTitle}>📊 Dashboard</h2>
      <p style={{ color: C.textMuted, marginBottom: 24 }}>
        Resumen de actividad de tu cuenta.
      </p>

      {/* KPI row */}
      <div style={{ display: 'flex', gap: 16, flexWrap: 'wrap', marginBottom: 24 }}>
        <StatCard label="Total comprobantes" value={dashboard.totalInvoices} color={C.blue} />
        <StatCard label="Ingresos totales"   value={`$${fmt(dashboard.totalRevenue)}`} color={C.green} />
        <StatCard label="Últimos 7 días"      value={dashboard.weekInvoices} sub="comprobantes emitidos" />
        <StatCard label="Ingresos (30 días)"  value={`$${fmt(dashboard.monthRevenue)}`} color={C.amber} />
        <StatCard label="Aceptados por DGI"   value={dashboard.acceptedByDgi} color={C.green} sub="comprobantes confirmados" />
      </div>

      {/* By type */}
      {dashboard.byType?.length > 0 && (
        <div style={{ background: '#fff', border: `1px solid ${C.border}`, borderRadius: 12, overflow: 'hidden' }}>
          <div style={{ padding: '14px 18px', borderBottom: `1px solid ${C.border}`, fontWeight: 600, fontSize: 14 }}>
            Comprobantes por tipo
          </div>
          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 13 }}>
            <thead>
              <tr style={{ background: C.bg }}>
                {['Tipo', 'Cantidad', 'Total'].map(h =>
                  <th key={h} style={{ padding: '10px 18px', textAlign: 'left', fontWeight: 600, fontSize: 12, color: C.textMuted }}>
                    {h}
                  </th>
                )}
              </tr>
            </thead>
            <tbody>
              {dashboard.byType.map(row => {
                const meta = CFE_META[row.tipoCfe]
                return (
                  <tr key={row.tipoCfe} style={{ borderTop: `1px solid ${C.border}` }}>
                    <td style={{ padding: '10px 18px' }}>
                      {meta
                        ? <span style={{ display: 'inline-flex', alignItems: 'center', gap: 6 }}>
                            <span>{meta.emoji}</span>
                            <span style={{ fontWeight: 500 }}>{meta.label}</span>
                            <span style={{ color: C.textMuted, fontSize: 11 }}>(Tipo {row.tipoCfe})</span>
                          </span>
                        : `Tipo ${row.tipoCfe}`}
                    </td>
                    <td style={{ padding: '10px 18px', fontWeight: 600 }}>{row.count}</td>
                    <td style={{ padding: '10px 18px', fontWeight: 600, color: C.green }}>
                      ${fmt(row.total)}
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}

      {dashboard.totalInvoices === 0 && (
        <div style={{ textAlign: 'center', padding: 48, color: C.textMuted, background: '#fff', borderRadius: 12, border: `1px solid ${C.border}` }}>
          <div style={{ fontSize: 36, marginBottom: 10 }}>🧾</div>
          <p style={{ margin: 0 }}>Aún no hay comprobantes emitidos.</p>
          <p style={{ margin: '6px 0 0', fontSize: 13 }}>Usá la pestaña <strong>Crear CFE</strong> para emitir tu primer comprobante.</p>
        </div>
      )}
    </div>
  )
}
