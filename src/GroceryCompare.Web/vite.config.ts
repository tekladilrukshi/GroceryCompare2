import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      // ASP.NET Core API dev port (src/GroceryCompare.Api launchSettings.json)
      '/api': 'http://localhost:5161',
    },
  },
})
