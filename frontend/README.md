# CCE Frontend

Nx 20 monorepo with two Angular 18 apps — `web-portal` (public, port 4200) and `admin-cms` (admin, port 4201).

## Prerequisites

- Node 20.x or 22.x LTS (`nvm use` reads `.nvmrc`)
- pnpm 9.x (via `corepack enable && corepack prepare pnpm@9 --activate`)

## Bootstrap

```bash
cd frontend
pnpm install
```

## Common commands

| Command                                                               | What it does                               |
| --------------------------------------------------------------------- | ------------------------------------------ |
| `pnpm nx serve web-portal`                                            | Dev server on http://localhost:4200        |
| `pnpm nx serve admin-cms`                                             | Dev server on http://localhost:4201        |
| `pnpm nx build web-portal`                                            | Production build to `dist/apps/web-portal` |
| `pnpm nx test web-portal --watch=false`                               | Run Jest tests once                        |
| `pnpm nx lint web-portal`                                             | ESLint + Prettier check                    |
| `pnpm nx e2e web-portal-e2e`                                          | Playwright E2E (Phase 14 fills these)      |
| `pnpm nx run-many -t build,lint,test --projects=web-portal,admin-cms` | Full smoke                                 |
| `pnpm nx graph`                                                       | Visualize project dependency graph         |

## Stack

- **Framework:** Angular 18.2 (esbuild bundler, SCSS, no SSR)
- **UI:** Angular Material 18 + Bootstrap 5 grid (utilities only — no Bootstrap components)
- **i18n:** `@ngx-translate/core` + `@ngx-translate/http-loader`
- **Auth:** `angular-auth-oidc-client` (Keycloak code-flow + PKCE)
- **Test:** Jest (unit) + Playwright (E2E with axe-core a11y, Phase 14)
- **Lint:** ESLint with `@angular-eslint/template` a11y rules + Prettier

## What's added in later phases

- Phase 10: Shared libs (`ui-kit`, `i18n`, `auth`, `api-client`, `contracts`)
- Phase 11: web-portal shell (header, locale switcher, router, /health page)
- Phase 12: admin-cms shell + OIDC login flow
- Phase 13: OpenAPI client generation pipeline
- Phase 14: Playwright + axe-core E2E coverage
