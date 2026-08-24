import { defineConfig } from 'vitepress'

export default defineConfig({
  title: 'DbEpoch',
  description: 'Database migrations that ship. A Flyway-style migration tool for PostgreSQL, SQL Server, MySQL, and SQLite.',

  base: '/DbEpoch/',

  head: [
    ['link', { rel: 'icon', type: 'image/png', href: '/DbEpoch/logo.png' }]
  ],

  themeConfig: {
    logo: '/logo.png',
    siteTitle: false,

    nav: [
      { text: 'Guide', link: '/guide/installation' },
      { text: 'Commands', link: '/commands/new' },
      { text: 'Reference', link: '/reference/global-options' },
      {
        text: 'v2.1.0',
        items: [
          { text: 'Changelog', link: 'https://github.com/AzimMahmud/DbEpoch/blob/main/CHANGELOG.md' },
          { text: 'GitHub', link: 'https://github.com/AzimMahmud/DbEpoch' }
        ]
      }
    ],

    sidebar: {
      '/guide/': [
        {
          text: 'Getting Started',
          items: [
            { text: 'Installation', link: '/guide/installation' },
            { text: 'Quick Start', link: '/guide/quick-start' },
            { text: 'Configuration', link: '/guide/configuration' },
            { text: 'Multi-Database Setup', link: '/guide/multi-database' }
          ]
        }
      ],

      '/commands/': [
        {
          text: 'Setup',
          items: [
            { text: 'new', link: '/commands/new' },
            { text: 'create', link: '/commands/create' },
            { text: 'init', link: '/commands/init' }
          ]
        },
        {
          text: 'Validation',
          items: [
            { text: 'validate', link: '/commands/validate' }
          ]
        },
        {
          text: 'Inspection',
          items: [
            { text: 'plan', link: '/commands/plan' },
            { text: 'status', link: '/commands/status' },
            { text: 'history', link: '/commands/history' },
            { text: 'info', link: '/commands/info' }
          ]
        },
        {
          text: 'Execution',
          items: [
            { text: 'migrate', link: '/commands/migrate' },
            { text: 'rollback', link: '/commands/rollback' },
            { text: 'repair', link: '/commands/repair' }
          ]
        }
      ],

      '/reference/': [
        {
          text: 'Reference',
          items: [
            { text: 'Global Options', link: '/reference/global-options' },
            { text: 'Script Conventions', link: '/reference/script-conventions' },
            { text: 'Tracking Tables', link: '/reference/tracking-tables' },
            { text: 'Architecture', link: '/reference/architecture' },
            { text: 'CI/CD Integration', link: '/reference/ci-cd' }
          ]
        }
      ]
    },

    socialLinks: [
      { icon: 'github', link: 'https://github.com/AzimMahmud/DbEpoch' }
    ],

    editLink: {
      pattern: 'https://github.com/AzimMahmud/DbEpoch/edit/main/docs/:path'
    },

    search: {
      provider: 'local'
    },

    footer: {
      message: 'Released under the MIT License.',
      copyright: 'Copyright 2024-present Azim Mahmud'
    }
  }
})
