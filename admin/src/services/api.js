/**
 * Cliente HTTP para comunicarse con la UruFactura CloudflareApi.
 * En desarrollo usa el proxy de Vite (/api → localhost:5100).
 * En producción usa la URL configurada en VITE_API_URL.
 */

const BASE_URL = import.meta.env.VITE_API_URL || '/api';

export async function apiRequest(path, { method = 'GET', body, tenantId } = {}) {
  const headers = { 'Content-Type': 'application/json' };
  if (tenantId) headers['X-Tenant-Id'] = tenantId;

  const res = await fetch(`${BASE_URL}${path}`, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined,
  });

  if (!res.ok) {
    const text = await res.text();
    throw new Error(text || `HTTP ${res.status}`);
  }

  const contentType = res.headers.get('content-type') || '';
  if (contentType.includes('application/json')) {
    return res.json();
  }
  if (contentType.includes('application/pdf')) {
    return res.blob();
  }
  return res.text();
}

// --- Endpoints ---

export const health = () => apiRequest('/health');

export const listarCaes = (tenantId) =>
  apiRequest('/cae', { tenantId });

export const registrarCae = (tenantId, cae) =>
  apiRequest('/cae', { method: 'POST', body: cae, tenantId });

export const advertenciasCaes = (tenantId) =>
  apiRequest('/cae/advertencias', { tenantId });

export const emitirCfe = (tenantId, cfeRequest) =>
  apiRequest('/cfe/enviar', { method: 'POST', body: cfeRequest, tenantId });

export const generarXml = (tenantId, cfeRequest) =>
  apiRequest('/cfe/xml', { method: 'POST', body: cfeRequest, tenantId });

export const generarPdfA4 = (tenantId, cfeRequest) =>
  apiRequest('/cfe/pdf/a4', { method: 'POST', body: cfeRequest, tenantId });

export const consultarCfe = (tenantId, consultaRequest) =>
  apiRequest('/cfe/consultar', { method: 'POST', body: consultaRequest, tenantId });
