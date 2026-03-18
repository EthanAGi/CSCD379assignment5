import { useRuntimeConfig } from '#imports'
import { useAuth } from '~/composables/useAuth'

type ApiFetchOptions = {
  method?: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE'
  body?: any
  headers?: Record<string, string>
}

export const useApi = () => {
  const config = useRuntimeConfig()
  const auth = useAuth()

  const apiFetch = async <T>(path: string, options: ApiFetchOptions = {}): Promise<T> => {
    auth.init()

    const token = auth.token.value
    const headers: Record<string, string> = {
      ...(options.headers || {})
    }

    const isFormData =
      typeof FormData !== 'undefined' && options.body instanceof FormData

    if (!isFormData) {
      headers['Content-Type'] = 'application/json'
    }

    if (token) {
      headers['Authorization'] = `Bearer ${token}`
      console.log('[useApi] Attached bearer token to request:', path)
    } else {
      console.log('[useApi] No token available for request:', path)
    }

    return await $fetch<T>(path, {
      baseURL: config.public.apiBase,
      method: options.method,
      body: options.body,
      headers
    })
  }

  return { apiFetch }
}