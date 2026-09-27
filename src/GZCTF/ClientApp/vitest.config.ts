import path from 'node:path'
import { defineConfig } from 'vitest/config'

export default defineConfig({
  resolve: {
    alias: {
      '@Api': path.resolve(import.meta.dirname, 'src/Api.ts'),
    },
  },
  test: {
    include: ['src/**/*.test.ts'],
  },
})
