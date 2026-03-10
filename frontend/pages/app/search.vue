<script setup lang="ts">
definePageMeta({ layout: 'app' })

const searchQuery = ref('')
const hasSearched = ref(false)

const submitSearch = () => {
  hasSearched.value = true
}
</script>

<template>
  <div class="search-page">
    <div class="search-container">
      <div class="search-header">
        <h1>Semantic Search</h1>
        <p>Search your manuscript by meaning, not just keywords</p>
      </div>

      <form class="search-form" @submit.prevent="submitSearch">
        <div class="search-box">
          <v-icon icon="mdi-magnify" size="24" color="#9ca3af" class="search-icon" />
          <input
            v-model="searchQuery"
            class="search-input"
            placeholder="Search your manuscript by meaning..."
          />
          <button type="submit" class="btn-primary search-btn">
            Search
          </button>
        </div>
      </form>

      <div v-if="!hasSearched" class="search-prestate">
        <div class="panel suggestion-panel">
          <h2>Try searching for:</h2>
          <div class="suggestion-grid">
            <button class="suggestion-card">
              <v-icon icon="mdi-magnify" size="16" color="#9ca3af" />
              <span>Scenes with a specific tone</span>
            </button>

            <button class="suggestion-card">
              <v-icon icon="mdi-magnify" size="16" color="#9ca3af" />
              <span>Character appearances or mentions</span>
            </button>

            <button class="suggestion-card">
              <v-icon icon="mdi-magnify" size="16" color="#9ca3af" />
              <span>References to locations or artifacts</span>
            </button>

            <button class="suggestion-card">
              <v-icon icon="mdi-magnify" size="16" color="#9ca3af" />
              <span>Dialogue around a theme or event</span>
            </button>
          </div>
        </div>

        <div class="panel recent-panel">
          <h2>Recent Searches</h2>
          <div class="empty-recent">
            <p>No recent searches yet.</p>
          </div>
        </div>
      </div>

      <div v-else class="search-results">
        <div class="results-head">
          <h2>Results</h2>
          <button class="clear-btn" @click="hasSearched = false">Clear search</button>
        </div>

        <div class="panel results-empty">
          <div class="empty-icon">
            <v-icon icon="mdi-file-document-outline" size="28" color="#818cf8" />
          </div>
          <h3>No results to display</h3>
          <p>Search results will appear here once semantic search is connected.</p>
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

.search-form {
  margin-bottom: 32px;
}

.search-box {
  position: relative;
  display: flex;
  align-items: center;
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

.search-prestate {
  display: grid;
  gap: 24px;
}

.suggestion-panel,
.recent-panel,
.results-empty {
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

.suggestion-card {
  display: flex;
  align-items: center;
  gap: 10px;
  width: 100%;
  text-align: left;
  border: 1px solid #2e2e2e;
  background: #1e1e1e;
  color: #d1d5db;
  border-radius: 12px;
  padding: 16px;
  cursor: pointer;
}

.suggestion-card:hover {
  background: #252525;
  border-color: rgba(79, 70, 229, 0.35);
  color: white;
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
  align-items: center;
  justify-content: space-between;
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
}

@media (max-width: 760px) {
  .search-page {
    padding: 20px;
  }

  .suggestion-grid {
    grid-template-columns: 1fr;
  }

  .results-head {
    flex-direction: column;
    align-items: flex-start;
    gap: 12px;
  }
}
</style>