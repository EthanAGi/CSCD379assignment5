export default defineNuxtConfig({
  compatibilityDate: '2026-03-09',
  devtools: { enabled: true },

  css: [
    'vuetify/styles',
    '@mdi/font/css/materialdesignicons.css',
    '~/assets/css/main.css'
  ],

  build: {
    transpile: ['vuetify']
  },

  vite: {
    ssr: {
      noExternal: ['vuetify']
    }
  },

  runtimeConfig: {
    public: {
      apiBase: 'http://localhost:5140/api'
    }
  }
})