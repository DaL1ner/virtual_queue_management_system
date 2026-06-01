import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import path from 'path'

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue()],
  // В production сборка будет раздаваться бэкендом по пути /app/
  // В dev-режиме (Vite dev server) base = '/'
  base: process.env.NODE_ENV === 'production' ? '/app/' : '/',
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src')
    }
  },
  server: {
    port: 5174,
    proxy: {
      '/api': {
        target: 'http://localhost:8080', // Ваш .NET бэкенд
        changeOrigin: true,
        secure: false // Для localhost без HTTPS
      }
    }
  }
})
