/**
 * UruErp API Proxy – Cloudflare Worker (Hono)
 *
 * Acts as a reverse proxy between the frontend (Cloudflare Pages) and the
 * backend API (Railway).  Every request to this Worker is forwarded upstream
 * with the original method, headers, and body, and the response is streamed
 * back to the caller.
 *
 * Benefits over a direct frontend → Railway connection:
 *   - Single public hostname for the API (CF Worker URL instead of Railway URL)
 *   - CORS handling in one place
 *   - Easy to add rate-limiting, auth checks, logging, etc. in future
 *   - Hides the Railway service URL from browser clients
 *
 * Environment Variables (set in wrangler.toml [vars] or the CF dashboard):
 *   UPSTREAM_API_URL  – Full base URL of the Railway API, e.g.
 *                       https://uruerp-api.up.railway.app
 *   CORS_ORIGINS      – Comma-separated allowed origins, e.g.
 *                       "https://app.pages.dev,https://yourdomain.com"
 *                       Defaults to "*" (open) when empty.
 */

import { Hono } from 'hono'
import { cors } from 'hono/cors'

const app = new Hono()

// ── CORS middleware ──────────────────────────────────────────────────────────
app.use('*', async (c, next) => {
  const rawOrigins = c.env?.CORS_ORIGINS ?? ''
  const allowedOrigins = rawOrigins
    ? rawOrigins.split(',').map((o) => o.trim()).filter(Boolean)
    : ['*']

  const corsMiddleware = cors({
    origin: allowedOrigins.length === 1 && allowedOrigins[0] === '*'
      ? '*'
      : (origin) => (allowedOrigins.includes(origin) ? origin : allowedOrigins[0]),
    allowMethods: ['GET', 'POST', 'PUT', 'PATCH', 'DELETE', 'OPTIONS'],
    allowHeaders: ['Content-Type', 'Authorization'],
    exposeHeaders: ['Content-Length', 'Content-Type'],
    maxAge: 86400,
    credentials: true,
  })

  return corsMiddleware(c, next)
})

// ── Health check (does not hit upstream) ────────────────────────────────────
app.get('/health', (c) => c.json({ ok: true, worker: 'api-proxy' }))

// ── Proxy all /api/* requests to the Railway upstream ───────────────────────
app.all('/api/*', async (c) => {
  const upstream = c.env?.UPSTREAM_API_URL

  if (!upstream) {
    return c.json({ error: 'UPSTREAM_API_URL is not configured.' }, 503)
  }

  const base = upstream.replace(/\/$/, '')
  const { pathname, search } = new URL(c.req.url)
  const targetUrl = `${base}${pathname}${search}`

  // Forward the original request headers, stripping hop-by-hop headers that
  // must not be forwarded to the upstream.
  const forwardHeaders = new Headers(c.req.raw.headers)
  forwardHeaders.delete('host')
  forwardHeaders.delete('cf-connecting-ip')
  forwardHeaders.delete('cf-ray')
  forwardHeaders.delete('cf-visitor')
  forwardHeaders.delete('x-forwarded-proto')
  forwardHeaders.delete('x-real-ip')

  // Add a header so the upstream can identify requests coming through the proxy
  forwardHeaders.set('x-forwarded-host', new URL(c.req.url).hostname)

  const upstreamRequest = new Request(targetUrl, {
    method: c.req.method,
    headers: forwardHeaders,
    body: ['GET', 'HEAD'].includes(c.req.method) ? undefined : c.req.raw.body,
    // Required for streaming body pass-through in the CF runtime
    duplex: 'half',
  })

  const upstreamResponse = await fetch(upstreamRequest)

  // Stream the upstream response back, preserving status and headers
  const responseHeaders = new Headers(upstreamResponse.headers)
  // Remove hop-by-hop headers from upstream response
  responseHeaders.delete('transfer-encoding')

  return new Response(upstreamResponse.body, {
    status: upstreamResponse.status,
    statusText: upstreamResponse.statusText,
    headers: responseHeaders,
  })
})

// ── Catch-all: return 404 for unrecognised paths ─────────────────────────────
app.notFound((c) => c.json({ error: 'Not found. All API paths must begin with /api/' }, 404))

export default app
