/**
 * UruErp Invoice Mailer – Cloudflare Worker
 *
 * Receives a POST /notify request from the UruErpApp.Api and sends a
 * transactional email using the MailChannels API (free for Cloudflare Workers).
 *
 * Secrets expected (set via `wrangler secret put`):
 *   SENDER_EMAIL        – e.g. "noreply@yourdomain.com"  (must be Cloudflare-verified)
 *   SENDER_NAME         – e.g. "UruErp Facturación"
 *   ALLOWED_API_SECRET  – arbitrary shared secret for simple caller auth
 *
 * @typedef {Object} InvoicePayload
 * @property {string}  tenantName
 * @property {string}  recipientEmail
 * @property {string}  recipientName
 * @property {number}  invoiceId
 * @property {number}  invoiceNumber
 * @property {number}  tipoCfe
 * @property {string}  tipoCfeLabel
 * @property {number}  total
 * @property {string|null} pdfUrl
 */

export default {
  async fetch(request, env) {
    if (request.method !== 'POST') {
      return new Response('Method Not Allowed', { status: 405 });
    }

    // ── Simple shared-secret auth ─────────────────────────────────────────
    const apiSecret = request.headers.get('X-Api-Secret');
    if (!env.ALLOWED_API_SECRET || apiSecret !== env.ALLOWED_API_SECRET) {
      return new Response('Unauthorized', { status: 401 });
    }

    /** @type {InvoicePayload} */
    let payload;
    try {
      payload = await request.json();
    } catch {
      return new Response('Invalid JSON body', { status: 400 });
    }

    const {
      tenantName, recipientEmail, recipientName,
      invoiceId, invoiceNumber, tipoCfeLabel, total, pdfUrl,
    } = payload;

    if (!recipientEmail || !invoiceNumber) {
      return new Response('Missing required fields', { status: 422 });
    }

    // ── Build email HTML ─────────────────────────────────────────────────
    const formattedTotal = Number(total).toLocaleString('es-UY', {
      style: 'currency',
      currency: 'UYU',
      minimumFractionDigits: 2,
    });

    const pdfSection = pdfUrl
      ? `<p style="margin:16px 0"><a href="${pdfUrl}" style="background:#2563eb;color:#fff;padding:10px 20px;border-radius:6px;text-decoration:none;font-weight:600">📄 Descargar PDF</a></p>`
      : '';

    const htmlBody = `<!doctype html>
<html lang="es"><head><meta charset="utf-8"><title>Nuevo comprobante</title></head>
<body style="font-family:system-ui,sans-serif;color:#0f172a;max-width:560px;margin:0 auto;padding:24px">
  <div style="background:#eff6ff;border:1px solid #bfdbfe;border-radius:12px;padding:24px;margin-bottom:20px">
    <h1 style="margin:0 0 4px;font-size:20px">🧾 Nuevo comprobante emitido</h1>
    <p style="margin:0;color:#64748b;font-size:14px">${tenantName}</p>
  </div>

  <p>Hola <strong>${recipientName}</strong>,</p>
  <p>Se emitió el siguiente comprobante fiscal:</p>

  <table style="width:100%;border-collapse:collapse;font-size:14px;margin:16px 0">
    <tr style="background:#f8fafc"><td style="padding:10px 12px;border:1px solid #e2e8f0;font-weight:600">Tipo</td>
      <td style="padding:10px 12px;border:1px solid #e2e8f0">${tipoCfeLabel}</td></tr>
    <tr><td style="padding:10px 12px;border:1px solid #e2e8f0;font-weight:600">Número</td>
      <td style="padding:10px 12px;border:1px solid #e2e8f0">${invoiceNumber}</td></tr>
    <tr style="background:#f8fafc"><td style="padding:10px 12px;border:1px solid #e2e8f0;font-weight:600">Total</td>
      <td style="padding:10px 12px;border:1px solid #e2e8f0;font-weight:700;color:#16a34a">${formattedTotal}</td></tr>
  </table>

  ${pdfSection}

  <hr style="border:none;border-top:1px solid #e2e8f0;margin:24px 0">
  <p style="font-size:12px;color:#64748b">Este email fue generado automáticamente por <strong>UruErp</strong>.
    ID interno: ${invoiceId}</p>
</body></html>`;

    // ── Send via MailChannels API ─────────────────────────────────────────
    const emailPayload = {
      personalizations: [{
        to: [{ email: recipientEmail, name: recipientName }],
      }],
      from: {
        email: env.SENDER_EMAIL,
        name: env.SENDER_NAME ?? 'UruErp Facturación',
      },
      subject: `Nuevo comprobante ${tipoCfeLabel} Nº ${invoiceNumber} – ${formattedTotal}`,
      content: [
        { type: 'text/html', value: htmlBody },
      ],
    };

    const mcResponse = await fetch('https://api.mailchannels.net/tx/v1/send', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(emailPayload),
    });

    if (!mcResponse.ok) {
      const body = await mcResponse.text();
      console.error('MailChannels error:', mcResponse.status, body);
      return new Response(`Email send failed: ${mcResponse.status}`, { status: 502 });
    }

    return new Response(JSON.stringify({ ok: true, invoiceId }), {
      status: 200,
      headers: { 'Content-Type': 'application/json' },
    });
  },
};
