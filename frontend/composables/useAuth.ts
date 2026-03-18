import { useState } from '#app'

const TOKEN_KEY = 'canonguard_token'

export const useAuth = () => {
  const token = useState<string | null>('auth_token', () => null)

  const init = () => {
    if (import.meta.server) return
    token.value = localStorage.getItem(TOKEN_KEY)
  }

  const setToken = (newToken: string | null) => {
    token.value = newToken

    if (import.meta.server) return

    if (newToken) {
      localStorage.setItem(TOKEN_KEY, newToken)
    } else {
      localStorage.removeItem(TOKEN_KEY)
    }
  }

  const clear = () => {
    setToken(null)
  }

  return {
    token,
    init,
    setToken,
    clear
  }
}