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
      { text: 'Documentation', link: '/guide/introduction' },
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
          text: 'Basics',
          items: [
            { text: 'Managing Columns', link: '/guide/managing-columns' },
            { text: 'Managing Indexes', link: '/guide/managing-indexes' },
            { text: 'Working with Constraints', link: '/guide/working-with-constraints' },
            { text: 'Working with Foreign Keys', link: '/guide/working-with-foreign-keys' },
            { text: 'Raw SQL Helper', link: '/guide/raw-sql' },
            { text: 'Profiles', link: '/guide/profiles' }
          ]
        },
        {
          text: 'Migration Runners',
          items: [
            { text: 'In-Process Runner', link: '/guide/runners/in-process' },
            { text: 'Console Tool (Migrate.exe)', link: '/guide/runners/console' },
            { text: 'dotnet-fm CLI', link: '/guide/runners/dotnet-fm' }
          ]
        },
        {
          text: 'Common Operations',
          items: [
            { text: 'Creating Tables', link: '/guide/operations/create-tables' },
            { text: 'Altering Tables', link: '/guide/operations/alter-tables' },
            { text: 'Managing Columns', link: '/guide/operations/columns' },
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
            { text: 'Conditional Logic', link: '/guide/advanced/conditional-logic' },
            { text: 'Custom Extensions', link: '/guide/advanced/custom-extensions' }
          ]
        },
        {
          text: 'Help & Support',
          items: [
            { text: 'FAQ', link: '/guide/faq' }
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