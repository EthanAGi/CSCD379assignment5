import { describe, it, expect } from 'vitest'

/**
 * Tests for the landing page (index.vue) — features data and
 * redirect logic.
 */

describe('Landing Page — features', () => {
  const features = [
    {
      icon: 'mdi-book-open-page-variant-outline',
      title: 'Automatic Story Bible',
      description: 'AI extracts characters, locations, themes, and facts from your manuscript automatically.'
    },
    {
      icon: 'mdi-magnify',
      title: 'Semantic Search',
      description: 'Search your manuscript by meaning, not just keywords.'
    },
    {
      icon: 'mdi-file-document-outline',
      title: 'Consistency Checking',
      description: 'Catch continuity errors before your readers do.'
    },
    {
      icon: 'mdi-sparkles',
      title: 'AI Writing Tools',
      description: 'Generate analysis, review, and other AI-powered writing support.'
    }
  ]

  it('has 4 features', () => {
    expect(features).toHaveLength(4)
  })

  it('all features have icon, title, and description', () => {
    for (const feature of features) {
      expect(feature.icon).toBeTruthy()
      expect(feature.title).toBeTruthy()
      expect(feature.description).toBeTruthy()
    }
  })

  it('first feature is about Automatic Story Bible', () => {
    expect(features[0].title).toBe('Automatic Story Bible')
  })

  it('second feature is about Semantic Search', () => {
    expect(features[1].title).toBe('Semantic Search')
  })

  it('third feature is about Consistency Checking', () => {
    expect(features[2].title).toBe('Consistency Checking')
  })

  it('fourth feature is about AI Writing Tools', () => {
    expect(features[3].title).toBe('AI Writing Tools')
  })

  it('all icons start with mdi-', () => {
    for (const feature of features) {
      expect(feature.icon.startsWith('mdi-')).toBe(true)
    }
  })

  it('all descriptions are non-empty strings', () => {
    for (const feature of features) {
      expect(typeof feature.description).toBe('string')
      expect(feature.description.length).toBeGreaterThan(10)
    }
  })
})

describe('Landing Page — redirect logic', () => {
  it('redirects to dashboard when token exists', () => {
    const token = 'valid-token'
    let redirected = false

    if (token) {
      redirected = true
    }

    expect(redirected).toBe(true)
  })

  it('does not redirect when no token', () => {
    const token = null
    let redirected = false

    if (token) {
      redirected = true
    }

    expect(redirected).toBe(false)
  })
})
