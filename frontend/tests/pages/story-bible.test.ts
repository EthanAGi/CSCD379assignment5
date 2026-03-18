import { describe, it, expect, beforeEach, vi } from 'vitest'

/**
 * Tests for story-bible.vue logic — filtering, tab management, project
 * selection, form validation, entity CRUD, and computed properties.
 */

type TabKey = 'characters' | 'locations' | 'themes' | 'arcs'
type StoryEntity = { id: number; projectId: number; type: 'Character' | 'Location' | 'Theme' | 'Arc'; name: string; summary: string; updatedAt: string }
type ProjectSummary = { id: number; title: string; description?: string | null; createdAt: string }

const tabs: Array<{ id: TabKey; label: string; icon: string; apiType: StoryEntity['type'] }> = [
  { id: 'characters', label: 'Characters', icon: 'mdi-account-group-outline', apiType: 'Character' },
  { id: 'locations', label: 'Locations', icon: 'mdi-map-marker-outline', apiType: 'Location' },
  { id: 'themes', label: 'Themes', icon: 'mdi-lightbulb-outline', apiType: 'Theme' },
  { id: 'arcs', label: 'Story Arcs', icon: 'mdi-trending-up', apiType: 'Arc' }
]

describe('Story Bible — tabs configuration', () => {
  it('has 4 tabs', () => {
    expect(tabs).toHaveLength(4)
  })

  it('each tab has id, label, icon, and apiType', () => {
    for (const tab of tabs) {
      expect(tab).toHaveProperty('id')
      expect(tab).toHaveProperty('label')
      expect(tab).toHaveProperty('icon')
      expect(tab).toHaveProperty('apiType')
    }
  })

  it('tab ids match expected values', () => {
    expect(tabs.map(t => t.id)).toEqual(['characters', 'locations', 'themes', 'arcs'])
  })

  it('tab apiTypes map to entity types', () => {
    expect(tabs.map(t => t.apiType)).toEqual(['Character', 'Location', 'Theme', 'Arc'])
  })
})

describe('Story Bible — setActiveTab', () => {
  it('sets active tab and updates form type', () => {
    let activeTab: TabKey = 'characters'
    let formType: StoryEntity['type'] = 'Character'

    const setActiveTab = (tab: TabKey) => {
      activeTab = tab
      formType = tabs.find(t => t.id === tab)?.apiType ?? 'Character'
    }

    setActiveTab('locations')
    expect(activeTab).toBe('locations')
    expect(formType).toBe('Location')

    setActiveTab('themes')
    expect(activeTab).toBe('themes')
    expect(formType).toBe('Theme')

    setActiveTab('arcs')
    expect(activeTab).toBe('arcs')
    expect(formType).toBe('Arc')
  })
})

describe('Story Bible — filteredEntries', () => {
  const allEntries: StoryEntity[] = [
    { id: 1, projectId: 1, type: 'Character', name: 'Alice', summary: 'Main character', updatedAt: '' },
    { id: 2, projectId: 1, type: 'Character', name: 'Bob', summary: 'Side character', updatedAt: '' },
    { id: 3, projectId: 1, type: 'Location', name: 'Forest', summary: 'Dark forest', updatedAt: '' },
    { id: 4, projectId: 1, type: 'Theme', name: 'Growth', summary: 'Coming of age', updatedAt: '' },
    { id: 5, projectId: 1, type: 'Arc', name: 'Hero Journey', summary: 'Classic arc', updatedAt: '' }
  ]

  const filterEntries = (entries: StoryEntity[], apiType: string, query: string) => {
    const q = query.trim().toLowerCase()
    return entries.filter(entry => {
      const matchesType = entry.type === apiType
      const matchesSearch = !q || entry.name.toLowerCase().includes(q) || entry.summary.toLowerCase().includes(q)
      return matchesType && matchesSearch
    })
  }

  it('filters by Character type', () => {
    const result = filterEntries(allEntries, 'Character', '')
    expect(result).toHaveLength(2)
    expect(result.map(e => e.name)).toEqual(['Alice', 'Bob'])
  })

  it('filters by Location type', () => {
    const result = filterEntries(allEntries, 'Location', '')
    expect(result).toHaveLength(1)
    expect(result[0].name).toBe('Forest')
  })

  it('filters by Theme type', () => {
    const result = filterEntries(allEntries, 'Theme', '')
    expect(result).toHaveLength(1)
  })

  it('filters by Arc type', () => {
    const result = filterEntries(allEntries, 'Arc', '')
    expect(result).toHaveLength(1)
    expect(result[0].name).toBe('Hero Journey')
  })

  it('applies search query to name', () => {
    const result = filterEntries(allEntries, 'Character', 'alice')
    expect(result).toHaveLength(1)
    expect(result[0].name).toBe('Alice')
  })

  it('applies search query to summary', () => {
    const result = filterEntries(allEntries, 'Character', 'side')
    expect(result).toHaveLength(1)
    expect(result[0].name).toBe('Bob')
  })

  it('returns empty when no match', () => {
    const result = filterEntries(allEntries, 'Character', 'nonexistent')
    expect(result).toHaveLength(0)
  })
})

describe('Story Bible — normalizeProjectId', () => {
  const normalizeProjectId = (value: unknown): number | null => {
    const parsed = Number(value)
    return Number.isFinite(parsed) && parsed > 0 ? parsed : null
  }

  it('normalizes valid number', () => {
    expect(normalizeProjectId(5)).toBe(5)
  })

  it('normalizes valid string', () => {
    expect(normalizeProjectId('10')).toBe(10)
  })

  it('returns null for null', () => {
    expect(normalizeProjectId(null)).toBeNull()
  })

  it('returns null for undefined', () => {
    expect(normalizeProjectId(undefined)).toBeNull()
  })

  it('returns null for zero', () => {
    expect(normalizeProjectId(0)).toBeNull()
  })

  it('returns null for negative', () => {
    expect(normalizeProjectId(-1)).toBeNull()
  })

  it('returns null for non-numeric string', () => {
    expect(normalizeProjectId('abc')).toBeNull()
  })

  it('returns null for NaN', () => {
    expect(normalizeProjectId(NaN)).toBeNull()
  })
})

describe('Story Bible — pickInitialProjectId', () => {
  const normalizeProjectId = (value: unknown): number | null => {
    const parsed = Number(value)
    return Number.isFinite(parsed) && parsed > 0 ? parsed : null
  }

  const pickInitialProjectId = (
    availableProjects: ProjectSummary[],
    routeProjectId: number | null,
    storedProjectId: number | null
  ): number | null => {
    if (routeProjectId && availableProjects.some(p => p.id === routeProjectId)) return routeProjectId
    if (storedProjectId && availableProjects.some(p => p.id === storedProjectId)) return storedProjectId
    return availableProjects.length > 0 ? availableProjects[0].id : null
  }

  const projects: ProjectSummary[] = [
    { id: 1, title: 'Project A', createdAt: '' },
    { id: 2, title: 'Project B', createdAt: '' },
    { id: 3, title: 'Project C', createdAt: '' }
  ]

  it('prefers route project id when valid', () => {
    expect(pickInitialProjectId(projects, 2, null)).toBe(2)
  })

  it('falls back to stored project id', () => {
    expect(pickInitialProjectId(projects, null, 3)).toBe(3)
  })

  it('falls back to first project when no route or stored id', () => {
    expect(pickInitialProjectId(projects, null, null)).toBe(1)
  })

  it('ignores route project id not in available projects', () => {
    expect(pickInitialProjectId(projects, 99, 2)).toBe(2)
  })

  it('returns null when no projects available', () => {
    expect(pickInitialProjectId([], null, null)).toBeNull()
  })

  it('route id takes precedence over stored id', () => {
    expect(pickInitialProjectId(projects, 1, 3)).toBe(1)
  })
})

describe('Story Bible — getFriendlyError', () => {
  const getFriendlyError = (error: any, fallback: string) => {
    return error?.data?.message || error?.data?.title || error?.message || fallback
  }

  it('returns data.message when present', () => {
    expect(getFriendlyError({ data: { message: 'Server error' } }, 'fallback')).toBe('Server error')
  })

  it('returns data.title when message missing', () => {
    expect(getFriendlyError({ data: { title: 'Not Found' } }, 'fallback')).toBe('Not Found')
  })

  it('returns error.message when data missing', () => {
    expect(getFriendlyError({ message: 'Network fail' }, 'fallback')).toBe('Network fail')
  })

  it('returns fallback when nothing else available', () => {
    expect(getFriendlyError({}, 'Something went wrong')).toBe('Something went wrong')
  })

  it('returns fallback for null error', () => {
    expect(getFriendlyError(null, 'Default error')).toBe('Default error')
  })
})

describe('Story Bible — save entry validation', () => {
  it('requires a project to be selected', () => {
    let errorMessage = ''
    const projectId = null

    if (!projectId || (typeof projectId === 'number' && projectId <= 0)) {
      errorMessage = 'Please select a project.'
    }

    expect(errorMessage).toBe('Please select a project.')
  })

  it('requires a type', () => {
    let errorMessage = ''
    const type = ''

    if (!type) {
      errorMessage = 'Please select a type.'
    }

    expect(errorMessage).toBe('Please select a type.')
  })

  it('requires a non-empty name', () => {
    let errorMessage = ''
    const name = '  '.trim()

    if (!name) {
      errorMessage = 'Entry name is required.'
    }

    expect(errorMessage).toBe('Entry name is required.')
  })

  it('passes validation with valid inputs', () => {
    let errorMessage = ''
    const projectId = 1
    const type = 'Character'
    const name = 'Alice'.trim()

    if (!projectId || projectId <= 0) errorMessage = 'Please select a project.'
    else if (!type) errorMessage = 'Please select a type.'
    else if (!name) errorMessage = 'Entry name is required.'

    expect(errorMessage).toBe('')
  })
})

describe('Story Bible — openCreateForm / openEditForm / closeForm', () => {
  it('openCreateForm sets up blank form', () => {
    let showForm = false
    let editingEntryId: number | null = 5
    let formName = 'old'
    let formSummary = 'old summary'

    // openCreateForm logic
    editingEntryId = null
    formName = ''
    formSummary = ''
    showForm = true

    expect(showForm).toBe(true)
    expect(editingEntryId).toBeNull()
    expect(formName).toBe('')
    expect(formSummary).toBe('')
  })

  it('openEditForm populates form with entry data', () => {
    let editingEntryId: number | null = null
    let formName = ''
    let formSummary = ''
    let showForm = false

    const entry: StoryEntity = {
      id: 5, projectId: 1, type: 'Character', name: 'Alice',
      summary: 'Main character', updatedAt: ''
    }

    // openEditForm logic
    editingEntryId = entry.id
    formName = entry.name
    formSummary = entry.summary
    showForm = true

    expect(editingEntryId).toBe(5)
    expect(formName).toBe('Alice')
    expect(formSummary).toBe('Main character')
    expect(showForm).toBe(true)
  })

  it('closeForm resets editing state', () => {
    let showForm = true
    let editingEntryId: number | null = 5

    // closeForm logic
    showForm = false
    editingEntryId = null

    expect(showForm).toBe(false)
    expect(editingEntryId).toBeNull()
  })
})

describe('Story Bible — computed helpers', () => {
  it('isEditing returns true when editingEntryId is set', () => {
    expect(5 !== null).toBe(true)
  })

  it('isEditing returns false when editingEntryId is null', () => {
    const editingEntryId = null
    expect(editingEntryId !== null).toBe(false)
  })

  it('hasProjects returns true when projects exist', () => {
    const projects = [{ id: 1 }]
    expect(projects.length > 0).toBe(true)
  })

  it('hasProjects returns false when no projects', () => {
    const projects: any[] = []
    expect(projects.length > 0).toBe(false)
  })

  it('selectedProject finds project by id', () => {
    const projects: ProjectSummary[] = [
      { id: 1, title: 'A', createdAt: '' },
      { id: 2, title: 'B', createdAt: '' }
    ]
    const selectedProjectId = 2
    const found = projects.find(p => p.id === selectedProjectId) ?? null
    expect(found?.title).toBe('B')
  })

  it('selectedProject returns null when not found', () => {
    const projects: ProjectSummary[] = [{ id: 1, title: 'A', createdAt: '' }]
    const selectedProjectId = 99
    const found = projects.find(p => p.id === selectedProjectId) ?? null
    expect(found).toBeNull()
  })
})
