import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../services/AuthContext.jsx';
import './Auth.css';

export default function Register() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [form, setForm] = useState({
    tenantId: '',
    razonSocial: '',
    rutEmisor: '',
    domicilioFiscal: '',
    ambiente: 'Homologacion',
  });
  const [error, setError] = useState('');

  const handleSubmit = (e) => {
    e.preventDefault();
    setError('');

    if (!form.tenantId || !form.razonSocial || !form.rutEmisor) {
      setError('Complete los campos obligatorios.');
      return;
    }

    if (!/^[a-zA-Z0-9_-]+$/.test(form.tenantId)) {
      setError('Tenant ID: solo letras, números, guión (-) y guión bajo (_).');
      return;
    }

    // Save tenant registration locally.
    // In production, this would call a registration endpoint.
    const tenantData = {
      tenantId: form.tenantId,
      razonSocial: form.razonSocial,
      rutEmisor: form.rutEmisor,
      domicilioFiscal: form.domicilioFiscal,
      ambiente: form.ambiente,
      connectedAt: new Date().toISOString(),
    };

    login(tenantData);
    navigate('/');
  };

  const update = (key) => (e) => setForm({ ...form, [key]: e.target.value });

  return (
    <div className="auth-page">
      <div className="auth-card card">
        <h1>Registrar Tenant</h1>
        <p className="subtitle">Configure un nuevo tenant para facturación electrónica</p>

        {error && <div className="error-msg">{error}</div>}

        <form onSubmit={handleSubmit}>
          <div className="field">
            <label htmlFor="tenantId">Tenant ID *</label>
            <input
              id="tenantId"
              type="text"
              placeholder="mi-empresa"
              value={form.tenantId}
              onChange={update('tenantId')}
              required
            />
            <small>Identificador único. Solo letras, números, - y _</small>
          </div>
          <div className="field">
            <label htmlFor="razonSocial">Razón Social *</label>
            <input
              id="razonSocial"
              type="text"
              placeholder="MI EMPRESA SA"
              value={form.razonSocial}
              onChange={update('razonSocial')}
              required
            />
          </div>
          <div className="field">
            <label htmlFor="rutEmisor">RUT Emisor (12 dígitos) *</label>
            <input
              id="rutEmisor"
              type="text"
              placeholder="210000000001"
              maxLength={12}
              value={form.rutEmisor}
              onChange={update('rutEmisor')}
              required
            />
          </div>
          <div className="field">
            <label htmlFor="domicilioFiscal">Domicilio Fiscal</label>
            <input
              id="domicilioFiscal"
              type="text"
              placeholder="AV ITALIA 1234"
              value={form.domicilioFiscal}
              onChange={update('domicilioFiscal')}
            />
          </div>
          <div className="field">
            <label htmlFor="ambiente">Ambiente</label>
            <select id="ambiente" value={form.ambiente} onChange={update('ambiente')}>
              <option value="Homologacion">Homologación (testing)</option>
              <option value="Produccion">Producción</option>
            </select>
          </div>
          <button type="submit" className="primary">Registrar</button>
        </form>

        <p className="auth-link">
          ¿Ya tiene un tenant? <Link to="/login">Ingresar</Link>
        </p>
      </div>
    </div>
  );
}
