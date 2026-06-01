import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../services/AuthContext.jsx';
import { sendCode, verifyCode, registerTenant } from '../services/auth.js';
import './Auth.css';

export default function Register() {
  const { login, setActiveTenant } = useAuth();
  const navigate = useNavigate();
  const [step, setStep] = useState('email'); // 'email' | 'code' | 'tenant'
  const [email, setEmail] = useState('');
  const [code, setCode] = useState('');
  const [sessionToken, setSessionToken] = useState('');
  const [form, setForm] = useState({
    tenantId: '',
    razonSocial: '',
    rutEmisor: '',
    domicilioFiscal: '',
    ambiente: 'Homologacion',
  });
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSendCode = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      await sendCode(email);
      setStep('code');
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleVerifyCode = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      const session = await verifyCode(email, code);
      setSessionToken(session.token);
      login(session);
      setStep('tenant');
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleRegister = async (e) => {
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

    if (!/^\d{12}$/.test(form.rutEmisor)) {
      setError('RUT Emisor debe ser un número de 12 dígitos.');
      return;
    }

    setLoading(true);
    try {
      const { tenant } = await registerTenant(sessionToken, form);
      setActiveTenant(tenant);
      navigate('/');
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const update = (key) => (e) => setForm({ ...form, [key]: e.target.value });

  return (
    <div className="auth-page">
      <div className="auth-card card">
        <h1>Registrar Tenant</h1>
        <p className="subtitle">
          {step === 'email' && 'Ingrese su email para comenzar el registro'}
          {step === 'code' && `Ingrese el código enviado a ${email}`}
          {step === 'tenant' && 'Configure los datos de su nuevo tenant'}
        </p>

        {error && <div className="error-msg">{error}</div>}

        {step === 'email' && (
          <form onSubmit={handleSendCode}>
            <div className="field">
              <label htmlFor="email">Email</label>
              <input
                id="email"
                type="email"
                placeholder="usuario@empresa.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
                autoFocus
              />
              <small>Se enviará un código de verificación a este email</small>
            </div>
            <button type="submit" className="primary" disabled={loading}>
              {loading ? 'Enviando...' : 'Enviar código'}
            </button>
          </form>
        )}

        {step === 'code' && (
          <form onSubmit={handleVerifyCode}>
            <div className="field">
              <label htmlFor="code">Código de verificación</label>
              <input
                id="code"
                type="text"
                placeholder="123456"
                maxLength={6}
                value={code}
                onChange={(e) => setCode(e.target.value.replace(/\D/g, ''))}
                required
                autoFocus
              />
            </div>
            <button type="submit" className="primary" disabled={loading}>
              {loading ? 'Verificando...' : 'Verificar email'}
            </button>
          </form>
        )}

        {step === 'tenant' && (
          <form onSubmit={handleRegister}>
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
            <button type="submit" className="primary" disabled={loading}>
              {loading ? 'Registrando...' : 'Registrar tenant'}
            </button>
          </form>
        )}

        <p className="auth-link">
          ¿Ya tiene un tenant? <Link to="/login">Ingresar</Link>
        </p>
      </div>
    </div>
  );
}
