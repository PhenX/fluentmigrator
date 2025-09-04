import { defineConfig } from 'vitepress'

export default defineConfig({
  title: 'FluentMigrator',
  description: 'A .NET migration framework for database schema management',
  
  // Ignore dead links for now - we'll create the missing pages
  ignoreDeadLinks: true,
  
  themeConfig: {
    logo: '/logo.svg',
    
    nav: [
      { text: 'Home', link: '/' },
      { text: 'Quick Start', link: '/guide/quick-start' },
      { text: 'API Reference', link: '/api/' },
      { text: 'Examples', link: '/examples/' },
      { 
        text: 'GitHub',
        link: 'https://github.com/fluentmigrator/fluentmigrator'
      }
    ],

    sidebar: {
      '/guide/': [
        {
          text: 'Introduction',
          items: [
            { text: 'What is FluentMigrator?', link: '/guide/introduction' },
            { text: 'Quick Start', link: '/guide/quick-start' },
            { text: 'Installation', link: '/guide/installation' }
          ]
        },
        {
          text: 'Common Operations',
          items: [
            { text: 'Creating Tables', link: '/guide/operations/create-tables' },
            { text: 'Altering Tables', link: '/guide/operations/alter-tables' },
            { text: 'Managing Columns', link: '/guide/operations/columns' },
            { text: 'Working with Indexes', link: '/guide/operations/indexes' },
            { text: 'Foreign Keys', link: '/guide/operations/foreign-keys' },
            { text: 'Data Operations', link: '/guide/operations/data' },
            { text: 'Schema Operations', link: '/guide/operations/schema' }
          ]
        },
        {
          text: 'Database Providers',
          items: [
            { text: 'SQL Server', link: '/guide/providers/sql-server' },
            { text: 'PostgreSQL', link: '/guide/providers/postgresql' },
            { text: 'MySQL', link: '/guide/providers/mysql' },
            { text: 'SQLite', link: '/guide/providers/sqlite' },
            { text: 'Oracle', link: '/guide/providers/oracle' },
            { text: 'Other Providers', link: '/guide/providers/others' }
          ]
        },
        {
          text: 'Advanced Topics',
          items: [
            { text: 'DBMS Extensions', link: '/guide/advanced/dbms-extensions' },
            { text: 'Edge Cases', link: '/guide/advanced/edge-cases' },
            { text: 'Best Practices', link: '/guide/advanced/best-practices' },
            { text: 'Migration Versioning', link: '/guide/advanced/versioning' },
            { text: 'Conditional Logic', link: '/guide/advanced/conditional-logic' }
          ]
        }
      ],
      '/api/': [
        {
          text: 'Core API',
          items: [
            { text: 'Migration Base', link: '/api/migration-base' },
            { text: 'Builders', link: '/api/builders' },
            { text: 'Expressions', link: '/api/expressions' }
          ]
        }
      ],
      '/examples/': [
        {
          text: 'Examples',
          items: [
            { text: 'Basic Examples', link: '/examples/basic' },
            { text: 'Advanced Scenarios', link: '/examples/advanced' },
            { text: 'Real-world Use Cases', link: '/examples/real-world' }
          ]
        }
      ]
    },

    socialLinks: [
      { icon: 'github', link: 'https://github.com/fluentmigrator/fluentmigrator' }
    ],

    footer: {
      message: 'Released under the Apache-2.0 License.',
      copyright: 'Copyright © 2007-2024 FluentMigrator Project'
    },

    search: {
      provider: 'local'
    }
  }
})