import DefaultTheme from 'vitepress/theme'
import './custom.css'
import Layout from './Layout.vue'
import HeroLogo from './components/HeroLogo.vue'

export default {
  extends: DefaultTheme,
  Layout,
  enhanceApp({ app }) {
    app.component('HeroLogo', HeroLogo)
  }
}
