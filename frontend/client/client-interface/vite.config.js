import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue()],
  // В production сборка будет раздаваться бэкендом по пути /client/
  // В dev-режиме (Vite dev server) base = '/'
  base: process.env.NODE_ENV === 'production' ? '/client/' : '/',
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:8080', // Ваш .NET бэкенд
        changeOrigin: true,
        secure: false // Для localhost без HTTPS
      }
    }
  }
})
