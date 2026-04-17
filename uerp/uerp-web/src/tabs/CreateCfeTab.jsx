import { useState } from 'react'
import { CFE_META, TIPO_IVA, defaultLine, defaultForm } from '../constants/cfeMeta.js'
import { C, lbl, inp, errorBox, successBox, sectionTitle, cfeBadge } from '../constants/styles.js'
import { useApi } from '../context/ApiContext.jsx'
import Modal from '../components/Modal.jsx'
import CodeBlock from '../components/CodeBlock.jsx'

export default function CreateCfeTab({ cfeTypes, onCreated }) {
  const apiFetch = useApi()
  const [selectedType, setSelectedType] = useState(null)
  const [form, setForm] = useState(defaultForm())
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState(null)
  const [success, setSuccess] = useState(null)
  const [showSdkModal, setShowSdkModal] = useState(false)

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
      const res = await apiFetch('/api/invoices', {
        method: 'POST', body: JSON.stringify(body),
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
              style={{ marginTop: 14, width: '100%', padding: '8px 0', background: meta.color, color: '#fff', border: 'none', borderRadius: 8, cursor: 'pointer', fontSize: 13, fontWeight: 600 }}>
              {'</>'} Ver código SDK
            </button>
          </div>

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
