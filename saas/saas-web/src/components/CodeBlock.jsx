import { C } from '../constants/styles.js'

export default function CodeBlock({ code }) {
  return (
    <pre style={{
      background: '#0f172a', color: '#e2e8f0', borderRadius: 8,
      padding: '12px 16px', fontSize: 12, overflowX: 'auto', margin: 0,
      fontFamily: '"Fira Code","Cascadia Code","Consolas",monospace', lineHeight: 1.65,
    }}>{code}</pre>
  )
}
