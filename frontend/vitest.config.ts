import { defineConfig } from 'vitest/config'
import { resolve } from 'path'

export default defineConfig({
  test: {
    globals: true,
    environment: 'happy-dom',
    include: ['tests/**/*.test.ts'],
    setupFiles: ['tests/setup.ts']
  },
  resolve: {
    alias: {
      '~': resolve(__dirname, '.'),
      '#imports': resolve(__dirname, 'tests/__mocks__/nuxt-imports.ts'),
      '#app': resolve(__dirname, 'tests/__mocks__/nuxt-app.ts')
    }
  }
})
