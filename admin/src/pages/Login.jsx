import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../services/AuthContext.jsx';
import { health } from '../services/api.js';
import './Auth.css';

export default function Login() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [form, setForm] = useState({ tenantId: '', apiUrl: '' });
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      // Verify connectivity
      await health();
      login({
        tenantId: form.tenantId || null,
        apiUrl: form.apiUrl || null,
        razonSocial: form.tenantId || 'Single-tenant',
        connectedAt: new Date().toISOString(),
      });
      navigate('/');
    } catch (err) {
      setError(`No se pudo conectar a la API: ${err.message}`);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="auth-page">
      <div className="auth-card card">
        <h1>UruFactura Admin</h1>
        <p className="subtitle">Ingrese su tenant para conectarse a la API</p>

        {error && <div className="error-msg">{error}</div>}

        <form onSubmit={handleSubmit}>
          <div className="field">
            <label htmlFor="tenantId">Tenant ID</label>
            <input
              id="tenantId"
              type="text"
              placeholder="empresa-abc (vacío = single-tenant)"
              value={form.tenantId}
              onChange={(e) => setForm({ ...form, tenantId: e.target.value })}
            />
          </div>
          <div className="field">
            <label htmlFor="apiUrl">URL de la API (opcional)</label>
            <input
              id="apiUrl"
              type="url"
              placeholder="https://urufactura-api.workers.dev"
              value={form.apiUrl}
              onChange={(e) => setForm({ ...form, apiUrl: e.target.value })}
            />
            <small>Dejar vacío para usar el proxy local</small>
          </div>
          <button type="submit" className="primary" disabled={loading}>
            {loading ? 'Conectando...' : 'Ingresar'}
          </button>
        </form>

        <p className="auth-link">
          ¿Primera vez? <Link to="/register">Registrar nuevo tenant</Link>
        </p>
      </div>
    </div>
  );
}
