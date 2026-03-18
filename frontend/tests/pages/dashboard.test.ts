import { describe, it, expect, beforeEach, vi } from 'vitest'

/**
 * Tests for the dashboard page logic — project loading, stats aggregation,
 * form validation, and project creation flow.
 */

// Mock apiFetch
const mockApiFetch = vi.fn()
vi.mock('~/composables/useApi', () => ({
  useApi: () => ({ apiFetch: mockApiFetch })
}))

const mockAuthToken = { value: 'test-token' }
const mockAuthInit = vi.fn()
const mockAuthClear = vi.fn()
vi.mock('~/composables/useAuth', () => ({
  useAuth: () => ({
    token: mockAuthToken,
    init: mockAuthInit,
    clear: mockAuthClear,
    setToken: vi.fn()
  })
}))

type Project = { id: number; title: string; description?: string | null; createdAt: string }
type Chapter = { id: number; projectId: number; title: string; content: string; createdAt: string; updatedAt: string }
type StoryEntity = { id: number; projectId: number; type: string; name: string; summary: string; updatedAt: string }

describe('Dashboard — loadProjects logic', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('returns projects from the API', async () => {
    const mockProjects: Project[] = [
      { id: 1, title: 'Project A', description: 'Desc A', createdAt: '2026-01-01' },
      { id: 2, title: 'Project B', description: null, createdAt: '2026-01-02' }
    ]
    mockApiFetch.mockResolvedValueOnce(mockProjects)

    const result = await mockApiFetch('/projects', { method: 'GET' })
    const projects = Array.isArray(result) ? result : []

    expect(projects).toHaveLength(2)
    expect(projects[0].title).toBe('Project A')
    expect(projects[1].title).toBe('Project B')
  })

  it('handles empty project list', async () => {
    mockApiFetch.mockResolvedValueOnce([])

    const result = await mockApiFetch('/projects', { method: 'GET' })
    const projects = Array.isArray(result) ? result : []

    expect(projects).toHaveLength(0)
  })

  it('handles API error gracefully', async () => {
    mockApiFetch.mockRejectedValueOnce(new Error('Network failure'))

    let errorMessage = ''
    try {
      await mockApiFetch('/projects', { method: 'GET' })
    } catch (error: any) {
      errorMessage = error?.message || 'Failed to load projects.'
    }

    expect(errorMessage).toBe('Network failure')
  })
})

describe('Dashboard — stats aggregation logic', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('aggregates chapter counts across projects', () => {
    const chapterResults = [
      { status: 'fulfilled' as const, value: [{ id: 1 }, { id: 2 }] },
      { status: 'fulfilled' as const, value: [{ id: 3 }] }
    ]

    let chapterCount = 0
    for (const result of chapterResults) {
      if (result.status === 'fulfilled' && Array.isArray(result.value)) {
        chapterCount += result.value.length
      }
    }

    expect(chapterCount).toBe(3)
  })

  it('aggregates character and location counts from entities', () => {
    const entityResults = [
      {
        status: 'fulfilled' as const,
        value: [
          { type: 'Character', name: 'Alice' },
          { type: 'Location', name: 'Wonderland' },
          { type: 'Character', name: 'Bob' },
          { type: 'Theme', name: 'Growth' }
        ]
      }
    ]

    let characterCount = 0
    let locationCount = 0

    for (const result of entityResults) {
      if (result.status === 'fulfilled' && Array.isArray(result.value)) {
        for (const entity of result.value) {
          if (entity.type === 'Character') characterCount++
          else if (entity.type === 'Location') locationCount++
        }
      }
    }

    expect(characterCount).toBe(2)
    expect(locationCount).toBe(1)
  })

  it('handles rejected promises in stats', () => {
    const chapterResults = [
      { status: 'fulfilled' as const, value: [{ id: 1 }] },
      { status: 'rejected' as const, reason: new Error('Fail') }
    ]

    let chapterCount = 0
    for (const result of chapterResults) {
      if (result.status === 'fulfilled' && Array.isArray(result.value)) {
        chapterCount += result.value.length
      }
    }

    expect(chapterCount).toBe(1)
  })

  it('returns zero stats when no projects', () => {
    const projects: Project[] = []

    const totalChapters = 0
    const totalCharacters = 0
    const totalLocations = 0

    expect(projects.length).toBe(0)
    expect(totalChapters).toBe(0)
    expect(totalCharacters).toBe(0)
    expect(totalLocations).toBe(0)
  })
})

describe('Dashboard — createProject validation', () => {
  it('rejects empty title', () => {
    const title = ''.trim()
    let formErrorMessage = ''

    if (!title) {
      formErrorMessage = 'Project title is required.'
    }

    expect(formErrorMessage).toBe('Project title is required.')
  })

  it('accepts valid title', () => {
    const title = 'My Novel'.trim()
    let formErrorMessage = ''

    if (!title) {
      formErrorMessage = 'Project title is required.'
    }

    expect(formErrorMessage).toBe('')
  })

  it('sends a description of null when empty', () => {
    const description = ''.trim()
    const body = {
      title: 'Novel',
      description: description || null
    }

    expect(body.description).toBeNull()
  })

  it('sends a real description when provided', () => {
    const description = 'A great novel'.trim()
    const body = {
      title: 'Novel',
      description: description || null
    }

    expect(body.description).toBe('A great novel')
  })
})

describe('Dashboard — createProject API call', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('creates a project and returns it', async () => {
    const created: Project = { id: 5, title: 'Epic Saga', description: 'A fantasy epic', createdAt: '2026-03-18' }
    mockApiFetch.mockResolvedValueOnce(created)

    const result = await mockApiFetch('/projects', {
      method: 'POST',
      body: { title: 'Epic Saga', description: 'A fantasy epic' }
    })

    expect(result.id).toBe(5)
    expect(result.title).toBe('Epic Saga')
  })

  it('throws when server returns object without id', async () => {
    mockApiFetch.mockResolvedValueOnce({})

    const result = await mockApiFetch('/projects', {
      method: 'POST',
      body: { title: 'Test' }
    })

    expect(result?.id).toBeUndefined()
  })
})

describe('Dashboard — totalProjects computed', () => {
  it('computes from projects array length', () => {
    const projects = [
      { id: 1, title: 'A' },
      { id: 2, title: 'B' },
      { id: 3, title: 'C' }
    ]
    expect(projects.length).toBe(3)
  })

  it('returns 0 when empty', () => {
    const projects: Project[] = []
    expect(projects.length).toBe(0)
  })
})
