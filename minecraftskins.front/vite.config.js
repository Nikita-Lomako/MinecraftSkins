import path from 'path'
import { fileURLToPath } from 'url'
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

const __dirname = fileURLToPath(new URL('.', import.meta.url))

// In dev, backend URL for proxy (e.g. from docker-compose). No default — set in .env.
const devApiOrigin = process.env.VITE_DEV_API_ORIGIN || ''

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      app: path.resolve(__dirname, './src/app'),
      shared: path.resolve(__dirname, './src/shared'),
      entities: path.resolve(__dirname, './src/entities'),
      features: path.resolve(__dirname, './src/features'),
      pages: path.resolve(__dirname, './src/pages'),
    },
  },
  server: {
    proxy: devApiOrigin
      ? {
          '/api': {
            target: devApiOrigin,
            changeOrigin: true,
          },
        }
      : undefined,
  },
})
