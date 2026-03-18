import { describe, it, expect, beforeEach, vi } from 'vitest'

// Mock $fetch globally before importing useApi
const mockFetch = vi.fn()
vi.stubGlobal('$fetch', mockFetch)

import { useApi } from '~/composables/useApi'

describe('useApi', () => {
  beforeEach(() => {
    localStorage.clear()
    vi.clearAllMocks()
    mockFetch.mockReset()
  })

  it('returns an object with apiFetch method', () => {
    const api = useApi()
    expect(api).toHaveProperty('apiFetch')
    expect(typeof api.apiFetch).toBe('function')
  })

  it('sends GET request with correct baseURL', async () => {
    mockFetch.mockResolvedValueOnce([{ id: 1, title: 'Project' }])
    const { apiFetch } = useApi()

    await apiFetch('/projects', { method: 'GET' })

    expect(mockFetch).toHaveBeenCalledWith('/projects', expect.objectContaining({
      baseURL: 'http://localhost:5140/api',
      method: 'GET'
    }))
  })

  it('sets Content-Type to application/json for non-FormData bodies', async () => {
    mockFetch.mockResolvedValueOnce({ id: 1 })
    const { apiFetch } = useApi()

    await apiFetch('/projects', {
      method: 'POST',
      body: { title: 'Test' }
    })

    expect(mockFetch).toHaveBeenCalledWith('/projects', expect.objectContaining({
      headers: expect.objectContaining({
        'Content-Type': 'application/json'
      })
    }))
  })

  it('attaches Authorization header when token exists', async () => {
    localStorage.setItem('canonguard_token', 'jwt-token-abc')
    mockFetch.mockResolvedValueOnce({ id: 1 })
    const { apiFetch } = useApi()

    await apiFetch('/projects', { method: 'GET' })

    expect(mockFetch).toHaveBeenCalledWith('/projects', expect.objectContaining({
      headers: expect.objectContaining({
        'Authorization': 'Bearer jwt-token-abc'
      })
    }))
  })

  it('does not attach Authorization header when no token', async () => {
    mockFetch.mockResolvedValueOnce([])
    const { apiFetch } = useApi()

    await apiFetch('/projects', { method: 'GET' })

    const callHeaders = mockFetch.mock.calls[0][1].headers
    expect(callHeaders).not.toHaveProperty('Authorization')
  })

  it('sends POST body correctly', async () => {
    mockFetch.mockResolvedValueOnce({ id: 1, title: 'New Project' })
    const { apiFetch } = useApi()
    const body = { title: 'New Project', description: 'A test project' }

    await apiFetch('/projects', { method: 'POST', body })

    expect(mockFetch).toHaveBeenCalledWith('/projects', expect.objectContaining({
      method: 'POST',
      body
    }))
  })

  it('returns typed response data', async () => {
    const mockData = { id: 5, title: 'My Project' }
    mockFetch.mockResolvedValueOnce(mockData)
    const { apiFetch } = useApi()

    const result = await apiFetch<{ id: number; title: string }>('/projects/5', { method: 'GET' })

    expect(result).toEqual(mockData)
  })

  it('propagates errors from $fetch', async () => {
    const error = new Error('Network error')
    mockFetch.mockRejectedValueOnce(error)
    const { apiFetch } = useApi()

    await expect(apiFetch('/projects', { method: 'GET' })).rejects.toThrow('Network error')
  })

  it('allows custom headers while preserving defaults', async () => {
    localStorage.setItem('canonguard_token', 'token-xyz')
    mockFetch.mockResolvedValueOnce({})
    const { apiFetch } = useApi()

    await apiFetch('/projects', {
      method: 'GET',
      headers: { 'X-Custom': 'value' }
    })

    expect(mockFetch).toHaveBeenCalledWith('/projects', expect.objectContaining({
      headers: expect.objectContaining({
        'X-Custom': 'value',
        'Content-Type': 'application/json',
        'Authorization': 'Bearer token-xyz'
      })
    }))
  })
})
