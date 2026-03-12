<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { navigateTo } from '#imports'
import { useApi } from '~/composables/useApi'

definePageMeta({ layout: 'app' })

type SearchResult = {
  projectId: number
  projectTitle: string
  chapterId: number
  chapterTitle: string
  text: string
  score: number
}

type SearchResponse = {
  projectId: number | null
  query: string
  results: SearchResult[]
}

type ProjectItem = {
  id: number
  title: string
  description?: string | null
  createdAt?: string
}

const { apiFetch } = useApi()

const ALL_PROJECTS_VALUE = 'all'

const searchQuery = ref('')
const hasSearched = ref(false)
const loading = ref(false)
const projectsLoading = ref(false)
const errorMessage = ref('')
const results = ref<SearchResult[]>([])
const recentSearches = ref<string[]>([])
const projects = ref<ProjectItem[]>([])
const selectedScope = ref<string>(ALL_PROJECTS_VALUE)

const suggestionItems = [
  'Scenes with a specific tone',
  'Character appearances or mentions',
  'References to locations or artifacts',
  'Dialogue around a theme or event'
]

const currentProjectId = computed<number | null>(() => {
  if (selectedScope.value === ALL_PROJECTS_VALUE) {
    return null
  }

  const parsed = Number(selectedScope.value)
  return Number.isFinite(parsed) && parsed > 0 ? parsed : null
})

const currentScopeLabel = computed(() => {
  if (selectedScope.value === ALL_PROJECTS_VALUE) {
    return 'All Projects'
  }

  const project = projects.value.find(p => p.id === currentProjectId.value)
  return project?.title ?? 'Selected Project'
})

const recentStorageKey = computed(() => {
  return currentProjectId.value == null
    ? 'canonguard_recent_searches_all_projects'
    : `canonguard_recent_searches_project_${currentProjectId.value}`
})

const loadProjects = async () => {
  projectsLoading.value = true

  try {
    const response = await apiFetch<ProjectItem[]>('/projects', {
      method: 'GET'
    })

    projects.value = Array.isArray(response) ? response : []
  } catch (error: any) {
    console.error(error)
    errorMessage.value =
      error?.data?.message ||
      error?.message ||
      'Failed to load your projects.'
  } finally {
    projectsLoading.value = false
  }
}

const loadRecentSearches = () => {
  if (import.meta.server) return

  try {
    const raw = localStorage.getItem(recentStorageKey.value)
    recentSearches.value = raw ? JSON.parse(raw) : []
  } catch {
    recentSearches.value = []
  }
}

const saveRecentSearch = (query: string) => {
  if (import.meta.server) return

  const trimmed = query.trim()
  if (!trimmed) return

  const updated = [
    trimmed,
    ...recentSearches.value.filter(item => item.toLowerCase() !== trimmed.toLowerCase())
  ].slice(0, 6)

  recentSearches.value = updated
  localStorage.setItem(recentStorageKey.value, JSON.stringify(updated))
}

const clearSearch = () => {
  hasSearched.value = false
  loading.value = false
  errorMessage.value = ''
  results.value = []
}

const scoreLabel = (score: number) => {
  const percent = Math.max(0, Math.min(100, Math.round(score * 100)))
  return `${percent}% match`
}

const openChapter = async (chapterId: number) => {
  await navigateTo(`/app/editor/${chapterId}`)
}

const buildSearchUrl = (query: string) => {
  const encodedQuery = encodeURIComponent(query)

  if (currentProjectId.value == null) {
    return `/semantic-search?query=${encodedQuery}&top=8`
  }

  return `/projects/${currentProjectId.value}/semantic-search?query=${encodedQuery}&top=8`
}

const runSearch = async (queryOverride?: string) => {
  const finalQuery = (queryOverride ?? searchQuery.value).trim()

  searchQuery.value = finalQuery
  hasSearched.value = true
  errorMessage.value = ''
  results.value = []

  if (!finalQuery) {
    errorMessage.value = 'Please enter a search query.'
    return
  }

  loading.value = true

  try {
    const response = await apiFetch<SearchResponse>(
      buildSearchUrl(finalQuery),
      { method: 'GET' }
    )

    results.value = response.results ?? []
    saveRecentSearch(finalQuery)
  } catch (error: any) {
    console.error(error)
    errorMessage.value =
      error?.data?.message ||
      error?.message ||
      'Something went wrong while running semantic search.'
  } finally {
    loading.value = false
  }
}

const submitSearch = async () => {
  await runSearch()
}

const useSuggestion = async (text: string) => {
  await runSearch(text)
}

watch(selectedScope, () => {
  loadRecentSearches()
  clearSearch()
})

onMounted(async () => {
  await loadProjects()
  loadRecentSearches()
})
</script>

<template>
  <div class="search-page">
    <div class="search-container">
      <div class="search-header">
        <h1>Semantic Search</h1>
        <p>Search your manuscript by meaning, not just keywords</p>
      </div>

      <div class="scope-row panel">
        <div class="scope-copy">
          <h2>Search Scope</h2>
          <p>Choose whether to search everything you’ve written or only one project.</p>
        </div>

        <div class="scope-control">
          <label class="scope-label" for="scope-select">Scope</label>
          <select id="scope-select" v-model="selectedScope" class="scope-select">
            <option :value="ALL_PROJECTS_VALUE">All Projects</option>
            <option
              v-for="project in projects"
              :key="project.id"
              :value="String(project.id)"
            >
              {{ project.title }}
            </option>
          </select>
        </div>
      </div>

      <form class="search-form" @submit.prevent="submitSearch">
        <div class="search-box">
          <v-icon icon="mdi-magnify" size="24" color="#9ca3af" class="search-icon" />
          <input
            v-model="searchQuery"
            class="search-input"
            placeholder="Search your manuscript by meaning..."
          />
          <button type="submit" class="btn-primary search-btn" :disabled="loading || projectsLoading">
            {{ loading ? 'Searching...' : 'Search' }}
          </button>
        </div>
      </form>

      <div v-if="errorMessage" class="panel error-panel">
        <v-icon icon="mdi-alert-circle-outline" size="18" color="#fca5a5" />
        <p>{{ errorMessage }}</p>
      </div>

      <div v-if="!hasSearched" class="search-prestate">
        <div class="panel suggestion-panel">
          <h2>Try searching in {{ currentScopeLabel }}:</h2>
          <div class="suggestion-grid">
            <button
              v-for="item in suggestionItems"
              :key="item"
              type="button"
              class="suggestion-card"
              @click="useSuggestion(item)"
            >
              <v-icon icon="mdi-magnify" size="16" color="#9ca3af" />
              <span>{{ item }}</span>
            </button>
          </div>
        </div>

        <div class="panel recent-panel">
          <h2>Recent Searches</h2>

          <div v-if="recentSearches.length" class="recent-list">
            <button
              v-for="item in recentSearches"
              :key="item"
              type="button"
              class="recent-item"
              @click="useSuggestion(item)"
            >
              <v-icon icon="mdi-history" size="16" color="#9ca3af" />
              <span>{{ item }}</span>
            </button>
          </div>

          <div v-else class="empty-recent">
            <p>No recent searches yet for this scope.</p>
          </div>
        </div>
      </div>

      <div v-else class="search-results">
        <div class="results-head">
          <div>
            <h2>Results</h2>
            <p class="results-subtext" v-if="searchQuery">
              Showing results for “{{ searchQuery }}” in {{ currentScopeLabel }}
            </p>
          </div>

          <button type="button" class="clear-btn" @click="clearSearch">
            Clear search
          </button>
        </div>

        <div v-if="loading" class="panel results-empty">
          <div class="empty-icon">
            <v-icon icon="mdi-loading mdi-spin" size="28" color="#818cf8" />
          </div>
          <h3>Searching your manuscript...</h3>
          <p>We’re finding the most semantically similar passages.</p>
        </div>

        <div v-else-if="results.length === 0" class="panel results-empty">
          <div class="empty-icon">
            <v-icon icon="mdi-file-document-outline" size="28" color="#818cf8" />
          </div>
          <h3>No results found</h3>
          <p>Try a broader search or save your chapters again to regenerate embeddings.</p>
        </div>

        <div v-else class="results-list">
          <div
            v-for="result in results"
            :key="`${result.projectId}-${result.chapterId}-${result.text}`"
            class="panel result-card"
          >
            <div class="result-top">
              <div>
                <p class="result-project">{{ result.projectTitle }}</p>
                <h3>{{ result.chapterTitle }}</h3>
                <p class="result-meta">
                  Chapter ID: {{ result.chapterId }}
                </p>
              </div>

              <div class="score-badge">
                {{ scoreLabel(result.score) }}
              </div>
            </div>

            <p class="result-text">
              {{ result.text }}
            </p>

            <div class="result-actions">
              <button type="button" class="btn-secondary" @click="openChapter(result.chapterId)">
                Open chapter
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.search-page {
  min-height: 100vh;
  background: #121212;
  padding: 32px;
}

.search-container {
  max-width: 1040px;
  margin: 0 auto;
}

.search-header {
  margin-bottom: 32px;
  text-align: center;
}

.search-header h1 {
  margin: 0 0 8px;
  font-size: 3rem;
  font-weight: 800;
  color: white;
}

.search-header p {
  margin: 0;
  color: #9ca3af;
}

.scope-row {
  display: flex;
  justify-content: space-between;
  gap: 20px;
  padding: 20px 24px;
  border-radius: 18px;
  margin-bottom: 20px;
  align-items: end;
}

.scope-copy h2 {
  margin: 0 0 8px;
  color: white;
  font-size: 1.1rem;
}

.scope-copy p {
  margin: 0;
  color: #9ca3af;
}

.scope-control {
  min-width: 260px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.scope-label {
  color: #d1d5db;
  font-size: 0.95rem;
}

.scope-select {
  border: 1px solid #2e2e2e;
  border-radius: 12px;
  background: #181818;
  color: white;
  padding: 12px 14px;
  outline: none;
}

.scope-select:focus {
  border-color: #4f46e5;
}

.search-form {
  margin-bottom: 20px;
}

.search-box {
  position: relative;
  display: flex;
  align-items: center;
  gap: 12px;
  background: #1e1e1e;
  border: 2px solid #2e2e2e;
  border-radius: 18px;
  padding: 8px 10px 8px 52px;
  min-height: 72px;
}

.search-box:focus-within {
  border-color: #4f46e5;
}

.search-icon {
  position: absolute;
  left: 18px;
  top: 50%;
  transform: translateY(-50%);
}

.search-input {
  flex: 1;
  border: 0;
  background: transparent;
  color: white;
  font-size: 1.05rem;
  outline: none;
}

.search-input::placeholder {
  color: #6b7280;
}

.search-btn {
  padding: 12px 20px;
}

.panel {
  background: #1e1e1e;
  border: 1px solid #2e2e2e;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.18);
}

.error-panel {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 14px 16px;
  border-radius: 14px;
  margin-bottom: 20px;
  border-color: rgba(239, 68, 68, 0.35);
  background: rgba(127, 29, 29, 0.18);
}

.error-panel p {
  margin: 0;
  color: #fecaca;
}

.search-prestate {
  display: grid;
  gap: 24px;
}

.suggestion-panel,
.recent-panel,
.results-empty,
.result-card {
  padding: 24px;
  border-radius: 18px;
}

.suggestion-panel h2,
.recent-panel h2,
.results-head h2 {
  margin: 0 0 18px;
  color: white;
  font-size: 1.35rem;
  font-weight: 700;
}

.suggestion-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 14px;
}

.suggestion-card,
.recent-item {
  display: flex;
  align-items: center;
  gap: 10px;
  width: 100%;
  text-align: left;
  border: 1px solid #2e2e2e;
  background: #181818;
  color: #d1d5db;
  border-radius: 12px;
  padding: 16px;
  cursor: pointer;
  transition: 0.18s ease;
}

.suggestion-card:hover,
.recent-item:hover {
  background: #252525;
  border-color: rgba(79, 70, 229, 0.35);
  color: white;
}

.recent-list {
  display: grid;
  gap: 12px;
}

.empty-recent {
  min-height: 80px;
  display: grid;
  place-items: center;
}

.empty-recent p {
  margin: 0;
  color: #9ca3af;
}

.search-results {
  display: grid;
  gap: 18px;
}

.results-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
}

.results-subtext {
  margin: 6px 0 0;
  color: #9ca3af;
}

.clear-btn {
  border: 0;
  background: transparent;
  color: #9ca3af;
  cursor: pointer;
}

.clear-btn:hover {
  color: white;
}

.results-empty {
  min-height: 260px;
  display: grid;
  place-items: center;
  text-align: center;
}

.empty-icon {
  width: 64px;
  height: 64px;
  border-radius: 14px;
  display: grid;
  place-items: center;
  background: rgba(79, 70, 229, 0.16);
  margin-bottom: 14px;
}

.results-empty h3 {
  margin: 0 0 8px;
  color: white;
  font-size: 1.25rem;
}

.results-empty p {
  margin: 0;
  color: #9ca3af;
  max-width: 520px;
}

.results-list {
  display: grid;
  gap: 16px;
}

.result-card {
  display: grid;
  gap: 18px;
}

.result-top {
  display: flex;
  justify-content: space-between;
  gap: 16px;
  align-items: flex-start;
}

.result-project {
  margin: 0 0 6px;
  color: #a5b4fc;
  font-size: 0.92rem;
  font-weight: 700;
}

.result-top h3 {
  margin: 0 0 6px;
  color: white;
  font-size: 1.1rem;
}

.result-meta {
  margin: 0;
  color: #9ca3af;
  font-size: 0.95rem;
}

.score-badge {
  white-space: nowrap;
  padding: 8px 12px;
  border-radius: 999px;
  background: rgba(79, 70, 229, 0.16);
  color: #c7d2fe;
  border: 1px solid rgba(99, 102, 241, 0.3);
  font-size: 0.9rem;
  font-weight: 600;
}

.result-text {
  margin: 0;
  color: #d1d5db;
  line-height: 1.7;
}

.result-actions {
  display: flex;
  justify-content: flex-start;
}

.btn-secondary {
  border: 1px solid #374151;
  background: #181818;
  color: white;
  border-radius: 10px;
  padding: 10px 14px;
  cursor: pointer;
}

.btn-secondary:hover {
  background: #252525;
}

@media (max-width: 760px) {
  .search-page {
    padding: 20px;
  }

  .scope-row,
  .results-head,
  .result-top,
  .search-box {
    flex-direction: column;
    align-items: stretch;
  }

  .scope-control {
    min-width: 100%;
  }

  .suggestion-grid {
    grid-template-columns: 1fr;
  }

  .score-badge {
    align-self: flex-start;
  }
}
</style>