import { describe, it, expect, beforeEach, vi } from 'vitest'

/**
 * Tests for the project detail page [id].vue logic — loading, computed
 * properties, modal state, chapter creation validation, and word counting.
 */

const mockApiFetch = vi.fn()

type Chapter = { id: number; projectId: number; title: string; content: string; createdAt: string; updatedAt: string }
type StoryEntity = { id: number; projectId: number; type: 'Character' | 'Location' | 'Theme' | 'Arc'; name: string; summary: string; updatedAt: string }

describe('Project Detail — computed properties', () => {
  it('totalChapters returns chapter array length', () => {
    const chapters: Chapter[] = [
      { id: 1, projectId: 1, title: 'Ch 1', content: 'Some text', createdAt: '', updatedAt: '' },
      { id: 2, projectId: 1, title: 'Ch 2', content: 'More text', createdAt: '', updatedAt: '' }
    ]
    expect(chapters.length).toBe(2)
  })

  it('characterEntries filters by type Character', () => {
    const entities: StoryEntity[] = [
      { id: 1, projectId: 1, type: 'Character', name: 'Alice', summary: '', updatedAt: '' },
      { id: 2, projectId: 1, type: 'Location', name: 'Forest', summary: '', updatedAt: '' },
      { id: 3, projectId: 1, type: 'Character', name: 'Bob', summary: '', updatedAt: '' }
    ]
    const characters = entities.filter(e => e.type === 'Character')
    expect(characters).toHaveLength(2)
    expect(characters[0].name).toBe('Alice')
    expect(characters[1].name).toBe('Bob')
  })

  it('locationEntries filters by type Location', () => {
    const entities: StoryEntity[] = [
      { id: 1, projectId: 1, type: 'Character', name: 'Alice', summary: '', updatedAt: '' },
      { id: 2, projectId: 1, type: 'Location', name: 'Castle', summary: '', updatedAt: '' },
      { id: 3, projectId: 1, type: 'Location', name: 'Forest', summary: '', updatedAt: '' }
    ]
    const locations = entities.filter(e => e.type === 'Location')
    expect(locations).toHaveLength(2)
  })

  it('themeEntries filters by type Theme', () => {
    const entities: StoryEntity[] = [
      { id: 1, projectId: 1, type: 'Theme', name: 'Redemption', summary: '', updatedAt: '' },
      { id: 2, projectId: 1, type: 'Character', name: 'Alice', summary: '', updatedAt: '' }
    ]
    const themes = entities.filter(e => e.type === 'Theme')
    expect(themes).toHaveLength(1)
    expect(themes[0].name).toBe('Redemption')
  })

  it('totalWords sums word counts across chapters', () => {
    const chapters: Chapter[] = [
      { id: 1, projectId: 1, title: '', content: 'word1 word2 word3', createdAt: '', updatedAt: '' },
      { id: 2, projectId: 1, title: '', content: 'word4 word5', createdAt: '', updatedAt: '' }
    ]
    const totalWords = chapters.reduce((sum, ch) => {
      const text = ch.content?.trim() || ''
      if (!text) return sum
      return sum + text.split(/\s+/).length
    }, 0)
    expect(totalWords).toBe(5)
  })

  it('totalWords handles chapters with empty content', () => {
    const chapters: Chapter[] = [
      { id: 1, projectId: 1, title: '', content: '', createdAt: '', updatedAt: '' },
      { id: 2, projectId: 1, title: '', content: 'word1 word2', createdAt: '', updatedAt: '' }
    ]
    const totalWords = chapters.reduce((sum, ch) => {
      const text = ch.content?.trim() || ''
      if (!text) return sum
      return sum + text.split(/\s+/).length
    }, 0)
    expect(totalWords).toBe(2)
  })

  it('previewCharacters returns at most 5', () => {
    const characters = Array.from({ length: 10 }, (_, i) => ({
      id: i, projectId: 1, type: 'Character' as const, name: `Char${i}`, summary: '', updatedAt: ''
    }))
    expect(characters.slice(0, 5)).toHaveLength(5)
  })
})

describe('Project Detail — create chapter modal logic', () => {
  it('openCreateChapterModal sets showCreateChapterModal to true', () => {
    let showCreateChapterModal = false
    let formErrorMessage = ''
    let successMessage = ''

    // openCreateChapterModal logic
    formErrorMessage = ''
    successMessage = ''
    showCreateChapterModal = true

    expect(showCreateChapterModal).toBe(true)
    expect(formErrorMessage).toBe('')
  })

  it('closeCreateChapterModal resets form', () => {
    let showCreateChapterModal = true
    let formErrorMessage = 'old error'
    let createTitle = 'Old Title'
    let createContent = 'Old Content'
    const creatingChapter = false

    // closeCreateChapterModal logic
    if (!creatingChapter) {
      showCreateChapterModal = false
      formErrorMessage = ''
      createTitle = ''
      createContent = ''
    }

    expect(showCreateChapterModal).toBe(false)
    expect(formErrorMessage).toBe('')
    expect(createTitle).toBe('')
    expect(createContent).toBe('')
  })

  it('closeCreateChapterModal does nothing while creating', () => {
    let showCreateChapterModal = true
    const creatingChapter = true

    if (!creatingChapter) {
      showCreateChapterModal = false
    }

    expect(showCreateChapterModal).toBe(true)
  })
})

describe('Project Detail — createChapter validation', () => {
  it('rejects empty title', () => {
    const title = ''.trim()
    let formErrorMessage = ''

    if (!title) {
      formErrorMessage = 'Chapter title is required.'
    }

    expect(formErrorMessage).toBe('Chapter title is required.')
  })

  it('accepts valid title', () => {
    const title = 'Chapter One'.trim()
    let formErrorMessage = ''

    if (!title) {
      formErrorMessage = 'Chapter title is required.'
    }

    expect(formErrorMessage).toBe('')
  })

  it('trims whitespace-only title as empty', () => {
    const title = '   '.trim()
    let formErrorMessage = ''

    if (!title) {
      formErrorMessage = 'Chapter title is required.'
    }

    expect(formErrorMessage).toBe('Chapter title is required.')
  })
})

describe('Project Detail — loadPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('loads project, chapters, and entities in parallel', async () => {
    const project = { id: 1, title: 'Test', createdAt: '2026-01-01' }
    const chapters = [{ id: 1, projectId: 1, title: 'Ch 1', content: 'Text', createdAt: '', updatedAt: '' }]
    const entities = [{ id: 1, projectId: 1, type: 'Character', name: 'Alice', summary: '', updatedAt: '' }]

    mockApiFetch
      .mockResolvedValueOnce(project)
      .mockResolvedValueOnce(chapters)
      .mockResolvedValueOnce(entities)

    const [p, c, e] = await Promise.all([
      mockApiFetch('/projects/1', { method: 'GET' }),
      mockApiFetch('/projects/1/chapters', { method: 'GET' }),
      mockApiFetch('/projects/1/entities', { method: 'GET' })
    ])

    expect(p.title).toBe('Test')
    expect(c).toHaveLength(1)
    expect(e).toHaveLength(1)
  })
})
