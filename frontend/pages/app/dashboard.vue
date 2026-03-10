<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { definePageMeta, navigateTo } from '#imports'
import { useApi } from '~/composables/useApi'
import { useAuth } from '~/composables/useAuth'

type Project = {
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

type StoryEntity = {
  id: number
  projectId: number
  type: 'Character' | 'Location' | 'Theme' | 'Arc'
  name: string
  summary: string
  updatedAt: string
}

definePageMeta({
  layout: 'app'
})

const { apiFetch } = useApi()
const auth = useAuth()

const projects = ref<Project[]>([])
const loading = ref(true)
const creating = ref(false)
const pageReady = ref(false)
const statsLoading = ref(false)
const showCreateModal = ref(false)

const errorMessage = ref('')
const successMessage = ref('')
const formErrorMessage = ref('')
const debugMessage = ref('Dashboard initializing...')

const totalChapters = ref(0)
const totalCharacters = ref(0)
const totalLocations = ref(0)

const createForm = reactive({
  title: '',
  description: ''
})

const totalProjects = computed(() => projects.value.length)

const logStep = (message: string) => {
  debugMessage.value = message
  console.log(`[Dashboard Debug] ${message}`)
}

const resetCreateForm = () => {
  createForm.title = ''
  createForm.description = ''
  formErrorMessage.value = ''
}

const openCreateModal = () => {
  formErrorMessage.value = ''
  showCreateModal.value = true
}

const closeCreateModal = () => {
  if (creating.value) return
  showCreateModal.value = false
  resetCreateForm()
}

const redirectToLogin = async () => {
  auth.clear()
  await navigateTo('/login', { replace: true })
}

const loadProjects = async () => {
  loading.value = true
  errorMessage.value = ''
  logStep('Loading projects...')

  try {
    const result = await apiFetch<Project[]>('/projects', {
      method: 'GET'
    })

    projects.value = Array.isArray(result) ? result : []
    logStep(`Loaded ${projects.value.length} project(s).`)
  } catch (error: any) {
    console.error('Failed to load projects:', error)

    if (error?.status === 401 || error?.data?.status === 401) {
      errorMessage.value = 'Your session has expired. Please sign in again.'
      await redirectToLogin()
      return
    }

    errorMessage.value =
      error?.data?.message ||
      error?.data?.title ||
      error?.message ||
      'Failed to load projects.'
  } finally {
    loading.value = false
  }
}

const loadAccountStats = async () => {
  statsLoading.value = true

  totalChapters.value = 0
  totalCharacters.value = 0
  totalLocations.value = 0

  if (projects.value.length === 0) {
    logStep('Loaded 0 project(s). Stats reset to zero.')
    statsLoading.value = false
    return
  }

  logStep(`Loading stats across ${projects.value.length} project(s)...`)

  try {
    const chapterRequests = projects.value.map(project =>
      apiFetch<Chapter[]>(`/projects/${project.id}/chapters`, {
        method: 'GET'
      })
    )

    const entityRequests = projects.value.map(project =>
      apiFetch<StoryEntity[]>(`/projects/${project.id}/entities`, {
        method: 'GET'
      }).catch(() => [])
    )

    const [chapterResults, entityResults] = await Promise.all([
      Promise.allSettled(chapterRequests),
      Promise.allSettled(entityRequests)
    ])

    let chapterCount = 0
    let characterCount = 0
    let locationCount = 0

    for (const result of chapterResults) {
      if (result.status === 'fulfilled' && Array.isArray(result.value)) {
        chapterCount += result.value.length
      }
    }

    for (const result of entityResults) {
      if (result.status === 'fulfilled' && Array.isArray(result.value)) {
        for (const entity of result.value) {
          if (entity.type === 'Character') {
            characterCount += 1
          } else if (entity.type === 'Location') {
            locationCount += 1
          }
        }
      }
    }

    totalChapters.value = chapterCount
    totalCharacters.value = characterCount
    totalLocations.value = locationCount

    logStep(
      `Stats loaded: ${totalProjects.value} project(s), ${totalChapters.value} chapter(s), ${totalCharacters.value} character(s), ${totalLocations.value} location(s).`
    )
  } catch (error: any) {
    console.error('Failed to load dashboard stats:', error)
    logStep('Projects loaded, but stats loading partially failed.')
  } finally {
    statsLoading.value = false
  }
}

const createProject = async () => {
  formErrorMessage.value = ''
  errorMessage.value = ''
  successMessage.value = ''

  const title = createForm.title.trim()
  const description = createForm.description.trim()

  if (!title) {
    formErrorMessage.value = 'Project title is required.'
    return
  }

  creating.value = true

  try {
    const created = await apiFetch<Project>('/projects', {
      method: 'POST',
      body: {
        title,
        description: description || null
      }
    })

    if (!created?.id) {
      throw new Error('The server did not return the created project.')
    }

    projects.value.unshift(created)
    successMessage.value = `Project "${created.title}" created successfully.`
    resetCreateForm()
    showCreateModal.value = false

    await loadAccountStats()
  } catch (error: any) {
    console.error('Failed to create project:', error)

    if (error?.status === 401 || error?.data?.status === 401) {
      formErrorMessage.value = 'Your session has expired. Please sign in again.'
      await redirectToLogin()
      return
    }

    formErrorMessage.value =
      error?.data?.message ||
      error?.data?.title ||
      error?.message ||
      'Failed to create project.'
  } finally {
    creating.value = false
  }
}

onMounted(async () => {
  auth.init()

  if (!auth.token.value) {
    await redirectToLogin()
    return
  }

  pageReady.value = true
  await loadProjects()
  await loadAccountStats()
})
</script>

<template>
  <div class="dashboard-page">
    <div class="page-container dashboard-inner">
      <div class="dashboard-header">
        <h1>Dashboard</h1>
        <p>Manage your writing projects and story bibles</p>
      </div>

      <div class="debug-box">
        <strong>Debug:</strong> {{ debugMessage }}
      </div>

      <div v-if="errorMessage" class="message error">
        {{ errorMessage }}
      </div>

      <div v-if="successMessage" class="message success">
        {{ successMessage }}
      </div>

      <div v-if="!pageReady" class="empty-panel panel">
        <div class="empty-icon">
          <v-progress-circular indeterminate color="#818cf8" :size="34" :width="3" />
        </div>
        <h3>Preparing dashboard...</h3>
        <p>Checking your session.</p>
      </div>

      <template v-else>
        <div class="stats-grid">
          <div class="stat-card panel">
            <div class="stat-top">
              <v-icon icon="mdi-file-document-outline" size="20" color="#818cf8" />
              <span>{{ statsLoading ? '—' : totalChapters }}</span>
            </div>
            <p>Total Chapters</p>
          </div>

          <div class="stat-card panel">
            <div class="stat-top">
              <v-icon icon="mdi-account-group-outline" size="20" color="#c084fc" />
              <span>{{ statsLoading ? '—' : totalCharacters }}</span>
            </div>
            <p>Characters</p>
          </div>

          <div class="stat-card panel">
            <div class="stat-top">
              <v-icon icon="mdi-map-marker-outline" size="20" color="#60a5fa" />
              <span>{{ statsLoading ? '—' : totalLocations }}</span>
            </div>
            <p>Locations</p>
          </div>

          <div class="stat-card panel">
            <div class="stat-top">
              <v-icon icon="mdi-book-open-page-variant-outline" size="20" color="#4ade80" />
              <span>{{ totalProjects }}</span>
            </div>
            <p>Projects</p>
          </div>
        </div>

        <section class="project-section">
          <div class="section-row">
            <h2>Your Projects</h2>

            <button
              type="button"
              class="btn-primary"
              @click="openCreateModal"
            >
              Create New Project
            </button>
          </div>

          <div v-if="loading" class="empty-panel panel">
            <div class="empty-icon">
              <v-progress-circular indeterminate color="#818cf8" :size="34" :width="3" />
            </div>
            <h3>Loading projects...</h3>
            <p>Please wait while your dashboard loads.</p>
          </div>

          <div v-else-if="projects.length === 0" class="empty-panel panel">
            <div class="empty-icon">
              <v-icon icon="mdi-folder-outline" size="28" color="#818cf8" />
            </div>
            <h3>No projects yet</h3>
            <p>Your created projects will appear here.</p>
          </div>

          <div v-else class="project-grid">
            <NuxtLink
              v-for="project in projects"
              :key="project.id"
              :to="`/app/project/${project.id}`"
              class="project-card panel"
            >
              <div class="project-card-top">
                <div class="project-icon">
                  <v-icon icon="mdi-folder-outline" size="20" color="#818cf8" />
                </div>
              </div>

              <h3>{{ project.title }}</h3>

              <p class="project-description">
                {{ project.description || 'No description provided yet.' }}
              </p>

              <div class="project-footer">
                <span>
                  <v-icon icon="mdi-calendar-outline" size="14" />
                  {{ new Date(project.createdAt).toLocaleDateString() }}
                </span>
                <span>
                  <v-icon icon="mdi-arrow-right" size="14" />
                  Open
                </span>
              </div>
            </NuxtLink>
          </div>
        </section>
      </template>
    </div>

    <div v-if="showCreateModal" class="modal-backdrop" @click.self="closeCreateModal">
      <div class="modal panel">
        <div class="modal-header">
          <h2>Create New Project</h2>
          <button class="icon-btn" type="button" @click="closeCreateModal" :disabled="creating">
            <v-icon icon="mdi-close" size="20" />
          </button>
        </div>

        <div v-if="formErrorMessage" class="message error modal-message">
          {{ formErrorMessage }}
        </div>

        <div class="field-wrap">
          <label class="form-label" for="project-title">Project Title</label>
          <input
            id="project-title"
            v-model="createForm.title"
            class="input-dark"
            placeholder="Enter your project title"
            :disabled="creating"
          />
        </div>

        <div class="field-wrap">
          <label class="form-label" for="project-description">Description</label>
          <textarea
            id="project-description"
            v-model="createForm.description"
            class="input-dark textarea-dark"
            placeholder="Enter a short description"
            :disabled="creating"
          />
        </div>

        <div class="modal-actions">
          <button
            type="button"
            class="btn-secondary"
            @click="closeCreateModal"
            :disabled="creating"
          >
            Cancel
          </button>

          <button
            type="button"
            class="btn-primary"
            :disabled="creating"
            @click="createProject"
          >
            {{ creating ? 'Creating...' : 'Create Project' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.dashboard-page {
  min-height: 100vh;
  background: #121212;
  padding: 32px 0;
}

.dashboard-inner {
  max-width: 1280px;
}

.dashboard-header {
  margin-bottom: 32px;
}

.dashboard-header h1 {
  margin: 0 0 8px;
  font-size: 3rem;
  font-weight: 800;
  color: white;
}

.dashboard-header p {
  margin: 0;
  color: #9ca3af;
}

.debug-box {
  margin-bottom: 16px;
  padding: 12px 14px;
  border-radius: 10px;
  background: rgba(59, 130, 246, 0.12);
  border: 1px solid rgba(59, 130, 246, 0.25);
  color: #93c5fd;
  font-size: 0.875rem;
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

.modal-message {
  margin-bottom: 16px;
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 16px;
  margin-bottom: 32px;
}

.stat-card {
  padding: 24px;
  border-radius: 16px;
}

.stat-top {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 8px;
}

.stat-top span {
  font-size: 2rem;
  font-weight: 800;
  color: white;
}

.stat-card p {
  margin: 0;
  color: #9ca3af;
  font-size: 0.875rem;
}

.section-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 24px;
  gap: 16px;
}

.section-row h2 {
  margin: 0;
  font-size: 2rem;
  font-weight: 700;
  color: white;
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
  min-height: 110px;
  resize: vertical;
}

.empty-panel {
  min-height: 260px;
  border-radius: 16px;
  display: grid;
  place-items: center;
  text-align: center;
  padding: 32px;
}

.empty-panel h3 {
  margin: 12px 0 6px;
  color: white;
  font-size: 1.25rem;
  font-weight: 700;
}

.empty-panel p {
  margin: 0;
  color: #9ca3af;
}

.empty-icon {
  width: 64px;
  height: 64px;
  border-radius: 14px;
  display: grid;
  place-items: center;
  background: rgba(79, 70, 229, 0.16);
}

.project-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 20px;
}

.project-card {
  display: block;
  padding: 22px;
  border-radius: 16px;
  transition: 0.2s ease;
}

.project-card:hover {
  transform: translateY(-2px);
  border-color: rgba(79, 70, 229, 0.4);
}

.project-card-top {
  display: flex;
  justify-content: space-between;
  margin-bottom: 18px;
}

.project-icon {
  width: 42px;
  height: 42px;
  border-radius: 12px;
  background: rgba(79, 70, 229, 0.16);
  display: grid;
  place-items: center;
}

.project-card h3 {
  margin: 0 0 10px;
  color: white;
  font-size: 1.2rem;
  font-weight: 700;
}

.project-description {
  margin: 0 0 18px;
  color: #9ca3af;
  line-height: 1.6;
  min-height: 52px;
}

.project-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  color: #9ca3af;
  font-size: 0.85rem;
}

.project-footer span {
  display: inline-flex;
  align-items: center;
  gap: 6px;
}

.modal-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.72);
  display: grid;
  place-items: center;
  padding: 20px;
  z-index: 50;
}

.modal {
  width: min(640px, 100%);
  border-radius: 20px;
  padding: 24px;
}

.modal-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 20px;
}

.modal-header h2 {
  margin: 0;
  color: white;
  font-size: 1.5rem;
}

.modal-actions {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  margin-top: 20px;
}

.icon-btn {
  width: 36px;
  height: 36px;
  border: 0;
  border-radius: 10px;
  background: #1f2937;
  color: #d1d5db;
  display: inline-grid;
  place-items: center;
  cursor: pointer;
}

.icon-btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.btn-secondary {
  border: 1px solid #374151;
  background: transparent;
  color: white;
  border-radius: 12px;
  padding: 10px 16px;
  cursor: pointer;
}

@media (max-width: 1100px) {
  .stats-grid,
  .project-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 760px) {
  .stats-grid,
  .project-grid {
    grid-template-columns: 1fr;
  }

  .section-row {
    flex-direction: column;
    align-items: flex-start;
  }

  .dashboard-header h1 {
    font-size: 2.3rem;
  }
}
</style>