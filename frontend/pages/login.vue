<script setup lang="ts">
import { ref, reactive, onMounted, nextTick } from 'vue'
import { definePageMeta, navigateTo } from '#imports'
import { useApi } from '~/composables/useApi'
import { useAuth } from '~/composables/useAuth'

definePageMeta({
  layout: false
})

const isLogin = ref(true)
const loading = ref(false)
const pageReady = ref(false)
const errorMessage = ref('')
const successMessage = ref('')
const debugMessage = ref('Auth page initializing...')

const form = reactive({
  name: '',
  email: '',
  password: ''
})

const { apiFetch } = useApi()
const auth = useAuth()

const logStep = (message: string) => {
  debugMessage.value = message
  console.log(`[Auth Debug] ${message}`)
}

const resetMessages = () => {
  errorMessage.value = ''
  successMessage.value = ''
}

const resetFormForModeSwitch = () => {
  form.name = ''
  form.email = ''
  form.password = ''
  resetMessages()
}

const switchToLogin = () => {
  isLogin.value = true
  resetFormForModeSwitch()
}

const switchToSignup = () => {
  isLogin.value = false
  resetFormForModeSwitch()
}

const goToDashboard = async () => {
  await navigateTo('/app/dashboard', { replace: true })
}

const handleSubmit = async () => {
  resetMessages()

  const email = form.email.trim()
  const password = form.password
  const name = form.name.trim()

  if (!email || !password) {
    errorMessage.value = 'Email and password are required.'
    return
  }

  if (!isLogin.value && !name) {
    errorMessage.value = 'Name is required.'
    return
  }

  loading.value = true

  try {
    if (isLogin.value) {
      const res = await apiFetch<{ token: string }>('/auth/login', {
        method: 'POST',
        body: {
          email,
          password
        }
      })

      if (!res?.token) {
        throw new Error('No token was returned from login.')
      }

      auth.setToken(res.token)
      auth.init()
      await nextTick()

      if (!auth.token.value) {
        throw new Error('Token was not persisted after login.')
      }

      await goToDashboard()
    } else {
      await apiFetch('/auth/register', {
        method: 'POST',
        body: {
          name,
          email,
          password
        }
      })

      successMessage.value = 'Account created successfully. You can now log in.'
      isLogin.value = true
      form.name = ''
      form.password = ''
    }
  } catch (error: any) {
    console.error('Authentication failed:', error)

    errorMessage.value =
      error?.data?.message ||
      error?.data?.title ||
      error?.message ||
      'Authentication failed. Please check your details and try again.'
  } finally {
    loading.value = false
  }
}

onMounted(async () => {
  logStep('Auth page mounted.')
  auth.init()

  if (auth.token.value) {
    await goToDashboard()
    return
  }

  pageReady.value = true
  logStep('No existing token found. Auth page ready.')
})
</script>

<template>
  <div class="login-page">
    <div class="login-shell">
      <div class="logo-wrap">
        <div class="logo-row">
          <v-icon icon="mdi-book-open-page-variant-outline" size="40" color="#6366f1" />
          <span>CanonGuard</span>
        </div>
        <p>AI Story Bible for Writers</p>
      </div>

      <div class="debug-box">
        <strong>Debug:</strong> {{ debugMessage }}
      </div>

      <div v-if="!pageReady" class="login-card panel loading-card">
        <v-progress-circular indeterminate color="#818cf8" :size="36" :width="3" />
        <p>Checking your session...</p>
      </div>

      <div v-else class="login-card panel">
        <div class="login-tabs">
          <button
            type="button"
            class="login-tab"
            :class="{ active: isLogin }"
            @click="switchToLogin"
          >
            Login
          </button>

          <button
            type="button"
            class="login-tab"
            :class="{ active: !isLogin }"
            @click="switchToSignup"
          >
            Sign Up
          </button>
        </div>

        <form class="login-form" @submit.prevent="handleSubmit">
          <div v-if="!isLogin">
            <label class="form-label" for="name">Full Name</label>
            <input
              id="name"
              v-model="form.name"
              class="input-dark"
              placeholder="Enter your name"
              :disabled="loading"
            />
          </div>

          <div>
            <label class="form-label" for="email">Email Address</label>
            <input
              id="email"
              v-model="form.email"
              type="email"
              class="input-dark"
              placeholder="Enter your email"
              :disabled="loading"
            />
          </div>

          <div>
            <label class="form-label" for="password">Password</label>
            <input
              id="password"
              v-model="form.password"
              type="password"
              class="input-dark"
              placeholder="Enter your password"
              :disabled="loading"
            />
          </div>

          <div v-if="isLogin" class="remember-row">
            <label class="remember-label">
              <input type="checkbox" disabled />
              <span>Remember me</span>
            </label>

            <button type="button" class="forgot-link">
              Forgot password?
            </button>
          </div>

          <div v-if="errorMessage" class="message error">
            {{ errorMessage }}
          </div>

          <div v-if="successMessage" class="message success">
            {{ successMessage }}
          </div>

          <button type="submit" class="btn-primary submit-btn" :disabled="loading">
            {{ loading ? 'Please wait...' : (isLogin ? 'Login' : 'Create Account') }}
          </button>

          <p v-if="!isLogin" class="terms-text">
            By signing up, you agree to our Terms of Service and Privacy Policy
          </p>
        </form>
      </div>

      <div class="back-home">
        <NuxtLink to="/">← Back to home</NuxtLink>
      </div>
    </div>
  </div>
</template>

<style scoped>
.login-page {
  min-height: 100vh;
  padding: 24px;
  display: grid;
  place-items: center;
  background: linear-gradient(135deg, #0a0a0a, #1a1a2e, #16213e);
}

.login-shell {
  width: 100%;
  max-width: 448px;
}

.logo-wrap {
  text-align: center;
  margin-bottom: 24px;
}

.logo-row {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 16px;
}

.logo-row span {
  font-size: 2rem;
  font-weight: 600;
  color: white;
}

.logo-wrap p {
  margin: 0;
  color: #9ca3af;
}

.debug-box {
  margin-bottom: 16px;
  padding: 12px 14px;
  border-radius: 10px;
  background: rgba(59, 130, 246, 0.12);
  border: 1px solid rgba(59, 130, 246, 0.25);
  color: #93c5fd;
  font-size: 0.875rem;
}

.login-card {
  overflow: hidden;
  border-radius: 24px;
  box-shadow: 0 30px 60px rgba(0, 0, 0, 0.45);
}

.loading-card {
  padding: 40px 32px;
  display: grid;
  place-items: center;
  gap: 14px;
  text-align: center;
}

.loading-card p {
  margin: 0;
  color: #9ca3af;
}

.login-tabs {
  display: flex;
  border-bottom: 1px solid #1f2937;
}

.login-tab {
  flex: 1;
  padding: 16px;
  border: 0;
  background: transparent;
  color: #9ca3af;
  cursor: pointer;
  transition: 0.2s ease;
}

.login-tab.active {
  background: #252525;
  color: white;
  border-bottom: 2px solid #4f46e5;
}

.login-form {
  padding: 32px;
  display: grid;
  gap: 24px;
}

.form-label {
  display: block;
  margin-bottom: 8px;
  color: #d1d5db;
  font-size: 0.875rem;
  font-weight: 500;
}

.remember-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.remember-label {
  display: flex;
  align-items: center;
  gap: 8px;
  color: #9ca3af;
  font-size: 0.875rem;
}

.forgot-link {
  border: 0;
  background: transparent;
  color: #818cf8;
  cursor: pointer;
}

.submit-btn {
  width: 100%;
}

.submit-btn:disabled {
  opacity: 0.7;
  cursor: not-allowed;
}

.terms-text {
  margin: 0;
  text-align: center;
  color: #9ca3af;
  font-size: 0.75rem;
}

.back-home {
  margin-top: 24px;
  text-align: center;
}

.back-home a {
  color: #9ca3af;
}

.back-home a:hover {
  color: white;
}

.message {
  padding: 12px 14px;
  border-radius: 10px;
  font-size: 0.875rem;
}

.message.error {
  background: rgba(239, 68, 68, 0.12);
  border: 1px solid rgba(239, 68, 68, 0.25);
  color: #fca5a5;
}

.message.success {
  background: rgba(34, 197, 94, 0.12);
  border: 1px solid rgba(34, 197, 94, 0.25);
  color: #86efac;
}
</style>