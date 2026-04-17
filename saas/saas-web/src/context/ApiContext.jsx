import { createContext, useContext } from 'react'

export const ApiContext = createContext({ token: null })

// Returns a fetch-compatible function pre-configured with auth header and
// the correct base URL for the current environment (dev proxy vs production).
export function useApi() {
  const { token } = useContext(ApiContext)
  // __API_BASE__ is injected at build time by vite.config.js.
  // In dev it is '' so all requests go through the Vite proxy (/api/...).
  // In production it is the full Railway URL (https://api.yourapp.railway.app).
  const base = typeof __API_BASE__ !== 'undefined' ? __API_BASE__ : ''

  return (path, options = {}) => {
    const headers = { 'Content-Type': 'application/json', ...options.headers }
    if (token) headers['Authorization'] = `Bearer ${token}`
    return fetch(`${base}${path}`, { ...options, headers })
  }
}
