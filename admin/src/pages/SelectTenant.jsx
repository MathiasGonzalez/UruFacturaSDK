import { useState, useEffect } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../services/AuthContext.jsx';
import { getTenants } from '../services/auth.js';
import './Auth.css';

export default function SelectTenant() {
  const { session, setActiveTenant } = useAuth();
  const navigate = useNavigate();
  const [tenants, setTenants] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    loadTenants();
  }, []);

  async function loadTenants() {
    try {
      const { tenants: list } = await getTenants(session.token);
      setTenants(list || []);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }

  function handleSelect(tenant) {
    setActiveTenant(tenant);
    navigate('/');
  }

  return (
    <div className="auth-page">
      <div className="auth-card card">
        <h1>Seleccionar Tenant</h1>
        <p className="subtitle">Sesión: {session?.email}</p>

        {error && <div className="error-msg">{error}</div>}

        {loading ? (
          <p>Cargando tenants...</p>
        ) : tenants.length === 0 ? (
          <div>
            <p>No tiene tenants registrados aún.</p>
            <Link to="/register" className="primary" style={{ display: 'inline-block', marginTop: '1rem', textDecoration: 'none' }}>
              Registrar nuevo tenant
            </Link>
          </div>
        ) : (
          <div className="tenant-list">
            {tenants.map((t) => (
              <button
                key={t.tenantId}
                className="tenant-item"
                onClick={() => handleSelect(t)}
              >
                <strong>{t.razonSocial}</strong>
                <span className="tenant-id">{t.tenantId}</span>
                <span className="tenant-env">{t.ambiente}</span>
              </button>
            ))}
          </div>
        )}

        <p className="auth-link">
          <Link to="/register">Registrar nuevo tenant</Link>
        </p>
      </div>
    </div>
  );
}
