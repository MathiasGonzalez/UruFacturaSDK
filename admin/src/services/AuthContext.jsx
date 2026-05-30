import { createContext, useContext, useState, useCallback } from 'react';

const AuthContext = createContext(null);

const SESSION_KEY = 'urufactura_session';
const TENANT_KEY = 'urufactura_active_tenant';

function loadSession() {
  try {
    const raw = localStorage.getItem(SESSION_KEY);
    if (!raw) return null;
    const session = JSON.parse(raw);
    // Check expiration
    if (session.expiresAt && new Date(session.expiresAt) < new Date()) {
      localStorage.removeItem(SESSION_KEY);
      return null;
    }
    return session;
  } catch {
    return null;
  }
}

function loadActiveTenant() {
  try {
    const raw = localStorage.getItem(TENANT_KEY);
    return raw ? JSON.parse(raw) : null;
  } catch {
    return null;
  }
}

export function AuthProvider({ children }) {
  const [session, setSessionState] = useState(loadSession);
  const [tenant, setTenantState] = useState(loadActiveTenant);

  const login = useCallback((sessionData) => {
    localStorage.setItem(SESSION_KEY, JSON.stringify(sessionData));
    setSessionState(sessionData);
  }, []);

  const setActiveTenant = useCallback((tenantData) => {
    localStorage.setItem(TENANT_KEY, JSON.stringify(tenantData));
    setTenantState(tenantData);
  }, []);

  const logout = useCallback(() => {
    localStorage.removeItem(SESSION_KEY);
    localStorage.removeItem(TENANT_KEY);
    setSessionState(null);
    setTenantState(null);
  }, []);

  const isAuthenticated = !!session?.token;

  return (
    <AuthContext.Provider value={{ session, tenant, login, logout, setActiveTenant, isAuthenticated }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be inside AuthProvider');
  return ctx;
}
