import { vi } from 'vitest'
import { ref } from 'vue'

export const useState = vi.fn((key: string, init?: () => any) => {
  return ref(init ? init() : null)
})
