import { useState, useEffect } from 'react'
import { C, injectStyles } from './constants/styles.js'
import ConfigStatusPill from './components/ConfigStatusPill.jsx'
import CreateCfeTab from './tabs/CreateCfeTab.jsx'
import DemoTab from './tabs/DemoTab.jsx'
import HistorialTab from './tabs/HistorialTab.jsx'

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
        {tab === 'crear'     && <CreateCfeTab cfeTypes={cfeTypes} onCreated={loadInvoices} />}
        {tab === 'demo'      && <DemoTab onCreated={loadInvoices} />}
        {tab === 'historial' && <HistorialTab invoices={invoices} onReload={loadInvoices} />}
      </main>
    </div>
  )
}
