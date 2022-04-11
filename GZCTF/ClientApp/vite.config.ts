import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

const TARGET = 'http://localhost:5000';

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': TARGET,
      '/swagger': TARGET,
      '/hub': { target: TARGET.replace('http:', 'ws:'), ws: true }
    }
  }
});
