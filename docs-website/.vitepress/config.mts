import {defineConfig} from 'vitepress'

const umamiScript: HeadConfig = ['script', {
    defer: 'true',
    src: 'https://cloud.umami.is/script.js',
    'data-website-id': '', // TODO: Add your Umami website ID here
}]

const baseHeaders: HeadConfig[] = [];

const headers = process.env.GITHUB_PAGES === 'true' ?
    [...baseHeaders, umamiScript] :
    baseHeaders;

// https://vitepress.dev/reference/site-config
export default defineConfig({
    title: 'FluentMigrator',
    description: 'A .NET migration framework for database schema management',
    head: headers,

    themeConfig: {
        outline: 'deep',
        logo: '/logo.svg',
        externalLinkIcon: true,

        nav: [
            {text: 'Home', link: '/'},
            {text: 'Documentation', link: '/guide/introduction'},
            {text: 'Release notes', link: 'https://github.com/fluentmigrator/fluentmigrator/releases'},
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
                        {text: 'What is FluentMigrator?', link: '/guide/introduction'},
                        {text: 'Quick Start', link: '/guide/quick-start'},
                        {text: 'Installation', link: '/guide/installation'}
                    ]
                },
                {
                    text: 'Common Operations',
                    items: [
                        {text: 'Creating Tables', link: '/guide/operations/create-tables'},
                        {text: 'Altering Tables', link: '/guide/operations/alter-tables'},
                        {text: 'Columns', link: '/guide/operations/columns'},
                        {text: 'Data Operations', link: '/guide/operations/data'},
                        {text: 'Schema Operations', link: '/guide/operations/schema'},
                        {text: 'SQL Scripts', link: '/guide/operations/sql-scripts'}
                    ]
                },
                {
                    text: 'Basics',
                    items: [
                        {text: 'Managing Columns', link: '/guide/managing-columns'},
                        {text: 'Managing Indexes', link: '/guide/managing-indexes'},
                        {text: 'Working with Constraints', link: '/guide/working-with-constraints'},
                        {text: 'Working with Foreign Keys', link: '/guide/working-with-foreign-keys'},
                        {text: 'Raw SQL Helpers', link: '/guide/raw-sql-scripts'}
                    ]
                },
                {
                    text: 'Migration Runners',
                    items: [
                        {text: 'In-Process Runner', link: '/guide/runners/in-process'},
                        {text: 'Console Tool (Migrate.exe)', link: '/guide/runners/console'},
                        {text: 'dotnet-fm CLI', link: '/guide/runners/dotnet-fm'}
                    ]
                },
                {
                    text: 'Database Providers',
                    items: [
                        {text: 'SQL Server', link: '/guide/providers/sql-server'},
                        {text: 'PostgreSQL', link: '/guide/providers/postgresql'},
                        {text: 'MySQL', link: '/guide/providers/mysql'},
                        {text: 'SQLite', link: '/guide/providers/sqlite'},
                        {text: 'Oracle', link: '/guide/providers/oracle'},
                        {text: 'Other Providers', link: '/guide/providers/others'}
                    ]
                },
                {
                    text: 'Migration Types',
                    items: [
                        {text: 'Maintenance Migrations', link: '/guide/migration-types/maintenance'},
                        {text: 'Auto-Reversing Migrations', link: '/guide/migration-types/auto-reversing'},
                        {text: 'Tags', link: '/guide/migration-types/tags'},
                        {text: 'Profiles', link: '/guide/migration-types/profiles'}
                    ]
                },
                {
                    text: 'Advanced Topics',
                    items: [
                        {text: 'DBMS Extensions', link: '/guide/advanced/dbms-extensions'},
                        {text: 'Best Practices', link: '/guide/advanced/best-practices'},
                        {text: 'Migration Versioning', link: '/guide/advanced/versioning'},
                        {text: 'Conditional Logic', link: '/guide/advanced/conditional-logic'},
                        {text: 'Custom Extensions', link: '/guide/advanced/custom-extensions'},
                        {text: 'Advanced Logic on Connection', link: '/guide/advanced/connection-logic'}
                    ]
                },
                {
                    text: 'Help & Support',
                    items: [
                        {text: 'FAQ', link: '/guide/faq'}
                    ]
                }
            ]
        },

        socialLinks: [
            {icon: 'github', link: 'https://github.com/fluentmigrator/fluentmigrator'}
        ],

        footer: {
            message: 'Released under the Apache-2.0 License.',
            copyright: 'Copyright © 2008-present FluentMigrator Project'
        },

        search: {
            provider: 'local'
        },

        editLink: {
            pattern: 'https://github.com/fluentmigrator/fluentmigrator/edit/main/docs-website/:path',
            text: 'Edit this page on GitHub',
        },

        lastUpdated: {
            text: 'Updated at',
            formatOptions: {
                dateStyle: 'full',
                timeStyle: 'medium',
            },
        },
    }
})
