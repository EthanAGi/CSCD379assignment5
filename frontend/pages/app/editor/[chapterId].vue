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

type StoryBibleEntity = {
  id: number
  projectId: number
  type: string
  name: string
  summary: string
  updatedAt: string
}

type CanonIssue = {
  passageText: string
  issue: string
  expectedCanon: string
  severity: 'warning' | 'error' | string
  supportingChapterId?: number | null
  supportingQuote: string
  entityName: string
}

type CanonCheckResponse = {
  chapterId: number
  projectId: number
  issues: CanonIssue[]
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
const checkingCanon = ref(false)
const loadingChapterEntities = ref(false)
const errorMessage = ref('')
const successMessage = ref('')

const editorRef = ref<HTMLElement | null>(null)

const detectedCharacters = ref<ExtractedEntity[]>([])
const detectedLocations = ref<ExtractedEntity[]>([])
const canonIssues = ref<CanonIssue[]>([])

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
  temp.innerHTML = stripCanonMarkupFromHtml(editorHtml.value)
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

const unwrapElement = (element: HTMLElement) => {
  const parent = element.parentNode
  if (!parent) return

  while (element.firstChild) {
    parent.insertBefore(element.firstChild, element)
  }

  parent.removeChild(element)
}

const removeCanonHighlightsFromDom = () => {
  if (!editorRef.value) return

  const highlighted = editorRef.value.querySelectorAll('.canon-issue-highlight')
  highlighted.forEach(node => unwrapElement(node as HTMLElement))
}

const stripCanonMarkupFromHtml = (html: string) => {
  if (typeof window === 'undefined') {
    return html.replace(/<span class="canon-issue-highlight"[^>]*>(.*?)<\/span>/gi, '$1')
  }

  const temp = document.createElement('div')
  temp.innerHTML = html

  temp.querySelectorAll('.canon-issue-highlight').forEach(node => {
    unwrapElement(node as HTMLElement)
  })

  return temp.innerHTML
}

const syncEditorFromDom = () => {
  if (!editorRef.value) return

  removeCanonHighlightsFromDom()
  editorHtml.value = editorRef.value.innerHTML
}

const writeEditorContentToDom = async () => {
  await nextTick()

  if (editorRef.value) {
    editorRef.value.innerHTML = stripCanonMarkupFromHtml(editorHtml.value || '<p></p>')
    applyCanonHighlights()
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

const normalizeExtractedEntity = (item: any): ExtractedEntity | null => {
  const name = String(item?.name ?? item?.Name ?? '').trim()
  if (!name) return null

  return {
    name,
    sourceQuote: String(item?.sourceQuote ?? item?.SourceQuote ?? '').trim(),
    confidence: Number(item?.confidence ?? item?.Confidence ?? 0) || 0
  }
}

const normalizeExtractionResponse = (raw: any): ChapterEntityExtractionResponse => {
  const rawCharacters = Array.isArray(raw?.characters)
    ? raw.characters
    : Array.isArray(raw?.Characters)
      ? raw.Characters
      : []

  const rawLocations = Array.isArray(raw?.locations)
    ? raw.locations
    : Array.isArray(raw?.Locations)
      ? raw.Locations
      : []

  return {
    chapterId: Number(raw?.chapterId ?? raw?.ChapterId ?? 0),
    projectId: Number(raw?.projectId ?? raw?.ProjectId ?? 0),
    characters: rawCharacters
      .map(normalizeExtractedEntity)
      .filter((item: ExtractedEntity | null): item is ExtractedEntity => item !== null),
    locations: rawLocations
      .map(normalizeExtractedEntity)
      .filter((item: ExtractedEntity | null): item is ExtractedEntity => item !== null)
  }
}

const normalizeStoryBibleEntity = (item: any): StoryBibleEntity | null => {
  const id = Number(item?.id ?? item?.Id ?? 0)
  const projectId = Number(item?.projectId ?? item?.ProjectId ?? 0)
  const type = String(item?.type ?? item?.Type ?? '').trim()
  const name = String(item?.name ?? item?.Name ?? '').trim()

  if (!id || !projectId || !type || !name) return null

  return {
    id,
    projectId,
    type,
    name,
    summary: String(item?.summary ?? item?.Summary ?? '').trim(),
    updatedAt: String(item?.updatedAt ?? item?.UpdatedAt ?? '')
  }
}

const normalizeCanonIssue = (item: any): CanonIssue | null => {
  const passageText = String(item?.passageText ?? item?.PassageText ?? '').trim()
  const issue = String(item?.issue ?? item?.Issue ?? '').trim()

  if (!passageText || !issue) return null

  return {
    passageText,
    issue,
    expectedCanon: String(item?.expectedCanon ?? item?.ExpectedCanon ?? '').trim(),
    severity: String(item?.severity ?? item?.Severity ?? 'warning').trim() || 'warning',
    supportingChapterId:
      item?.supportingChapterId ?? item?.SupportingChapterId ?? null,
    supportingQuote: String(item?.supportingQuote ?? item?.SupportingQuote ?? '').trim(),
    entityName: String(item?.entityName ?? item?.EntityName ?? '').trim()
  }
}

const normalizeCanonCheckResponse = (raw: any): CanonCheckResponse => {
  const rawIssues = Array.isArray(raw?.issues)
    ? raw.issues
    : Array.isArray(raw?.Issues)
      ? raw.Issues
      : []

  return {
    chapterId: Number(raw?.chapterId ?? raw?.ChapterId ?? 0),
    projectId: Number(raw?.projectId ?? raw?.ProjectId ?? 0),
    issues: rawIssues
      .map(normalizeCanonIssue)
      .filter((item: CanonIssue | null): item is CanonIssue => item !== null)
  }
}

const dedupeEntitiesByName = (items: ExtractedEntity[]) => {
  const seen = new Set<string>()
  return items.filter(item => {
    const key = item.name.trim().toLowerCase()
    if (!key || seen.has(key)) return false
    seen.add(key)
    return true
  })
}

const escapeRegExp = (value: string) => value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')

const chapterContainsEntity = (chapterText: string, entityName: string) => {
  const trimmedText = chapterText.trim()
  const trimmedName = entityName.trim()

  if (!trimmedText || !trimmedName) return false

  const escapedName = escapeRegExp(trimmedName).replace(/\s+/g, '\\s+')
  const regex = new RegExp(`\\b${escapedName}\\b`, 'i')
  return regex.test(trimmedText)
}

const tryHighlightPassage = (passageText: string, tooltip: string) => {
  if (!editorRef.value) return false

  const target = passageText.trim()
  if (!target) return false

  const walker = document.createTreeWalker(
    editorRef.value,
    NodeFilter.SHOW_TEXT,
    {
      acceptNode(node) {
        const parentElement = node.parentElement
        if (!node.nodeValue?.trim()) return NodeFilter.FILTER_REJECT
        if (parentElement?.closest('.canon-issue-highlight')) return NodeFilter.FILTER_REJECT
        return NodeFilter.FILTER_ACCEPT
      }
    }
  )

  const targetLower = target.toLowerCase()

  let currentNode: Node | null = walker.nextNode()
  while (currentNode) {
    const textNode = currentNode as Text
    const text = textNode.nodeValue ?? ''
    const index = text.toLowerCase().indexOf(targetLower)

    if (index >= 0) {
      const range = document.createRange()
      range.setStart(textNode, index)
      range.setEnd(textNode, index + target.length)

      const span = document.createElement('span')
      span.className = 'canon-issue-highlight'
      span.setAttribute('title', tooltip)

      try {
        range.surroundContents(span)
        return true
      } catch {
        return false
      }
    }

    currentNode = walker.nextNode()
  }

  return false
}

const applyCanonHighlights = () => {
  if (!editorRef.value) return

  removeCanonHighlightsFromDom()

  for (const issue of canonIssues.value) {
    const tooltip = `${issue.issue}${issue.expectedCanon ? ` | Canon: ${issue.expectedCanon}` : ''}`
    tryHighlightPassage(issue.passageText, tooltip)
  }
}

const loadStoryBibleMatchesForChapter = async () => {
  if (!chapter.value?.projectId) return

  loadingChapterEntities.value = true

  try {
    const rawEntities = await apiFetch<any[]>(`/projects/${chapter.value.projectId}/entities`, {
      method: 'GET'
    })

    const allEntities = (rawEntities ?? [])
      .map(normalizeStoryBibleEntity)
      .filter((item: StoryBibleEntity | null): item is StoryBibleEntity => item !== null)

    const text = editorText.value

    const matchingCharacters: ExtractedEntity[] = allEntities
      .filter(entity => entity.type.toLowerCase() === 'character')
      .filter(entity => chapterContainsEntity(text, entity.name))
      .map(entity => ({
        name: entity.name,
        sourceQuote: '',
        confidence: 1
      }))

    const matchingLocations: ExtractedEntity[] = allEntities
      .filter(entity => entity.type.toLowerCase() === 'location')
      .filter(entity => chapterContainsEntity(text, entity.name))
      .map(entity => ({
        name: entity.name,
        sourceQuote: '',
        confidence: 1
      }))

    detectedCharacters.value = dedupeEntitiesByName(matchingCharacters)
    detectedLocations.value = dedupeEntitiesByName(matchingLocations)
  } catch (error: any) {
    console.error('Failed to load chapter entities:', error)

    if (error?.status === 401 || error?.data?.status === 401) {
      await redirectToLogin()
      return
    }
  } finally {
    loadingChapterEntities.value = false
  }
}

const runCanonChecks = async () => {
  if (!chapterId.value || Number.isNaN(chapterId.value)) return

  checkingCanon.value = true
  canonIssues.value = []

  try {
    removeCanonHighlightsFromDom()
    syncEditorFromDom()

    const rawResult = await apiFetch<any>(`/chapters/${chapterId.value}/canon-checks`, {
      method: 'POST'
    })

    const result = normalizeCanonCheckResponse(rawResult)
    canonIssues.value = result.issues

    await nextTick()
    applyCanonHighlights()
  } catch (error: any) {
    console.error('Failed to run canon checks:', error)

    if (error?.status === 401 || error?.data?.status === 401) {
      await redirectToLogin()
      return
    }
  } finally {
    checkingCanon.value = false
  }
}

const saveChapterInternal = async (showSuccessMessage = true) => {
  const trimmedTitle = title.value.trim()

  if (!trimmedTitle) {
    errorMessage.value = 'Chapter title is required.'
    return false
  }

  saving.value = true

  try {
    removeCanonHighlightsFromDom()
    syncEditorFromDom()

    const cleanedHtml = stripCanonMarkupFromHtml(editorHtml.value)

    const updated = await apiFetch<Chapter>(`/chapters/${chapterId.value}`, {
      method: 'PUT',
      body: {
        title: trimmedTitle,
        content: cleanedHtml
      }
    })

    chapter.value = updated
    title.value = updated.title

    editorHtml.value =
      updated.content && looksLikeHtml(updated.content)
        ? updated.content
        : plainTextToHtml(updated.content ?? '')

    await writeEditorContentToDom()
    await loadStoryBibleMatchesForChapter()

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
    await loadStoryBibleMatchesForChapter()
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

  removeCanonHighlightsFromDom()
  syncEditorFromDom()

  if (!title.value.trim()) {
    errorMessage.value = 'Chapter title is required.'
    return
  }

  if (!editorText.value.trim()) {
    errorMessage.value = 'Chapter content is required before extraction.'
    return
  }

  extracting.value = true
  detectedCharacters.value = []
  detectedLocations.value = []
  canonIssues.value = []

  try {
    const saved = await saveChapterInternal(false)
    if (!saved) return

    const rawResult = await apiFetch<any>(
      `/chapters/${chapterId.value}/extract-entities`,
      {
        method: 'POST'
      }
    )

    const result = normalizeExtractionResponse(rawResult)

    detectedCharacters.value = dedupeEntitiesByName(result.characters)
    detectedLocations.value = dedupeEntitiesByName(result.locations)

    await loadStoryBibleMatchesForChapter()
    await runCanonChecks()

    successMessage.value =
      canonIssues.value.length > 0
        ? `AI extraction completed. ${canonIssues.value.length} canon issue(s) flagged.`
        : 'AI extraction completed successfully.'
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

const onEditorInput = () => {
  canonIssues.value = []
  removeCanonHighlightsFromDom()
  syncEditorFromDom()
}

watch(editorHtml, async () => {
  if (!loading.value && editorRef.value && editorRef.value.innerHTML !== stripCanonMarkupFromHtml(editorHtml.value)) {
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
            :disabled="loading || saving || extracting || checkingCanon"
          />
          <p>Open and edit your selected chapter</p>
        </div>
      </div>

      <div class="header-right">
        <span>{{ wordCount }} words</span>

        <button
          class="btn-secondary extract-btn"
          :disabled="loading || saving || extracting || checkingCanon"
          @click="extractEntities"
        >
          <v-icon icon="mdi-robot-outline" size="16" />
          {{ extracting ? 'Extracting...' : checkingCanon ? 'Checking Canon...' : 'Extract AI' }}
        </button>

        <button
          class="btn-primary save-btn"
          :disabled="loading || saving || extracting || checkingCanon"
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
              @input="onEditorInput"
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
            <div><span>Canon Warnings</span><strong>{{ canonIssues.length }}</strong></div>
            <div><span>Read Time</span><strong>{{ readTimeMinutes }} min</strong></div>
          </div>
        </div>

        <div class="panel side-card">
          <div class="side-card-header">
            <h3>AI Extraction</h3>
            <button
              class="mini-extract-btn"
              :disabled="loading || saving || extracting || checkingCanon"
              @click="extractEntities"
            >
              {{ extracting ? 'Running...' : checkingCanon ? 'Checking...' : 'Run' }}
            </button>
          </div>

          <p class="side-help">
            Save and analyze this chapter with your AI backend to detect story bible entities and flag canon contradictions directly in the editor.
          </p>
        </div>

        <div class="panel side-card">
          <h3>Detected Characters</h3>

          <div v-if="detectedCharacters.length" class="detected-list">
            <div
              v-for="(character, index) in detectedCharacters"
              :key="`char-${character.name}-${index}`"
              class="detected-pill purple"
            >
              <span class="pill-badge">{{ character.name.charAt(0) }}</span>
              <div class="detected-content">
                <span class="detected-name">{{ character.name }}</span>
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
              v-for="(location, index) in detectedLocations"
              :key="`loc-${location.name}-${index}`"
              class="detected-pill green"
            >
              <span class="pill-badge">
                <v-icon icon="mdi-map-marker" size="12" />
              </span>
              <div class="detected-content">
                <span class="detected-name">{{ location.name }}</span>
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

:deep(.canon-issue-highlight) {
  display: inline;
  text-decoration-line: underline;
  text-decoration-style: wavy;
  text-decoration-color: #ef4444;
  text-decoration-thickness: 2px;
  text-underline-offset: 3px;
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
  align-items: center;
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
  min-width: 0;
}

.detected-name {
  color: white;
  font-size: 0.9rem;
  font-weight: 600;
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