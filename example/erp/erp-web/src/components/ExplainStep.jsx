import { C } from '../constants/styles.js'

export default function ExplainStep({ n, title, color, children }) {
  return (
    <div style={{ display: 'flex', gap: 12 }}>
      <div style={{
        width: 28, height: 28, borderRadius: '50%', background: color + '22',
        border: `2px solid ${color}`, color, fontWeight: 700, fontSize: 13,
        display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0, marginTop: 1,
      }}>{n}</div>
      <div style={{ flex: 1 }}>
        <div style={{ fontSize: 13, fontWeight: 600, color, marginBottom: 5 }}>{title}</div>
        {children}
      </div>
    </div>
  )
}
