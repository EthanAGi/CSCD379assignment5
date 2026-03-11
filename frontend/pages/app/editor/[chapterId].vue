<script setup lang="ts">
import { ref, computed, onMounted, nextTick, watch } from 'vue'
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

type ExtractedEntity = {
  name: string
  sourceQuote: string
  confidence: number
}

type ChapterEntityExtractionResponse = {
  chapterId: number
  projectId: number
  characters: ExtractedEntity[]
  locations: ExtractedEntity[]
}

const route = useRoute()
const { apiFetch } = useApi()
const auth = useAuth()

const chapterId = computed(() => Number(route.params.chapterId))

const chapter = ref<Chapter | null>(null)
const title = ref('')
const editorHtml = ref('')
const fontSize = ref(16)
const fontFamily = ref('Georgia')

const loading = ref(true)
const saving = ref(false)
const extracting = ref(false)
const errorMessage = ref('')
const successMessage = ref('')

const editorRef = ref<HTMLElement | null>(null)

const detectedCharacters = ref<ExtractedEntity[]>([])
const detectedLocations = ref<ExtractedEntity[]>([])

const fontOptions = [
  'Georgia',
  'Times New Roman',
  'Arial',
  'Verdana',
  'Trebuchet MS',
  'Courier New'
]

const editorText = computed(() => {
  if (!editorHtml.value) return ''
  if (typeof window === 'undefined') return editorHtml.value.replace(/<[^>]*>/g, ' ')
  const temp = document.createElement('div')
  temp.innerHTML = editorHtml.value
  return temp.textContent || temp.innerText || ''
})

const wordCount = computed(() => {
  const text = editorText.value.trim()
  if (!text) return 0
  return text.split(/\s+/).length
})

const charCount = computed(() => editorText.value.length)

const readTimeMinutes = computed(() => {
  const words = wordCount.value
  if (words === 0) return 0
  return Math.max(1, Math.ceil(words / 200))
})

const redirectToLogin = async () => {
  auth.clear()
  await navigateTo('/login', { replace: true })
}

const escapeHtml = (value: string) =>
  value
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;')
    .replaceAll("'", '&#39;')

const plainTextToHtml = (value: string) => {
  const trimmed = value ?? ''
  if (!trimmed.trim()) return '<p></p>'

  return trimmed
    .split(/\n{2,}/)
    .map(block => `<p>${escapeHtml(block).replace(/\n/g, '<br>')}</p>`)
    .join('')
}

const looksLikeHtml = (value: string) => /<\/?[a-z][\s\S]*>/i.test(value)

const syncEditorFromDom = () => {
  if (!editorRef.value) return
  editorHtml.value = editorRef.value.innerHTML
}

const writeEditorContentToDom = async () => {
  await nextTick()

  if (editorRef.value) {
    editorRef.value.innerHTML = editorHtml.value || '<p></p>'
  }
}

const focusEditor = () => {
  nextTick(() => {
    editorRef.value?.focus()
  })
}

const runCommand = (command: string, value?: string) => {
  focusEditor()
  document.execCommand('styleWithCSS', false, 'true')
  document.execCommand(command, false, value)
  syncEditorFromDom()
}

const setBlock = (tag: 'p' | 'h1' | 'h2' | 'blockquote') => {
  focusEditor()
  document.execCommand('formatBlock', false, tag)
  syncEditorFromDom()
}

const insertLink = () => {
  const url = window.prompt('Enter link URL')
  if (!url) return
  runCommand('createLink', url)
}

const clearFormatting = () => {
  focusEditor()
  document.execCommand('removeFormat')
  document.execCommand('unlink')
  syncEditorFromDom()
}

const increaseFontSize = () => {
  fontSize.value = Math.min(32, fontSize.value + 1)
}

const decreaseFontSize = () => {
  fontSize.value = Math.max(12, fontSize.value - 1)
}

const applyFontFamily = () => {
  runCommand('fontName', fontFamily.value)
}

const saveChapterInternal = async (showSuccessMessage = true) => {
  const trimmedTitle = title.value.trim()

  if (!trimmedTitle) {
    errorMessage.value = 'Chapter title is required.'
    return false
  }

  saving.value = true

  try {
    syncEditorFromDom()

    const updated = await apiFetch<Chapter>(`/chapters/${chapterId.value}`, {
      method: 'PUT',
      body: {
        title: trimmedTitle,
        content: editorHtml.value
      }
    })

    chapter.value = updated
    title.value = updated.title

    editorHtml.value =
      updated.content && looksLikeHtml(updated.content)
        ? updated.content
        : plainTextToHtml(updated.content ?? '')

    await writeEditorContentToDom()

    if (showSuccessMessage) {
      successMessage.value = 'Chapter saved successfully.'
    }

    return true
  } catch (error: any) {
    console.error('Failed to save chapter:', error)

    if (error?.status === 401 || error?.data?.status === 401) {
      await redirectToLogin()
      return false
    }

    errorMessage.value =
      error?.data?.message ||
      error?.data?.title ||
      error?.message ||
      'Failed to save chapter.'

    return false
  } finally {
    saving.value = false
  }
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

    editorHtml.value =
      result.content && looksLikeHtml(result.content)
        ? result.content
        : plainTextToHtml(result.content ?? '')

    loading.value = false
    await writeEditorContentToDom()
    return
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
    if (loading.value) {
      loading.value = false
    }
  }
}

const saveChapter = async () => {
  errorMessage.value = ''
  successMessage.value = ''
  await saveChapterInternal(true)
}

const extractEntities = async () => {
  errorMessage.value = ''
  successMessage.value = ''

  if (!chapterId.value || Number.isNaN(chapterId.value)) {
    errorMessage.value = 'Invalid chapter id.'
    return
  }

  extracting.value = true

  try {
    const saved = await saveChapterInternal(false)
    if (!saved) return

    const result = await apiFetch<ChapterEntityExtractionResponse>(
      `/chapters/${chapterId.value}/extract-entities`,
      {
        method: 'POST'
      }
    )

    detectedCharacters.value = result.characters ?? []
    detectedLocations.value = result.locations ?? []

    successMessage.value = 'AI extraction completed successfully.'
  } catch (error: any) {
    console.error('Failed to extract entities:', error)

    if (error?.status === 401 || error?.data?.status === 401) {
      await redirectToLogin()
      return
    }

    errorMessage.value =
      error?.data?.message ||
      error?.data?.title ||
      error?.message ||
      'Failed to extract entities.'
  } finally {
    extracting.value = false
  }
}

watch(editorHtml, async () => {
  if (!loading.value && editorRef.value && editorRef.value.innerHTML !== editorHtml.value) {
    await writeEditorContentToDom()
  }
})

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
            :disabled="loading || saving || extracting"
          />
          <p>Open and edit your selected chapter</p>
        </div>
      </div>

      <div class="header-right">
        <span>{{ wordCount }} words</span>

        <button
          class="btn-secondary extract-btn"
          :disabled="loading || saving || extracting"
          @click="extractEntities"
        >
          <v-icon icon="mdi-robot-outline" size="16" />
          {{ extracting ? 'Extracting...' : 'Extract AI' }}
        </button>

        <button
          class="btn-primary save-btn"
          :disabled="loading || saving || extracting"
          @click="saveChapter"
        >
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
            <div class="toolbar-group">
              <button class="toolbar-select-btn" type="button">
                <v-icon icon="mdi-format-font" size="16" />
              </button>

              <select v-model="fontFamily" class="toolbar-select" @change="applyFontFamily">
                <option v-for="font in fontOptions" :key="font" :value="font">
                  {{ font }}
                </option>
              </select>
            </div>

            <div class="tool-sep" />

            <div class="toolbar-group compact">
              <button class="icon-btn" type="button" @click="decreaseFontSize">
                <v-icon icon="mdi-minus" size="16" />
              </button>
              <span class="font-size">{{ fontSize }}</span>
              <button class="icon-btn" type="button" @click="increaseFontSize">
                <v-icon icon="mdi-plus" size="16" />
              </button>
            </div>

            <div class="tool-sep" />

            <button class="icon-btn" type="button" title="Bold" @click="runCommand('bold')">
              <v-icon icon="mdi-format-bold" size="18" />
            </button>
            <button class="icon-btn" type="button" title="Italic" @click="runCommand('italic')">
              <v-icon icon="mdi-format-italic" size="18" />
            </button>
            <button class="icon-btn" type="button" title="Underline" @click="runCommand('underline')">
              <v-icon icon="mdi-format-underline" size="18" />
            </button>
            <button class="icon-btn" type="button" title="Strike" @click="runCommand('strikeThrough')">
              <v-icon icon="mdi-format-strikethrough-variant" size="18" />
            </button>

            <div class="tool-sep" />

            <button class="icon-btn" type="button" title="Link" @click="insertLink">
              <v-icon icon="mdi-link-variant" size="18" />
            </button>
            <button class="icon-btn" type="button" title="Remove Link" @click="runCommand('unlink')">
              <v-icon icon="mdi-link-variant-off" size="18" />
            </button>

            <div class="tool-sep" />

            <button class="icon-btn" type="button" title="Align Left" @click="runCommand('justifyLeft')">
              <v-icon icon="mdi-format-align-left" size="18" />
            </button>
            <button class="icon-btn" type="button" title="Align Center" @click="runCommand('justifyCenter')">
              <v-icon icon="mdi-format-align-center" size="18" />
            </button>
            <button class="icon-btn" type="button" title="Align Right" @click="runCommand('justifyRight')">
              <v-icon icon="mdi-format-align-right" size="18" />
            </button>
            <button class="icon-btn" type="button" title="Justify" @click="runCommand('justifyFull')">
              <v-icon icon="mdi-format-align-justify" size="18" />
            </button>

            <div class="tool-sep" />

            <button class="icon-btn" type="button" title="Bulleted List" @click="runCommand('insertUnorderedList')">
              <v-icon icon="mdi-format-list-bulleted" size="18" />
            </button>
            <button class="icon-btn" type="button" title="Numbered List" @click="runCommand('insertOrderedList')">
              <v-icon icon="mdi-format-list-numbered" size="18" />
            </button>

            <div class="tool-sep" />

            <button class="icon-btn" type="button" title="Paragraph" @click="setBlock('p')">
              <v-icon icon="mdi-format-paragraph" size="18" />
            </button>
            <button class="icon-btn" type="button" title="Heading" @click="setBlock('h1')">
              <v-icon icon="mdi-format-header-1" size="18" />
            </button>
            <button class="icon-btn" type="button" title="Subheading" @click="setBlock('h2')">
              <v-icon icon="mdi-format-header-2" size="18" />
            </button>
            <button class="icon-btn" type="button" title="Quote" @click="setBlock('blockquote')">
              <v-icon icon="mdi-format-quote-close" size="18" />
            </button>

            <div class="tool-sep" />

            <button class="icon-btn" type="button" title="Undo" @click="runCommand('undo')">
              <v-icon icon="mdi-undo" size="18" />
            </button>
            <button class="icon-btn" type="button" title="Redo" @click="runCommand('redo')">
              <v-icon icon="mdi-redo" size="18" />
            </button>
            <button class="icon-btn" type="button" title="Clear Formatting" @click="clearFormatting">
              <v-icon icon="mdi-format-clear" size="18" />
            </button>
          </div>
        </div>

        <div class="editor-surface">
          <div v-if="loading" class="loading-box">
            <h3>Loading chapter...</h3>
            <p>Please wait while the editor loads.</p>
          </div>

          <div v-else class="editor-wrap">
            <div
              ref="editorRef"
              class="editor-content"
              contenteditable="true"
              spellcheck="true"
              :style="{ fontSize: `${fontSize}px`, fontFamily }"
              @input="syncEditorFromDom"
            ></div>
          </div>
        </div>
      </div>

      <aside class="editor-side">
        <div class="panel side-card">
          <h3>Chapter Stats</h3>
          <div class="stats-list">
            <div><span>Words</span><strong>{{ wordCount }}</strong></div>
            <div><span>Characters</span><strong>{{ charCount }}</strong></div>
            <div><span>Detected Characters</span><strong>{{ detectedCharacters.length }}</strong></div>
            <div><span>Detected Locations</span><strong>{{ detectedLocations.length }}</strong></div>
            <div><span>Read Time</span><strong>{{ readTimeMinutes }} min</strong></div>
          </div>
        </div>

        <div class="panel side-card">
          <div class="side-card-header">
            <h3>AI Extraction</h3>
            <button
              class="mini-extract-btn"
              :disabled="loading || saving || extracting"
              @click="extractEntities"
            >
              {{ extracting ? 'Running...' : 'Run' }}
            </button>
          </div>

          <p class="side-help">
            Save and analyze this chapter with your Foundry-powered backend to detect characters and locations.
          </p>
        </div>

        <div class="panel side-card">
          <h3>Detected Characters</h3>

          <div v-if="detectedCharacters.length" class="detected-list">
            <div
              v-for="character in detectedCharacters"
              :key="`char-${character.name}`"
              class="detected-pill purple"
            >
              <span class="pill-badge">{{ character.name.charAt(0) }}</span>
              <div class="detected-content">
                <span class="detected-name">{{ character.name }}</span>
                <small class="detected-meta">
                  {{ Math.round((character.confidence || 0) * 100) }}% confidence
                </small>
                <small v-if="character.sourceQuote" class="detected-quote">
                  “{{ character.sourceQuote }}”
                </small>
              </div>
            </div>
          </div>

          <div v-else class="empty-detected">
            <p>No characters extracted yet.</p>
          </div>
        </div>

        <div class="panel side-card">
          <h3>Detected Locations</h3>

          <div v-if="detectedLocations.length" class="detected-list">
            <div
              v-for="location in detectedLocations"
              :key="`loc-${location.name}`"
              class="detected-pill green"
            >
              <span class="pill-badge">
                <v-icon icon="mdi-map-marker" size="12" />
              </span>
              <div class="detected-content">
                <span class="detected-name">{{ location.name }}</span>
                <small class="detected-meta">
                  {{ Math.round((location.confidence || 0) * 100) }}% confidence
                </small>
                <small v-if="location.sourceQuote" class="detected-quote">
                  “{{ location.sourceQuote }}”
                </small>
              </div>
            </div>
          </div>

          <div v-else class="empty-detected">
            <p>No locations extracted yet.</p>
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

.save-btn,
.extract-btn {
  display: inline-flex;
  align-items: center;
  gap: 8px;
}

.btn-primary,
.btn-secondary,
.mini-extract-btn {
  border: 0;
  cursor: pointer;
  transition: 0.2s ease;
}

.btn-primary:disabled,
.btn-secondary:disabled,
.mini-extract-btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.btn-primary {
  padding: 10px 14px;
  border-radius: 10px;
  background: linear-gradient(135deg, #6366f1, #4f46e5);
  color: white;
  font-weight: 600;
}

.btn-secondary {
  padding: 10px 14px;
  border-radius: 10px;
  background: #252525;
  border: 1px solid #374151;
  color: white;
  font-weight: 600;
}

.btn-secondary:hover,
.btn-primary:hover,
.mini-extract-btn:hover {
  transform: translateY(-1px);
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
  grid-template-columns: 1fr 320px;
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

.toolbar-group {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding: 0 4px;
}

.toolbar-group.compact {
  gap: 6px;
}

.toolbar-select-btn {
  width: 34px;
  height: 34px;
  border: 1px solid #374151;
  background: #252525;
  border-radius: 8px;
  color: white;
  display: inline-grid;
  place-items: center;
}

.toolbar-select {
  min-width: 120px;
  height: 34px;
  border: 1px solid #374151;
  background: #252525;
  border-radius: 8px;
  color: white;
  padding: 0 10px;
  outline: none;
}

.icon-btn {
  width: 34px;
  height: 34px;
  border: 0;
  background: transparent;
  border-radius: 8px;
  color: #9ca3af;
  cursor: pointer;
  display: inline-grid;
  place-items: center;
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

.editor-content {
  width: 100%;
  min-height: 800px;
  padding: 0;
  background: transparent;
  border: 0;
  color: #f3f4f6;
  line-height: 1.8;
  outline: none;
  white-space: pre-wrap;
}

.editor-content p {
  margin: 0 0 1em;
}

.editor-content h1,
.editor-content h2,
.editor-content blockquote,
.editor-content ul,
.editor-content ol {
  margin: 0 0 1em;
}

.editor-content blockquote {
  border-left: 3px solid #4f46e5;
  padding-left: 14px;
  color: #d1d5db;
}

.editor-side {
  width: 320px;
  padding: 24px;
  background: #1e1e1e;
  border-left: 1px solid #1f2937;
  overflow: auto;
  display: grid;
  gap: 24px;
}

.side-card {
  padding: 16px;
  background: #181818;
  border: 1px solid #232323;
  border-radius: 16px;
}

.side-card h3 {
  margin: 0 0 16px;
  color: white;
  font-size: 0.95rem;
}

.side-card-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  margin-bottom: 12px;
}

.side-card-header h3 {
  margin: 0;
}

.side-help {
  margin: 0;
  color: #9ca3af;
  font-size: 0.875rem;
  line-height: 1.5;
}

.mini-extract-btn {
  padding: 8px 12px;
  border-radius: 10px;
  background: #312e81;
  color: white;
  font-size: 0.8rem;
  font-weight: 600;
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

.detected-list {
  display: grid;
  gap: 12px;
}

.detected-pill {
  display: flex;
  align-items: flex-start;
  gap: 10px;
  padding: 10px 12px;
  border-radius: 12px;
  background: #252525;
  color: white;
}

.detected-pill.purple .pill-badge {
  background: rgba(168, 85, 247, 0.22);
  color: #d8b4fe;
}

.detected-pill.green .pill-badge {
  background: rgba(34, 197, 94, 0.18);
  color: #86efac;
}

.pill-badge {
  width: 24px;
  height: 24px;
  border-radius: 8px;
  display: inline-grid;
  place-items: center;
  font-size: 0.75rem;
  font-weight: 700;
  flex: 0 0 24px;
}

.detected-content {
  display: grid;
  gap: 4px;
  min-width: 0;
}

.detected-name {
  color: white;
  font-size: 0.9rem;
  font-weight: 600;
}

.detected-meta {
  color: #9ca3af;
  font-size: 0.75rem;
}

.detected-quote {
  color: #d1d5db;
  font-size: 0.75rem;
  line-height: 1.45;
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