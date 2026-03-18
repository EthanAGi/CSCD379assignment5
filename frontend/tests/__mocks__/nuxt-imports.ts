import { vi } from 'vitest'
import { ref, computed, reactive, onMounted, watch, nextTick } from 'vue'

export const navigateTo = vi.fn()
export const definePageMeta = vi.fn()
export const useRoute = vi.fn(() => ({
  params: {},
  query: {}
}))
export const useRouter = vi.fn(() => ({
  push: vi.fn(),
  replace: vi.fn()
}))
export const useRuntimeConfig = vi.fn(() => ({
  public: {
    apiBase: 'http://localhost:5140/api'
  }
}))

export { ref, computed, reactive, onMounted, watch, nextTick }
