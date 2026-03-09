import { useAuth } from '~/composables/useAuth'

export const useApi = () => {
  const config = useRuntimeConfig()
  const { token } = useAuth()

  const apiFetch = async <T>(
    url: string,
    options: Record<string, any> = {}
  ) => {
    return await $fetch<T>(`${config.public.apiBase}${url}`, {
      ...options,
      headers: {
        ...(options.headers || {}),
        ...(token.value
          ? { Authorization: `Bearer ${token.value}` }
          : {})
      }
    })
  }

  return { apiFetch }
}