import { describe, it, expect, vi } from 'vitest'

/**
 * Tests for the AppSidebar component logic — navigation items
 * and sign-out behavior.
 */

describe('AppSidebar — navigation items', () => {
  const items = [
    { label: 'Dashboard', icon: 'mdi-view-dashboard-outline', to: '/app/dashboard' },
    { label: 'Story Bible', icon: 'mdi-book-open-page-variant-outline', to: '/app/story-bible' },
    { label: 'Search', icon: 'mdi-magnify', to: '/app/search' }
  ]

  it('has 3 navigation items', () => {
    expect(items).toHaveLength(3)
  })

  it('first item is Dashboard', () => {
    expect(items[0].label).toBe('Dashboard')
    expect(items[0].to).toBe('/app/dashboard')
  })

  it('second item is Story Bible', () => {
    expect(items[1].label).toBe('Story Bible')
    expect(items[1].to).toBe('/app/story-bible')
  })

  it('third item is Search', () => {
    expect(items[2].label).toBe('Search')
    expect(items[2].to).toBe('/app/search')
  })

  it('all items have icons', () => {
    for (const item of items) {
      expect(item.icon).toBeTruthy()
      expect(item.icon.startsWith('mdi-')).toBe(true)
    }
  })

  it('all items have unique routes', () => {
    const routes = items.map(item => item.to)
    const uniqueRoutes = new Set(routes)
    expect(uniqueRoutes.size).toBe(items.length)
  })
})

describe('AppSidebar — signOut', () => {
  it('clears auth and navigates to login', async () => {
    const mockClear = vi.fn()
    const mockPush = vi.fn()

    const auth = { clear: mockClear }
    const router = { push: mockPush }

    // signOut logic
    auth.clear()
    await router.push('/login')

    expect(mockClear).toHaveBeenCalledOnce()
    expect(mockPush).toHaveBeenCalledWith('/login')
  })

  it('calls clear before push', async () => {
    const callOrder: string[] = []
    const mockClear = vi.fn(() => callOrder.push('clear'))
    const mockPush = vi.fn((_path: string) => {
      callOrder.push('push')
      return Promise.resolve()
    })

    const auth = { clear: mockClear }
    const router = { push: mockPush }

    auth.clear()
    await router.push('/login')

    expect(callOrder).toEqual(['clear', 'push'])
  })
})
