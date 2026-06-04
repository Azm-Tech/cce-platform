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
