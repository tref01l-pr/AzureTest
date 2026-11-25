import { fileURLToPath, URL } from 'node:url'
import react from '@vitejs/plugin-react'
import { defineConfig, loadEnv } from 'vite'

const dir = (path: string) => fileURLToPath(new URL(path, import.meta.url))

const ROOT = dir('..')
const rootEnv = loadEnv('development', ROOT, '')
const apiPort = rootEnv.API_PORT || '5041'
const webPort = Number(rootEnv.WEB_PORT || 3000)

export default defineConfig({
  envDir: ROOT,
  plugins: [react()],
  resolve: {
    alias: {
      '@app': dir('./src/app'),
      '@pages': dir('./src/pages'),
      '@widgets': dir('./src/widgets'),
      '@features': dir('./src/features'),
      '@entities': dir('./src/entities'),
      '@shared': dir('./src/shared'),
      '@tests': dir('./tests'),
    },
  },
  server: {
    port: webPort,
    strictPort: true,
    proxy: {
      '/api': {
        target: `http://localhost:${apiPort}`,
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/api/, ''),
      },
    },
  },
})
