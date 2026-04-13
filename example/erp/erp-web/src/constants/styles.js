// ─── Design Tokens ────────────────────────────────────────────────────────────
export const C = {
  blue: '#2563eb', blueL: '#eff6ff', blueBorder: '#bfdbfe',
  green: '#16a34a', greenL: '#f0fdf4', greenBorder: '#bbf7d0',
  red: '#dc2626', redL: '#fef2f2', redBorder: '#fecaca',
  violet: '#7c3aed', violetL: '#f5f3ff', violetBorder: '#ddd6fe',
  amber: '#d97706', amberL: '#fffbeb', amberBorder: '#fde68a',
  slate: '#475569', slateL: '#f8fafc', slateBorder: '#e2e8f0',
  border: '#e2e8f0', bg: '#f8fafc', card: '#fff',
  text: '#0f172a', textMuted: '#64748b',
}

// ─── Style Objects ─────────────────────────────────────────────────────────────
export const sectionTitle  = { fontSize: 20, fontWeight: 700, marginBottom: 8, color: C.text, marginTop: 0 }
export const lbl           = { display: 'block', fontSize: 12, fontWeight: 600, marginTop: 8, marginBottom: 2, color: C.textMuted }
export const inp           = { display: 'block', padding: '7px 10px', border: `1px solid ${C.border}`, borderRadius: 8, fontSize: 14, width: '100%', boxSizing: 'border-box' }
export const th            = { padding: '8px 10px', textAlign: 'left', fontWeight: 600, fontSize: 12, color: C.textMuted }
export const td            = { padding: '8px 10px', fontSize: 13 }
export const errorBox      = { marginTop: 10, padding: '8px 12px', background: C.redL,   border: `1px solid ${C.redBorder}`,   borderRadius: 8, fontSize: 13, color: C.red }
export const successBox    = { marginTop: 10, padding: '8px 12px', background: C.greenL, border: `1px solid ${C.greenBorder}`, borderRadius: 8, fontSize: 13, color: C.green }
export const codeInline    = { background: C.slateL, border: `1px solid ${C.slateBorder}`, borderRadius: 4, padding: '1px 5px', fontSize: 12, fontFamily: 'monospace' }
export const cfeBadge      = (color, bg) => ({ display: 'inline-flex', alignItems: 'center', gap: 4, background: bg ?? color + '18', color, border: `1px solid ${color}44`, borderRadius: 20, padding: '2px 10px', fontSize: 12, fontWeight: 600 })
export const primaryBtn    = color => ({ padding: '9px 20px', background: color, color: '#fff', border: 'none', borderRadius: 8, cursor: 'pointer', fontWeight: 600, fontSize: 14 })

export function injectStyles() {
  if (document.getElementById('uf-styles')) return
  const s = document.createElement('style')
  s.id = 'uf-styles'
  s.textContent = `
    * { box-sizing: border-box; }
    body { margin: 0; }
    .uf-modal    { animation: uf-up   0.2s  cubic-bezier(0.16,1,0.3,1); }
    .uf-overlay  { animation: uf-fade 0.15s ease; }
    .uf-dropdown { animation: uf-up   0.15s cubic-bezier(0.16,1,0.3,1); }
    @keyframes uf-up   { from { opacity:0; transform:translateY(14px); } to { opacity:1; transform:translateY(0); } }
    @keyframes uf-fade { from { opacity:0; } to { opacity:1; } }
    input:focus, select:focus { border-color: #2563eb !important; box-shadow: 0 0 0 3px #2563eb22; outline: none; }
    button:not(:disabled):hover { filter: brightness(0.94); }
  `
  document.head.appendChild(s)
}
