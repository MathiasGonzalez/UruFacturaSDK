import { useAuth } from '../services/AuthContext.jsx';

export default function Configuracion() {
  const { tenant } = useAuth();

  return (
    <div>
      <h1 style={{ marginBottom: '1.5rem' }}>Configuración</h1>

      <div className="card" style={{ marginBottom: '1.5rem' }}>
        <h3 style={{ marginBottom: '1rem' }}>Datos del Tenant</h3>
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <tbody>
            <Row label="Tenant ID" value={tenant?.tenantId || '(default)'} />
            <Row label="Razón Social" value={tenant?.razonSocial} />
            <Row label="RUT Emisor" value={tenant?.rutEmisor} />
            <Row label="Domicilio Fiscal" value={tenant?.domicilioFiscal} />
            <Row label="Ambiente" value={tenant?.ambiente} />
            <Row label="Conectado desde" value={tenant?.connectedAt ? new Date(tenant.connectedAt).toLocaleString() : '-'} />
          </tbody>
        </table>
      </div>

      <div className="card" style={{ marginBottom: '1.5rem' }}>
        <h3 style={{ marginBottom: '1rem' }}>Variables de entorno requeridas</h3>
        <p style={{ fontSize: '0.9rem', color: 'var(--text-muted)', marginBottom: '1rem' }}>
          Para que este tenant funcione en producción, configure las siguientes variables en Cloudflare Secrets:
        </p>
        <pre style={{ background: 'var(--bg)', padding: '1rem', borderRadius: 'var(--radius)', fontSize: '0.8rem', overflow: 'auto' }}>
{`# Obligatorias
Tenants__${tenant?.tenantId || '<tenant-id>'}__RutEmisor=${tenant?.rutEmisor || '<12 dígitos>'}
Tenants__${tenant?.tenantId || '<tenant-id>'}__RazonSocialEmisor=${tenant?.razonSocial || '<razón social>'}
Tenants__${tenant?.tenantId || '<tenant-id>'}__DomicilioFiscal=${tenant?.domicilioFiscal || '<domicilio>'}
Tenants__${tenant?.tenantId || '<tenant-id>'}__CertificadoBase64=<base64 del .p12>
Tenants__${tenant?.tenantId || '<tenant-id>'}__PasswordCertificado=<contraseña>

# Opcionales
Tenants__${tenant?.tenantId || '<tenant-id>'}__Ambiente=${tenant?.ambiente || 'Homologacion'}
Tenants__${tenant?.tenantId || '<tenant-id>'}__Caes=[{"NroSerie":"CAE001","Tipo":101,"RangoDesde":1,"RangoHasta":1000,"FechaVencimiento":"2026-12-31"}]`}
        </pre>
      </div>

      <div className="card">
        <h3 style={{ marginBottom: '0.75rem' }}>🔮 Próximamente</h3>
        <ul style={{ paddingLeft: '1.5rem', fontSize: '0.9rem', color: 'var(--text-muted)' }}>
          <li>Subir certificado .p12 desde la web</li>
          <li>Rotación automática de certificados</li>
          <li>Historial de CFEs emitidos</li>
          <li>Webhooks para notificaciones DGI</li>
          <li>Panel de métricas y uso</li>
        </ul>
      </div>
    </div>
  );
}

function Row({ label, value }) {
  return (
    <tr style={{ borderBottom: '1px solid var(--border)' }}>
      <td style={{ padding: '0.5rem 0', fontWeight: 500, color: 'var(--text-muted)', width: '40%', fontSize: '0.9rem' }}>{label}</td>
      <td style={{ padding: '0.5rem 0', fontSize: '0.9rem' }}>{value || '-'}</td>
    </tr>
  );
}
