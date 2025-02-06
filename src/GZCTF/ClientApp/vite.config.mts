import eslintPlugin from '@nabla/vite-plugin-eslint'
import react from '@vitejs/plugin-react'
import process from 'process'
import { defineConfig, loadEnv } from 'vite'
import banner from 'vite-plugin-banner'
import { optimizeCssModules } from 'vite-plugin-optimize-css-modules'
import Pages from 'vite-plugin-pages'
import { prismjsPlugin } from 'vite-plugin-prismjs'
import webfontDownload from 'vite-plugin-webfont-dl'
import tsconfigPaths from 'vite-tsconfig-paths'
import i18nVirtualManifest from './plugins/vite-i18n-virtual-manifest'

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd())

  const TARGET = env.VITE_BACKEND_URL ?? 'http://localhost:8080'

  const BANNER =
    `/* The GZCTF Project @${env.VITE_APP_GIT_NAME ?? 'unknown'}\n * \n` +
    ` * Commit  : ${env.VITE_APP_GIT_SHA ?? 'Unofficial build version'}\n` +
    ` * Build   : ${env.VITE_APP_BUILD_TIMESTAMP ?? new Date().toISOString()}\n` +
    ' * License : GNU Affero General Public License v3.0\n * \n' +
    ' * Copyright © 2022-now @GZTimeWalker, All Rights Reserved.\n */'

  console.log(`Using backend URL: ${TARGET}`)

  return {
    server: {
      port: 63000,
      proxy: {
        '/api': TARGET,
        '/swagger': TARGET,
        '/assets': TARGET,
        '/hub': { target: TARGET.replace('http', 'ws'), ws: true },
        '/favicon.webp': TARGET,
      },
    },
    preview: {
      port: 64000,
    },
    build: {
      outDir: 'build',
      assetsDir: 'static',
      cssCodeSplit: false,
      chunkSizeWarningLimit: 1600,
      reportCompressedSize: false,
      rollupOptions: {
        output: {
          compact: true,
          hashCharacters: 'base36',
          chunkFileNames: 'static/[hash].js',
          assetFileNames: 'static/[hash].[ext]',
          entryFileNames: 'static/[name].[hash].js',
        },
      },
    },
    html: {
      cspNonce: '%nonce%',
    },
    plugins: [
      react(),
      banner(BANNER),
      tsconfigPaths(),
      eslintPlugin(), // only for development
      webfontDownload(
        [
          'https://fonts.googleapis.com/css2?family=JetBrains+Mono:ital,wght@0,100..800;1,100..800&display=swap',
          'https://fonts.googleapis.com/css2?family=IBM+Plex+Sans:ital,wght@0,300;0,400;0,500;0,600;0,700;1,300;1,400;1,500;1,700&display=swap',
        ],
        { injectAsStyleTag: false, async: false }
      ),
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
      i18nVirtualManifest(),
      optimizeCssModules(),
    ],
  }
})
