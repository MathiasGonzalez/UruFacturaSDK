import { createContext, useContext, useState, useCallback } from 'react';

const AuthContext = createContext(null);

const STORAGE_KEY = 'urufactura_tenant';

function loadFromStorage() {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? JSON.parse(raw) : null;
  } catch {
    return null;
  }
}

export function AuthProvider({ children }) {
  const [tenant, setTenantState] = useState(loadFromStorage);

  const login = useCallback((tenantData) => {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(tenantData));
    setTenantState(tenantData);
  }, []);

  const logout = useCallback(() => {
    localStorage.removeItem(STORAGE_KEY);
    setTenantState(null);
  }, []);

  return (
    <AuthContext.Provider value={{ tenant, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be inside AuthProvider');
  return ctx;
}
