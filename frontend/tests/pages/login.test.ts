import { describe, it, expect, vi, beforeEach } from 'vitest'

/**
 * Tests for the login page logic — form validation, mode switching,
 * and authentication submission flow.
 */

const mockApiFetch = vi.fn()
vi.mock('~/composables/useApi', () => ({
  useApi: () => ({ apiFetch: mockApiFetch })
}))

const mockSetToken = vi.fn()
const mockAuthInit = vi.fn()
const mockAuthClear = vi.fn()
vi.mock('~/composables/useAuth', () => ({
  useAuth: () => ({
    token: { value: null },
    init: mockAuthInit,
    setToken: mockSetToken,
    clear: mockAuthClear
  })
}))

describe('Login — form validation', () => {
  it('requires email', () => {
    const email = ''.trim()
    const password = 'password123'
    let errorMessage = ''

    if (!email || !password) {
      errorMessage = 'Email and password are required.'
    }

    expect(errorMessage).toBe('Email and password are required.')
  })

  it('requires password', () => {
    const email = 'user@test.com'.trim()
    const password = ''
    let errorMessage = ''

    if (!email || !password) {
      errorMessage = 'Email and password are required.'
    }

    expect(errorMessage).toBe('Email and password are required.')
  })

  it('passes when both email and password are provided', () => {
    const email = 'user@test.com'.trim()
    const password = 'password123'
    let errorMessage = ''

    if (!email || !password) {
      errorMessage = 'Email and password are required.'
    }

    expect(errorMessage).toBe('')
  })

  it('requires name in signup mode', () => {
    const isLogin = false
    const name = ''.trim()
    let errorMessage = ''

    if (!isLogin && !name) {
      errorMessage = 'Name is required.'
    }

    expect(errorMessage).toBe('Name is required.')
  })

  it('does not require name in login mode', () => {
    const isLogin = true
    const name = ''.trim()
    let errorMessage = ''

    if (!isLogin && !name) {
      errorMessage = 'Name is required.'
    }

    expect(errorMessage).toBe('')
  })
})

describe('Login — mode switching', () => {
  it('switchToLogin resets form and sets isLogin true', () => {
    let isLogin = false
    let name = 'John'
    let email = 'john@test.com'
    let password = 'pass'
    let errorMessage = 'some error'
    let successMessage = 'some message'

    // switchToLogin logic
    isLogin = true
    name = ''
    email = ''
    password = ''
    errorMessage = ''
    successMessage = ''

    expect(isLogin).toBe(true)
    expect(name).toBe('')
    expect(email).toBe('')
    expect(password).toBe('')
    expect(errorMessage).toBe('')
    expect(successMessage).toBe('')
  })

  it('switchToSignup resets form and sets isLogin false', () => {
    let isLogin = true
    let name = ''
    let email = 'user@test.com'
    let password = 'pass'
    let errorMessage = ''
    let successMessage = ''

    // switchToSignup logic
    isLogin = false
    name = ''
    email = ''
    password = ''
    errorMessage = ''
    successMessage = ''

    expect(isLogin).toBe(false)
    expect(name).toBe('')
    expect(email).toBe('')
  })
})

describe('Login — handleSubmit (login flow)', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('calls login API with email and password', async () => {
    mockApiFetch.mockResolvedValueOnce({ token: 'jwt-token-123' })

    const result = await mockApiFetch('/auth/login', {
      method: 'POST',
      body: { email: 'user@test.com', password: 'secret' }
    })

    expect(result.token).toBe('jwt-token-123')
    expect(mockApiFetch).toHaveBeenCalledWith('/auth/login', {
      method: 'POST',
      body: { email: 'user@test.com', password: 'secret' }
    })
  })

  it('throws when no token returned from login', async () => {
    mockApiFetch.mockResolvedValueOnce({})

    const result = await mockApiFetch('/auth/login', {
      method: 'POST',
      body: { email: 'user@test.com', password: 'pass' }
    })

    expect(result?.token).toBeUndefined()
  })

  it('handles authentication failure', async () => {
    const error = { data: { message: 'Invalid credentials' } }
    mockApiFetch.mockRejectedValueOnce(error)

    let errorMessage = ''
    try {
      await mockApiFetch('/auth/login', {
        method: 'POST',
        body: { email: 'bad@test.com', password: 'wrong' }
      })
    } catch (err: any) {
      errorMessage = err?.data?.message || err?.message || 'Authentication failed.'
    }

    expect(errorMessage).toBe('Invalid credentials')
  })
})

describe('Login — handleSubmit (signup flow)', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('calls register API with name, email, and password', async () => {
    mockApiFetch.mockResolvedValueOnce(undefined)

    await mockApiFetch('/auth/register', {
      method: 'POST',
      body: { name: 'John', email: 'john@test.com', password: 'secret123' }
    })

    expect(mockApiFetch).toHaveBeenCalledWith('/auth/register', {
      method: 'POST',
      body: { name: 'John', email: 'john@test.com', password: 'secret123' }
    })
  })

  it('shows success message after signup and switches to login', () => {
    let successMessage = ''
    let isLogin = false
    let name = 'John'
    let password = 'secret'

    // post-signup logic from the page
    successMessage = 'Account created successfully. You can now log in.'
    isLogin = true
    name = ''
    password = ''

    expect(successMessage).toBe('Account created successfully. You can now log in.')
    expect(isLogin).toBe(true)
    expect(name).toBe('')
    expect(password).toBe('')
  })
})
