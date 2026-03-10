<script setup lang="ts">
import { ref } from 'vue'
import { definePageMeta, navigateTo } from '#imports'
import { useAuth } from '~/composables/useAuth'

definePageMeta({ layout: 'app' })

const auth = useAuth()

const showLogoutDialog = ref(false)
const loggingOut = ref(false)

const openLogoutDialog = () => {
  showLogoutDialog.value = true
}

const closeLogoutDialog = () => {
  if (loggingOut.value) return
  showLogoutDialog.value = false
}

const handleLogout = async () => {
  if (loggingOut.value) return

  loggingOut.value = true

  try {
    auth.setToken(null)
    showLogoutDialog.value = false
    await navigateTo('/login')
  } finally {
    loggingOut.value = false
  }
}
</script>

<template>
  <div class="settings-page">
    <div class="page-container">
      <div class="settings-header">
        <h1>Settings</h1>
        <p>Manage your account and application preferences.</p>
      </div>

      <div class="settings-grid">
        <section class="panel settings-card">
          <div class="card-header">
            <div class="icon-wrap">
              <v-icon icon="mdi-account-circle-outline" size="24" color="#818cf8" />
            </div>
            <div>
              <h2>Account</h2>
              <p>Manage your sign-in session and account access.</p>
            </div>
          </div>

          <div class="card-body">
            <div class="setting-row">
              <div>
                <h3>Logout</h3>
                <p>Sign out of CanonGuard on this device.</p>
              </div>

              <button class="btn-danger" type="button" @click="openLogoutDialog">
                <v-icon icon="mdi-logout" size="18" />
                Logout
              </button>
            </div>
          </div>
        </section>

        <section class="panel settings-card">
          <div class="card-header">
            <div class="icon-wrap">
              <v-icon icon="mdi-cog-outline" size="24" color="#60a5fa" />
            </div>
            <div>
              <h2>Application</h2>
              <p>General app settings and preferences.</p>
            </div>
          </div>

          <div class="card-body">
            <div class="placeholder-box">
              <p>More settings can go here later, like theme, editor preferences, and notifications.</p>
            </div>
          </div>
        </section>
      </div>
    </div>

    <v-dialog v-model="showLogoutDialog" max-width="460" persistent>
      <v-card class="logout-dialog">
        <v-card-title class="dialog-title">Log out?</v-card-title>

        <v-card-text class="dialog-body">
          <p class="dialog-text">
            You are about to sign out of CanonGuard on this device.
          </p>
        </v-card-text>

        <v-card-actions class="dialog-actions">
          <button
            type="button"
            class="btn-ghost"
            :disabled="loggingOut"
            @click="closeLogoutDialog"
          >
            Cancel
          </button>

          <button
            type="button"
            class="btn-danger"
            :disabled="loggingOut"
            @click="handleLogout"
          >
            {{ loggingOut ? 'Logging out...' : 'Logout' }}
          </button>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<style scoped>
.settings-page {
  min-height: 100vh;
  padding: 32px 0;
  background: #121212;
}

.settings-header {
  margin-bottom: 32px;
}

.settings-header h1 {
  margin: 0 0 8px;
  font-size: 3rem;
  font-weight: 800;
  color: white;
}

.settings-header p {
  margin: 0;
  color: #9ca3af;
}

.settings-grid {
  display: grid;
  gap: 24px;
}

.settings-card {
  border-radius: 18px;
  overflow: hidden;
}

.card-header {
  padding: 22px 24px;
  border-bottom: 1px solid #2e2e2e;
  display: flex;
  align-items: center;
  gap: 16px;
}

.icon-wrap {
  width: 48px;
  height: 48px;
  border-radius: 14px;
  background: rgba(79, 70, 229, 0.16);
  display: grid;
  place-items: center;
}

.card-header h2 {
  margin: 0 0 4px;
  color: white;
  font-size: 1.25rem;
  font-weight: 700;
}

.card-header p {
  margin: 0;
  color: #9ca3af;
}

.card-body {
  padding: 24px;
}

.setting-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
}

.setting-row h3 {
  margin: 0 0 6px;
  color: white;
  font-size: 1rem;
  font-weight: 700;
}

.setting-row p {
  margin: 0;
  color: #9ca3af;
}

.placeholder-box {
  border: 1px dashed rgba(255, 255, 255, 0.12);
  border-radius: 14px;
  padding: 20px;
  color: #9ca3af;
}

.logout-dialog {
  background: #1e1e1e !important;
  color: white;
  border: 1px solid #2e2e2e;
  border-radius: 18px;
}

.dialog-title {
  padding: 22px 24px 10px;
  font-size: 1.25rem;
  font-weight: 700;
}

.dialog-body {
  padding: 0 24px 8px;
}

.dialog-text {
  margin: 0;
  color: #d1d5db;
  line-height: 1.6;
}

.dialog-actions {
  padding: 18px 24px 24px;
  display: flex;
  justify-content: flex-end;
  gap: 12px;
}

.btn-danger {
  border: 0;
  border-radius: 12px;
  padding: 12px 18px;
  background: linear-gradient(90deg, #dc2626, #b91c1c);
  color: white;
  display: inline-flex;
  align-items: center;
  gap: 8px;
  font-weight: 600;
  cursor: pointer;
  transition: 0.2s ease;
}

.btn-danger:hover {
  filter: brightness(1.08);
}

.btn-danger:disabled {
  opacity: 0.7;
  cursor: not-allowed;
}

@media (max-width: 760px) {
  .settings-header h1 {
    font-size: 2.3rem;
  }

  .setting-row {
    flex-direction: column;
    align-items: flex-start;
  }
}
</style>