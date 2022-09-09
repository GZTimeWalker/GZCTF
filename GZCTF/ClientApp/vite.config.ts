import eslintPlugin from '@nabla/vite-plugin-eslint'
import react from '@vitejs/plugin-react'
import { defineConfig, loadEnv } from 'vite'
import Pages from 'vite-plugin-pages'
import prismjs from 'vite-plugin-prismjs'
import webfontDownload from 'vite-plugin-webfont-dl'
import tsconfigPaths from 'vite-tsconfig-paths'

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd())

  const TARGET = env.VITE_BACKEND_URL ?? 'http://localhost:5000'

  return {
    server: {
      port: 3000,
      proxy: {
        '/api': TARGET,
        '/swagger': TARGET,
        '/assets': TARGET,
        '/hub': { target: TARGET.replace('http', 'ws'), ws: true },
      },
    },
    build: {
      outDir: 'build',
      assetsDir: 'static',
      chunkSizeWarningLimit: 2000,
      rollupOptions: {
        output: {
          chunkFileNames: 'static/[hash].js',
          assetFileNames: 'static/[hash].[ext]',
          entryFileNames: 'static/[name].[hash].js',
          compact: true,
        },
      },
    },
    esbuild: {
      logOverride: { 'this-is-undefined-in-esm': 'silent' },
    },
    plugins: [
      react(),
      eslintPlugin(), // only for development
      webfontDownload([
        'https://fonts.googleapis.com/css2?family=JetBrains+Mono&display=swap',
        'https://fonts.googleapis.com/css2?family=IBM+Plex+Sans:ital,wght@0,400;0,500;1,400&display=swap',
      ]),
      Pages({
        dirs: [{ dir: 'src/pages', baseRoute: '' }],
      }),
      tsconfigPaths(),
      prismjs({
        languages: 'all',
        plugins: ['line-numbers'],
        css: true,
      }),
    ],
  }
})
