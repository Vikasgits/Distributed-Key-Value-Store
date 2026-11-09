import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  // The Master's CORS policy only allows http://localhost:3000, so fail loudly if the port is taken.
  server: { port: 3000, strictPort: true },
})
