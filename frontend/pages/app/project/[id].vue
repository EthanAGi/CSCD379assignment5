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

type StoryEntity = {
  id: number
  projectId: number
  type: 'Character' | 'Location' | 'Theme' | 'Arc'
  name: string
  summary: string
  updatedAt: string
}

const route = useRoute()
const { apiFetch } = useApi()
const auth = useAuth()

const projectId = computed(() => Number(route.params.id))

const project = ref<ProjectDetail | null>(null)
const chapters = ref<Chapter[]>([])
const entities = ref<StoryEntity[]>([])

const pageLoading = ref(true)
const creatingChapter = ref(false)
const showCreateChapterModal = ref(false)

const errorMessage = ref('')
const successMessage = ref('')
const formErrorMessage = ref('')

const createChapterForm = reactive({
  title: '',
  content: ''
})

/* ✅ NEW: STRIP HTML FUNCTION */
const stripHtml = (html: string) => {
  if (!html) return ''
  return html
    .replace(/<br\s*\/?>/gi, '\n')
    .replace(/<\/p>/gi, '\n')
    .replace(/<[^>]*>/g, '')
}

const totalChapters = computed(() => chapters.value.length)

const characterEntries = computed(() =>
  entities.value.filter(entity => entity.type === 'Character')
)

const locationEntries = computed(() =>
  entities.value.filter(entity => entity.type === 'Location')
)

const themeEntries = computed(() =>
  entities.value.filter(entity => entity.type === 'Theme')
)

const totalCharacters = computed(() => characterEntries.value.length)
const totalLocations = computed(() => locationEntries.value.length)

const totalWords = computed(() => {
  return chapters.value.reduce((sum, chapter) => {
    const text = stripHtml(chapter.content || '').trim()
    if (!text) return sum
    return sum + text.split(/\s+/).length
  }, 0)
})

const previewCharacters = computed(() => characterEntries.value.slice(0, 5))
const previewLocations = computed(() => locationEntries.value.slice(0, 5))
const previewThemes = computed(() => themeEntries.value.slice(0, 5))

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

const loadEntities = async () => {
  const result = await apiFetch<StoryEntity[]>(`/projects/${projectId.value}/entities`, {
    method: 'GET'
  })
  entities.value = Array.isArray(result) ? result : []
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

    await Promise.all([loadProject(), loadChapters(), loadEntities()])
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

const openCreateChapterModal = () => {
  formErrorMessage.value = ''
  successMessage.value = ''
  showCreateChapterModal.value = true
}

const closeCreateChapterModal = () => {
  if (creatingChapter.value) return

  showCreateChapterModal.value = false
  formErrorMessage.value = ''
  createChapterForm.title = ''
  createChapterForm.content = ''
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
      body: { title, content }
    })

    closeCreateChapterModal()
    await navigateTo(`/app/editor/${created.id}`)
  } catch (error: any) {
    formErrorMessage.value =
      error?.data?.message ||
      error?.data?.title ||
      error?.message ||
      'Failed to create chapter.'
  } finally {
    creatingChapter.value = false
  }
}

onMounted(loadPage)
</script>

<template>
  <div class="workspace-page">
    <div class="page-container">

      <!-- Header -->
      <div class="workspace-header">
        <h1>{{ project?.title || 'Project Workspace' }}</h1>
        <p>{{ project?.description }}</p>
      </div>

      <!-- Chapters -->
      <div class="chapter-list">
        <NuxtLink
          v-for="chapter in chapters"
          :key="chapter.id"
          :to="`/app/editor/${chapter.id}`"
          class="chapter-card"
        >
          <h3>{{ chapter.title }}</h3>

          <!-- ✅ FIXED HERE -->
          <p>
            {{ stripHtml(chapter.content || '') || 'No content yet.' }}
          </p>

          <span>{{ new Date(chapter.updatedAt).toLocaleDateString() }}</span>
        </NuxtLink>
      </div>

    </div>
  </div>
</template>