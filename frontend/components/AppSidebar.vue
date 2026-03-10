<script setup lang="ts">
const items = [
  { label: 'Dashboard', icon: 'mdi-view-dashboard-outline', to: '/app/dashboard' },
  { label: 'Story Bible', icon: 'mdi-book-open-page-variant-outline', to: '/app/story-bible' },
  { label: 'Search', icon: 'mdi-magnify', to: '/app/search' }
]
</script>

<template>
  <aside class="sidebar">
    <div>
      <div class="brand">CanonGuard</div>

      <nav class="nav">
        <NuxtLink
          v-for="item in items"
          :key="item.label"
          :to="item.to"
          class="nav-link"
          active-class="nav-link-active"
        >
          <v-icon :icon="item.icon" size="20" />
          <span>{{ item.label }}</span>
        </NuxtLink>
      </nav>
    </div>

    <div class="settings-link">
      <NuxtLink to="/app/settings" class="nav-link" active-class="nav-link-active">
        <v-icon icon="mdi-cog-outline" size="20" />
        <span>Settings</span>
      </NuxtLink>
    </div>
  </aside>
</template>

<style scoped>
.sidebar {
  width: 240px;
  min-height: 100vh;
  background: #141416;
  border-right: 1px solid rgba(255, 255, 255, 0.06);
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  padding: 24px 16px;
}

.brand {
  font-size: 2rem;
  font-weight: 800;
  letter-spacing: -0.03em;
  margin-bottom: 28px;
  color: #ffffff;
}

.nav {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.nav-link {
  display: flex;
  align-items: center;
  gap: 12px;
  text-decoration: none;
  color: #d2d6dc;
  padding: 14px 14px;
  border-radius: 14px;
  transition: 0.2s ease;
  font-size: 1rem;
  font-weight: 500;
}

.nav-link:hover {
  background: rgba(255, 255, 255, 0.05);
  color: white;
}

.nav-link-active {
  background: linear-gradient(90deg, #4f46e5, #6d28d9);
  color: white;
  box-shadow: 0 8px 24px rgba(99, 102, 241, 0.3);
}

.settings-link {
  padding-top: 20px;
}

@media (max-width: 960px) {
  .sidebar {
    display: none;
  }
}
</style>