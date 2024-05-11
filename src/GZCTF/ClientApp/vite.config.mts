import eslintPlugin from '@nabla/vite-plugin-eslint'
import react from '@vitejs/plugin-react'
import process from 'process'
import { defineConfig, loadEnv } from 'vite'
import i18nextLoader from '@kainstar/vite-plugin-i18next-loader'
import Pages from 'vite-plugin-pages'
import { prismjsPlugin } from 'vite-plugin-prismjs'
import webfontDownload from 'vite-plugin-webfont-dl'
import tsconfigPaths from 'vite-tsconfig-paths'

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd())

  const TARGET = env.VITE_BACKEND_URL ?? 'http://localhost:5000'

  return {
    server: {
      port: 63000,
      proxy: {
        '/api': TARGET,
        '/swagger': TARGET,
        '/assets': TARGET,
        '/hub': { target: TARGET.replace('http', 'ws'), ws: true },
      },
    },
    preview: {
      port: 64000,
    },
    build: {
      outDir: 'build',
      target: ['es2020'],
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
    plugins: [
      react(),
      tsconfigPaths(),
      eslintPlugin(), // only for development
      webfontDownload([
        'https://fonts.googleapis.com/css2?family=JetBrains+Mono&display=swap',
        'https://fonts.googleapis.com/css2?family=IBM+Plex+Sans:ital,wght@0,400;0,500;1,400&display=swap',
      ]),
      Pages({
        dirs: [
          {
            dir: './src/pages',
            baseRoute: '',
            filePattern: '**/*.tsx',
          },
        ],
      }),
      prismjsPlugin({
        languages: 'all',
        css: true,
      }),
      i18nextLoader({
        paths: ['./src/locales'],
        include: ['**/*.json']
      }),
    ],
  }
})
