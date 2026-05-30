/**
 * Auth API client – communicates with Cloudflare Pages Functions (/auth/*).
 * In local dev, Vite proxies /auth → wrangler pages dev functions.
 */

const AUTH_BASE = import.meta.env.VITE_AUTH_URL || '';

async function authRequest(path, { method = 'POST', body, token } = {}) {
  const headers = { 'Content-Type': 'application/json' };
  if (token) headers['Authorization'] = 'Bearer ' + token;

  const res = await fetch(`${AUTH_BASE}${path}`, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined,
  });

  const data = await res.json();

  if (!res.ok) {
    throw new Error(data.error || `HTTP ${res.status}`);
  }

  return data;
}

/** Send a verification code to email */
export const sendCode = (email) =>
  authRequest('/auth/send-code', { body: { email } });

/** Verify code and get session token */
export const verifyCode = (email, code) =>
  authRequest('/auth/verify-code', { body: { email, code } });

/** Register a new tenant (requires auth token) */
export const registerTenant = (token, tenantData) =>
  authRequest('/auth/register', { body: tenantData, token });

/** Get current session info + tenants */
export const getSession = (token) =>
  authRequest('/auth/session', { method: 'GET', token });

/** List user's tenants */
export const getTenants = (token) =>
  authRequest('/auth/tenants', { method: 'GET', token });
