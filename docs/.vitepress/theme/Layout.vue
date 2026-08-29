<script setup>
import DefaultTheme from 'vitepress/theme'
import HeroLogo from './components/HeroLogo.vue'

const { Layout } = DefaultTheme
</script>

<template>
  <Layout>
    <template #home-hero-image>
      <HeroLogo />
    </template>
  </Layout>
</template>
