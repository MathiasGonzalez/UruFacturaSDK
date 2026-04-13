import { useEffect, useRef } from 'react'
import { C } from '../constants/styles.js'

export default function Modal({ title, onClose, accentColor, children }) {
  const modalRef = useRef(null)
  const titleId = useRef(`uf-modal-title-${Math.random().toString(36).slice(2, 11)}`)

  useEffect(() => {
    const previousFocus = document.activeElement
    modalRef.current?.focus()

    const handleKeyDown = e => { if (e.key === 'Escape') onClose() }
    document.addEventListener('keydown', handleKeyDown)

    return () => {
      document.removeEventListener('keydown', handleKeyDown)
      previousFocus?.focus()
    }
  }, [onClose])

  return (
    <div className="uf-overlay"
      style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.45)', zIndex: 1000, display: 'flex', alignItems: 'center', justifyContent: 'center', padding: 24, backdropFilter: 'blur(3px)' }}
      onClick={e => { if (e.target === e.currentTarget) onClose() }}>
      <div
        ref={modalRef}
        className="uf-modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId.current}
        tabIndex={-1}
        style={{ background: '#fff', borderRadius: 16, maxWidth: 600, width: '100%', boxShadow: '0 20px 60px rgba(0,0,0,0.25)', maxHeight: '90vh', overflowY: 'auto' }}>
        <div style={{ padding: '16px 20px', borderBottom: `1px solid ${C.border}`, display: 'flex', alignItems: 'center', justifyContent: 'space-between', background: `${accentColor}11`, borderRadius: '16px 16px 0 0', position: 'sticky', top: 0, zIndex: 1 }}>
          <strong id={titleId.current} style={{ fontSize: 15, color: accentColor }}>{title}</strong>
          <button onClick={onClose} aria-label="Cerrar modal"
            style={{ border: 'none', background: 'none', cursor: 'pointer', fontSize: 22, color: C.textMuted, lineHeight: 1, padding: '0 4px' }}>
            ×
          </button>
        </div>
        <div style={{ padding: 20 }}>{children}</div>
      </div>
    </div>
  )
}
