import { C } from '../constants/styles.js'

export default function Modal({ title, onClose, accentColor, children }) {
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
