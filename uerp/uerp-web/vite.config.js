import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// In production (Cloudflare Pages) the frontend talks to the api-proxy
// Cloudflare Worker, which in turn proxies requests to the Railway API.
// Set VITE_API_URL to the Worker URL in the Cloudflare Pages build settings,
// e.g. https://api-proxy.your-account.workers.dev
//
// In development, Aspire injects the API URL through `services__api__http__0`
// and Vite proxies /api requests directly to the local API.
const apiTarget =
  process.env.services__api__http__0 ?? 'http://localhost:5000'

export default defineConfig({
  plugins: [react()],
  server: {
    port: parseInt(process.env.PORT ?? '5173'),
    strictPort: true,
    proxy: {
      '/api': { target: apiTarget, changeOrigin: true },
    },
  },
  define: {
    // Expose the public API base URL at build time for production bundles.
    // Set VITE_API_URL in Cloudflare Pages build settings to your
    // api-proxy Worker URL (e.g. https://api-proxy.your-account.workers.dev).
    __API_BASE__: JSON.stringify(process.env.VITE_API_URL ?? ''),
  },
})
