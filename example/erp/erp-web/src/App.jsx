import { useState, useEffect } from 'react'

const TIPO_IVA = { 1: 'Exento', 2: 'IVA Mínimo 10%', 3: 'IVA Básico 22%' }

const defaultLine = () => ({ nombreItem: '', cantidad: 1, precioUnitario: 0, indFactIva: 3 })

const defaultForm = () => ({
  tipoCfe: 101,
  numero: 1,
  rutReceptor: '',
  nombreReceptor: '',
  detalle: [defaultLine()],
})

// Status badge colours
const statusColor = { ok: '#16a34a', warn: '#d97706', error: '#dc2626' }

export default function App() {
  const [cfeTypes, setCfeTypes] = useState([])
  const [configStatus, setConfigStatus] = useState(null)
  const [invoices, setInvoices] = useState([])
  const [form, setForm] = useState(defaultForm())
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)

  useEffect(() => {
    load()
    fetchCfeTypes()
    fetchConfigStatus()
  }, [])

  async function load() {
    const res = await fetch('/api/invoices')
    setInvoices(await res.json())
  }

  async function fetchCfeTypes() {
    const res = await fetch('/api/cfe-types')
    setCfeTypes(await res.json())
  }

  async function fetchConfigStatus() {
    const res = await fetch('/api/config/status')
    setConfigStatus(await res.json())
  }

  function setField(key, value) {
    setForm(f => ({ ...f, [key]: value }))
  }

  function setLine(i, key, value) {
    setForm(f => {
      const detalle = [...f.detalle]
      detalle[i] = { ...detalle[i], [key]: value }
      return { ...f, detalle }
    })
  }

  function addLine() {
    setForm(f => ({ ...f, detalle: [...f.detalle, defaultLine()] }))
  }

  function removeLine(i) {
    setForm(f => ({ ...f, detalle: f.detalle.filter((_, idx) => idx !== i) }))
  }

  async function submit(e) {
    e.preventDefault()
    setLoading(true)
    setError(null)
    try {
      const body = {
        tipoCfe: Number(form.tipoCfe),
        numero: Number(form.numero),
        rutReceptor: form.rutReceptor || null,
        nombreReceptor: form.nombreReceptor || null,
        detalle: form.detalle.map(l => ({
          nombreItem: l.nombreItem,
          cantidad: Number(l.cantidad),
          precioUnitario: Number(l.precioUnitario),
          indFactIva: Number(l.indFactIva),
        })),
      }
      const res = await fetch('/api/invoices', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
      })
      if (!res.ok) {
        const err = await res.json().catch(() => ({ title: res.statusText }))
        throw new Error(err.detail ?? err.title ?? 'Error desconocido')
      }
      setForm(defaultForm())
      await load()
    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  async function downloadPdf(invoice) {
    const res = await fetch(`/api/invoices/${invoice.id}/pdf`)
    if (!res.ok) { alert('No se pudo generar el PDF'); return }
    const blob = await res.blob()
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `factura-${invoice.numero}.pdf`
    a.click()
    URL.revokeObjectURL(url)
  }

  // Derive label from cfeTypes list (populated from API)
  function tipoCfeLabel(value) {
    return cfeTypes.find(t => t.value === value)?.label ?? String(value)
  }

  return (
    <div style={{ fontFamily: 'system-ui, sans-serif', maxWidth: 1160, margin: '0 auto', padding: 24 }}>
      <h1 style={{ marginBottom: 16 }}>ERP Demo – UruFactura SDK</h1>

      {/* ── Config status banner ── */}
      <ConfigStatusBanner status={configStatus} onRefresh={fetchConfigStatus} />

      <div style={{ display: 'flex', gap: 32, alignItems: 'flex-start', flexWrap: 'wrap', marginTop: 24 }}>
        {/* ── Form ── */}
        <form onSubmit={submit} style={{ minWidth: 400, flex: '0 0 auto' }}>
          <h2>Nueva Factura</h2>

          <label style={lbl}>Tipo CFE</label>
          <select value={form.tipoCfe} onChange={e => setField('tipoCfe', e.target.value)} style={inp}>
            {cfeTypes.map(t => (
              <option key={t.value} value={t.value}>{t.label} ({t.value})</option>
            ))}
          </select>

          <label style={lbl}>Número</label>
          <input type="number" min="1" required value={form.numero}
            onChange={e => setField('numero', e.target.value)} style={inp} />

          <label style={lbl}>RUT Receptor (opcional)</label>
          <input value={form.rutReceptor} onChange={e => setField('rutReceptor', e.target.value)} style={inp} />

          <label style={lbl}>Nombre Receptor (opcional)</label>
          <input value={form.nombreReceptor} onChange={e => setField('nombreReceptor', e.target.value)} style={inp} />

          <h3 style={{ marginTop: 16 }}>Detalle</h3>
          {form.detalle.map((line, i) => (
            <div key={i} style={{ display: 'flex', gap: 6, marginBottom: 8, flexWrap: 'wrap' }}>
              <input placeholder="Ítem" required value={line.nombreItem}
                onChange={e => setLine(i, 'nombreItem', e.target.value)}
                style={{ ...inp, width: 130 }} />
              <input type="number" placeholder="Cant." min="0.01" step="0.01" required value={line.cantidad}
                onChange={e => setLine(i, 'cantidad', e.target.value)}
                style={{ ...inp, width: 70 }} />
              <input type="number" placeholder="Precio" min="0" step="0.01" required value={line.precioUnitario}
                onChange={e => setLine(i, 'precioUnitario', e.target.value)}
                style={{ ...inp, width: 90 }} />
              <select value={line.indFactIva} onChange={e => setLine(i, 'indFactIva', e.target.value)}
                style={{ ...inp, width: 130 }}>
                {Object.entries(TIPO_IVA).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
              </select>
              {form.detalle.length > 1 &&
                <button type="button" onClick={() => removeLine(i)} style={{ cursor: 'pointer' }}>✕</button>}
            </div>
          ))}
          <button type="button" onClick={addLine} style={{ marginBottom: 12, cursor: 'pointer' }}>+ Línea</button>

          {error && <p style={{ color: statusColor.error, margin: '8px 0' }}>{error}</p>}

          <br />
          <button type="submit" disabled={loading}
            style={{ padding: '8px 20px', cursor: 'pointer', background: '#0066cc', color: '#fff', border: 'none', borderRadius: 4 }}>
            {loading ? 'Procesando…' : 'Crear & Firmar CFE'}
          </button>
        </form>

        {/* ── Invoice list ── */}
        <div style={{ flex: 1, minWidth: 320 }}>
          <h2>Comprobantes ({invoices.length})</h2>
          {invoices.length === 0
            ? <p style={{ color: '#666' }}>Sin comprobantes aún.</p>
            : (
              <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 14 }}>
                <thead>
                  <tr style={{ background: '#f0f0f0' }}>
                    {['#', 'Tipo', 'N°', 'Fecha', 'Receptor', 'Total', 'DGI', ''].map(h =>
                      <th key={h} style={th}>{h}</th>)}
                  </tr>
                </thead>
                <tbody>
                  {invoices.map(inv => (
                    <tr key={inv.id} style={{ borderBottom: '1px solid #ddd' }}>
                      <td style={td}>{inv.id}</td>
                      <td style={td}>{tipoCfeLabel(inv.tipoCfe)}</td>
                      <td style={td}>{inv.numero}</td>
                      <td style={td}>{inv.fechaEmision?.slice(0, 10)}</td>
                      <td style={td}>{inv.nombreReceptor ?? '—'}</td>
                      <td style={td}>${inv.montoTotal.toFixed(2)}</td>
                      <td style={td}>{inv.aceptadoPorDgi ? '✅' : '—'}</td>
                      <td style={td}>
                        <button onClick={() => downloadPdf(inv)}
                          style={{ cursor: 'pointer', fontSize: 12 }}>PDF</button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
        </div>
      </div>
    </div>
  )
}

function ConfigStatusBanner({ status, onRefresh }) {
  if (!status) return <p style={{ color: '#888' }}>Verificando configuración…</p>

  const color = status.ok ? statusColor.ok : statusColor.error
  const bg = status.ok ? '#f0fdf4' : '#fef2f2'
  const border = status.ok ? '#bbf7d0' : '#fecaca'

  return (
    <div style={{ background: bg, border: `1px solid ${border}`, borderRadius: 6, padding: '12px 16px' }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 10, flexWrap: 'wrap' }}>
        <span style={{ fontSize: 18 }}>{status.ok ? '✅' : '❌'}</span>
        <strong style={{ color }}>
          {status.ok ? 'Ambiente configurado correctamente' : 'Configuración incompleta'}
        </strong>
        <span style={{ fontSize: 13, color: '#555', marginLeft: 4 }}>
          Ambiente: <b>{status.ambiente}</b>
          {status.rutEmisor && <> · RUT: <b>{status.rutEmisor}</b></>}
          {status.razonSocial && <> · <b>{status.razonSocial}</b></>}
          {status.certificado && (
            <> · Certificado: <b style={{ color: status.certificadoExiste ? statusColor.ok : statusColor.error }}>
              {status.certificadoExiste ? 'encontrado ✓' : 'NO encontrado ✗'}
            </b></>
          )}
        </span>
        <button onClick={onRefresh}
          style={{ marginLeft: 'auto', fontSize: 12, cursor: 'pointer', padding: '2px 10px' }}>
          ↻ Verificar
        </button>
      </div>
      {status.issues?.length > 0 && (
        <ul style={{ margin: '8px 0 0 28px', padding: 0, fontSize: 13, color: statusColor.error }}>
          {status.issues.map((issue, i) => <li key={i}>{issue}</li>)}
        </ul>
      )}
    </div>
  )
}

const lbl = { display: 'block', fontSize: 13, fontWeight: 600, marginTop: 10, marginBottom: 2 }
const inp = { display: 'block', padding: '6px 8px', border: '1px solid #ccc', borderRadius: 4, fontSize: 14, width: '100%', boxSizing: 'border-box' }
const th = { padding: '6px 8px', textAlign: 'left', fontWeight: 600 }
const td = { padding: '6px 8px' }
