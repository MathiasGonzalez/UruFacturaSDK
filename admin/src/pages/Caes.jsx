import { useState, useEffect } from 'react';
import { useAuth } from '../services/AuthContext.jsx';
import { listarCaes, registrarCae, advertenciasCaes } from '../services/api.js';

export default function Caes() {
  const { tenant } = useAuth();
  const [caes, setCaes] = useState([]);
  const [warnings, setWarnings] = useState([]);
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState({
    nroSerie: '',
    tipo: 101,
    rangoDesde: 1,
    rangoHasta: 1000,
    fechaVencimiento: '',
  });
  const [error, setError] = useState('');

  useEffect(() => { loadCaes(); }, [tenant]);

  async function loadCaes() {
    try {
      const [c, w] = await Promise.all([
        listarCaes(tenant?.tenantId),
        advertenciasCaes(tenant?.tenantId).catch(() => []),
      ]);
      setCaes(Array.isArray(c) ? c : []);
      setWarnings(Array.isArray(w) ? w : []);
    } catch (err) {
      setError(err.message);
    }
  }

  async function handleRegister(e) {
    e.preventDefault();
    setError('');
    try {
      await registrarCae(tenant?.tenantId, {
        NroSerie: form.nroSerie,
        Tipo: Number(form.tipo),
        RangoDesde: Number(form.rangoDesde),
        RangoHasta: Number(form.rangoHasta),
        FechaVencimiento: form.fechaVencimiento,
      });
      setShowForm(false);
      await loadCaes();
    } catch (err) {
      setError(err.message);
    }
  }

  const update = (key) => (e) => setForm({ ...form, [key]: e.target.value });

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem' }}>
        <h1>CAEs</h1>
        <button className="primary" onClick={() => setShowForm(!showForm)}>
          {showForm ? 'Cancelar' : '+ Registrar CAE'}
        </button>
      </div>

      {error && <div className="error-msg">{error}</div>}

      {showForm && (
        <div className="card" style={{ marginBottom: '1.5rem' }}>
          <h3 style={{ marginBottom: '1rem' }}>Nuevo CAE</h3>
          <form onSubmit={handleRegister} style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: '1rem' }}>
            <div className="field">
              <label>Nro Serie</label>
              <input type="text" value={form.nroSerie} onChange={update('nroSerie')} placeholder="CAE001" required />
            </div>
            <div className="field">
              <label>Tipo</label>
              <input type="number" value={form.tipo} onChange={update('tipo')} required />
            </div>
            <div className="field">
              <label>Rango Desde</label>
              <input type="number" value={form.rangoDesde} onChange={update('rangoDesde')} required />
            </div>
            <div className="field">
              <label>Rango Hasta</label>
              <input type="number" value={form.rangoHasta} onChange={update('rangoHasta')} required />
            </div>
            <div className="field">
              <label>Vencimiento</label>
              <input type="date" value={form.fechaVencimiento} onChange={update('fechaVencimiento')} required />
            </div>
            <div className="field" style={{ display: 'flex', alignItems: 'flex-end' }}>
              <button type="submit" className="primary">Registrar</button>
            </div>
          </form>
        </div>
      )}

      {warnings.length > 0 && (
        <div className="card" style={{ marginBottom: '1rem', background: '#fff3cd' }}>
          <strong>⚠️ Advertencias:</strong>
          <ul style={{ margin: '0.5rem 0 0 1.5rem' }}>
            {warnings.map((w, i) => <li key={i}>{w.detalle || w.Detalle || JSON.stringify(w)}</li>)}
          </ul>
        </div>
      )}

      <div className="card">
        {caes.length === 0 ? (
          <p style={{ color: 'var(--text-muted)' }}>No hay CAEs registrados.</p>
        ) : (
          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
            <thead>
              <tr style={{ borderBottom: '2px solid var(--border)' }}>
                <th style={{ textAlign: 'left', padding: '0.5rem' }}>Serie</th>
                <th style={{ textAlign: 'left', padding: '0.5rem' }}>Tipo</th>
                <th style={{ textAlign: 'right', padding: '0.5rem' }}>Desde</th>
                <th style={{ textAlign: 'right', padding: '0.5rem' }}>Hasta</th>
                <th style={{ textAlign: 'left', padding: '0.5rem' }}>Vencimiento</th>
              </tr>
            </thead>
            <tbody>
              {caes.map((cae, i) => (
                <tr key={cae.nroSerie || cae.NroSerie || i} style={{ borderBottom: '1px solid var(--border)' }}>
                  <td style={{ padding: '0.5rem' }}>{cae.nroSerie || cae.NroSerie}</td>
                  <td style={{ padding: '0.5rem' }}>{cae.tipoCfe ?? cae.TipoCfe ?? cae.tipo ?? cae.Tipo}</td>
                  <td style={{ padding: '0.5rem', textAlign: 'right' }}>{cae.rangoDesde ?? cae.RangoDesde}</td>
                  <td style={{ padding: '0.5rem', textAlign: 'right' }}>{cae.rangoHasta ?? cae.RangoHasta}</td>
                  <td style={{ padding: '0.5rem' }}>{cae.fechaVencimiento || cae.FechaVencimiento || '-'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}
