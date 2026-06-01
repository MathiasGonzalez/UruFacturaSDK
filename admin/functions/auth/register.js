/**
 * POST /auth/register
 *
 * Registers a new tenant. Requires a valid session token (email verified).
 *
 * Request body: {
 *   "tenantId": "mi-empresa",
 *   "razonSocial": "MI EMPRESA SA",
 *   "rutEmisor": "210000000001",
 *   "domicilioFiscal": "AV ITALIA 1234",
 *   "ambiente": "Homologacion"
 * }
 *
 * KV binding: AUTH_SESSIONS (validate token), TENANTS (tenant registry)
 */

export async function onRequestPost(context) {
  const { request, env } = context;

  const corsHeaders = {
    'Access-Control-Allow-Origin': env.ALLOWED_ORIGIN || '*',
    'Access-Control-Allow-Methods': 'POST, OPTIONS',
    'Access-Control-Allow-Headers': 'Content-Type, Authorization',
  };

  try {
    // Validate session token
    const authHeader = request.headers.get('Authorization');
    if (!authHeader || !authHeader.startsWith('Bearer ')) {
      return jsonResponse({ error: 'Token requerido' }, 401, corsHeaders);
    }

    const token = authHeader.slice(7);
    const session = await env.AUTH_SESSIONS.get(`session:${token}`, 'json');

    if (!session || session.expiresAt < Date.now()) {
      return jsonResponse({ error: 'Sesión expirada' }, 401, corsHeaders);
    }

    const body = await request.json();
    const { tenantId, razonSocial, rutEmisor, domicilioFiscal, ambiente } = body;

    // Validate required fields
    if (!tenantId || !razonSocial || !rutEmisor) {
      return jsonResponse({ error: 'tenantId, razonSocial y rutEmisor son obligatorios' }, 400, corsHeaders);
    }

    if (!/^[a-zA-Z0-9_-]+$/.test(tenantId)) {
      return jsonResponse({ error: 'tenantId: solo letras, números, guión (-) y guión bajo (_)' }, 400, corsHeaders);
    }

    if (!/^\d{12}$/.test(rutEmisor)) {
      return jsonResponse({ error: 'rutEmisor debe ser un número de 12 dígitos' }, 400, corsHeaders);
    }

    // Check if tenant already exists
    const existing = await env.TENANTS.get(`tenant:${tenantId}`, 'json');
    if (existing) {
      return jsonResponse({ error: 'Tenant ID ya existe' }, 409, corsHeaders);
    }

    // Register tenant
    const tenantData = {
      tenantId,
      razonSocial,
      rutEmisor,
      domicilioFiscal: domicilioFiscal || '',
      ambiente: ambiente || 'Homologacion',
      ownerEmail: session.email,
      createdAt: new Date().toISOString(),
    };

    await env.TENANTS.put(`tenant:${tenantId}`, JSON.stringify(tenantData));

    // Also store owner → tenant mapping for listing
    const ownerKey = `owner:${session.email}`;
    const ownerTenants = await env.TENANTS.get(ownerKey, 'json') || [];
    ownerTenants.push(tenantId);
    await env.TENANTS.put(ownerKey, JSON.stringify(ownerTenants));

    return jsonResponse({ success: true, tenant: tenantData }, 201, corsHeaders);
  } catch (err) {
    return jsonResponse({ error: 'Error interno' }, 500, corsHeaders);
  }
}

export async function onRequestOptions(context) {
  return new Response(null, {
    status: 204,
    headers: {
      'Access-Control-Allow-Origin': context.env.ALLOWED_ORIGIN || '*',
      'Access-Control-Allow-Methods': 'POST, OPTIONS',
      'Access-Control-Allow-Headers': 'Content-Type, Authorization',
    },
  });
}

function jsonResponse(data, status, headers) {
  return new Response(JSON.stringify(data), {
    status,
    headers: { 'Content-Type': 'application/json', ...headers },
  });
}
