import { useState, useEffect, useCallback } from 'react'
import { C, injectStyles } from './constants/styles.js'
import { ApiContext } from './context/ApiContext.jsx'
import { useApi } from './context/ApiContext.jsx'
import AuthForm from './components/AuthForm.jsx'
import ConfigStatusPill from './components/ConfigStatusPill.jsx'
import DashboardTab from './tabs/DashboardTab.jsx'
import CreateCfeTab from './tabs/CreateCfeTab.jsx'
import DemoTab from './tabs/DemoTab.jsx'
import HistorialTab from './tabs/HistorialTab.jsx'

const AUTH_KEY = 'urufactura_token'

// Inner component that has access to ApiContext (useApi needs the provider above)
function AppShell({ token, onLogout }) {
  const apiFetch = useApi()
  const [tab, setTab]               = useState('dashboard')
  const [cfeTypes, setCfeTypes]     = useState([])
  const [configStatus, setConfigStatus] = useState(null)
  const [invoices, setInvoices]     = useState([])
  const [dashboard, setDashboard]   = useState(null)

  const loadAll = useCallback(async () => {
    try {
      const [invRes, dashRes, configRes, typesRes] = await Promise.all([
        apiFetch('/api/invoices'),
        apiFetch('/api/dashboard'),
        apiFetch('/api/config/status'),
        fetch('/api/cfe-types'),
      ])
      if (invRes.ok)     setInvoices(await invRes.json())
      if (dashRes.ok)    setDashboard(await dashRes.json())
      if (configRes.ok)  setConfigStatus(await configRes.json())
      else               setConfigStatus({ ok: false, ambiente: '?', issues: [`Error HTTP ${configRes.status}`] })
      if (typesRes.ok)   setCfeTypes(await typesRes.json())
    } catch (e) {
      setConfigStatus({ ok: false, ambiente: '?', issues: [`No se pudo conectar: ${e.message}`] })
    }
  }, [apiFetch])

  async function refreshConfig() {
    try {
      const res = await apiFetch('/api/config/status')
      if (res.ok) setConfigStatus(await res.json())
      else setConfigStatus({ ok: false, ambiente: '?', issues: [`Error HTTP ${res.status}`] })
    } catch (e) {
      setConfigStatus({ ok: false, ambiente: '?', issues: [`No se pudo conectar: ${e.message}`] })
    }
  }

  useEffect(() => { loadAll() }, [loadAll])

  const tabs = [
    ['dashboard', '📊 Dashboard'],
    ['crear',     '✏️ Crear CFE'],
    ['demo',      '🎬 Modo Demo'],
    ['historial', '📋 Historial'],
  ]

  return (
    <div style={{ fontFamily: '"Inter", system-ui, sans-serif', background: C.bg, minHeight: '100vh' }}>
      {/* ── Header ── */}
      <header style={{ background: '#fff', borderBottom: `1px solid ${C.border}`, position: 'sticky', top: 0, zIndex: 50 }}>
        <div style={{ maxWidth: 1240, margin: '0 auto', padding: '0 24px', display: 'flex', alignItems: 'center', gap: 16, height: 56 }}>
          <span style={{ fontWeight: 700, fontSize: 17, color: C.text, whiteSpace: 'nowrap' }}>🧾 UruFactura SaaS</span>
          <nav style={{ display: 'flex', gap: 2, marginLeft: 16 }}>
            {tabs.map(([id, label]) => (
              <button key={id} onClick={() => setTab(id)}
                style={{
                  padding: '5px 14px', border: 'none', borderRadius: 6, cursor: 'pointer', fontSize: 13,
                  fontWeight: tab === id ? 600 : 400,
                  background: tab === id ? C.blueL : 'transparent',
                  color:      tab === id ? C.blue  : C.textMuted,
                }}>
                {label}
              </button>
            ))}
          </nav>
          <div style={{ marginLeft: 'auto', display: 'flex', alignItems: 'center', gap: 12 }}>
            <ConfigStatusPill status={configStatus} onRefresh={refreshConfig} />
            <button onClick={onLogout}
              style={{ fontSize: 12, padding: '4px 12px', border: `1px solid ${C.border}`, borderRadius: 20, cursor: 'pointer', background: '#fff', color: C.textMuted }}>
              Salir
            </button>
          </div>
        </div>
      </header>

      {/* ── Body ── */}
      <main style={{ maxWidth: 1240, margin: '0 auto', padding: '28px 24px' }}>
        {tab === 'dashboard' && <DashboardTab dashboard={dashboard} />}
        {tab === 'crear'     && <CreateCfeTab cfeTypes={cfeTypes} onCreated={loadAll} />}
        {tab === 'demo'      && <DemoTab onCreated={loadAll} />}
        {tab === 'historial' && <HistorialTab invoices={invoices} onReload={loadAll} />}
      </main>
    </div>
  )
}

export default function App() {
  const [token, setToken] = useState(() => localStorage.getItem(AUTH_KEY))

  useEffect(() => { injectStyles() }, [])

  function handleLogin(newToken) {
    localStorage.setItem(AUTH_KEY, newToken)
    setToken(newToken)
  }

  function handleLogout() {
    localStorage.removeItem(AUTH_KEY)
    setToken(null)
  }

  if (!token) return <AuthForm onLogin={handleLogin} />

  return (
    <ApiContext.Provider value={{ token }}>
      <AppShell token={token} onLogout={handleLogout} />
    </ApiContext.Provider>
  )
}
