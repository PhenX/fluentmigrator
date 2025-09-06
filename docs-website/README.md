# FluentMigrator Documentation Website

This directory contains the VitePress documentation website for FluentMigrator.

## Overview

The documentation provides comprehensive coverage of FluentMigrator including:

- **Introduction**: What is FluentMigrator and why use it
- **Quick Start Guide**: Get up and running in minutes
- **Installation**: Package installation and setup
- **Common Operations**: Creating tables, indexes, constraints, and data operations
- **Database Providers**: Provider-specific features and configuration
  - SQL Server
  - PostgreSQL 
  - MySQL
  - SQLite
  - Oracle
  - And more...
- **Advanced Topics**: DBMS extensions, edge cases, best practices
- **API Reference**: Complete API documentation
- **Examples**: Basic to advanced real-world scenarios

## Development

### Prerequisites

- Node.js 18+ 
- npm

### Setup

```bash
cd docs-website
npm install
```

### Development Server

```bash
npm run docs:dev
```

The site will be available at http://localhost:5173

### Build

```bash
npm run docs:build
```

The built site will be in `.vitepress/dist/`

### Preview Built Site

```bash
npm run docs:preview
```

## Structure

```
docs-website/
├── .vitepress/
│   └── config.js          # VitePress configuration
├── guide/                 # Main documentation
│   ├── operations/        # Common operations guides
│   ├── providers/         # Database provider guides
│   └── advanced/          # Advanced topics
├── api/                   # API reference
├── examples/              # Code examples
├── public/                # Static assets (images, etc.)
└── index.md              # Homepage
```

## Features

- **Modern Design**: Clean, responsive design with dark mode support
- **Search**: Built-in search functionality
- **Navigation**: Intuitive sidebar navigation
- **Code Highlighting**: Syntax highlighting for C# and SQL
- **Mobile Friendly**: Responsive design that works on all devices
- **Fast**: Built with VitePress for optimal performance

## Contributing

To contribute to the documentation:

1. Make your changes to the markdown files
2. Test locally with `npm run docs:dev`
3. Build to verify: `npm run docs:build`
4. Submit your changes

## Deployment

The site can be deployed to various platforms:

### GitHub Pages

Add to `.github/workflows/deploy-docs.yml`:

```yaml
name: Deploy Documentation

on:
  push:
    branches: [main]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '18'
          
      - name: Install dependencies
        run: |
          cd docs-website
          npm ci
          
      - name: Build documentation
        run: |
          cd docs-website
          npm run docs:build
          
      - name: Deploy to GitHub Pages
        uses: peaceiris/actions-gh-pages@v3
        with:
          github_token: ${{ secrets.GITHUB_TOKEN }}
          publish_dir: docs-website/.vitepress/dist
```

### Netlify

1. Connect your repository to Netlify
2. Set build command: `cd docs-website && npm run docs:build`
3. Set publish directory: `docs-website/.vitepress/dist`

### Vercel

1. Connect your repository to Vercel
2. Set root directory: `docs-website`
3. Build command: `npm run docs:build`
4. Output directory: `.vitepress/dist`

## License

This documentation is released under the same Apache-2.0 License as FluentMigrator.