<script setup lang="ts">
import { ref, reactive, computed, onMounted, watch } from 'vue'
import { definePageMeta, navigateTo, useRoute } from '#imports'
import { useApi } from '~/composables/useApi'
import { useAuth } from '~/composables/useAuth'

definePageMeta({ layout: 'app' })

type TabKey = 'characters' | 'locations' | 'themes' | 'arcs'

type StoryEntity = {
  id: number
  projectId: number
  type: 'Character' | 'Location' | 'Theme' | 'Arc'
  name: string
  summary: string
  updatedAt: string
}

type ProjectSummary = {
  id: number
  title: string
  description?: string | null
  createdAt: string
}

const LAST_PROJECT_KEY = 'canonguard_story_bible_project_id'

const route = useRoute()
const { apiFetch } = useApi()
const auth = useAuth()

const activeTab = ref<TabKey>('characters')
const searchQuery = ref('')

const tabs: Array<{ id: TabKey; label: string; icon: string; apiType: StoryEntity['type'] }> = [
  { id: 'characters', label: 'Characters', icon: 'mdi-account-group-outline', apiType: 'Character' },
  { id: 'locations', label: 'Locations', icon: 'mdi-map-marker-outline', apiType: 'Location' },
  { id: 'themes', label: 'Themes', icon: 'mdi-lightbulb-outline', apiType: 'Theme' },
  { id: 'arcs', label: 'Story Arcs', icon: 'mdi-trending-up', apiType: 'Arc' }
]

const projects = ref<ProjectSummary[]>([])
const selectedProjectId = ref<number | null>(null)

const allEntries = ref<StoryEntity[]>([])
const loadingProjects = ref(true)
const loadingEntries = ref(false)
const saving = ref(false)
const deletingId = ref<number | null>(null)

const errorMessage = ref('')
const successMessage = ref('')
const projectLoadMessage = ref('')
const debugMessage = ref('Story Bible initializing...')

const showForm = ref(false)
const editingEntryId = ref<number | null>(null)
const initialized = ref(false)

const form = reactive({
  projectId: null as number | null,
  type: 'Character' as StoryEntity['type'],
  name: '',
  summary: ''
})

const currentTabConfig = computed(() => {
  return tabs.find(tab => tab.id === activeTab.value) ?? tabs[0]
})

const selectedProject = computed(() => {
  return projects.value.find(p => p.id === selectedProjectId.value) ?? null
})

const filteredEntries = computed(() => {
  const activeType = currentTabConfig.value.apiType
  const query = searchQuery.value.trim().toLowerCase()

  return allEntries.value.filter(entry => {
    const matchesType = entry.type === activeType
    const matchesSearch =
      !query ||
      entry.name.toLowerCase().includes(query) ||
      entry.summary.toLowerCase().includes(query)

    return matchesType && matchesSearch
  })
})

const isEditing = computed(() => editingEntryId.value !== null)
const hasProjects = computed(() => projects.value.length > 0)

const logStep = (message: string) => {
  debugMessage.value = message
  console.log(`[Story Bible Debug] ${message}`)
}

function setActiveTab(tab: TabKey) {
  activeTab.value = tab
  form.type = tabs.find(t => t.id === tab)?.apiType ?? 'Character'
}

function normalizeProjectId(value: unknown): number | null {
  const parsed = Number(value)
  return Number.isFinite(parsed) && parsed > 0 ? parsed : null
}

function getRouteProjectId(): number | null {
  const queryValue = Array.isArray(route.query.projectId)
    ? route.query.projectId[0]
    : route.query.projectId

  return normalizeProjectId(queryValue)
}

function getStoredProjectId(): number | null {
  if (import.meta.server) return null
  return normalizeProjectId(localStorage.getItem(LAST_PROJECT_KEY))
}

function storeProjectId(projectId: number | null) {
  if (import.meta.server) return

  if (projectId) {
    localStorage.setItem(LAST_PROJECT_KEY, String(projectId))
  } else {
    localStorage.removeItem(LAST_PROJECT_KEY)
  }
}

function pickInitialProjectId(availableProjects: ProjectSummary[]): number | null {
  const routeProjectId = getRouteProjectId()
  if (routeProjectId && availableProjects.some(project => project.id === routeProjectId)) {
    return routeProjectId
  }

  const storedProjectId = getStoredProjectId()
  if (storedProjectId && availableProjects.some(project => project.id === storedProjectId)) {
    return storedProjectId
  }

  return availableProjects.length > 0 ? availableProjects[0].id : null
}

const redirectToLogin = async () => {
  auth.clear()
  await navigateTo('/login', { replace: true })
}

function getFriendlyError(error: any, fallback: string) {
  return (
    error?.data?.message ||
    error?.data?.title ||
    error?.message ||
    fallback
  )
}

async function loadProjects() {
  loadingProjects.value = true
  errorMessage.value = ''
  projectLoadMessage.value = ''
  logStep('Loading projects...')

  try {
    const response = await apiFetch<ProjectSummary[]>('/projects', {
      method: 'GET'
    })

    const normalized = Array.isArray(response) ? response : []

    projects.value = normalized.map(project => ({
      ...project,
      id: Number(project.id)
    }))

    if (projects.value.length === 0) {
      selectedProjectId.value = null
      allEntries.value = []
      projectLoadMessage.value = 'No projects were found for this account.'
      logStep('Loaded 0 project(s).')
      return
    }

    selectedProjectId.value = pickInitialProjectId(projects.value)
    storeProjectId(selectedProjectId.value)
    logStep(`Loaded ${projects.value.length} project(s).`)
  } catch (error: any) {
    console.error('Failed to load projects:', error)

    projects.value = []
    selectedProjectId.value = null
    allEntries.value = []

    if (error?.status === 401 || error?.data?.status === 401) {
      errorMessage.value = 'Your session has expired. Please sign in again.'
      await redirectToLogin()
      return
    }

    errorMessage.value = getFriendlyError(error, 'Failed to load projects.')
    projectLoadMessage.value = 'Because the projects request failed, the Story Bible page has no project context.'
    logStep('Project loading failed.')
  } finally {
    loadingProjects.value = false
  }
}

async function loadEntries() {
  errorMessage.value = ''

  if (!selectedProjectId.value) {
    allEntries.value = []
    return
  }

  loadingEntries.value = true
  logStep(`Loading entries for project ${selectedProjectId.value}...`)

  try {
    const response = await apiFetch<StoryEntity[]>(`/projects/${selectedProjectId.value}/entities`, {
      method: 'GET'
    })

    allEntries.value = Array.isArray(response) ? response : []
    logStep(`Loaded ${allEntries.value.length} entr${allEntries.value.length === 1 ? 'y' : 'ies'}.`)
  } catch (error: any) {
    console.error('Failed to load story bible entries:', error)

    allEntries.value = []

    if (error?.status === 401 || error?.data?.status === 401) {
      errorMessage.value = 'Your session has expired. Please sign in again.'
      await redirectToLogin()
      return
    }

    errorMessage.value = getFriendlyError(error, 'Failed to load story bible entries.')
    logStep('Entry loading failed.')
  } finally {
    loadingEntries.value = false
  }
}

function openCreateForm() {
  errorMessage.value = ''
  successMessage.value = ''

  if (!hasProjects.value) {
    errorMessage.value = 'No projects are available for this account.'
    return
  }

  editingEntryId.value = null
  form.projectId = selectedProjectId.value
  form.type = currentTabConfig.value.apiType
  form.name = ''
  form.summary = ''
  showForm.value = true
}

function openEditForm(entry: StoryEntity) {
  editingEntryId.value = entry.id
  form.projectId = entry.projectId
  form.type = entry.type
  form.name = entry.name
  form.summary = entry.summary
  errorMessage.value = ''
  successMessage.value = ''
  showForm.value = true
}

function closeForm() {
  showForm.value = false
  editingEntryId.value = null
}

async function saveEntry() {
  errorMessage.value = ''
  successMessage.value = ''

  if (!form.projectId || form.projectId <= 0) {
    errorMessage.value = 'Please select a project.'
    return
  }

  if (!form.type) {
    errorMessage.value = 'Please select a type.'
    return
  }

  if (!form.name.trim()) {
    errorMessage.value = 'Entry name is required.'
    return
  }

  saving.value = true

  try {
    if (editingEntryId.value !== null) {
      const previousProjectId = selectedProjectId.value

      const updated = await apiFetch<StoryEntity>(`/entities/${editingEntryId.value}`, {
        method: 'PUT',
        body: {
          projectId: form.projectId,
          type: form.type,
          name: form.name.trim(),
          summary: form.summary.trim()
        }
      })

      if (updated.projectId === previousProjectId) {
        allEntries.value = allEntries.value.map(entry =>
          entry.id === updated.id ? updated : entry
        )
      } else {
        allEntries.value = allEntries.value.filter(entry => entry.id !== updated.id)
      }

      successMessage.value = 'Entry updated successfully.'
    } else {
      const created = await apiFetch<StoryEntity>(`/projects/${form.projectId}/entities`, {
        method: 'POST',
        body: {
          type: form.type,
          name: form.name.trim(),
          summary: form.summary.trim()
        }
      })

      if (selectedProjectId.value === form.projectId) {
        allEntries.value = [created, ...allEntries.value]
      }

      successMessage.value = 'Entry created successfully.'
    }

    closeForm()
  } catch (error: any) {
    console.error('Failed to save entry:', error)

    if (error?.status === 401 || error?.data?.status === 401) {
      errorMessage.value = 'Your session has expired. Please sign in again.'
      await redirectToLogin()
      return
    }

    errorMessage.value = getFriendlyError(error, 'Failed to save entry.')
  } finally {
    saving.value = false
  }
}

async function deleteEntry(entry: StoryEntity) {
  const confirmed = window.confirm(`Delete "${entry.name}"?`)
  if (!confirmed) return

  errorMessage.value = ''
  successMessage.value = ''
  deletingId.value = entry.id

  try {
    await apiFetch(`/entities/${entry.id}`, {
      method: 'DELETE'
    })

    allEntries.value = allEntries.value.filter(item => item.id !== entry.id)
    successMessage.value = 'Entry deleted successfully.'
  } catch (error: any) {
    console.error('Failed to delete entry:', error)

    if (error?.status === 401 || error?.data?.status === 401) {
      errorMessage.value = 'Your session has expired. Please sign in again.'
      await redirectToLogin()
      return
    }

    errorMessage.value = getFriendlyError(error, 'Failed to delete entry.')
  } finally {
    deletingId.value = null
  }
}

function onProjectChange(event: Event) {
  const target = event.target as HTMLSelectElement
  selectedProjectId.value = normalizeProjectId(target.value)
}

function onFormProjectChange(event: Event) {
  const target = event.target as HTMLSelectElement
  form.projectId = normalizeProjectId(target.value)
}

function formatDate(value: string) {
  return new Date(value).toLocaleString()
}

watch(selectedProjectId, async (newValue, oldValue) => {
  storeProjectId(newValue)

  if (!initialized.value) return
  if (newValue !== oldValue) {
    await loadEntries()
  }
})

onMounted(async () => {
  auth.init()

  if (!auth.token.value) {
    await redirectToLogin()
    return
  }

  initialized.value = true
  await loadProjects()

  if (selectedProjectId.value) {
    await loadEntries()
  }
})
</script>

<template>
  <div class="story-page">
    <div class="page-container">
      <div class="story-header">
        <div>
          <h1>Story Bible</h1>
          <p>Your complete story knowledge base</p>
        </div>

        <button class="btn-primary add-btn" @click="openCreateForm">
          <v-icon icon="mdi-plus" size="18" />
          Add Entry
        </button>
      </div>

      <div v-if="debugMessage" class="message info-message">
        <strong>Debug:</strong> {{ debugMessage }}
      </div>

      <div v-if="errorMessage" class="message error-message">
        {{ errorMessage }}
      </div>

      <div v-if="successMessage" class="message success-message">
        {{ successMessage }}
      </div>

      <div v-if="projectLoadMessage" class="message info-message">
        {{ projectLoadMessage }}
      </div>

      <div class="toolbar-row">
        <div class="project-picker panel">
          <div class="picker-header">
            <label class="picker-label" for="projectSelect">Project</label>
            <button class="refresh-btn" type="button" @click="loadProjects" :disabled="loadingProjects">
              {{ loadingProjects ? 'Loading...' : 'Refresh' }}
            </button>
          </div>

          <select
            id="projectSelect"
            :value="selectedProjectId ?? ''"
            class="input"
            :disabled="loadingProjects"
            @change="onProjectChange"
          >
            <option value="" disabled>
              {{ loadingProjects ? 'Loading projects...' : hasProjects ? 'Select a project' : 'No projects available' }}
            </option>
            <option v-for="project in projects" :key="project.id" :value="project.id">
              {{ project.title }}
            </option>
          </select>

          <div class="picker-note" v-if="!loadingProjects && !hasProjects">
            No owned projects were returned for the current account.
          </div>
        </div>

        <div class="project-meta panel" v-if="selectedProject">
          <div class="project-meta-title">{{ selectedProject.title }}</div>
          <div class="project-meta-subtitle">
            {{ selectedProject.description || 'No project description.' }}
          </div>
        </div>
      </div>

      <div class="search-wrap">
        <div class="search-bar panel">
          <v-icon icon="mdi-magnify" size="20" color="#9ca3af" />
          <input
            v-model="searchQuery"
            class="search-input"
            placeholder="Search story bible..."
            :disabled="!selectedProjectId"
          />
        </div>
      </div>

      <div class="tab-bar">
        <button
          v-for="tab in tabs"
          :key="tab.id"
          class="tab-btn"
          :class="{ active: activeTab === tab.id }"
          @click="setActiveTab(tab.id)"
        >
          <v-icon :icon="tab.icon" size="16" />
          {{ tab.label }}
        </button>
      </div>

      <div class="content-area">
        <div v-if="loadingProjects || loadingEntries" class="empty-card panel">
          <h3>Loading entries...</h3>
          <p>Please wait while your story bible loads.</p>
        </div>

        <div v-else-if="!hasProjects" class="empty-card panel">
          <h3>No projects found for this account</h3>
          <p>Your created projects will appear once the project list loads successfully.</p>
        </div>

        <div v-else-if="!selectedProjectId" class="empty-card panel">
          <h3>No project selected</h3>
          <p>Select a project above to view and create story bible entries.</p>
        </div>

        <div v-else-if="filteredEntries.length === 0" class="empty-card panel">
          <div
            class="empty-icon"
            :class="{
              purple: activeTab === 'characters',
              blue: activeTab === 'locations',
              pink: activeTab === 'themes',
              green: activeTab === 'arcs'
            }"
          >
            <v-icon
              :icon="currentTabConfig.icon"
              size="28"
              :color="
                activeTab === 'characters'
                  ? '#c084fc'
                  : activeTab === 'locations'
                    ? '#60a5fa'
                    : activeTab === 'themes'
                      ? '#f472b6'
                      : '#4ade80'
              "
            />
          </div>

          <h3>No {{ currentTabConfig.label.toLowerCase() }} yet</h3>
          <p>
            Click <strong>Add Entry</strong> to manually create your first
            {{ currentTabConfig.label.toLowerCase().slice(0, -1) || 'entry' }}
            for <strong>{{ selectedProject?.title }}</strong>.
          </p>
        </div>

        <div v-else class="entry-grid">
          <div v-for="entry in filteredEntries" :key="entry.id" class="entry-card panel">
            <div class="entry-top">
              <div>
                <div class="entry-type">{{ entry.type }}</div>
                <h3>{{ entry.name }}</h3>
              </div>

              <div class="entry-actions">
                <button class="icon-btn" @click="openEditForm(entry)" title="Edit entry">
                  <v-icon icon="mdi-pencil-outline" size="18" />
                </button>
                <button
                  class="icon-btn danger"
                  @click="deleteEntry(entry)"
                  :disabled="deletingId === entry.id"
                  title="Delete entry"
                >
                  <v-icon icon="mdi-delete-outline" size="18" />
                </button>
              </div>
            </div>

            <p class="entry-summary">
              {{ entry.summary || 'No summary provided.' }}
            </p>

            <div class="entry-meta">
              Updated {{ formatDate(entry.updatedAt) }}
            </div>
          </div>
        </div>
      </div>
    </div>

    <div v-if="showForm" class="modal-backdrop" @click.self="closeForm">
      <div class="modal panel">
        <div class="modal-header">
          <h2>{{ isEditing ? 'Edit Entry' : 'Add New Entry' }}</h2>
          <button class="icon-btn" @click="closeForm">
            <v-icon icon="mdi-close" size="20" />
          </button>
        </div>

        <div class="form-grid">
          <label class="field">
            <span>Project</span>
            <select
              :value="form.projectId ?? ''"
              class="input"
              @change="onFormProjectChange"
            >
              <option value="" disabled>Select a project</option>
              <option v-for="project in projects" :key="project.id" :value="project.id">
                {{ project.title }}
              </option>
            </select>
          </label>

          <label class="field">
            <span>Type</span>
            <select v-model="form.type" class="input">
              <option value="Character">Character</option>
              <option value="Location">Location</option>
              <option value="Theme">Theme</option>
              <option value="Arc">Arc</option>
            </select>
          </label>

          <label class="field field-full">
            <span>Name</span>
            <input
              v-model="form.name"
              class="input"
              type="text"
              placeholder="Enter a name..."
            />
          </label>

          <label class="field field-full">
            <span>Summary</span>
            <textarea
              v-model="form.summary"
              class="input textarea"
              rows="6"
              placeholder="Write a summary for this entry..."
            />
          </label>
        </div>

        <div class="modal-actions">
          <button class="btn-secondary" @click="closeForm">Cancel</button>
          <button class="btn-primary" @click="saveEntry" :disabled="saving">
            {{ saving ? 'Saving...' : isEditing ? 'Update Entry' : 'Create Entry' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.story-page {
  min-height: 100vh;
  background: #121212;
  padding: 32px 0;
}

.story-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 24px;
  gap: 16px;
}

.story-header h1 {
  margin: 0 0 8px;
  font-size: 3rem;
  font-weight: 800;
  color: white;
}

.story-header p {
  margin: 0;
  color: #9ca3af;
}

.add-btn {
  display: inline-flex;
  align-items: center;
  gap: 8px;
}

.message {
  border-radius: 14px;
  padding: 12px 14px;
  margin-bottom: 16px;
  font-size: 0.95rem;
}

.error-message {
  background: rgba(239, 68, 68, 0.12);
  color: #fca5a5;
  border: 1px solid rgba(239, 68, 68, 0.2);
}

.success-message {
  background: rgba(34, 197, 94, 0.12);
  color: #86efac;
  border: 1px solid rgba(34, 197, 94, 0.2);
}

.info-message {
  background: rgba(59, 130, 246, 0.12);
  color: #93c5fd;
  border: 1px solid rgba(59, 130, 246, 0.2);
}

.toolbar-row {
  display: grid;
  grid-template-columns: 320px 1fr;
  gap: 16px;
  margin-bottom: 24px;
}

.project-picker,
.project-meta {
  padding: 16px;
  border-radius: 16px;
}

.picker-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  margin-bottom: 8px;
}

.picker-label {
  display: block;
  color: #d1d5db;
  font-size: 0.92rem;
  font-weight: 600;
}

.refresh-btn {
  border: 1px solid #374151;
  background: transparent;
  color: #d1d5db;
  border-radius: 10px;
  padding: 8px 12px;
  cursor: pointer;
}

.refresh-btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.picker-note {
  margin-top: 10px;
  color: #9ca3af;
  font-size: 0.88rem;
  line-height: 1.4;
}

.project-meta-title {
  color: white;
  font-size: 1rem;
  font-weight: 700;
  margin-bottom: 6px;
}

.project-meta-subtitle {
  color: #9ca3af;
  line-height: 1.5;
}

.search-wrap {
  margin-bottom: 24px;
}

.search-bar {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 14px 16px;
  border-radius: 14px;
}

.search-input {
  flex: 1;
  border: 0;
  background: transparent;
  color: white;
  outline: none;
  font-size: 0.95rem;
}

.search-input::placeholder {
  color: #6b7280;
}

.tab-bar {
  display: flex;
  gap: 4px;
  border-bottom: 1px solid #2e2e2e;
  margin-bottom: 24px;
  overflow-x: auto;
}

.tab-btn {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  border: 0;
  background: transparent;
  color: #9ca3af;
  padding: 14px 18px;
  cursor: pointer;
  border-bottom: 2px solid transparent;
  white-space: nowrap;
}

.tab-btn.active {
  color: white;
  border-bottom-color: #4f46e5;
  background: rgba(79, 70, 229, 0.08);
}

.content-area {
  display: grid;
}

.entry-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
  gap: 18px;
}

.entry-card {
  border-radius: 18px;
  padding: 20px;
}

.entry-top {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
  margin-bottom: 16px;
}

.entry-type {
  display: inline-block;
  margin-bottom: 8px;
  padding: 6px 10px;
  border-radius: 999px;
  background: rgba(79, 70, 229, 0.14);
  color: #a5b4fc;
  font-size: 0.8rem;
  font-weight: 600;
}

.entry-card h3 {
  margin: 0;
  color: white;
  font-size: 1.15rem;
  font-weight: 700;
}

.entry-summary {
  color: #d1d5db;
  margin: 0 0 16px;
  line-height: 1.6;
  white-space: pre-wrap;
}

.entry-meta {
  color: #6b7280;
  font-size: 0.82rem;
}

.entry-actions {
  display: flex;
  gap: 8px;
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

.icon-btn.danger {
  color: #fca5a5;
}

.icon-btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.empty-card {
  min-height: 360px;
  border-radius: 18px;
  display: grid;
  place-items: center;
  text-align: center;
  padding: 32px;
}

.empty-icon {
  width: 72px;
  height: 72px;
  border-radius: 18px;
  display: grid;
  place-items: center;
  margin-bottom: 16px;
}

.empty-icon.purple { background: rgba(192, 132, 252, 0.14); }
.empty-icon.blue { background: rgba(96, 165, 250, 0.14); }
.empty-icon.pink { background: rgba(244, 114, 182, 0.14); }
.empty-icon.green { background: rgba(74, 222, 128, 0.14); }

.empty-card h3 {
  margin: 0 0 8px;
  color: white;
  font-size: 1.35rem;
  font-weight: 700;
}

.empty-card p {
  margin: 0;
  color: #9ca3af;
  max-width: 560px;
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
  width: min(720px, 100%);
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

.form-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 16px;
}

.field {
  display: grid;
  gap: 8px;
}

.field-full {
  grid-column: 1 / -1;
}

.field span {
  color: #d1d5db;
  font-size: 0.92rem;
  font-weight: 600;
}

.input {
  width: 100%;
  border: 1px solid #374151;
  background: #111827;
  color: white;
  border-radius: 12px;
  padding: 12px 14px;
  outline: none;
}

.textarea {
  resize: vertical;
  min-height: 140px;
}

.modal-actions {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  margin-top: 20px;
}

.btn-secondary {
  border: 1px solid #374151;
  background: transparent;
  color: white;
  border-radius: 12px;
  padding: 10px 16px;
  cursor: pointer;
}

@media (max-width: 760px) {
  .story-header {
    flex-direction: column;
    align-items: flex-start;
  }

  .toolbar-row {
    grid-template-columns: 1fr;
  }

  .form-grid {
    grid-template-columns: 1fr;
  }
}
</style>