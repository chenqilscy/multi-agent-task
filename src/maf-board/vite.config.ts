import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    host: true,
    allowedHosts: ['host.docker.internal', 'localhost', '.localhost'],
    proxy: {
      '/api': {
        target: 'http://localhost:5012',
        changeOrigin: true,
      },
      '/hub': {
        target: 'http://localhost:5012',
        changeOrigin: true,
        ws: true,
      },
    },
  },
})
