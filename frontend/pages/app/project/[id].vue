<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute, navigateTo, definePageMeta } from '#imports'
import { useApi } from '~/composables/useApi'
import { useAuth } from '~/composables/useAuth'

definePageMeta({ layout: 'app' })

type ProjectDetail = {
  id: number
  title: string
  description?: string | null
  createdAt: string
}

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

const projectId = computed(() => Number(route.params.id))

const project = ref<ProjectDetail | null>(null)
const chapters = ref<Chapter[]>([])

const pageLoading = ref(true)
const creatingChapter = ref(false)

const errorMessage = ref('')
const successMessage = ref('')
const formErrorMessage = ref('')

const createChapterForm = reactive({
  title: '',
  content: ''
})

const totalChapters = computed(() => chapters.value.length)

const totalWords = computed(() => {
  return chapters.value.reduce((sum, chapter) => {
    const text = chapter.content?.trim() || ''
    if (!text) return sum
    return sum + text.split(/\s+/).length
  }, 0)
})

const redirectToLogin = async () => {
  auth.clear()
  await navigateTo('/login', { replace: true })
}

const loadProject = async () => {
  const result = await apiFetch<ProjectDetail>(`/projects/${projectId.value}`, {
    method: 'GET'
  })
  project.value = result
}

const loadChapters = async () => {
  const result = await apiFetch<Chapter[]>(`/projects/${projectId.value}/chapters`, {
    method: 'GET'
  })
  chapters.value = Array.isArray(result) ? result : []
}

const loadPage = async () => {
  pageLoading.value = true
  errorMessage.value = ''

  try {
    auth.init()

    if (!auth.token.value) {
      await redirectToLogin()
      return
    }

    await Promise.all([loadProject(), loadChapters()])
  } catch (error: any) {
    console.error('Failed to load workspace:', error)

    if (error?.status === 401 || error?.data?.status === 401) {
      await redirectToLogin()
      return
    }

    errorMessage.value =
      error?.data?.message ||
      error?.data?.title ||
      error?.message ||
      'Failed to load project workspace.'
  } finally {
    pageLoading.value = false
  }
}

const createChapter = async () => {
  formErrorMessage.value = ''
  successMessage.value = ''

  const title = createChapterForm.title.trim()
  const content = createChapterForm.content.trim()

  if (!title) {
    formErrorMessage.value = 'Chapter title is required.'
    return
  }

  creatingChapter.value = true

  try {
    const created = await apiFetch<Chapter>(`/projects/${projectId.value}/chapters`, {
      method: 'POST',
      body: {
        title,
        content
      }
    })

    if (!created?.id) {
      throw new Error('The server did not return the created chapter.')
    }

    await navigateTo(`/app/editor/${created.id}`)
  } catch (error: any) {
    console.error('Failed to create chapter:', error)

    formErrorMessage.value =
      error?.data?.message ||
      error?.data?.title ||
      error?.message ||
      'Failed to create chapter.'
  } finally {
    creatingChapter.value = false
  }
}

onMounted(async () => {
  await loadPage()
})
</script>

<template>
  <div class="workspace-page">
    <div class="page-container">
      <div class="workspace-header">
        <h1>{{ project?.title || 'Project Workspace' }}</h1>
        <p>{{ project?.description || 'Manage chapters, extracted story elements, and project structure' }}</p>
      </div>

      <div v-if="errorMessage" class="message error">
        {{ errorMessage }}
      </div>

      <div v-if="successMessage" class="message success">
        {{ successMessage }}
      </div>

      <div v-if="pageLoading" class="panel section-empty">
        <h3>Loading project...</h3>
        <p>Please wait while the workspace loads.</p>
      </div>

      <template v-else>
        <div class="workspace-stats">
          <div class="stat-box gradient-indigo">
            <v-icon icon="mdi-file-document-outline" size="32" color="#818cf8" />
            <strong>{{ totalChapters }}</strong>
            <span>Chapters</span>
          </div>

          <div class="stat-box gradient-purple">
            <v-icon icon="mdi-account-group-outline" size="32" color="#c084fc" />
            <strong>—</strong>
            <span>Characters</span>
          </div>

          <div class="stat-box gradient-blue">
            <v-icon icon="mdi-map-marker-outline" size="32" color="#60a5fa" />
            <strong>—</strong>
            <span>Locations</span>
          </div>

          <div class="stat-box gradient-green">
            <v-icon icon="mdi-book-open-page-variant-outline" size="32" color="#4ade80" />
            <strong>{{ totalWords }}</strong>
            <span>Total Words</span>
          </div>
        </div>

        <div class="workspace-grid">
          <div class="workspace-left">
            <div class="panel">
              <div class="card-head">
                <h2>Chapters</h2>
              </div>

              <div class="chapter-create">
                <div v-if="formErrorMessage" class="message error">
                  {{ formErrorMessage }}
                </div>

                <div class="field-wrap">
                  <label class="form-label" for="chapter-title">Chapter Title</label>
                  <input
                    id="chapter-title"
                    v-model="createChapterForm.title"
                    class="input-dark"
                    placeholder="Enter chapter title"
                    :disabled="creatingChapter"
                  />
                </div>

                <div class="field-wrap">
                  <label class="form-label" for="chapter-content">Content</label>
                  <textarea
                    id="chapter-content"
                    v-model="createChapterForm.content"
                    class="input-dark textarea-dark"
                    placeholder="Start writing your chapter..."
                    :disabled="creatingChapter"
                  />
                </div>

                <div class="chapter-create-actions">
                  <button class="btn-primary small-btn" :disabled="creatingChapter" @click="createChapter">
                    <v-icon icon="mdi-plus" size="16" />
                    {{ creatingChapter ? 'Creating...' : 'New Chapter' }}
                  </button>
                </div>
              </div>

              <div v-if="chapters.length === 0" class="section-empty">
                <div class="empty-icon">
                  <v-icon icon="mdi-file-document-outline" size="26" color="#818cf8" />
                </div>
                <h3>No chapters yet</h3>
                <p>Chapters will appear here after they are created.</p>
              </div>

              <div v-else class="chapter-list">
                <NuxtLink
                  v-for="chapter in chapters"
                  :key="chapter.id"
                  :to="`/app/editor/${chapter.id}`"
                  class="chapter-card"
                >
                  <h3>{{ chapter.title }}</h3>
                  <p>{{ chapter.content || 'No content yet.' }}</p>
                  <span>{{ new Date(chapter.updatedAt).toLocaleDateString() }}</span>
                </NuxtLink>
              </div>
            </div>

            <div class="panel theme-card">
              <h2>Themes</h2>
              <div class="section-empty compact">
                <p>No extracted themes yet.</p>
              </div>
            </div>
          </div>

          <div class="workspace-right">
            <div class="panel">
              <div class="card-head">
                <h2>Characters</h2>
                <button class="view-link">View All</button>
              </div>

              <div class="section-empty compact">
                <p>No characters available yet.</p>
              </div>
            </div>

            <div class="panel">
              <div class="card-head">
                <h2>Locations</h2>
                <button class="view-link">View All</button>
              </div>

              <div class="section-empty compact">
                <p>No locations available yet.</p>
              </div>
            </div>
          </div>
        </div>
      </template>
    </div>
  </div>
</template>

<style scoped>
.workspace-page {
  min-height: 100vh;
  padding: 32px 0;
  background: #121212;
}

.workspace-header {
  margin-bottom: 32px;
}

.workspace-header h1 {
  margin: 0 0 8px;
  font-size: 3rem;
  color: white;
  font-weight: 800;
}

.workspace-header p {
  margin: 0;
  color: #9ca3af;
}

.message {
  margin-bottom: 20px;
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

.workspace-stats {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 16px;
  margin-bottom: 32px;
}

.stat-box {
  padding: 24px;
  border-radius: 16px;
}

.stat-box strong {
  display: block;
  margin: 10px 0 4px;
  font-size: 2rem;
  color: white;
}

.stat-box span {
  color: #d1d5db;
  font-size: 0.875rem;
}

.workspace-grid {
  display: grid;
  grid-template-columns: 2fr 1fr;
  gap: 24px;
}

.workspace-left,
.workspace-right {
  display: grid;
  gap: 24px;
  align-self: start;
}

.card-head {
  padding: 20px 24px;
  border-bottom: 1px solid #2e2e2e;
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.card-head h2,
.theme-card h2 {
  margin: 0;
  color: white;
  font-size: 1.25rem;
}

.chapter-create {
  padding: 24px;
  border-bottom: 1px solid #2e2e2e;
}

.field-wrap {
  margin-bottom: 18px;
}

.form-label {
  display: block;
  margin-bottom: 8px;
  color: #d1d5db;
  font-size: 0.875rem;
  font-weight: 500;
}

.textarea-dark {
  min-height: 120px;
  resize: vertical;
}

.chapter-create-actions {
  display: flex;
  justify-content: flex-end;
}

.small-btn {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding: 10px 14px;
}

.view-link {
  border: 0;
  background: transparent;
  color: #818cf8;
  cursor: pointer;
}

.theme-card {
  padding: 24px;
}

.section-empty {
  min-height: 220px;
  padding: 24px;
  display: grid;
  place-items: center;
  text-align: center;
}

.section-empty.compact {
  min-height: 140px;
}

.section-empty h3 {
  margin: 12px 0 6px;
  color: white;
  font-size: 1.1rem;
}

.section-empty p {
  margin: 0;
  color: #9ca3af;
}

.empty-icon {
  width: 56px;
  height: 56px;
  border-radius: 14px;
  display: grid;
  place-items: center;
  background: rgba(79, 70, 229, 0.16);
}

.chapter-list {
  padding: 24px;
  display: grid;
  gap: 16px;
}

.chapter-card {
  display: block;
  padding: 18px;
  border: 1px solid #2e2e2e;
  border-radius: 14px;
  background: #181818;
  transition: 0.2s ease;
}

.chapter-card:hover {
  border-color: rgba(79, 70, 229, 0.4);
  transform: translateY(-1px);
}

.chapter-card h3 {
  margin: 0 0 8px;
  color: white;
}

.chapter-card p {
  margin: 0 0 10px;
  color: #9ca3af;
  line-height: 1.5;
}

.chapter-card span {
  font-size: 0.8rem;
  color: #6b7280;
}

@media (max-width: 1100px) {
  .workspace-stats {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .workspace-grid {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 760px) {
  .workspace-stats {
    grid-template-columns: 1fr;
  }

  .workspace-header h1 {
    font-size: 2.3rem;
  }
}
</style>