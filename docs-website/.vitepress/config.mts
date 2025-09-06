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
            {text: 'Documentation', link: '/intro/quick-start'},
            {text: 'Release notes', link: 'https://github.com/fluentmigrator/fluentmigrator/releases'},
            {
                text: 'GitHub',
                link: 'https://github.com/fluentmigrator/fluentmigrator'
            }
        ],

        sidebar: {
            '/': [
                {
                    text: 'Introduction',
                    items: [
                        {text: 'Quick Start', link: '/intro/quick-start'},
                        {text: 'Installation', link: '/intro/installation'},
                        {text: 'FAQ', link: '/intro/faq'}
                    ]
                },
                {
                    text: 'Common Operations',
                    items: [
                        {text: 'Creating Tables', link: '/operations/create-tables'},
                        {text: 'Altering Tables', link: '/operations/alter-tables'},
                        {text: 'Columns', link: '/operations/columns'},
                        {text: 'Data Operations', link: '/operations/data'},
                        {text: 'Schema Operations', link: '/operations/schema'},
                        {text: 'SQL Scripts', link: '/operations/sql-scripts'}
                    ]
                },
                {
                    text: 'Basics',
                    items: [
                        {text: 'Managing Columns', link: '/basics/columns'},
                        {text: 'Managing Indexes', link: '/basics/indexes'},
                        {text: 'Working with Constraints', link: '/basics/constraints'},
                        {text: 'Working with Foreign Keys', link: '/basics/foreign-keys'},
                        {text: 'Raw SQL Helpers', link: '/basics/raw-sql-scripts'}
                    ]
                },
                {
                    text: 'Migration Runners',
                    items: [
                        {text: 'In-Process Runner', link: '/runners/in-process'},
                        {text: 'Console Tool (Migrate.exe)', link: '/runners/console'},
                        {text: 'dotnet-fm CLI', link: '/runners/dotnet-fm'}
                    ]
                },
                {
                    text: 'Database Providers',
                    items: [
                        {text: 'SQL Server', link: '/providers/sql-server'},
                        {text: 'PostgreSQL', link: '/providers/postgresql'},
                        {text: 'MySQL', link: '/providers/mysql'},
                        {text: 'SQLite', link: '/providers/sqlite'},
                        {text: 'Oracle', link: '/providers/oracle'},
                        {text: 'Other Providers', link: '/providers/others'}
                    ]
                },
                {
                    text: 'Migration Types',
                    items: [
                        {text: 'Maintenance Migrations', link: '/migration-types/maintenance'},
                        {text: 'Auto-Reversing Migrations', link: '/migration-types/auto-reversing'},
                        {text: 'Tags', link: '/migration-types/tags'},
                        {text: 'Profiles', link: '/migration-types/profiles'}
                    ]
                },
                {
                    text: 'Advanced Topics',
                    items: [
                        {text: 'DBMS Extensions', link: '/advanced/dbms-extensions'},
                        {text: 'Best Practices', link: '/advanced/best-practices'},
                        {text: 'Migration Versioning', link: '/advanced/versioning'},
                        {text: 'Conditional Logic', link: '/advanced/conditional-logic'},
                        {text: 'Custom Extensions', link: '/advanced/custom-extensions'},
                        {text: 'Advanced Logic on Connection', link: '/advanced/connection-logic'}
                    ]
                },
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
