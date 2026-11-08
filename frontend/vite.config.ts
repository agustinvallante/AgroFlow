/// <reference types="vitest/config" />
import react from '@vitejs/plugin-react'
import { defineConfig, loadEnv } from 'vite'
import { DASHBOARD_KEY_ENV, createApiProxy, readDashboardKey } from './server/dashboardProxy.ts'

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), 'VITE_')
  // Sin prefijo VITE_: Vite nunca expone esta variable al navegador. loadEnv
  // la toma del entorno del proceso o de .env.local (ignorado por Git).
  const dashboardKey = readDashboardKey(loadEnv(mode, process.cwd(), ['VITE_', DASHBOARD_KEY_ENV]))

  // El navegador llama a su propio origen (/api/...) y Vite reenvía a la
  // API. Así no dependemos de la política CORS del backend ni del puerto
  // en que abra el frontend. Las rutas del dashboard reciben la clave del
  // lado servidor (server/dashboardProxy.ts).
  const proxy = createApiProxy(env.VITE_API_URL || 'http://localhost:5000', dashboardKey)

  return {
    plugins: [react()],
    server: { proxy },
    preview: { proxy },
    test: {
      environment: 'jsdom',
      include: ['src/**/*.test.{ts,tsx}', 'server/**/*.test.ts'],
    },
  }
})
