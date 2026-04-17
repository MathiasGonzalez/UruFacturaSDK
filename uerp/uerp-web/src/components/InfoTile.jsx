import { C } from '../constants/styles.js'

export default function InfoTile({ label, value }) {
  return (
    <div style={{ background: C.slateL, borderRadius: 6, padding: '7px 10px' }}>
      <div style={{ fontSize: 11, color: C.textMuted, marginBottom: 2 }}>{label}</div>
      <div style={{ fontSize: 13, fontWeight: 600 }}>{value}</div>
    </div>
  )
}
