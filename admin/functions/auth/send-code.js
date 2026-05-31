/**
 * POST /auth/send-code
 *
 * Sends a 6-digit verification code to the provided email address.
 * Uses Cloudflare's MailChannels integration (free for Workers) to send email.
 *
 * Request body: { "email": "user@example.com" }
 *
 * KV binding: AUTH_CODES (stores code → email mapping with 10 min TTL)
 * Env vars: MAIL_FROM (sender address), APP_NAME (display name)
 */

export async function onRequestPost(context) {
  const { request, env } = context;

  const corsHeaders = {
    'Access-Control-Allow-Origin': env.ALLOWED_ORIGIN || '*',
    'Access-Control-Allow-Methods': 'POST, OPTIONS',
    'Access-Control-Allow-Headers': 'Content-Type',
  };

  try {
    const { email } = await request.json();

    if (!email || !isValidEmail(email)) {
      return jsonResponse({ error: 'Email inválido' }, 400, corsHeaders);
    }

    const code = generateCode();
    const key = `code:${email.toLowerCase()}`;

    // Store code in KV with 10-minute expiration
    await env.AUTH_CODES.put(key, JSON.stringify({
      code,
      email: email.toLowerCase(),
      createdAt: Date.now(),
      attempts: 0,
    }), { expirationTtl: 600 });

    // Send email via MailChannels (Cloudflare Workers integration)
    const mailFrom = env.MAIL_FROM || 'noreply@urufactura.dev';
    const appName = env.APP_NAME || 'UruFactura Admin';

    const emailSent = await sendEmail({
      from: mailFrom,
      fromName: appName,
      to: email,
      subject: `${appName} - Código de verificación: ${code}`,
      html: buildEmailHtml(code, appName),
      env,
    });

    if (!emailSent) {
      return jsonResponse({ error: 'Error al enviar email' }, 500, corsHeaders);
    }

    return jsonResponse({ success: true, message: 'Código enviado' }, 200, corsHeaders);
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
      'Access-Control-Allow-Headers': 'Content-Type',
    },
  });
}

function generateCode() {
  const array = new Uint32Array(1);
  crypto.getRandomValues(array);
  return String(array[0] % 1000000).padStart(6, '0');
}

function isValidEmail(email) {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
}

function jsonResponse(data, status, headers) {
  return new Response(JSON.stringify(data), {
    status,
    headers: { 'Content-Type': 'application/json', ...headers },
  });
}

async function sendEmail({ from, fromName, to, subject, html, env }) {
  // Option 1: Use the dedicated email worker via service binding (recommended)
  // Configure in wrangler.toml: [[services]] binding = "EMAIL_WORKER" service = "urufactura-email"
  if (env.EMAIL_WORKER) {
    const headers = { 'Content-Type': 'application/json' };
    if (env.EMAIL_API_KEY) {
      headers['Authorization'] = `****** {env.EMAIL_API_KEY}`;
    }
    const response = await env.EMAIL_WORKER.fetch(new Request('https://email/send', {
      method: 'POST',
      headers,
      body: JSON.stringify({ to, subject, html }),
    }));
    return response.ok;
  }

  // Option 2: Use a custom email API (Resend, SendGrid) directly
  if (env.EMAIL_API_URL && env.EMAIL_API_KEY) {
    return sendViaCustomApi(env, { from, fromName, to, subject, html });
  }

  // Option 3: Use Cloudflare MailChannels Send API (free for Workers)
  // Docs: https://developers.cloudflare.com/workers/tutorials/send-emails-with-mailchannels/
  const payload = {
    personalizations: [{ to: [{ email: to }] }],
    from: { email: from, name: fromName },
    subject,
    content: [{ type: 'text/html', value: html }],
  };

  const response = await fetch('https://api.mailchannels.net/tx/v1/send', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  });

  return response.status === 202 || response.ok;
}

async function sendViaCustomApi(env, { from, fromName, to, subject, html }) {
  // Generic email API integration (works with Resend, SendGrid, etc.)
  const response = await fetch(env.EMAIL_API_URL, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': 'Bearer ' + env.EMAIL_API_KEY,
    },
    body: JSON.stringify({
      from: fromName + ' <' + from + '>',
      to: [to],
      subject,
      html,
    }),
  });
  return response.ok;
}

function buildEmailHtml(code, appName) {
  return `
<!DOCTYPE html>
<html>
<head><meta charset="utf-8"></head>
<body style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; padding: 40px 20px; background: #f5f5f5;">
  <div style="max-width: 400px; margin: 0 auto; background: white; border-radius: 8px; padding: 40px; box-shadow: 0 2px 8px rgba(0,0,0,0.1);">
    <h2 style="color: #1a1a2e; margin-top: 0;">${appName}</h2>
    <p style="color: #555;">Tu código de verificación es:</p>
    <div style="background: #f0f4ff; border-radius: 8px; padding: 20px; text-align: center; margin: 20px 0;">
      <span style="font-size: 32px; font-weight: bold; letter-spacing: 8px; color: #1a1a2e;">${code}</span>
    </div>
    <p style="color: #888; font-size: 14px;">Este código expira en 10 minutos. Si no solicitaste este código, ignora este mensaje.</p>
  </div>
</body>
</html>`;
}
