import { useState } from 'react'
import { C, lbl, inp, errorBox } from '../constants/styles.js'

const base = typeof __API_BASE__ !== 'undefined' ? __API_BASE__ : ''

export default function AuthForm({ onLogin }) {
  const [mode, setMode]         = useState('login')   // 'login' | 'register'
  const [email, setEmail]       = useState('')
  const [password, setPassword] = useState('')
  const [name, setName]         = useState('')
  const [company, setCompany]   = useState('')
  const [loading, setLoading]   = useState(false)
  const [error, setError]       = useState(null)

  async function submit(e) {
    e.preventDefault()
    setLoading(true)
    setError(null)
    try {
      const body = mode === 'login'
        ? { email, password }
        : { email, password, name, companyName: company }
      const res = await fetch(`${base}/api/auth/${mode}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
      })
      if (!res.ok) {
        const err = await res.json().catch(() => ({ detail: res.statusText }))
        throw new Error(err.detail ?? err.title ?? 'Error desconocido')
      }
      const { token } = await res.json()
      onLogin(token)
    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div style={{
      minHeight: '100vh', background: C.bg,
      display: 'flex', alignItems: 'center', justifyContent: 'center',
      fontFamily: '"Inter", system-ui, sans-serif', padding: 24,
    }}>
      <div style={{ width: '100%', maxWidth: 420 }}>
        {/* Logo */}
        <div style={{ textAlign: 'center', marginBottom: 32 }}>
          <div style={{ fontSize: 40, marginBottom: 8 }}>🧾</div>
          <h1 style={{ margin: 0, fontSize: 24, fontWeight: 700, color: C.text }}>UruFactura SaaS</h1>
          <p style={{ margin: '6px 0 0', color: C.textMuted, fontSize: 14 }}>
            Facturación electrónica para Uruguay
          </p>
        </div>

        {/* Card */}
        <div style={{
          background: '#fff', border: `1px solid ${C.border}`, borderRadius: 16,
          padding: 32, boxShadow: '0 4px 24px rgba(0,0,0,0.06)',
        }}>
          {/* Tab switcher */}
          <div style={{ display: 'flex', marginBottom: 24, borderBottom: `1px solid ${C.border}` }}>
            {[['login', 'Iniciar sesión'], ['register', 'Crear cuenta']].map(([m, label]) => (
              <button key={m} onClick={() => { setMode(m); setError(null) }}
                style={{
                  flex: 1, padding: '10px 0', border: 'none', background: 'none',
                  cursor: 'pointer', fontSize: 14, fontWeight: mode === m ? 700 : 400,
                  color: mode === m ? C.blue : C.textMuted,
                  borderBottom: `2px solid ${mode === m ? C.blue : 'transparent'}`,
                  marginBottom: -1,
                }}>
                {label}
              </button>
            ))}
          </div>

          <form onSubmit={submit}>
            {mode === 'register' && (
              <>
                <label style={lbl}>Empresa / Nombre comercial *</label>
                <input required value={company} onChange={e => setCompany(e.target.value)}
                  placeholder="Mi Empresa S.A." style={inp} />
                <label style={lbl}>Tu nombre *</label>
                <input required value={name} onChange={e => setName(e.target.value)}
                  placeholder="Juan Pérez" style={inp} />
              </>
            )}

            <label style={lbl}>Email *</label>
            <input type="email" required value={email} onChange={e => setEmail(e.target.value)}
              placeholder="juan@empresa.com" style={inp} />

            <label style={lbl}>Contraseña *</label>
            <input type="password" required minLength={6} value={password}
              onChange={e => setPassword(e.target.value)}
              placeholder="Mínimo 6 caracteres" style={inp} />

            {error && <div style={errorBox}>{error}</div>}

            <button type="submit" disabled={loading}
              style={{
                marginTop: 20, width: '100%', padding: '11px 0',
                background: C.blue, color: '#fff', border: 'none',
                borderRadius: 10, cursor: loading ? 'not-allowed' : 'pointer',
                fontWeight: 700, fontSize: 15, opacity: loading ? 0.7 : 1,
              }}>
              {loading
                ? '⏳ Procesando…'
                : mode === 'login' ? '→ Ingresar' : '✓ Crear cuenta'}
            </button>
          </form>
        </div>
      </div>
    </div>
  )
}
