import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../services/AuthContext.jsx';
import { sendCode, verifyCode, getTenants } from '../services/auth.js';
import './Auth.css';

export default function Login() {
  const { login, setActiveTenant } = useAuth();
  const navigate = useNavigate();
  const [step, setStep] = useState('email'); // 'email' | 'code'
  const [email, setEmail] = useState('');
  const [code, setCode] = useState('');
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
      login(session);

      // Load user's tenants and auto-select if only one
      try {
        const { tenants } = await getTenants(session.token);
        if (tenants && tenants.length === 1) {
          setActiveTenant(tenants[0]);
        }
      } catch {
        // No tenants yet — that's fine
      }

      navigate('/');
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="auth-page">
      <div className="auth-card card">
        <h1>UruFactura Admin</h1>
        <p className="subtitle">
          {step === 'email'
            ? 'Ingrese su email para recibir un código de verificación'
            : `Ingrese el código enviado a ${email}`}
        </p>

        {error && <div className="error-msg">{error}</div>}

        {step === 'email' ? (
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
            </div>
            <button type="submit" className="primary" disabled={loading}>
              {loading ? 'Enviando...' : 'Enviar código'}
            </button>
          </form>
        ) : (
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
              <small>Código de 6 dígitos enviado a {email}</small>
            </div>
            <button type="submit" className="primary" disabled={loading}>
              {loading ? 'Verificando...' : 'Verificar'}
            </button>
            <button
              type="button"
              className="secondary"
              onClick={() => { setStep('email'); setCode(''); setError(''); }}
            >
              Cambiar email
            </button>
          </form>
        )}

        <p className="auth-link">
          ¿Primera vez? <Link to="/register">Registrar nuevo tenant</Link>
        </p>
      </div>
    </div>
  );
}
