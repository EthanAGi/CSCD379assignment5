<script setup lang="ts">
import { useSidebar } from '~/composables/useSidebar'

const { open } = useSidebar()
</script>

<template>
  <div class="app-layout">
    <header class="mobile-header">
      <button class="burger-btn" type="button" @click="open">
        <v-icon icon="mdi-menu" size="24" />
      </button>
      <span class="mobile-brand">CanonGuard</span>
    </header>

    <AppSidebar />

    <main class="app-main">
      <slot />
    </main>
  </div>
</template>

<style scoped>
.app-layout {
  min-height: 100vh;
  display: flex;
  background: #121212;
}

.app-main {
  flex: 1;
  overflow: auto;
}

.mobile-header {
  display: none;
}

.burger-btn {
  width: 40px;
  height: 40px;
  border: 0;
  border-radius: 10px;
  background: rgba(255, 255, 255, 0.08);
  color: #d1d5db;
  display: inline-grid;
  place-items: center;
  cursor: pointer;
  transition: background 0.2s ease;
}

.burger-btn:hover {
  background: rgba(255, 255, 255, 0.14);
  color: white;
}

.mobile-brand {
  font-size: 1.25rem;
  font-weight: 700;
  color: white;
}

@media (max-width: 960px) {
  .mobile-header {
    display: flex;
    align-items: center;
    gap: 12px;
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    height: 56px;
    padding: 0 16px;
    background: #141416;
    border-bottom: 1px solid rgba(255, 255, 255, 0.06);
    z-index: 40;
  }

  .app-main {
    padding-top: 56px;
  }
}
</style>