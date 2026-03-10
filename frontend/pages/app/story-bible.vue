<script setup lang="ts">
definePageMeta({ layout: 'app' })

type TabKey = 'characters' | 'locations' | 'themes' | 'arcs'
const activeTab = ref<TabKey>('characters')

const tabs = [
  { id: 'characters', label: 'Characters', icon: 'mdi-account-group-outline' },
  { id: 'locations', label: 'Locations', icon: 'mdi-map-marker-outline' },
  { id: 'themes', label: 'Themes', icon: 'mdi-lightbulb-outline' },
  { id: 'arcs', label: 'Story Arcs', icon: 'mdi-trending-up' }
]
</script>

<template>
  <div class="story-page">
    <div class="page-container">
      <div class="story-header">
        <div>
          <h1>Story Bible</h1>
          <p>Your complete story knowledge base</p>
        </div>

        <button class="btn-primary add-btn">
          <v-icon icon="mdi-plus" size="18" />
          Add Entry
        </button>
      </div>

      <div class="search-wrap">
        <div class="search-bar panel">
          <v-icon icon="mdi-magnify" size="20" color="#9ca3af" />
          <input class="search-input" placeholder="Search story bible..." />
        </div>
      </div>

      <div class="tab-bar">
        <button
          v-for="tab in tabs"
          :key="tab.id"
          class="tab-btn"
          :class="{ active: activeTab === tab.id }"
          @click="activeTab = tab.id as TabKey"
        >
          <v-icon :icon="tab.icon" size="16" />
          {{ tab.label }}
        </button>
      </div>

      <div class="content-area">
        <div v-if="activeTab === 'characters'" class="empty-card panel">
          <div class="empty-icon purple">
            <v-icon icon="mdi-account-group-outline" size="28" color="#c084fc" />
          </div>
          <h3>No characters yet</h3>
          <p>Extracted or manually added characters will appear here.</p>
        </div>

        <div v-else-if="activeTab === 'locations'" class="empty-card panel">
          <div class="empty-icon blue">
            <v-icon icon="mdi-map-marker-outline" size="28" color="#60a5fa" />
          </div>
          <h3>No locations yet</h3>
          <p>Extracted or manually added locations will appear here.</p>
        </div>

        <div v-else-if="activeTab === 'themes'" class="empty-card panel">
          <div class="empty-icon pink">
            <v-icon icon="mdi-lightbulb-outline" size="28" color="#f472b6" />
          </div>
          <h3>No themes yet</h3>
          <p>Detected themes will appear here after analysis.</p>
        </div>

        <div v-else class="empty-card panel">
          <div class="empty-icon green">
            <v-icon icon="mdi-trending-up" size="28" color="#4ade80" />
          </div>
          <h3>No story arcs yet</h3>
          <p>Story arcs will appear here after extraction or manual entry.</p>
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
}

@media (max-width: 760px) {
  .story-header {
    flex-direction: column;
    align-items: flex-start;
    gap: 16px;
  }
}
</style>