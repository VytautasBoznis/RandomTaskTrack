import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  server: {
    port: 3000,
    // In production the ingress serves this bundle and the API from one origin,
    // so requests go to a relative /api. This proxy reproduces that in dev.
    proxy: {
      '/api': 'http://localhost:5080',
    },
  },
});
