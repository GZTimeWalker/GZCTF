import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';
import Pages from 'vite-plugin-pages';
import progress from 'vite-plugin-progress'
import { chunkSplitPlugin } from 'vite-plugin-chunk-split';

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
    assetsDir: 'static'
  },
  esbuild: {
    logOverride: { 'this-is-undefined-in-esm': 'silent' },
  },
  plugins: [
    react(),
    progress(),
    chunkSplitPlugin({
      customSplitting: {
        'account': [/src\/pages\/account\/.*/],
      }
    }),
    Pages({
      dirs: [{ dir: 'src/pages', baseRoute: '' }],
    }),
  ],
});
