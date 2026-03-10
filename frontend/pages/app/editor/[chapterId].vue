<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute, navigateTo } from '#imports'
import { useApi } from '~/composables/useApi'
import { useAuth } from '~/composables/useAuth'

definePageMeta({ layout: 'app' })

type Chapter = {
  id: number
  projectId: number
  title: string
  content: string
  createdAt: string
  updatedAt: string
}

const route = useRoute()
const { apiFetch } = useApi()
const auth = useAuth()

const chapterId = computed(() => Number(route.params.chapterId))

const chapter = ref<Chapter | null>(null)
const title = ref('')
const content = ref('')
const fontSize = ref(16)

const loading = ref(true)
const saving = ref(false)
const errorMessage = ref('')
const successMessage = ref('')

const wordCount = computed(() => {
  const text = content.value.trim()
  if (!text) return 0
  return text.split(/\s+/).length
})

const charCount = computed(() => content.value.length)

const readTimeMinutes = computed(() => {
  const words = wordCount.value
  if (words === 0) return 0
  return Math.max(1, Math.ceil(words / 200))
})

const redirectToLogin = async () => {
  auth.clear()
  await navigateTo('/login', { replace: true })
}

const loadChapter = async () => {
  loading.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    auth.init()

    if (!auth.token.value) {
      await redirectToLogin()
      return
    }

    const result = await apiFetch<Chapter>(`/chapters/${chapterId.value}`, {
      method: 'GET'
    })

    chapter.value = result
    title.value = result.title
    content.value = result.content ?? ''
  } catch (error: any) {
    console.error('Failed to load chapter:', error)

    if (error?.status === 401 || error?.data?.status === 401) {
      await redirectToLogin()
      return
    }

    errorMessage.value =
      error?.data?.message ||
      error?.data?.title ||
      error?.message ||
      'Failed to load chapter.'
  } finally {
    loading.value = false
  }
}

const saveChapter = async () => {
  errorMessage.value = ''
  successMessage.value = ''

  const trimmedTitle = title.value.trim()

  if (!trimmedTitle) {
    errorMessage.value = 'Chapter title is required.'
    return
  }

  saving.value = true

  try {
    const updated = await apiFetch<Chapter>(`/chapters/${chapterId.value}`, {
      method: 'PUT',
      body: {
        title: trimmedTitle,
        content: content.value
      }
    })

    chapter.value = updated
    title.value = updated.title
    content.value = updated.content
    successMessage.value = 'Chapter saved successfully.'
  } catch (error: any) {
    console.error('Failed to save chapter:', error)

    if (error?.status === 401 || error?.data?.status === 401) {
      await redirectToLogin()
      return
    }

    errorMessage.value =
      error?.data?.message ||
      error?.data?.title ||
      error?.message ||
      'Failed to save chapter.'
  } finally {
    saving.value = false
  }
}

onMounted(async () => {
  await loadChapter()
})
</script>

<template>
  <div class="editor-page">
    <div class="editor-header">
      <div class="header-left">
        <NuxtLink
          :to="chapter ? `/app/project/${chapter.projectId}` : '/app/dashboard'"
          class="back-btn"
        >
          <v-icon icon="mdi-arrow-left" size="20" />
        </NuxtLink>

        <div class="header-meta">
          <input
            v-model="title"
            class="chapter-title-input"
            type="text"
            placeholder="Chapter title"
            :disabled="loading || saving"
          />
          <p>Open and edit your selected chapter</p>
        </div>
      </div>

      <div class="header-right">
        <span>{{ wordCount }} words</span>
        <button class="btn-primary save-btn" :disabled="loading || saving" @click="saveChapter">
          <v-icon icon="mdi-content-save-outline" size="16" />
          {{ saving ? 'Saving...' : 'Save' }}
        </button>
      </div>
    </div>

    <div v-if="errorMessage" class="message error">
      {{ errorMessage }}
    </div>

    <div v-if="successMessage" class="message success">
      {{ successMessage }}
    </div>

    <div class="editor-body">
      <div class="editor-main">
        <div class="toolbar">
          <div class="tool-row">
            <button class="tool-btn" type="button">
              <v-icon icon="mdi-format-font" size="16" />
              Georgia
              <v-icon icon="mdi-chevron-down" size="14" />
            </button>

            <div class="tool-sep" />

            <button
              class="icon-btn"
              type="button"
              @click="fontSize = Math.max(12, fontSize - 1)"
            >
              <v-icon icon="mdi-minus" size="16" />
            </button>
            <span class="font-size">{{ fontSize }}</span>
            <button
              class="icon-btn"
              type="button"
              @click="fontSize = Math.min(32, fontSize + 1)"
            >
              <v-icon icon="mdi-plus" size="16" />
            </button>
          </div>
        </div>

        <div class="editor-surface">
          <div v-if="loading" class="loading-box">
            <h3>Loading chapter...</h3>
            <p>Please wait while the editor loads.</p>
          </div>

          <div v-else class="editor-wrap">
            <textarea
              v-model="content"
              class="editor-textarea"
              :style="{ fontSize: `${fontSize}px`, fontFamily: 'Georgia, serif' }"
              placeholder="Start writing your story..."
            />
          </div>
        </div>
      </div>

      <aside class="editor-side">
        <div class="panel side-card">
          <h3>Chapter Stats</h3>
          <div class="stats-list">
            <div><span>Words</span><strong>{{ wordCount }}</strong></div>
            <div><span>Characters</span><strong>{{ charCount }}</strong></div>
            <div><span>Locations</span><strong>—</strong></div>
            <div><span>Read Time</span><strong>{{ readTimeMinutes }} min</strong></div>
          </div>
        </div>

        <div class="panel side-card">
          <h3>Detected in this Chapter</h3>
          <div class="empty-detected">
            <p>No extracted entities yet.</p>
          </div>
        </div>
      </aside>
    </div>
  </div>
</template>

<style scoped>
.editor-page {
  height: 100vh;
  background: #121212;
  display: flex;
  flex-direction: column;
}

.editor-header {
  padding: 16px 24px;
  background: #1e1e1e;
  border-bottom: 1px solid #1f2937;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
}

.header-left,
.header-right {
  display: flex;
  align-items: center;
  gap: 16px;
}

.header-left {
  min-width: 0;
}

.header-meta {
  min-width: 0;
}

.back-btn {
  color: #9ca3af;
}

.back-btn:hover {
  color: white;
}

.chapter-title-input {
  width: min(520px, 60vw);
  max-width: 100%;
  margin: 0 0 4px;
  background: transparent;
  border: 0;
  outline: none;
  color: white;
  font-size: 1.125rem;
  font-weight: 700;
}

.editor-header p,
.header-right span {
  margin: 0;
  color: #9ca3af;
  font-size: 0.875rem;
}

.save-btn {
  display: inline-flex;
  align-items: center;
  gap: 8px;
}

.message {
  margin: 12px 24px 0;
  padding: 12px 14px;
  border-radius: 10px;
  font-size: 0.875rem;
}

.message.error {
  background: rgba(239, 68, 68, 0.12);
  border: 1px solid rgba(239, 68, 68, 0.25);
  color: #fca5a5;
}

.message.success {
  background: rgba(34, 197, 94, 0.12);
  border: 1px solid rgba(34, 197, 94, 0.25);
  color: #86efac;
}

.editor-body {
  flex: 1;
  display: grid;
  grid-template-columns: 1fr 256px;
  overflow: hidden;
}

.editor-main {
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.toolbar {
  background: #1e1e1e;
  border-bottom: 1px solid #1f2937;
  padding: 12px 24px;
}

.tool-row {
  display: flex;
  align-items: center;
  gap: 4px;
  flex-wrap: wrap;
  max-width: 1100px;
  margin: 0 auto;
}

.tool-btn {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding: 8px 12px;
  background: #252525;
  border: 1px solid #374151;
  border-radius: 8px;
  color: white;
}

.icon-btn {
  width: 34px;
  height: 34px;
  border: 0;
  background: transparent;
  border-radius: 8px;
  color: #9ca3af;
  cursor: pointer;
}

.icon-btn:hover {
  background: #2a2a2a;
  color: white;
}

.tool-sep {
  width: 1px;
  height: 24px;
  background: #374151;
  margin: 0 6px;
}

.font-size {
  width: 32px;
  text-align: center;
  color: white;
  font-size: 0.875rem;
}

.editor-surface {
  flex: 1;
  overflow: auto;
  padding: 32px;
}

.loading-box {
  max-width: 960px;
  margin: 0 auto;
  padding: 24px;
  border-radius: 16px;
  background: #1e1e1e;
  border: 1px solid #2e2e2e;
  text-align: center;
}

.loading-box h3 {
  margin: 0 0 8px;
  color: white;
}

.loading-box p {
  margin: 0;
  color: #9ca3af;
}

.editor-wrap {
  max-width: 960px;
  margin: 0 auto;
}

.editor-textarea {
  width: 100%;
  min-height: 800px;
  resize: none;
  background: transparent;
  border: 0;
  color: #f3f4f6;
  line-height: 1.8;
  outline: none;
}

.editor-side {
  width: 256px;
  padding: 24px;
  background: #1e1e1e;
  border-left: 1px solid #1f2937;
  overflow: auto;
  display: grid;
  gap: 24px;
}

.side-card {
  padding: 16px;
}

.side-card h3 {
  margin: 0 0 16px;
  color: white;
  font-size: 0.95rem;
}

.stats-list {
  display: grid;
  gap: 12px;
}

.stats-list div {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.stats-list span {
  color: #9ca3af;
  font-size: 0.875rem;
}

.stats-list strong {
  color: white;
  font-size: 0.875rem;
}

.empty-detected {
  min-height: 100px;
  display: grid;
  place-items: center;
  text-align: center;
}

.empty-detected p {
  margin: 0;
  color: #9ca3af;
  font-size: 0.875rem;
}

@media (max-width: 1100px) {
  .editor-body {
    grid-template-columns: 1fr;
  }

  .editor-side {
    width: 100%;
    border-left: 0;
    border-top: 1px solid #1f2937;
  }
}
</style>