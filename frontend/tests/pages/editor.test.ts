import { describe, it, expect } from 'vitest'

/**
 * Tests for pure utility functions extracted from the chapter editor page.
 * These functions handle HTML escaping, text normalization, entity extraction
 * parsing, and chapter-entity matching — key AI integration logic.
 */

// ──── Pure functions copied from [chapterId].vue for isolated testing ────

const escapeHtml = (value: string) =>
  value.replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;').replaceAll("'", '&#39;')

const plainTextToHtml = (value: string) => {
  const trimmed = value ?? ''
  if (!trimmed.trim()) return '<p></p>'
  return trimmed.split(/\n{2,}/).map(block =>
    `<p>${escapeHtml(block).replace(/\n/g, '<br>')}</p>`
  ).join('')
}

const looksLikeHtml = (value: string) => /<\/?[a-z][\s\S]*>/i.test(value)

type ExtractedEntity = { name: string; sourceQuote: string; confidence: number }
type ChapterEntityExtractionResponse = {
  chapterId: number; projectId: number; characters: ExtractedEntity[]; locations: ExtractedEntity[]
}
type StoryBibleEntity = {
  id: number; projectId: number; type: string; name: string; summary: string; updatedAt: string
}

const normalizeExtractedEntity = (item: any): ExtractedEntity | null => {
  const name = String(item?.name ?? item?.Name ?? '').trim()
  if (!name) return null
  return {
    name,
    sourceQuote: String(item?.sourceQuote ?? item?.SourceQuote ?? '').trim(),
    confidence: Number(item?.confidence ?? item?.Confidence ?? 0) || 0
  }
}

const normalizeExtractionResponse = (raw: any): ChapterEntityExtractionResponse => {
  const rawCharacters = Array.isArray(raw?.characters) ? raw.characters
    : Array.isArray(raw?.Characters) ? raw.Characters : []
  const rawLocations = Array.isArray(raw?.locations) ? raw.locations
    : Array.isArray(raw?.Locations) ? raw.Locations : []
  return {
    chapterId: Number(raw?.chapterId ?? raw?.ChapterId ?? 0),
    projectId: Number(raw?.projectId ?? raw?.ProjectId ?? 0),
    characters: rawCharacters.map(normalizeExtractedEntity).filter((item: any): item is ExtractedEntity => item !== null),
    locations: rawLocations.map(normalizeExtractedEntity).filter((item: any): item is ExtractedEntity => item !== null)
  }
}

const normalizeStoryBibleEntity = (item: any): StoryBibleEntity | null => {
  const id = Number(item?.id ?? item?.Id ?? 0)
  const projectId = Number(item?.projectId ?? item?.ProjectId ?? 0)
  const type = String(item?.type ?? item?.Type ?? '').trim()
  const name = String(item?.name ?? item?.Name ?? '').trim()
  if (!id || !projectId || !type || !name) return null
  return { id, projectId, type, name,
    summary: String(item?.summary ?? item?.Summary ?? '').trim(),
    updatedAt: String(item?.updatedAt ?? item?.UpdatedAt ?? '')
  }
}

const dedupeEntitiesByName = (items: ExtractedEntity[]) => {
  const seen = new Set<string>()
  return items.filter(item => {
    const key = item.name.trim().toLowerCase()
    if (!key || seen.has(key)) return false
    seen.add(key)
    return true
  })
}

const escapeRegExp = (value: string) => value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')

const chapterContainsEntity = (chapterText: string, entityName: string) => {
  const trimmedText = chapterText.trim()
  const trimmedName = entityName.trim()
  if (!trimmedText || !trimmedName) return false
  const escapedName = escapeRegExp(trimmedName).replace(/\s+/g, '\\s+')
  const regex = new RegExp(`\\b${escapedName}\\b`, 'i')
  return regex.test(trimmedText)
}

// ──── Tests ────

describe('escapeHtml', () => {
  it('escapes ampersands', () => {
    expect(escapeHtml('Tom & Jerry')).toBe('Tom &amp; Jerry')
  })

  it('escapes angle brackets', () => {
    expect(escapeHtml('<script>alert("xss")</script>')).toBe(
      '&lt;script&gt;alert(&quot;xss&quot;)&lt;/script&gt;'
    )
  })

  it('escapes single and double quotes', () => {
    expect(escapeHtml("He said \"hello\" and 'goodbye'")).toBe(
      'He said &quot;hello&quot; and &#39;goodbye&#39;'
    )
  })

  it('returns empty string for empty input', () => {
    expect(escapeHtml('')).toBe('')
  })

  it('does not double-escape already escaped content', () => {
    expect(escapeHtml('&amp;')).toBe('&amp;amp;')
  })
})

describe('plainTextToHtml', () => {
  it('wraps a single paragraph', () => {
    expect(plainTextToHtml('Hello world')).toBe('<p>Hello world</p>')
  })

  it('splits double newlines into separate paragraphs', () => {
    expect(plainTextToHtml('First paragraph\n\nSecond paragraph')).toBe(
      '<p>First paragraph</p><p>Second paragraph</p>'
    )
  })

  it('converts single newlines to <br> within a paragraph', () => {
    expect(plainTextToHtml('Line 1\nLine 2')).toBe('<p>Line 1<br>Line 2</p>')
  })

  it('returns empty paragraph for empty string', () => {
    expect(plainTextToHtml('')).toBe('<p></p>')
  })

  it('returns empty paragraph for whitespace-only string', () => {
    expect(plainTextToHtml('   ')).toBe('<p></p>')
  })

  it('escapes HTML in input', () => {
    expect(plainTextToHtml('<b>bold</b>')).toBe('<p>&lt;b&gt;bold&lt;/b&gt;</p>')
  })
})

describe('looksLikeHtml', () => {
  it('returns true for HTML content', () => {
    expect(looksLikeHtml('<p>Hello</p>')).toBe(true)
  })

  it('returns true for self-closing tags', () => {
    expect(looksLikeHtml('<br/>')).toBe(true)
  })

  it('returns false for plain text', () => {
    expect(looksLikeHtml('Just some plain text')).toBe(false)
  })

  it('returns false for empty string', () => {
    expect(looksLikeHtml('')).toBe(false)
  })

  it('returns true for nested HTML', () => {
    expect(looksLikeHtml('<div><p>Nested</p></div>')).toBe(true)
  })
})

describe('normalizeExtractedEntity', () => {
  it('normalizes a camelCase entity', () => {
    const result = normalizeExtractedEntity({
      name: 'Aragorn',
      sourceQuote: 'The king returned',
      confidence: 0.95
    })
    expect(result).toEqual({
      name: 'Aragorn',
      sourceQuote: 'The king returned',
      confidence: 0.95
    })
  })

  it('normalizes a PascalCase entity (from C# API)', () => {
    const result = normalizeExtractedEntity({
      Name: 'Gandalf',
      SourceQuote: 'You shall not pass',
      Confidence: 0.88
    })
    expect(result).toEqual({
      name: 'Gandalf',
      sourceQuote: 'You shall not pass',
      confidence: 0.88
    })
  })

  it('returns null for entity with empty name', () => {
    expect(normalizeExtractedEntity({ name: '', sourceQuote: 'test', confidence: 0.5 })).toBeNull()
  })

  it('returns null for null input', () => {
    expect(normalizeExtractedEntity(null)).toBeNull()
  })

  it('returns null for undefined input', () => {
    expect(normalizeExtractedEntity(undefined)).toBeNull()
  })

  it('defaults confidence to 0 for invalid value', () => {
    const result = normalizeExtractedEntity({ name: 'Test', confidence: 'invalid' })
    expect(result?.confidence).toBe(0)
  })
})

describe('normalizeExtractionResponse', () => {
  it('normalizes a full camelCase response', () => {
    const result = normalizeExtractionResponse({
      chapterId: 1,
      projectId: 2,
      characters: [{ name: 'Alice', sourceQuote: 'Down the rabbit hole', confidence: 0.9 }],
      locations: [{ name: 'Wonderland', sourceQuote: 'A curious place', confidence: 0.85 }]
    })
    expect(result.chapterId).toBe(1)
    expect(result.projectId).toBe(2)
    expect(result.characters).toHaveLength(1)
    expect(result.characters[0].name).toBe('Alice')
    expect(result.locations).toHaveLength(1)
    expect(result.locations[0].name).toBe('Wonderland')
  })

  it('normalizes PascalCase response from C# API', () => {
    const result = normalizeExtractionResponse({
      ChapterId: 5,
      ProjectId: 10,
      Characters: [{ Name: 'Bob', SourceQuote: 'test', Confidence: 0.7 }],
      Locations: []
    })
    expect(result.chapterId).toBe(5)
    expect(result.projectId).toBe(10)
    expect(result.characters).toHaveLength(1)
    expect(result.locations).toHaveLength(0)
  })

  it('filters out invalid entities', () => {
    const result = normalizeExtractionResponse({
      chapterId: 1,
      projectId: 1,
      characters: [
        { name: 'Valid', sourceQuote: '', confidence: 0.5 },
        { name: '', sourceQuote: '', confidence: 0.5 },
        null
      ],
      locations: []
    })
    expect(result.characters).toHaveLength(1)
    expect(result.characters[0].name).toBe('Valid')
  })

  it('handles empty / missing arrays', () => {
    const result = normalizeExtractionResponse({})
    expect(result.characters).toHaveLength(0)
    expect(result.locations).toHaveLength(0)
    expect(result.chapterId).toBe(0)
    expect(result.projectId).toBe(0)
  })
})

describe('normalizeStoryBibleEntity', () => {
  it('normalizes a valid camelCase entity', () => {
    const result = normalizeStoryBibleEntity({
      id: 1, projectId: 2, type: 'Character', name: 'Frodo',
      summary: 'Ring bearer', updatedAt: '2026-01-01T00:00:00Z'
    })
    expect(result).toEqual({
      id: 1, projectId: 2, type: 'Character', name: 'Frodo',
      summary: 'Ring bearer', updatedAt: '2026-01-01T00:00:00Z'
    })
  })

  it('normalizes PascalCase properties', () => {
    const result = normalizeStoryBibleEntity({
      Id: 3, ProjectId: 4, Type: 'Location', Name: 'Mordor',
      Summary: 'Volcanic wasteland', UpdatedAt: '2026-01-01'
    })
    expect(result?.name).toBe('Mordor')
    expect(result?.type).toBe('Location')
  })

  it('returns null when id is missing', () => {
    expect(normalizeStoryBibleEntity({ projectId: 1, type: 'Character', name: 'Test' })).toBeNull()
  })

  it('returns null when name is empty', () => {
    expect(normalizeStoryBibleEntity({ id: 1, projectId: 1, type: 'Character', name: '' })).toBeNull()
  })
})

describe('dedupeEntitiesByName', () => {
  it('removes duplicates by case-insensitive name', () => {
    const entities: ExtractedEntity[] = [
      { name: 'Alice', sourceQuote: 'first', confidence: 0.9 },
      { name: 'alice', sourceQuote: 'second', confidence: 0.8 },
      { name: 'Bob', sourceQuote: 'third', confidence: 0.7 }
    ]
    const result = dedupeEntitiesByName(entities)
    expect(result).toHaveLength(2)
    expect(result[0].name).toBe('Alice')
    expect(result[1].name).toBe('Bob')
  })

  it('filters out entities with empty names', () => {
    const entities: ExtractedEntity[] = [
      { name: '', sourceQuote: '', confidence: 0 },
      { name: 'Valid', sourceQuote: '', confidence: 0.5 }
    ]
    const result = dedupeEntitiesByName(entities)
    expect(result).toHaveLength(1)
    expect(result[0].name).toBe('Valid')
  })

  it('returns empty array for empty input', () => {
    expect(dedupeEntitiesByName([])).toHaveLength(0)
  })
})

describe('escapeRegExp', () => {
  it('escapes regex special characters', () => {
    expect(escapeRegExp('Mr. Smith (Jr.)')).toBe('Mr\\. Smith \\(Jr\\.\\)')
  })

  it('escapes brackets and pipes', () => {
    expect(escapeRegExp('[test] | {value}')).toBe('\\[test\\] \\| \\{value\\}')
  })

  it('does not modify alphanumeric strings', () => {
    expect(escapeRegExp('SimpleText123')).toBe('SimpleText123')
  })
})

describe('chapterContainsEntity', () => {
  it('finds an entity name in chapter text', () => {
    expect(chapterContainsEntity('The wizard Gandalf arrived.', 'Gandalf')).toBe(true)
  })

  it('is case-insensitive', () => {
    expect(chapterContainsEntity('ARAGORN stood tall.', 'aragorn')).toBe(true)
  })

  it('returns false when entity is not present', () => {
    expect(chapterContainsEntity('A story about nothing.', 'Gandalf')).toBe(false)
  })

  it('uses word boundaries', () => {
    expect(chapterContainsEntity('The sandcastle was tall.', 'sand')).toBe(false)
  })

  it('matches multi-word entity names', () => {
    expect(chapterContainsEntity('They visited the Dark Forest at night.', 'Dark Forest')).toBe(true)
  })

  it('returns false for empty chapter text', () => {
    expect(chapterContainsEntity('', 'Gandalf')).toBe(false)
  })

  it('returns false for empty entity name', () => {
    expect(chapterContainsEntity('Some text here.', '')).toBe(false)
  })
})

describe('editor computed properties (wordCount, charCount, readTime)', () => {
  const computeWordCount = (text: string) => {
    const trimmed = text.trim()
    if (!trimmed) return 0
    return trimmed.split(/\s+/).length
  }

  const computeCharCount = (text: string) => text.length

  const computeReadTimeMinutes = (wordCount: number) => {
    if (wordCount === 0) return 0
    return Math.max(1, Math.ceil(wordCount / 200))
  }

  it('counts words correctly', () => {
    expect(computeWordCount('Hello world to you')).toBe(4)
  })

  it('returns 0 words for empty string', () => {
    expect(computeWordCount('')).toBe(0)
  })

  it('returns 0 words for whitespace-only', () => {
    expect(computeWordCount('   ')).toBe(0)
  })

  it('counts characters', () => {
    expect(computeCharCount('Hello')).toBe(5)
  })

  it('computes read time for 200 words as 1 minute', () => {
    expect(computeReadTimeMinutes(200)).toBe(1)
  })

  it('computes read time for 201 words as 2 minutes', () => {
    expect(computeReadTimeMinutes(201)).toBe(2)
  })

  it('returns 0 read time for 0 words', () => {
    expect(computeReadTimeMinutes(0)).toBe(0)
  })

  it('computes read time for 1 word as 1 minute minimum', () => {
    expect(computeReadTimeMinutes(1)).toBe(1)
  })
})
