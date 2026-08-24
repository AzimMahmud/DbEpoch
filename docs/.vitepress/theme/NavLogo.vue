<script setup lang="ts">
import { useData } from 'vitepress'
const { isDark } = useData()
</script>

<template>
  <img
    :src="isDark ? '/DbEpoch/logo-dark.png' : '/DbEpoch/logo-light.png'"
    alt="DbEpoch"
    class="nav-logo"
  />
</template>

<style scoped>
.nav-logo {
  height: 28px;
  width: auto;
  object-fit: contain;
}
</style>
