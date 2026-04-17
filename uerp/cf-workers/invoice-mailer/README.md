# invoice-mailer – Cloudflare Worker

Sends transactional invoice-notification emails via **MailChannels** (free for Cloudflare Workers).

## Setup

```bash
npm install
```

## Configure secrets

```bash
# The verified sender address in Cloudflare Email Routing
wrangler secret put SENDER_EMAIL

# Friendly display name
wrangler secret put SENDER_NAME

# Shared secret the API sends in the X-Api-Secret header
wrangler secret put ALLOWED_API_SECRET
```

## Local development

```bash
npm run dev
```

The Worker listens at `http://localhost:8787`.

## Deploy

```bash
npm run deploy
```

Or use the GitHub Actions workflow `deploy-workers.yml`.

## Payload (POST /notify)

```json
{
  "tenantName": "Mi Empresa S.A.",
  "recipientEmail": "cliente@example.com",
  "recipientName": "Juan Pérez",
  "invoiceId": 42,
  "invoiceNumber": 1001,
  "tipoCfe": 111,
  "tipoCfeLabel": "EFactura",
  "total": 12350.00,
  "pdfUrl": "https://pub.r2.dev/..."
}
```

Set `X-Api-Secret: <your-secret>` header on all requests.
