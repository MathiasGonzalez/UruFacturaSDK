# api-proxy – Cloudflare Worker (Hono)

Reverse-proxy gateway between the **UruErp frontend (Cloudflare Pages)** and the
**backend API (Railway)**. Built with [Hono](https://hono.dev).

## Architecture

```
[uerp-web · CF Pages]
       │  fetch /api/...
       ▼
[api-proxy · CF Workers]  ← this Worker
       │  proxy request
       ▼
[UruErpApp.Api · Railway]
```

The Worker sits between the browser and Railway so that:
- The Railway service URL is never exposed to browser clients
- CORS is handled in one place
- Rate-limiting, auth checks, and logging can be added without touching the API

## Environment variables

| Name | Where | Description |
|------|-------|-------------|
| `UPSTREAM_API_URL` | `wrangler.toml` / CF dashboard | Full Railway API URL, e.g. `https://uruerp-api.up.railway.app` |
| `CORS_ORIGINS` | CF dashboard | Comma-separated allowed origins. Defaults to `*`. |

## Secrets

None required. If you add JWT validation middleware in the future, set `JWT_SECRET`
to the same value as `Jwt__Secret` in the Railway API.

## Local development

```bash
npm install
# Set UPSTREAM_API_URL to your local or remote API:
UPSTREAM_API_URL=http://localhost:5000 npm run dev
```

The Worker listens at `http://localhost:8787`.

## Deploy

```bash
# Edit wrangler.toml first: set UPSTREAM_API_URL to the Railway API URL
npm run deploy
```

Or use the GitHub Actions workflow `deploy-workers.yml` (the `deploy-api-proxy` job).

## Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/health` | Health check (not proxied) |
| `*` | `/api/*` | Proxied to upstream Railway API |
