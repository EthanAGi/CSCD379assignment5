<script setup lang="ts">
import { onMounted } from 'vue'
import { navigateTo } from '#imports'
import { useAuth } from '~/composables/useAuth'

const auth = useAuth()

const features = [
  {
    icon: 'mdi-book-open-page-variant-outline',
    title: 'Automatic Story Bible',
    description: 'AI extracts characters, locations, themes, and facts from your manuscript automatically.'
  },
  {
    icon: 'mdi-magnify',
    title: 'Semantic Search',
    description: 'Search your manuscript by meaning, not just keywords.'
  },
  {
    icon: 'mdi-file-document-outline',
    title: 'Consistency Checking',
    description: 'Catch continuity errors before your readers do.'
  },
  {
    icon: 'mdi-sparkles',
    title: 'AI Writing Tools',
    description: 'Generate analysis, review, and other AI-powered writing support.'
  }
]

onMounted(async () => {
  auth.init()

  if (auth.token.value) {
    await navigateTo('/app/dashboard')
  }
})
</script>

<template>
  <div class="landing-page">
    <header class="landing-header">
      <div class="page-container header-inner">
        <div class="brand">
          <v-icon icon="mdi-book-open-page-variant-outline" size="32" color="#6366f1" />
          <span>CanonGuard</span>
        </div>

        <div class="header-actions">
          <NuxtLink to="/login" class="header-link">Login</NuxtLink>
          <NuxtLink to="/login" class="btn-primary signup-link">Sign Up</NuxtLink>
        </div>
      </div>
    </header>

    <section class="hero">
      <div class="page-container">
        <div class="hero-copy">
          <h1>CanonGuard – AI Story Bible for Writers</h1>
          <p>
            Automatically extract characters, locations, themes, and facts from your manuscript
            and keep your story consistent.
          </p>

          <div class="hero-actions">
            <NuxtLink to="/login" class="btn-primary hero-btn">Get Started Free</NuxtLink>
          </div>
        </div>
      </div>
    </section>

    <section class="features">
      <div class="page-container">
        <h2>Everything You Need to Keep Your Story Consistent</h2>

        <div class="feature-grid">
          <div v-for="feature in features" :key="feature.title" class="feature-card panel">
            <div class="feature-icon">
              <v-icon :icon="feature.icon" size="24" color="#818cf8" />
            </div>
            <h3>{{ feature.title }}</h3>
            <p>{{ feature.description }}</p>
          </div>
        </div>
      </div>
    </section>

    <section class="benefits">
      <div class="page-container">
        <div class="benefit-panel">
          <h2>Write With Confidence</h2>

          <div class="benefit-list">
            <div class="benefit-row">
              <div class="benefit-check">
                <v-icon icon="mdi-check" size="14" color="white" />
              </div>
              <p>Keep your worldbuilding organized and searchable.</p>
            </div>

            <div class="benefit-row">
              <div class="benefit-check">
                <v-icon icon="mdi-check" size="14" color="white" />
              </div>
              <p>Surface important details across long-form writing.</p>
            </div>

            <div class="benefit-row">
              <div class="benefit-check">
                <v-icon icon="mdi-check" size="14" color="white" />
              </div>
              <p>Use AI tools without losing track of continuity.</p>
            </div>
          </div>

          <div class="benefit-action">
            <NuxtLink to="/login" class="btn-primary hero-btn">Start Writing Smarter Today</NuxtLink>
          </div>
        </div>
      </div>
    </section>

    <footer class="landing-footer">
      <div class="page-container footer-inner">
        <div class="brand small">
          <v-icon icon="mdi-book-open-page-variant-outline" size="24" color="#6366f1" />
          <span>CanonGuard</span>
        </div>
        <p>© 2026 CanonGuard. All rights reserved.</p>
      </div>
    </footer>
  </div>
</template>

<style scoped>
.landing-page {
  min-height: 100vh;
  background: linear-gradient(to bottom, #0a0a0a, #1a1a2e);
}

.landing-header {
  border-bottom: 1px solid #1f2937;
  background: rgba(0, 0, 0, 0.3);
  backdrop-filter: blur(10px);
}

.header-inner,
.footer-inner {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 0;
}

.brand {
  display: flex;
  align-items: center;
  gap: 8px;
}

.brand span {
  font-size: 2rem;
  font-weight: 600;
  color: white;
}

.brand.small span {
  font-size: 1.125rem;
}

.header-actions {
  display: flex;
  align-items: center;
  gap: 16px;
}

.header-link {
  color: #d1d5db;
}

.header-link:hover {
  color: white;
}

.signup-link {
  display: inline-flex;
  align-items: center;
}

.hero {
  padding: 96px 0;
  text-align: center;
}

.hero-copy {
  max-width: 900px;
  margin: 0 auto;
}

.hero-copy h1 {
  margin: 0 0 24px;
  font-size: clamp(2.75rem, 6vw, 4.5rem);
  line-height: 1.1;
  font-weight: 800;
  color: white;
}

.hero-copy p {
  margin: 0 auto 48px;
  max-width: 760px;
  font-size: 1.25rem;
  line-height: 1.7;
  color: #d1d5db;
}

.hero-actions {
  display: flex;
  justify-content: center;
  gap: 16px;
  flex-wrap: wrap;
}

.hero-btn {
  padding: 16px 28px;
  font-size: 1.05rem;
}

.features,
.benefits {
  padding: 96px 0;
}

.features h2,
.benefit-panel h2 {
  margin: 0 0 48px;
  font-size: 2.5rem;
  font-weight: 800;
  color: white;
  text-align: center;
}

.feature-grid {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 24px;
}

.feature-card {
  padding: 24px;
  transition: 0.2s ease;
}

.feature-card:hover {
  border-color: rgba(79, 70, 229, 0.5);
  box-shadow: 0 20px 40px rgba(79, 70, 229, 0.08);
}

.feature-icon {
  width: 48px;
  height: 48px;
  border-radius: 12px;
  background: rgba(79, 70, 229, 0.2);
  display: grid;
  place-items: center;
  margin-bottom: 16px;
}

.feature-card h3 {
  margin: 0 0 8px;
  color: white;
  font-size: 1.25rem;
  font-weight: 700;
}

.feature-card p {
  margin: 0;
  color: #9ca3af;
  line-height: 1.7;
}

.benefit-panel {
  padding: 48px;
  border-radius: 24px;
  border: 1px solid rgba(99, 102, 241, 0.28);
  background: linear-gradient(135deg, rgba(49, 46, 129, 0.3), rgba(88, 28, 135, 0.2));
}

.benefit-list {
  max-width: 720px;
  margin: 0 auto;
}

.benefit-row {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 16px;
}

.benefit-check {
  width: 24px;
  height: 24px;
  border-radius: 999px;
  background: #4f46e5;
  display: grid;
  place-items: center;
  flex-shrink: 0;
}

.benefit-row p {
  margin: 0;
  color: #d1d5db;
  font-size: 1.125rem;
}

.benefit-action {
  margin-top: 32px;
  text-align: center;
}

.landing-footer {
  margin-top: 96px;
  border-top: 1px solid #1f2937;
  background: rgba(0, 0, 0, 0.3);
  backdrop-filter: blur(10px);
}

.landing-footer p {
  color: #9ca3af;
  font-size: 0.875rem;
}

@media (max-width: 1100px) {
  .feature-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 760px) {
  .header-inner,
  .footer-inner {
    flex-direction: column;
  }

  .header-actions {
    margin-top: 16px;
  }

  .feature-grid {
    grid-template-columns: 1fr;
  }
}
</style>