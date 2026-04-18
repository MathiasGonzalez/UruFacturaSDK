import { createContext, useContext } from 'react'

export const ApiContext = createContext({ token: null })

// Returns a fetch-compatible function pre-configured with auth header and
// the correct base URL for the current environment.
//
// Production: __API_BASE__ is set to the api-proxy Cloudflare Worker URL
//   (e.g. https://api-proxy.your-account.workers.dev) so all requests go
//   through the Worker reverse proxy → Railway API.
// Development: __API_BASE__ is '' so all /api/... requests go through the
//   Vite dev-server proxy → local API directly.
export function useApi() {
  const { token } = useContext(ApiContext)
  const base = typeof __API_BASE__ !== 'undefined' ? __API_BASE__ : ''

  return (path, options = {}) => {
    const headers = { 'Content-Type': 'application/json', ...options.headers }
    if (token) headers['Authorization'] = `Bearer ${token}`
    return fetch(`${base}${path}`, { ...options, headers })
  }
}
