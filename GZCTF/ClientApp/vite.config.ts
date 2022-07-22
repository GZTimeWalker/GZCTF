import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';
import progress from 'vite-plugin-progress'
import Pages from 'vite-plugin-pages';

const TARGET = 'http://localhost:5000';

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
    target: 'esnext',
    rollupOptions: {
      output: {
        chunkFileNames: "static/[hash].js",
        assetFileNames: "static/[hash].[ext]",
        entryFileNames: "static/[name].js",
        compact: true
      }
    }
  },
  esbuild: {
    logOverride: { 'this-is-undefined-in-esm': 'silent' },
  },
  plugins: [
    react(),
    progress(),
    Pages({
      dirs: [{ dir: 'src/pages', baseRoute: '' }]
    })
  ],
});
