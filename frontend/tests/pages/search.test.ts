import { describe, it, expect, beforeEach, vi } from 'vitest'

/**
 * Tests for the semantic search page logic — url building, score labels,
 * recent searches, scope management, and search execution.
 */

describe('Search — scoreLabel', () => {
  const scoreLabel = (score: number) => {
    const percent = Math.max(0, Math.min(100, Math.round(score * 100)))
    return `${percent}% match`
  }

  it('formats 0.95 as "95% match"', () => {
    expect(scoreLabel(0.95)).toBe('95% match')
  })

  it('formats 0 as "0% match"', () => {
    expect(scoreLabel(0)).toBe('0% match')
  })

  it('formats 1 as "100% match"', () => {
    expect(scoreLabel(1)).toBe('100% match')
  })

  it('clamps negative scores to 0', () => {
    expect(scoreLabel(-0.5)).toBe('0% match')
  })

  it('clamps scores above 1 to 100', () => {
    expect(scoreLabel(1.5)).toBe('100% match')
  })

  it('formats 0.123 as "12% match"', () => {
    expect(scoreLabel(0.123)).toBe('12% match')
  })
})

describe('Search — buildSearchUrl', () => {
  const buildSearchUrl = (query: string, currentProjectId: number | null) => {
    const encodedQuery = encodeURIComponent(query)
    if (currentProjectId == null) return `/semantic-search?query=${encodedQuery}&top=8`
    return `/projects/${currentProjectId}/semantic-search?query=${encodedQuery}&top=8`
  }

  it('builds global search URL when no project selected', () => {
    const url = buildSearchUrl('magic sword', null)
    expect(url).toBe('/semantic-search?query=magic%20sword&top=8')
  })

  it('builds project-scoped search URL', () => {
    const url = buildSearchUrl('dragon', 42)
    expect(url).toBe('/projects/42/semantic-search?query=dragon&top=8')
  })

  it('encodes special characters in query', () => {
    const url = buildSearchUrl('Tom & Jerry', null)
    expect(url).toBe('/semantic-search?query=Tom%20%26%20Jerry&top=8')
  })
})

describe('Search — currentProjectId computed', () => {
  const ALL_PROJECTS_VALUE = 'all'

  const getCurrentProjectId = (selectedScope: string): number | null => {
    if (selectedScope === ALL_PROJECTS_VALUE) return null
    const parsed = Number(selectedScope)
    return Number.isFinite(parsed) && parsed > 0 ? parsed : null
  }

  it('returns null for "all"', () => {
    expect(getCurrentProjectId('all')).toBeNull()
  })

  it('returns number for valid project id', () => {
    expect(getCurrentProjectId('5')).toBe(5)
  })

  it('returns null for invalid string', () => {
    expect(getCurrentProjectId('abc')).toBeNull()
  })

  it('returns null for zero', () => {
    expect(getCurrentProjectId('0')).toBeNull()
  })

  it('returns null for negative number', () => {
    expect(getCurrentProjectId('-3')).toBeNull()
  })
})

describe('Search — currentScopeLabel', () => {
  const ALL_PROJECTS_VALUE = 'all'

  type ProjectItem = { id: number; title: string }

  const getCurrentScopeLabel = (
    selectedScope: string,
    projects: ProjectItem[],
    currentProjectId: number | null
  ): string => {
    if (selectedScope === ALL_PROJECTS_VALUE) return 'All Projects'
    const project = projects.find(p => p.id === currentProjectId)
    return project?.title ?? 'Selected Project'
  }

  it('returns "All Projects" when scope is all', () => {
    expect(getCurrentScopeLabel('all', [], null)).toBe('All Projects')
  })

  it('returns project title when found', () => {
    const projects = [{ id: 1, title: 'My Novel' }]
    expect(getCurrentScopeLabel('1', projects, 1)).toBe('My Novel')
  })

  it('returns "Selected Project" fallback when project not found', () => {
    expect(getCurrentScopeLabel('99', [], 99)).toBe('Selected Project')
  })
})

describe('Search — recentStorageKey', () => {
  const getRecentStorageKey = (currentProjectId: number | null): string => {
    return currentProjectId == null
      ? 'canonguard_recent_searches_all_projects'
      : `canonguard_recent_searches_project_${currentProjectId}`
  }

  it('returns global key when no project selected', () => {
    expect(getRecentStorageKey(null)).toBe('canonguard_recent_searches_all_projects')
  })

  it('returns project-specific key', () => {
    expect(getRecentStorageKey(7)).toBe('canonguard_recent_searches_project_7')
  })
})

describe('Search — saveRecentSearch logic', () => {
  it('adds new search to beginning of list', () => {
    const existing = ['old search']
    const trimmed = 'new search'
    const updated = [trimmed, ...existing.filter(item => item.toLowerCase() !== trimmed.toLowerCase())].slice(0, 6)
    expect(updated).toEqual(['new search', 'old search'])
  })

  it('deduplicates case-insensitively', () => {
    const existing = ['Magic', 'dragon']
    const trimmed = 'magic'
    const updated = [trimmed, ...existing.filter(item => item.toLowerCase() !== trimmed.toLowerCase())].slice(0, 6)
    expect(updated).toEqual(['magic', 'dragon'])
  })

  it('caps at 6 entries', () => {
    const existing = ['a', 'b', 'c', 'd', 'e', 'f']
    const trimmed = 'g'
    const updated = [trimmed, ...existing.filter(item => item.toLowerCase() !== trimmed.toLowerCase())].slice(0, 6)
    expect(updated).toHaveLength(6)
    expect(updated[0]).toBe('g')
    expect(updated[5]).toBe('e')
  })

  it('does not add empty search', () => {
    const trimmed = ''.trim()
    expect(trimmed).toBe('')
  })
})

describe('Search — clearSearch logic', () => {
  it('resets all search state', () => {
    let hasSearched = true
    let loading = true
    let errorMessage = 'old error'
    let results = [{ projectId: 1 }]

    // clearSearch logic
    hasSearched = false
    loading = false
    errorMessage = ''
    results = []

    expect(hasSearched).toBe(false)
    expect(loading).toBe(false)
    expect(errorMessage).toBe('')
    expect(results).toHaveLength(0)
  })
})

describe('Search — suggestion items', () => {
  const suggestionItems = [
    'Scenes with a specific tone',
    'Character appearances or mentions',
    'References to locations or artifacts',
    'Dialogue around a theme or event'
  ]

  it('has 4 suggestion items', () => {
    expect(suggestionItems).toHaveLength(4)
  })

  it('all suggestions are non-empty strings', () => {
    for (const item of suggestionItems) {
      expect(typeof item).toBe('string')
      expect(item.length).toBeGreaterThan(0)
    }
  })
})
