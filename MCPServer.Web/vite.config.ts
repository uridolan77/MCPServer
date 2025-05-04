import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import path from 'path';

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 2101,
    proxy: {
      '/api': {
        target: 'http://localhost:2000',
        changeOrigin: true,
        secure: false,
      },
      '/hubs': {
        target: 'http://localhost:2000',
        changeOrigin: true,
        secure: false,
        ws: true, // Enable WebSocket proxy for SignalR
      }
    }
  },
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src')
    }
  }
});
