/**
 * POST /auth/verify-code
 *
 * Verifies the 6-digit code sent to the user's email and returns a JWT session token.
 *
 * Request body: { "email": "user@example.com", "code": "123456" }
 * Response: { "token": "...", "email": "...", "expiresAt": "..." }
 *
 * KV binding: AUTH_CODES (verification codes), AUTH_SESSIONS (active sessions)
 * Env vars: JWT_SECRET (HMAC key for token signing)
 */

export async function onRequestPost(context) {
  const { request, env } = context;

  const corsHeaders = {
    'Access-Control-Allow-Origin': env.ALLOWED_ORIGIN || '*',
    'Access-Control-Allow-Methods': 'POST, OPTIONS',
    'Access-Control-Allow-Headers': 'Content-Type, Authorization',
  };

  try {
    const { email, code } = await request.json();

    if (!email || !code) {
      return jsonResponse({ error: 'Email y código requeridos' }, 400, corsHeaders);
    }

    const key = `code:${email.toLowerCase()}`;
    const stored = await env.AUTH_CODES.get(key, 'json');

    if (!stored) {
      return jsonResponse({ error: 'Código expirado o no encontrado' }, 401, corsHeaders);
    }

    // Rate limit: max 5 attempts per code
    if (stored.attempts >= 5) {
      await env.AUTH_CODES.delete(key);
      return jsonResponse({ error: 'Demasiados intentos. Solicite un nuevo código.' }, 429, corsHeaders);
    }

    if (stored.code !== code) {
      // Increment attempts
      stored.attempts += 1;
      await env.AUTH_CODES.put(key, JSON.stringify(stored), { expirationTtl: 600 });
      return jsonResponse({ error: 'Código incorrecto' }, 401, corsHeaders);
    }

    // Code is valid - delete it (one-time use)
    await env.AUTH_CODES.delete(key);

    // Generate session token (JWT-like HMAC token)
    const expiresAt = Date.now() + (24 * 60 * 60 * 1000); // 24 hours
    const token = await createToken(email.toLowerCase(), expiresAt, env.JWT_SECRET);

    // Store session in KV for server-side validation
    const sessionKey = `session:${token}`;
    await env.AUTH_SESSIONS.put(sessionKey, JSON.stringify({
      email: email.toLowerCase(),
      createdAt: Date.now(),
      expiresAt,
    }), { expirationTtl: 86400 }); // 24h TTL

    return jsonResponse({
      token,
      email: email.toLowerCase(),
      expiresAt: new Date(expiresAt).toISOString(),
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
      'Access-Control-Allow-Methods': 'POST, OPTIONS',
      'Access-Control-Allow-Headers': 'Content-Type, Authorization',
    },
  });
}

async function createToken(email, expiresAt, secret) {
  const header = base64url(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const payload = base64url(JSON.stringify({ sub: email, exp: expiresAt }));
  const data = `${header}.${payload}`;

  if (!secret) {
    throw new Error('JWT_SECRET is not configured');
  }

  const key = await crypto.subtle.importKey(
    'raw',
    new TextEncoder().encode(secret),
    { name: 'HMAC', hash: 'SHA-256' },
    false,
    ['sign']
  );

  const signature = await crypto.subtle.sign('HMAC', key, new TextEncoder().encode(data));
  const sig = base64urlFromBuffer(new Uint8Array(signature));

  return `${data}.${sig}`;
}

function base64url(str) {
  return btoa(unescape(encodeURIComponent(str)))
    .replace(/\+/g, '-')
    .replace(/\//g, '_')
    .replace(/=+$/, '');
}

function base64urlFromBuffer(buf) {
  return btoa(String.fromCharCode(...buf))
    .replace(/\+/g, '-')
    .replace(/\//g, '_')
    .replace(/=+$/, '');
}

function jsonResponse(data, status, headers) {
  return new Response(JSON.stringify(data), {
    status,
    headers: { 'Content-Type': 'application/json', ...headers },
  });
}
