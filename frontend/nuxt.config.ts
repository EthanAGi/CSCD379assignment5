export default defineNuxtConfig({
  compatibilityDate: '2026-03-09',

  devtools: { enabled: true },

  ssr: false,

  css: [
    'vuetify/styles',
    '@mdi/font/css/materialdesignicons.css',
    '~/assets/css/main.css'
  ],//Push

  build: {
    transpile: ['vuetify']
  },

  runtimeConfig: {
    public: {
      apiBase: process.env.NUXT_PUBLIC_API_BASE || 'http://localhost:5140/api'
    }
  }
})