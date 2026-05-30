/**
 * GET /auth/session
 *
 * Returns current session info and associated tenants.
 * Requires Authorization: ****** header.
 *
 * KV binding: AUTH_SESSIONS, TENANTS
 */

export async function onRequestGet(context) {
  const { request, env } = context;

  const corsHeaders = {
    'Access-Control-Allow-Origin': env.ALLOWED_ORIGIN || '*',
    'Access-Control-Allow-Methods': 'GET, OPTIONS',
    'Access-Control-Allow-Headers': 'Content-Type, Authorization',
  };

  try {
    const authHeader = request.headers.get('Authorization');
    if (!authHeader || !authHeader.startsWith('Bearer ')) {
      return jsonResponse({ error: 'Token requerido' }, 401, corsHeaders);
    }

    const token = authHeader.slice(7);
    const session = await env.AUTH_SESSIONS.get(`session:${token}`, 'json');

    if (!session || session.expiresAt < Date.now()) {
      return jsonResponse({ error: 'Sesión expirada' }, 401, corsHeaders);
    }

    // Get user's tenants
    const ownerTenants = await env.TENANTS.get(`owner:${session.email}`, 'json') || [];

    // Load tenant details
    const tenants = [];
    for (const id of ownerTenants) {
      const tenant = await env.TENANTS.get(`tenant:${id}`, 'json');
      if (tenant) tenants.push(tenant);
    }

    return jsonResponse({
      email: session.email,
      createdAt: new Date(session.createdAt).toISOString(),
      expiresAt: new Date(session.expiresAt).toISOString(),
      tenants,
    }, 200, corsHeaders);
  } catch (err) {
    return jsonResponse({ error: 'Error interno' }, 500, corsHeaders);
  }
}

export async function onRequestOptions(context) {
  return new Response(null, {
    status: 204,
    headers: {
      'Access-Control-Allow-Origin': context.env.ALLOWED_ORIGIN || '*',
      'Access-Control-Allow-Methods': 'GET, OPTIONS',
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
