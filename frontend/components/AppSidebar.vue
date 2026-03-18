<script setup lang="ts">
import { watch } from 'vue'
import { useRouter, useRoute } from '#imports'
import { useAuth } from '~/composables/useAuth'
import { useSidebar } from '~/composables/useSidebar'

const router = useRouter()
const route = useRoute()
const auth = useAuth()
const { isOpen, close } = useSidebar()

const items = [
  { label: 'Dashboard', icon: 'mdi-view-dashboard-outline', to: '/app/dashboard' },
  { label: 'Story Bible', icon: 'mdi-book-open-page-variant-outline', to: '/app/story-bible' },
  { label: 'Search', icon: 'mdi-magnify', to: '/app/search' }
]

const signOut = async () => {
  close()
  auth.clear()
  await router.push('/login')
}

watch(() => route.fullPath, () => {
  close()
})
</script>

<template>
  <Transition name="backdrop-fade">
    <div v-if="isOpen" class="sidebar-backdrop" @click="close" />
  </Transition>

  <aside class="sidebar" :class="{ 'sidebar-open': isOpen }">
    <div class="sidebar-inner">
      <div>
        <div class="brand-row">
          <div class="brand">CanonGuard</div>
          <button class="close-btn" type="button" @click="close">
            <v-icon icon="mdi-close" size="20" />
          </button>
        </div>

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

      <div class="bottom-actions">
        <button type="button" class="nav-link signout-btn" @click="signOut">
          <v-icon icon="mdi-logout" size="20" />
          <span>Sign Out</span>
        </button>
      </div>
    </div>
  </aside>
</template>

<style scoped>
.sidebar {
  position: sticky;
  top: 0;
  align-self: flex-start;
  width: 240px;
  height: 100vh;
  flex: 0 0 240px;
  background: #141416;
  border-right: 1px solid rgba(255, 255, 255, 0.06);
  overflow: hidden;
}

.sidebar-inner {
  height: 100%;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  padding: 24px 16px;
}

.brand-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 28px;
}

.brand {
  font-size: 2rem;
  font-weight: 800;
  letter-spacing: -0.03em;
  color: #ffffff;
  line-height: 1.1;
}

.close-btn {
  display: none;
  width: 36px;
  height: 36px;
  border: 0;
  border-radius: 10px;
  background: rgba(255, 255, 255, 0.08);
  color: #d1d5db;
  place-items: center;
  cursor: pointer;
}

.close-btn:hover {
  background: rgba(255, 255, 255, 0.14);
  color: white;
}

.nav {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.nav-link {
  width: 100%;
  display: flex;
  align-items: center;
  gap: 12px;
  text-decoration: none;
  color: #d2d6dc;
  padding: 14px 14px;
  border-radius: 14px;
  transition: background 0.2s ease, color 0.2s ease, box-shadow 0.2s ease, transform 0.2s ease;
  font-size: 1rem;
  font-weight: 500;
  box-sizing: border-box;
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

.bottom-actions {
  padding-top: 20px;
}

.signout-btn {
  border: 0;
  background: rgba(239, 68, 68, 0.1);
  color: #fca5a5;
  cursor: pointer;
  text-align: left;
}

.signout-btn:hover {
  background: rgba(239, 68, 68, 0.16);
  color: #fecaca;
}

.sidebar-backdrop {
  display: none;
}

/* Backdrop fade transition */
.backdrop-fade-enter-active,
.backdrop-fade-leave-active {
  transition: opacity 0.3s ease;
}

.backdrop-fade-enter-from,
.backdrop-fade-leave-to {
  opacity: 0;
}

@media (max-width: 960px) {
  .sidebar {
    position: fixed;
    top: 0;
    left: 0;
    z-index: 51;
    transform: translateX(-100%);
    transition: transform 0.3s ease;
  }

  .sidebar.sidebar-open {
    transform: translateX(0);
  }

  .close-btn {
    display: inline-grid;
  }

  .sidebar-backdrop {
    display: block;
    position: fixed;
    inset: 0;
    background: rgba(0, 0, 0, 0.6);
    z-index: 50;
  }
}
</style>