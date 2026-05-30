// @ts-check

/**
 * Cloudflare Worker – UruFactura Email Service
 *
 * Dedicated worker for sending transactional emails (verification codes,
 * notifications). Used by the admin Pages Functions via service binding or
 * direct HTTP call.
 *
 * Supports:
 *   - MailChannels (free for Workers, requires SPF DNS record)
 *   - Custom API (Resend, SendGrid) via EMAIL_API_URL + EMAIL_API_KEY
 *
 * Environment variables:
 *   MAIL_FROM       — Sender email address
 *   APP_NAME        — Display name in emails
 *   EMAIL_API_URL   — (optional) Custom email provider endpoint
 *   EMAIL_API_KEY   — (optional) ****** for custom provider
 *   ALLOWED_ORIGINS — Comma-separated allowed origins for CORS
 */

export default {
  /**
   * @param {Request} request
   * @param {Record<string, string>} env
   * @returns {Promise<Response>}
   */
  async fetch(request, env) {
    const corsHeaders = buildCorsHeaders(request, env);

    if (request.method === 'OPTIONS') {
      return new Response(null, { status: 204, headers: corsHeaders });
    }

    if (request.method !== 'POST') {
      return jsonResponse({ error: 'Method not allowed' }, 405, corsHeaders);
    }

    const url = new URL(request.url);

    if (url.pathname === '/send') {
      return handleSend(request, env, corsHeaders);
    }

    return jsonResponse({ error: 'Not found' }, 404, corsHeaders);
  },
};

/**
 * POST /send
 * Body: { to, subject, html, text? }
 */
async function handleSend(request, env, corsHeaders) {
  try {
    const { to, subject, html, text } = await request.json();

    if (!to || !subject || !html) {
      return jsonResponse(
        { error: 'Missing required fields: to, subject, html' },
        400,
        corsHeaders,
      );
    }

    const from = env.MAIL_FROM || 'noreply@urufactura.dev';
    const fromName = env.APP_NAME || 'UruFactura';

    const success = await sendEmail({ from, fromName, to, subject, html, text, env });

    if (!success) {
      return jsonResponse({ error: 'Failed to send email' }, 502, corsHeaders);
    }

    return jsonResponse({ success: true }, 200, corsHeaders);
  } catch (err) {
    return jsonResponse({ error: 'Internal error' }, 500, corsHeaders);
  }
}

/**
 * Send email via MailChannels or custom API.
 */
async function sendEmail({ from, fromName, to, subject, html, text, env }) {
  // If a custom email API is configured, use that
  if (env.EMAIL_API_URL && env.EMAIL_API_KEY) {
    return sendViaCustomApi(env, { from, fromName, to, subject, html, text });
  }

  // Default: MailChannels (free for Cloudflare Workers)
  // Requires SPF record: v=spf1 a mx include:relay.mailchannels.net ~all
  const payload = {
    personalizations: [{ to: [{ email: to }] }],
    from: { email: from, name: fromName },
    subject,
    content: [
      { type: 'text/html', value: html },
      ...(text ? [{ type: 'text/plain', value: text }] : []),
    ],
  };

  const response = await fetch('https://api.mailchannels.net/tx/v1/send', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  });

  return response.status === 202 || response.ok;
}

/**
 * Send via custom email API (Resend, SendGrid compatible).
 */
async function sendViaCustomApi(env, { from, fromName, to, subject, html, text }) {
  const authHeader = 'Bearer ' + env.EMAIL_API_KEY;
  const response = await fetch(env.EMAIL_API_URL, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': authHeader,
    },
    body: JSON.stringify({
      from: `${fromName} <${from}>`,
      to: [to],
      subject,
      html,
      ...(text ? { text } : {}),
    }),
  });
  return response.ok;
}

function buildCorsHeaders(request, env) {
  const origin = request.headers.get('Origin') || '';
  const allowed = (env.ALLOWED_ORIGINS || '*').split(',').map((s) => s.trim());
  const allowOrigin = allowed.includes('*') || allowed.includes(origin) ? origin || '*' : '';

  return {
    'Access-Control-Allow-Origin': allowOrigin,
    'Access-Control-Allow-Methods': 'POST, OPTIONS',
    'Access-Control-Allow-Headers': 'Content-Type, Authorization',
  };
}

function jsonResponse(data, status, headers) {
  return new Response(JSON.stringify(data), {
    status,
    headers: { 'Content-Type': 'application/json', ...headers },
  });
}
