<script setup>
import { ref, onMounted, onUnmounted } from 'vue'

const isDark = ref(false)

const updateTheme = () => {
  isDark.value = document.documentElement.classList.contains('dark')
}

let observer

onMounted(() => {
  updateTheme()
  observer = new MutationObserver(updateTheme)
  observer.observe(document.documentElement, {
    attributes: true,
    attributeFilter: ['class']
  })
})

onUnmounted(() => {
  observer?.disconnect()
})
</script>

<template>
  <div class="hero-logo-wrapper">
    <img
      class="hero-logo-light"
      :class="{ active: !isDark }"
      src="/logo.png"
      alt="dbsh"
    />
    <img
      class="hero-logo-dark"
      :class="{ active: isDark }"
      src="/logo-white.png"
      alt="dbsh"
    />
  </div>
</template>

<style scoped>
.hero-logo-wrapper {
  position: relative;
  display: inline-flex;
  justify-content: center;
  align-items: center;
  width: 320px;
  height: 120px;
}

.hero-logo-light,
.hero-logo-dark {
  position: absolute;
  width: 100%;
  height: auto;
  max-height: 100%;
  object-fit: contain;
  opacity: 0;
  transition: opacity 0.4s ease, filter 0.4s ease;
  filter: drop-shadow(0 2px 12px rgba(0, 0, 0, 0.08));
}

.hero-logo-light.active {
  opacity: 1;
}

.hero-logo-dark.active {
  opacity: 1;
  filter: drop-shadow(0 2px 16px rgba(255, 255, 255, 0.15));
}
</style>
