import DefaultTheme from 'vitepress/theme'
import './custom.css'

export default {
  extends: DefaultTheme,
  enhanceApp({ app }) {
    // Swap nav logo based on theme
    if (typeof window !== 'undefined') {
      const swapLogo = () => {
        const img = document.querySelector('.VPNavBar .logo img') as HTMLImageElement
        if (!img) return
        img.src = '/icon.png'
      }

      // Run on load
      setTimeout(swapLogo, 100)

      // Watch for theme changes
      const observer = new MutationObserver(swapLogo)
      observer.observe(document.documentElement, {
        attributes: true,
        attributeFilter: ['class']
      })
    }
  }
}
