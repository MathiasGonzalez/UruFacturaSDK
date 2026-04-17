import { useState } from 'react'
import { C } from '../constants/styles.js'
import PillRow from './PillRow.jsx'

export default function ConfigStatusPill({ status, onRefresh }) {
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
            {status.rutEmisor   && <PillRow label="RUT Emisor"   value={status.rutEmisor} />}
            {status.razonSocial && <PillRow label="Razón Social" value={status.razonSocial} />}
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
