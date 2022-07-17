import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';
import Pages from 'vite-plugin-pages';

const TARGET = 'http://localhost:5000';

// https://vitejs.dev/config/
export default defineConfig({
  server: {
    port: 3000,
    proxy: {
      '/api': TARGET,
      '/swagger': TARGET,
      '/assets': TARGET,
      '/hub': { target: TARGET.replace('http:', 'ws:'), ws: true },
    },
  },
  build: {
    outDir: 'build',
    assetsDir: 'static',
  },
  plugins: [
    react(),
    Pages({
      dirs: [{ dir: 'src/pages', baseRoute: '' }],
    }),
  ],
  esbuild: {
    logOverride: { 'this-is-undefined-in-esm': 'silent' },
  },
});
