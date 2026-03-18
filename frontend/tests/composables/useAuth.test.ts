import { describe, it, expect, beforeEach, vi } from 'vitest'
import { useAuth } from '~/composables/useAuth'

describe('useAuth', () => {
  beforeEach(() => {
    localStorage.clear()
    vi.clearAllMocks()
  })

  it('initializes with null token', () => {
    const auth = useAuth()
    expect(auth.token.value).toBeNull()
  })

  it('reads token from localStorage on init()', () => {
    localStorage.setItem('canonguard_token', 'test-jwt-token')
    const auth = useAuth()
    auth.init()
    expect(auth.token.value).toBe('test-jwt-token')
  })

  it('returns null when no token in localStorage', () => {
    const auth = useAuth()
    auth.init()
    expect(auth.token.value).toBeNull()
  })

  it('setToken() stores token in state and localStorage', () => {
    const auth = useAuth()
    auth.setToken('new-token-123')
    expect(auth.token.value).toBe('new-token-123')
    expect(localStorage.setItem).toHaveBeenCalledWith('canonguard_token', 'new-token-123')
  })

  it('setToken(null) removes token from state and localStorage', () => {
    const auth = useAuth()
    auth.setToken('existing-token')
    auth.setToken(null)
    expect(auth.token.value).toBeNull()
    expect(localStorage.removeItem).toHaveBeenCalledWith('canonguard_token')
  })

  it('clear() removes token', () => {
    const auth = useAuth()
    auth.setToken('token-to-clear')
    expect(auth.token.value).toBe('token-to-clear')
    auth.clear()
    expect(auth.token.value).toBeNull()
    expect(localStorage.removeItem).toHaveBeenCalledWith('canonguard_token')
  })

  it('exposes token, init, setToken, and clear', () => {
    const auth = useAuth()
    expect(auth).toHaveProperty('token')
    expect(auth).toHaveProperty('init')
    expect(auth).toHaveProperty('setToken')
    expect(auth).toHaveProperty('clear')
    expect(typeof auth.init).toBe('function')
    expect(typeof auth.setToken).toBe('function')
    expect(typeof auth.clear).toBe('function')
  })
})
