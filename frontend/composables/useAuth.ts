export const useAuth = () => {
  const token = useState<string | null>('token', () => null)

  const setToken = (value: string | null) => {
    token.value = value
    if (import.meta.client) {
      if (value) localStorage.setItem('token', value)
      else localStorage.removeItem('token')
    }
  }

  const init = () => {
    if (import.meta.client) {
      token.value = localStorage.getItem('token')
    }
  }

  return {
    token,
    isLoggedIn: computed(() => !!token.value),
    setToken,
    init
  }
}