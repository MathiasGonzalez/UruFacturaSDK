import { useState, useEffect } from 'react';
import { useAuth } from '../services/AuthContext.jsx';
import { health, listarCaes, advertenciasCaes } from '../services/api.js';
import './Dashboard.css';

export default function Dashboard() {
  const { tenant } = useAuth();
  const [status, setStatus] = useState(null);
  const [caes, setCaes] = useState([]);
  const [warnings, setWarnings] = useState([]);
  const [error, setError] = useState('');

  useEffect(() => {
    loadData();
  }, [tenant]);

  async function loadData() {
    setError('');
    try {
      const [h, c, w] = await Promise.all([
        health(),
        listarCaes(tenant?.tenantId).catch(() => []),
        advertenciasCaes(tenant?.tenantId).catch(() => []),
      ]);
      setStatus(h);
      setCaes(Array.isArray(c) ? c : []);
      setWarnings(Array.isArray(w) ? w : []);
    } catch (err) {
      setError(`Error al conectar: ${err.message}`);
    }
  }

  return (
    <div className="dashboard">
      <h1>Dashboard</h1>

      {error && <div className="error-msg">{error}</div>}

      <div className="stats-grid">
        <div className="card stat">
          <span className="stat-label">Estado API</span>
          <span className={`stat-value ${status ? 'ok' : 'offline'}`}>
            {status ? '🟢 Online' : '⚪ Sin datos'}
          </span>
        </div>
        <div className="card stat">
          <span className="stat-label">Tenant</span>
          <span className="stat-value">{tenant?.tenantId || 'default'}</span>
        </div>
        <div className="card stat">
          <span className="stat-label">CAEs activos</span>
          <span className="stat-value">{caes.length}</span>
        </div>
        <div className="card stat">
          <span className="stat-label">Advertencias</span>
          <span className={`stat-value ${warnings.length > 0 ? 'warn' : ''}`}>
            {warnings.length}
          </span>
        </div>
      </div>

      {warnings.length > 0 && (
        <div className="card warnings-section">
          <h3>⚠️ Advertencias de CAEs</h3>
          <ul>
            {warnings.map((w, i) => (
              <li key={i}>{w.detalle || w.Detalle || JSON.stringify(w)}</li>
            ))}
          </ul>
        </div>
      )}

      <div className="card info-section">
        <h3>Información del Tenant</h3>
        <table>
          <tbody>
            <tr><td>Razón Social</td><td>{tenant?.razonSocial || '-'}</td></tr>
            <tr><td>RUT Emisor</td><td>{tenant?.rutEmisor || '-'}</td></tr>
            <tr><td>Ambiente</td><td>{tenant?.ambiente || '-'}</td></tr>
            <tr><td>Conectado</td><td>{tenant?.connectedAt ? new Date(tenant.connectedAt).toLocaleString() : '-'}</td></tr>
          </tbody>
        </table>
      </div>
    </div>
  );
}
