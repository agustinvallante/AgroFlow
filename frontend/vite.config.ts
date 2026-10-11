/// <reference types="vitest/config" />
import react from '@vitejs/plugin-react'
import { defineConfig, loadEnv, type ProxyOptions } from 'vite'

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), 'VITE_')

  // El navegador llama a su propio origen (/api/...) y Vite reenvía a la
  // API. Así no dependemos de la política CORS del backend ni del puerto
  // en que abra el frontend.
  const apiProxy: ProxyOptions = {
    target: env.VITE_API_URL || 'http://localhost:5000',
    changeOrigin: true,
    // Si la API se levanta con el perfil https redirige a
    // https://localhost:7138; la redirección se sigue del lado del servidor
    // y se acepta el certificado de desarrollo.
    followRedirects: true,
    secure: false,
  }
  const proxy = { '/api': apiProxy, '/health': apiProxy }

  return {
    plugins: [react()],
    server: { proxy },
    preview: { proxy },
    test: {
      environment: 'jsdom',
      include: ['src/**/*.test.{ts,tsx}'],
    },
  }
})
