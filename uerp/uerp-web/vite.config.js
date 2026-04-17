import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// In production (Cloudflare Pages) the frontend talks directly to the Railway
// API via the VITE_API_URL build variable.  In dev, Aspire injects the API
// URL through `services__api__http__0` and Vite proxies /api requests.
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
    // Set VITE_API_URL in Cloudflare Pages build env to your Railway API URL.
    __API_BASE__: JSON.stringify(process.env.VITE_API_URL ?? ''),
  },
})
