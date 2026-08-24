import DefaultTheme from 'vitepress/theme'
import './custom.css'
import { watch } from 'vue'

export default {
  extends: DefaultTheme,
  enhanceApp({ app }) {
    // Use icon.png for both light and dark themes
    if (typeof window !== 'undefined') {
      const setLogo = () => {
        const img = document.querySelector('.VPNavBar .logo img') as HTMLImageElement
        if (!img) return
        img.src = '/DbEpoch/icon.png'
      }

      // Run on load
      setTimeout(setLogo, 100)

      // Watch for theme changes to ensure icon.png stays
      const observer = new MutationObserver(setLogo)
      observer.observe(document.documentElement, {
        attributes: true,
        attributeFilter: ['class']
      })
    }
  }
}
