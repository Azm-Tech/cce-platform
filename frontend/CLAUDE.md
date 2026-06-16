# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 📚 Full documentation lives in `../../cce-platform-docs/`

| Doc | When to read |
|---|---|
| `cce-platform-docs/technical/FRONTEND_GUIDE.md` | **Read before coding** — architecture, auth, i18n, feature anatomy |
| `cce-platform-docs/technical/CONVENTIONS.md` | Code patterns to match |
| `cce-platform-docs/technical/API_INTEGRATION.md` | HTTP layer, endpoints, envelope rules |
| `cce-platform-docs/agile/PROGRESS.md` | ⭐ Story status tracker — **update it after completing any story** |
| `cce-platform-docs/agile/stories/` | Acceptance criteria per user story |
| `cce-platform-docs/reference/users.md` | Test credentials |

## Commands (run from this directory, always pnpm)

```bash
pnpm nx serve web-portal                 # public app :4200
pnpm nx serve admin-cms                  # admin app :4201
pnpm nx build <app>
pnpm nx test <app> --watch=false         # add --testFile=<path> for a single spec
pnpm nx lint <app>
pnpm nx run-many -t build,lint,test --projects=web-portal,admin-cms   # full smoke
```

## Critical gotchas (cause real bugs)

1. **Envelope auto-unwrap (admin-cms):** any response from a URL containing `/api/admin/` is unwrapped by `apiEnvelopeInterceptor` — services type the **inner** data shape and must NOT access `.data`. Web-portal endpoints are NOT unwrapped.
2. **i18n:** translation keys must exist in BOTH `libs/i18n/src/lib/i18n/ar.json` and `en.json`. Default language is Arabic — always verify RTL (logical CSS properties, icon flips).
3. **Auth:** access token in a signal (memory), refresh token in `localStorage['cce_rt']`. `tokenInterceptor` skips `/api/auth/*`. Interceptor order differs per app — see FRONTEND_GUIDE §4.3.
4. **Style:** standalone components + signals + `OnPush` + new control flow (`@if`/`@for`) only. Material form fields are globally `outline`. Selector prefix `cce-`.
5. **Services return `Result<T>`** (`{ok:true,value}|{ok:false,error}`) — components never try/catch; map `error.kind` to `('errors.'+kind) | transloco`.
6. **Definition of done includes updating** `cce-platform-docs/agile/PROGRESS.md` (status + dated log entry).
7. **NEVER hardcode a color.** No hex (`#fff`), named (`white`), `rgb()`, or `hsl()` literals in any `.scss`/`.ts`/`.html` — not even "just this once". All colors MUST be design tokens. The single source of truth is `libs/ui-kit/src/lib/styles/_palette.scss`; it emits CSS custom properties consumed everywhere as `var(--…)`. Use `var(--neutrals--50)`, `var(--color-brand)`, etc. For opacity use the auto-derived channels: `rgba(var(--color-brand-rgb), 0.5)`. Need a color the tokens don't have? Add it to `_palette.scss` first, then reference the var — never inline the literal. This applies to every AI agent. (Only `rgba(0,0,0,…)`/`rgba(255,255,255,…)` pure black/white scrims are tolerated, and even those prefer a token.) **Enforced** in `.scss` by stylelint (`.stylelintrc.json`): run `pnpm stylelint` or `pnpm nx run-many -t stylelint`. Inline `styles:` in `.ts` are not linted — keep them token-only by hand.

## CSS & styling rules (read before touching any `.scss`)

**Stack:** Angular Material (M2 theme) + token-driven SCSS (`cce-*` BEM). **No Tailwind** — it was removed; do NOT reintroduce utility classes (`flex`, `p-4`, `bg-primary`). No third styling paradigm.

**Tokens & theme**
- All design values flow from `libs/ui-kit/src/lib/styles/_palette.scss` → `_css-vars.scss` (emits CSS vars + auto-derived `--x-rgb` channels) → `_dga-theme.scss` (Material M2 theme). To rebrand, edit `_palette.scss` only.
- Theme is **M2**. Do NOT add an M3 prebuilt theme (e.g. `prebuilt-themes/*.css`) — it conflicts and bloats. `--mat-sys-*` tokens are shimmed to the palette in `_css-vars.scss`; use those, don't reintroduce a prebuilt.

**Performance (these caused real wins/regressions — keep them)**
- **Honor reduced-motion:** a global `prefers-reduced-motion` guard lives in `cce-theme`. New continuously-running (`infinite`) animations must be acceptable when neutralised; pause off-screen ones.
- **Blur is expensive:** avoid `backdrop-filter`/`filter: blur()` on large or scrolling surfaces; keep radius ≤ ~10px (shared glass = `--cce-fancy-glass-blur`). A radial-gradient is already soft — don't stack a big blur on it.
- **Never** `background-attachment: fixed` (full repaint per scroll frame).
- **Never** `transition: all` — list explicit compositor-friendly props (`transform`, `opacity`, `box-shadow`, `border-color`, …); prefer animating `transform`/`opacity`.
- Use `content-visibility: auto` + `contain-intrinsic-size` on repeated below-the-fold blocks (list cards already do).
- Feature-only CSS (e.g. Quill) must NOT be global — ship it as a lazy bundle (see `project.json` `styles` + `cce-rich-text-editor` on-demand load), not in `styles.scss`.

**Architecture truths (don't waste effort fighting these)**
- Component styles are **automatically route-split** (lazy chunks) — do NOT try to "move page CSS out of the global bundle"; it's already isolated. The global `styles.css` is Material theme + `_fancy`/`_admin-polish` only.
- SCSS `@mixin`/`@extend` does **NOT** reduce shipped bytes (component styles are isolated; mixins re-emit per component). Dedup is a **maintainability** win, not a size one. The only real size lever for a heavy page is **splitting it into smaller sub-components**, not mangling its CSS.
- Keep a component `.scss` under the **20KB** budget (`anyComponentStyle`); if a page is legitimately larger (rich landing page), that's a *warning*, not a failure — split into section components rather than obfuscate.

**Workflow — before calling CSS work done**
- Run `pnpm nx run-many -t stylelint` (color rule) **and** `-t build` (size budgets: `styles` bundle + `anyComponentStyle`). Both must pass.
- Verify **RTL** (logical properties: `margin-inline`, `inset-inline-*`; flip directional icons).
- Keep selector prefix `cce-`; avoid new `::ng-deep` and `!important` (existing debt — don't add to it).
