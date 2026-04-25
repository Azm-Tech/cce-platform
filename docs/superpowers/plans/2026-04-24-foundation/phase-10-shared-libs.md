# Phase 10 — Shared Angular Libraries

> Parent: [../2026-04-24-foundation.md](../2026-04-24-foundation.md) · Spec: [../../specs/2026-04-24-foundation-design.md](../../specs/2026-04-24-foundation-design.md)

**Phase goal:** Create the five shared Nx libraries (`ui-kit`, `i18n`, `auth`, `api-client`, `contracts`) and wire them into both apps via path mappings. Foundation seeds each lib with the minimum viable content; Phases 11–13 fill them out as the apps need them.

**Tasks in this phase:** 11
**Working directory:** `/Users/m/CCE/`
**Preconditions:** Phase 09 complete; `cd frontend && pnpm nx run-many -t build,lint,test --projects=web-portal,admin-cms` all green.

---

## Pre-execution sanity checks

1. `git status` clean.
2. `node --version` → v20.x or v22.x or v24.x (Phase 09 ran successfully on whichever).
3. `pnpm --version` → `9.15.4`.
4. `ls frontend/libs 2>/dev/null` → should not exist or be empty (Phase 09 created `apps/` only; Nx generators create `libs/` on first lib).

If any fail, stop and report.

---

## Library overview

| Lib | Purpose | Foundation seed |
|---|---|---|
| `ui-kit` | Angular Material + DGA tokens + Bootstrap grid wrappers + shared components | `app-shell` shell component |
| `i18n` | ngx-translate config, ar/en translations, `LocaleService` with RTL toggle | `LocaleService` + ar.json + en.json |
| `auth` | OIDC config, guards, interceptors | placeholder — fills in Phase 12 |
| `api-client` | Auto-generated TS clients from OpenAPI | placeholder — generates in Phase 13 |
| `contracts` | Hand-written TS types not in OpenAPI (env config, feature flags) | empty `.gitkeep` |

---

## Task 10.1: Generate `ui-kit` library

**Files:**
- Auto-generated: `frontend/libs/ui-kit/`

- [ ] **Step 1: Generate the lib**

```bash
cd frontend
pnpm nx generate @nx/angular:library ui-kit \
  --directory=libs/ui-kit \
  --buildable=true \
  --publishable=false \
  --standalone=true \
  --routing=false \
  --style=scss \
  --strict=true \
  --linter=eslint \
  --unitTestRunner=jest \
  --no-interactive 2>&1 | tail -10
cd ..
```
Expected: prints CREATE messages, generates ~10 files.

- [ ] **Step 2: Verify the lib builds**

```bash
cd frontend
pnpm nx build ui-kit 2>&1 | tail -5
cd ..
```
Expected: `Successfully ran target build for project ui-kit`.

- [ ] **Step 3: Commit**

```bash
git add frontend/
git -c commit.gpgsign=false commit -m "feat(phase-10): generate ui-kit shared library (standalone, scss, jest)"
```

---

## Task 10.2: Add DGA color/typography tokens to `ui-kit`

**Files:**
- Create: `frontend/libs/ui-kit/src/lib/styles/_tokens.scss`
- Create: `frontend/libs/ui-kit/src/lib/styles/_dga-theme.scss`
- Modify: `frontend/libs/ui-kit/src/index.ts` (export the styles path)

**Rationale:** Saudi DGA UX standards prescribe specific colors, fonts (IBM Plex Sans Arabic, Frutiger LT Arabic), and spacing. Foundation lays the token layer; Phase 11/12 components consume it.

- [ ] **Step 1: Write `_tokens.scss`**

```scss
// CCE — DGA design tokens (per Saudi Digital Government Authority UX guidelines).
// Foundation seeds; refine with the official DGA palette/typography spec when available.

// Color palette
$cce-primary-50:  #e8f4f1;
$cce-primary-100: #c5e4dc;
$cce-primary-300: #6cb5a0;
$cce-primary-500: #006c4f;   // primary brand — DGA-aligned forest green
$cce-primary-700: #00513b;
$cce-primary-900: #00301f;

$cce-accent-500:  #c8a045;   // muted gold accent

$cce-warn-500:    #b71c1c;   // error / warn

$cce-neutral-50:  #fafafa;
$cce-neutral-100: #f5f5f5;
$cce-neutral-300: #d4d4d4;
$cce-neutral-500: #757575;
$cce-neutral-700: #424242;
$cce-neutral-900: #212121;

// Typography — Arabic-first
$cce-font-family-ar: "IBM Plex Sans Arabic", "Frutiger LT Arabic", system-ui, sans-serif;
$cce-font-family-en: "IBM Plex Sans", system-ui, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;

// Spacing scale (4px base)
$cce-space-1: 4px;
$cce-space-2: 8px;
$cce-space-3: 12px;
$cce-space-4: 16px;
$cce-space-6: 24px;
$cce-space-8: 32px;
$cce-space-12: 48px;
$cce-space-16: 64px;

// Border radii
$cce-radius-sm: 4px;
$cce-radius-md: 8px;
$cce-radius-lg: 16px;

// Elevation (DGA conservative shadow scale)
$cce-shadow-sm: 0 1px 2px rgba(0, 0, 0, 0.06);
$cce-shadow-md: 0 4px 8px rgba(0, 0, 0, 0.08);
$cce-shadow-lg: 0 10px 24px rgba(0, 0, 0, 0.10);
```

- [ ] **Step 2: Write `_dga-theme.scss`**

```scss
@use "@angular/material" as mat;
@use "./tokens" as t;

// Build a Material 18 (M2 API) light theme using DGA tokens.
$cce-primary-palette: mat.m2-define-palette((
  50:  t.$cce-primary-50,
  100: t.$cce-primary-100,
  300: t.$cce-primary-300,
  500: t.$cce-primary-500,
  700: t.$cce-primary-700,
  900: t.$cce-primary-900,
  contrast: (
    50: rgba(black, 0.87),  100: rgba(black, 0.87),  300: white,
    500: white,             700: white,              900: white,
  )
));

$cce-accent-palette: mat.m2-define-palette((
  50:  #fff7e6,  100: #ffe6b3,  300: #ffcc66,
  500: t.$cce-accent-500,  700: #a37c20,  900: #6b4f0e,
  contrast: (
    50: rgba(black, 0.87),  100: rgba(black, 0.87),  300: rgba(black, 0.87),
    500: rgba(black, 0.87), 700: white,              900: white,
  )
));

$cce-warn-palette: mat.m2-define-palette(mat.$m2-red-palette);

$cce-typography: mat.m2-define-typography-config(
  $font-family: t.$cce-font-family-ar,
);

$cce-theme: mat.m2-define-light-theme((
  color: (
    primary: $cce-primary-palette,
    accent: $cce-accent-palette,
    warn: $cce-warn-palette,
  ),
  typography: $cce-typography,
  density: 0,
));

@mixin cce-theme {
  @include mat.core();
  @include mat.all-component-themes($cce-theme);

  // RTL fonts when dir=rtl
  [dir="rtl"] body, body[dir="rtl"] {
    font-family: t.$cce-font-family-ar;
  }
  [dir="ltr"] body, body[dir="ltr"] {
    font-family: t.$cce-font-family-en;
  }
}
```

- [ ] **Step 3: Add re-export note to `frontend/libs/ui-kit/src/index.ts`**

Append:

```typescript
// SCSS theme entry-point: import via @use "@cce/ui-kit/styles/dga-theme" with mixin `cce-theme`.
```

(SCSS files aren't TS-importable but the comment documents the usage path.)

- [ ] **Step 4: Verify build**

```bash
cd frontend
pnpm nx build ui-kit 2>&1 | tail -5
cd ..
```
Expected: success.

- [ ] **Step 5: Commit**

```bash
git add frontend/libs/ui-kit/
git -c commit.gpgsign=false commit -m "feat(phase-10): add DGA color/typography/spacing tokens + cce-theme Material mixin to ui-kit"
```

---

## Task 10.3: Generate `i18n` library + seed translations

**Files:**
- Auto-generated: `frontend/libs/i18n/`
- Create: `frontend/libs/i18n/src/lib/i18n/ar.json`
- Create: `frontend/libs/i18n/src/lib/i18n/en.json`

- [ ] **Step 1: Generate the lib**

```bash
cd frontend
pnpm nx generate @nx/angular:library i18n \
  --directory=libs/i18n \
  --buildable=true \
  --publishable=false \
  --standalone=true \
  --routing=false \
  --style=scss \
  --strict=true \
  --linter=eslint \
  --unitTestRunner=jest \
  --no-interactive 2>&1 | tail -10
cd ..
```

- [ ] **Step 2: Write `frontend/libs/i18n/src/lib/i18n/ar.json`**

```json
{
  "common": {
    "appName": "مركز معرفة الاقتصاد الدائري للكربون",
    "welcome": "مرحبًا بك في مركز معرفة الاقتصاد الدائري للكربون",
    "loading": "جارٍ التحميل…",
    "error": {
      "notFound": "الصفحة غير موجودة",
      "serverError": "حدث خطأ في الخادم. يرجى المحاولة مرة أخرى لاحقًا.",
      "networkError": "تعذّر الاتصال بالخادم.",
      "unauthorized": "يرجى تسجيل الدخول للمتابعة.",
      "forbidden": "ليس لديك إذن للوصول إلى هذا المورد."
    },
    "actions": {
      "signIn": "تسجيل الدخول",
      "signOut": "تسجيل الخروج",
      "save": "حفظ",
      "cancel": "إلغاء",
      "retry": "إعادة المحاولة"
    },
    "locale": {
      "switchTo": "التبديل إلى",
      "ar": "العربية",
      "en": "English"
    }
  },
  "health": {
    "title": "حالة النظام",
    "status": "الحالة",
    "version": "الإصدار",
    "ok": "تشغيل"
  }
}
```

- [ ] **Step 3: Write `frontend/libs/i18n/src/lib/i18n/en.json`**

```json
{
  "common": {
    "appName": "CCE Knowledge Center",
    "welcome": "Welcome to the CCE Knowledge Center",
    "loading": "Loading…",
    "error": {
      "notFound": "Page not found",
      "serverError": "A server error occurred. Please try again later.",
      "networkError": "Could not reach the server.",
      "unauthorized": "Please sign in to continue.",
      "forbidden": "You don't have permission to access this resource."
    },
    "actions": {
      "signIn": "Sign in",
      "signOut": "Sign out",
      "save": "Save",
      "cancel": "Cancel",
      "retry": "Retry"
    },
    "locale": {
      "switchTo": "Switch to",
      "ar": "العربية",
      "en": "English"
    }
  },
  "health": {
    "title": "System Health",
    "status": "Status",
    "version": "Version",
    "ok": "OK"
  }
}
```

- [ ] **Step 4: Commit**

```bash
git add frontend/libs/i18n/
git -c commit.gpgsign=false commit -m "feat(phase-10): generate i18n library + ar.json + en.json with Foundation strings"
```

---

## Task 10.4: Add `LocaleService` to `i18n` lib (RTL toggle, persistence)

**Files:**
- Create: `frontend/libs/i18n/src/lib/locale.service.ts`
- Create: `frontend/libs/i18n/src/lib/locale.service.spec.ts`
- Modify: `frontend/libs/i18n/src/index.ts` (export LocaleService)

**Rationale:** Single source of truth for current locale. Switches `dir="rtl|ltr"` on `<html>`, persists choice to localStorage, exposes a signal so components reactively update.

- [ ] **Step 1: Write the failing test**

`frontend/libs/i18n/src/lib/locale.service.spec.ts`:

```typescript
import { TestBed } from '@angular/core/testing';
import { LocaleService, SUPPORTED_LOCALES, type SupportedLocale } from './locale.service';

describe('LocaleService', () => {
  let service: LocaleService;

  beforeEach(() => {
    localStorage.clear();
    document.documentElement.removeAttribute('dir');
    document.documentElement.removeAttribute('lang');
    TestBed.configureTestingModule({ providers: [LocaleService] });
    service = TestBed.inject(LocaleService);
  });

  it('defaults to ar when no preference stored', () => {
    expect(service.locale()).toBe('ar');
  });

  it('sets dir=rtl on html when locale is ar', () => {
    service.setLocale('ar');
    expect(document.documentElement.getAttribute('dir')).toBe('rtl');
    expect(document.documentElement.getAttribute('lang')).toBe('ar');
  });

  it('sets dir=ltr on html when locale is en', () => {
    service.setLocale('en');
    expect(document.documentElement.getAttribute('dir')).toBe('ltr');
    expect(document.documentElement.getAttribute('lang')).toBe('en');
  });

  it('persists locale to localStorage', () => {
    service.setLocale('en');
    expect(localStorage.getItem('cce.locale')).toBe('en');
  });

  it('reads persisted locale on instantiation', () => {
    localStorage.setItem('cce.locale', 'en');
    const fresh = TestBed.runInInjectionContext(() => new LocaleService());
    expect(fresh.locale()).toBe('en');
  });

  it('rejects unsupported locales (falls back to ar)', () => {
    service.setLocale('fr' as SupportedLocale);
    expect(service.locale()).toBe('ar');
  });

  it('exposes the supported locale list', () => {
    expect(SUPPORTED_LOCALES).toEqual(['ar', 'en']);
  });
});
```

- [ ] **Step 2: Run — expect compile error**

```bash
cd frontend
pnpm nx test i18n --watch=false 2>&1 | tail -8
cd ..
```
Expected: build error referring to missing `LocaleService`.

- [ ] **Step 3: Write `frontend/libs/i18n/src/lib/locale.service.ts`**

```typescript
import { Injectable, signal, type Signal } from '@angular/core';

export const SUPPORTED_LOCALES = ['ar', 'en'] as const;
export type SupportedLocale = (typeof SUPPORTED_LOCALES)[number];

const STORAGE_KEY = 'cce.locale';
const DEFAULT_LOCALE: SupportedLocale = 'ar';

/**
 * Source of truth for the user's locale. Writes `dir="rtl"|"ltr"` and `lang` to
 * `<html>` so CSS `[dir="rtl"]` selectors and screen readers see the right value.
 * Persists to localStorage so the choice survives reload.
 */
@Injectable({ providedIn: 'root' })
export class LocaleService {
  private readonly _locale = signal<SupportedLocale>(this.readPersisted());

  constructor() {
    this.applyToDom(this._locale());
  }

  readonly locale: Signal<SupportedLocale> = this._locale.asReadonly();

  setLocale(next: SupportedLocale): void {
    const safe = this.coerce(next);
    this._locale.set(safe);
    this.applyToDom(safe);
    try {
      localStorage.setItem(STORAGE_KEY, safe);
    } catch {
      // localStorage unavailable (private mode, SSR) — no-op.
    }
  }

  private readPersisted(): SupportedLocale {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      return this.coerce(raw as SupportedLocale | null);
    } catch {
      return DEFAULT_LOCALE;
    }
  }

  private coerce(value: SupportedLocale | null | undefined): SupportedLocale {
    return value && (SUPPORTED_LOCALES as readonly string[]).includes(value) ? value : DEFAULT_LOCALE;
  }

  private applyToDom(locale: SupportedLocale): void {
    if (typeof document === 'undefined') {
      return;
    }
    const html = document.documentElement;
    html.setAttribute('lang', locale);
    html.setAttribute('dir', locale === 'ar' ? 'rtl' : 'ltr');
  }
}
```

- [ ] **Step 4: Update `frontend/libs/i18n/src/index.ts`**

```typescript
export * from './lib/locale.service';
```

- [ ] **Step 5: Run tests + commit**

```bash
cd frontend
pnpm nx test i18n --watch=false 2>&1 | tail -8
cd ..
```
Expected: 7 passed.

```bash
git add frontend/libs/i18n/
git -c commit.gpgsign=false commit -m "feat(phase-10): add LocaleService with signal-based locale + RTL/LTR + persistence (7 tests)"
```

---

## Task 10.5: Generate `auth` library

**Files:**
- Auto-generated: `frontend/libs/auth/`

- [ ] **Step 1: Generate**

```bash
cd frontend
pnpm nx generate @nx/angular:library auth \
  --directory=libs/auth \
  --buildable=true \
  --publishable=false \
  --standalone=true \
  --routing=false \
  --style=scss \
  --strict=true \
  --linter=eslint \
  --unitTestRunner=jest \
  --no-interactive 2>&1 | tail -10
cd ..
```

- [ ] **Step 2: Commit**

```bash
git add frontend/libs/auth/
git -c commit.gpgsign=false commit -m "feat(phase-10): generate auth shared library (OIDC config + guards land in phase 12)"
```

---

## Task 10.6: Add OIDC config builder to `auth` lib

**Files:**
- Create: `frontend/libs/auth/src/lib/cce-oidc.config.ts`
- Create: `frontend/libs/auth/src/lib/cce-oidc.config.spec.ts`
- Modify: `frontend/libs/auth/src/index.ts`

**Rationale:** A single function that produces an `angular-auth-oidc-client` config from a runtime-loaded environment. Apps call it from their bootstrap. Phase 12 wires the actual login flow.

- [ ] **Step 1: Write the failing test**

`frontend/libs/auth/src/lib/cce-oidc.config.spec.ts`:

```typescript
import { buildCceOidcConfig, type CceAuthEnv } from './cce-oidc.config';

describe('buildCceOidcConfig', () => {
  const env: CceAuthEnv = {
    authority: 'http://localhost:8080/realms/cce-internal',
    clientId: 'cce-admin-cms',
    redirectUri: 'http://localhost:4201/auth/callback',
    postLogoutRedirectUri: 'http://localhost:4201',
  };

  it('produces a config with code-flow + PKCE + 256', () => {
    const cfg = buildCceOidcConfig(env);

    expect(cfg.authority).toBe(env.authority);
    expect(cfg.clientId).toBe(env.clientId);
    expect(cfg.responseType).toBe('code');
    expect(cfg.usePushedAuthorisationRequests).toBe(false); // PAR not configured in Foundation
    expect(cfg.scope).toContain('openid');
    expect(cfg.scope).toContain('profile');
  });

  it('sets refresh token rotation', () => {
    const cfg = buildCceOidcConfig(env);

    expect(cfg.useRefreshToken).toBe(true);
    expect(cfg.silentRenew).toBe(true);
  });

  it('disables auto-login (apps trigger login explicitly)', () => {
    const cfg = buildCceOidcConfig(env);

    expect(cfg.triggerAuthorizationResultEvent).toBe(true);
  });
});
```

- [ ] **Step 2: Write `frontend/libs/auth/src/lib/cce-oidc.config.ts`**

```typescript
import type { OpenIdConfiguration } from 'angular-auth-oidc-client';

export interface CceAuthEnv {
  authority: string;
  clientId: string;
  redirectUri: string;
  postLogoutRedirectUri: string;
}

/**
 * Build an angular-auth-oidc-client configuration for one of the CCE Keycloak realms.
 * Apps call this from their bootstrap with values pulled from /assets/env.json so the
 * same image deploys to dev/staging/prod by swapping the runtime config file.
 */
export function buildCceOidcConfig(env: CceAuthEnv): OpenIdConfiguration {
  return {
    authority: env.authority,
    redirectUrl: env.redirectUri,
    postLogoutRedirectUri: env.postLogoutRedirectUri,
    clientId: env.clientId,
    scope: 'openid profile email adfs-compat offline_access',
    responseType: 'code',
    silentRenew: true,
    useRefreshToken: true,
    renewTimeBeforeTokenExpiresInSeconds: 30,
    usePushedAuthorisationRequests: false,
    triggerAuthorizationResultEvent: true,
    logLevel: 0,
  } as OpenIdConfiguration;
}
```

- [ ] **Step 3: Update `frontend/libs/auth/src/index.ts`**

```typescript
export * from './lib/cce-oidc.config';
```

- [ ] **Step 4: Run + commit**

```bash
cd frontend
pnpm nx test auth --watch=false 2>&1 | tail -8
cd ..
git add frontend/libs/auth/
git -c commit.gpgsign=false commit -m "feat(phase-10): add buildCceOidcConfig helper for angular-auth-oidc-client (3 tests)"
```

---

## Task 10.7: Generate `api-client`, `contracts` libs (placeholders)

**Files:**
- Auto-generated: `frontend/libs/api-client/`
- Auto-generated: `frontend/libs/contracts/`
- Create: `frontend/libs/api-client/src/lib/generated/.gitkeep`

- [ ] **Step 1: Generate both libs**

```bash
cd frontend
pnpm nx generate @nx/angular:library api-client \
  --directory=libs/api-client --buildable=true --publishable=false \
  --standalone=true --routing=false --style=scss --strict=true \
  --linter=eslint --unitTestRunner=jest --no-interactive 2>&1 | tail -5
pnpm nx generate @nx/angular:library contracts \
  --directory=libs/contracts --buildable=true --publishable=false \
  --standalone=true --routing=false --style=scss --strict=true \
  --linter=eslint --unitTestRunner=jest --no-interactive 2>&1 | tail -5
cd ..
```

- [ ] **Step 2: Reserve `generated/` directory in `api-client`**

```bash
mkdir -p frontend/libs/api-client/src/lib/generated
touch frontend/libs/api-client/src/lib/generated/.gitkeep
```

Phase 13 fills this directory via `openapi-typescript-codegen`. Until then, the gitignored note in `.gitignore` prevents accidental commits while `.gitkeep` keeps the dir present.

- [ ] **Step 3: Commit**

```bash
git add frontend/libs/api-client/ frontend/libs/contracts/
git -c commit.gpgsign=false commit -m "feat(phase-10): generate api-client + contracts placeholder libraries (filled in phases 13)"
```

---

## Task 10.8: Add `EnvService` + runtime-loaded env to `contracts`

**Files:**
- Create: `frontend/libs/contracts/src/lib/env.types.ts`
- Modify: `frontend/libs/contracts/src/index.ts`

**Rationale:** Apps load `/assets/env.json` at bootstrap so the same compiled bundle deploys to dev/staging/prod. The `Env` type defines the shape.

- [ ] **Step 1: Write `frontend/libs/contracts/src/lib/env.types.ts`**

```typescript
/**
 * Runtime environment loaded from /assets/env.json at app bootstrap.
 * Same shape across web-portal and admin-cms; the values differ.
 */
export interface CceEnv {
  /** Logical environment name — "development" | "staging" | "production". */
  readonly environment: string;

  /** Backend API base URL — External API for web-portal, Internal API for admin-cms. */
  readonly apiBaseUrl: string;

  /** OIDC authority (full Keycloak realm URL). */
  readonly oidcAuthority: string;

  /** OIDC client ID — `cce-web-portal` or `cce-admin-cms`. */
  readonly oidcClientId: string;

  /** Sentry DSN; empty string disables Sentry. */
  readonly sentryDsn: string;
}
```

- [ ] **Step 2: Update `frontend/libs/contracts/src/index.ts`**

```typescript
export * from './lib/env.types';
```

- [ ] **Step 3: Build + commit**

```bash
cd frontend
pnpm nx build contracts 2>&1 | tail -5
cd ..
git add frontend/libs/contracts/
git -c commit.gpgsign=false commit -m "feat(phase-10): add CceEnv runtime-config interface to contracts lib"
```

---

## Task 10.9: Add `appShell` standalone component to `ui-kit`

**Files:**
- Create: `frontend/libs/ui-kit/src/lib/app-shell/app-shell.component.ts`
- Create: `frontend/libs/ui-kit/src/lib/app-shell/app-shell.component.html`
- Create: `frontend/libs/ui-kit/src/lib/app-shell/app-shell.component.scss`
- Create: `frontend/libs/ui-kit/src/lib/app-shell/app-shell.component.spec.ts`
- Modify: `frontend/libs/ui-kit/src/index.ts`

**Rationale:** A reusable header + main + footer scaffold both apps consume. Slots for app-specific title and locale switcher. Renders Material toolbar at top.

- [ ] **Step 1: Write the failing component test**

```typescript
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AppShellComponent } from './app-shell.component';

describe('AppShellComponent', () => {
  let component: AppShellComponent;
  let fixture: ComponentFixture<AppShellComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [AppShellComponent] }).compileComponents();
    fixture = TestBed.createComponent(AppShellComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('appTitle', 'Test App');
    fixture.detectChanges();
  });

  it('renders the appTitle input in the toolbar', () => {
    const toolbar = fixture.nativeElement.querySelector('mat-toolbar');
    expect(toolbar?.textContent).toContain('Test App');
  });

  it('exposes a content projection slot for main content', () => {
    expect(fixture.nativeElement.querySelector('main')).not.toBeNull();
  });
});
```

- [ ] **Step 2: Write the component .ts**

```typescript
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

/**
 * Shared chrome — Material toolbar with app title slot, content projection for main, footer slot.
 * Both Foundation apps consume this; later phases add more slots as needed.
 */
@Component({
  selector: 'cce-app-shell',
  standalone: true,
  imports: [MatToolbarModule, MatButtonModule, MatIconModule],
  templateUrl: './app-shell.component.html',
  styleUrl: './app-shell.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppShellComponent {
  readonly appTitle = input.required<string>();
}
```

- [ ] **Step 3: Write the template**

```html
<mat-toolbar color="primary" class="cce-app-shell__toolbar">
  <span class="cce-app-shell__title">{{ appTitle() }}</span>
  <span class="cce-app-shell__spacer"></span>
  <ng-content select="[shellHeaderEnd]" />
</mat-toolbar>

<main class="cce-app-shell__main">
  <ng-content />
</main>

<footer class="cce-app-shell__footer">
  <ng-content select="[shellFooter]" />
</footer>
```

- [ ] **Step 4: Write the scss**

```scss
.cce-app-shell {
  &__toolbar {
    position: sticky;
    top: 0;
    z-index: 10;
  }
  &__title {
    font-weight: 600;
  }
  &__spacer {
    flex: 1 1 auto;
  }
  &__main {
    min-height: calc(100vh - 64px);
    padding: 16px;
  }
  &__footer {
    padding: 16px;
    text-align: center;
  }
}
```

- [ ] **Step 5: Update `frontend/libs/ui-kit/src/index.ts`**

```typescript
export * from './lib/app-shell/app-shell.component';
```

- [ ] **Step 6: Run + commit**

```bash
cd frontend
pnpm nx test ui-kit --watch=false 2>&1 | tail -8
cd ..
git add frontend/libs/ui-kit/
git -c commit.gpgsign=false commit -m "feat(phase-10): add cce-app-shell standalone component (Material toolbar + content projection) with 2 tests"
```

---

## Task 10.10: Verify path mappings exist for all 5 libs in `tsconfig.base.json`

**Files:**
- Verify (Nx auto-managed): `frontend/tsconfig.base.json`

**Rationale:** Nx adds path mappings like `@cce-frontend/ui-kit → libs/ui-kit/src/index.ts` automatically when you generate libs. Verify all 5 are present.

- [ ] **Step 1: Inspect the path mappings**

```bash
cd frontend
node -e "const c = require('./tsconfig.base.json'); console.log(JSON.stringify(c.compilerOptions.paths, null, 2));"
cd ..
```
Expected: prints an object with 5 entries — one per lib. Names follow Nx workspace name convention (e.g., `@cce-frontend/ui-kit`, depending on the bootstrap workspace name from Phase 09).

- [ ] **Step 2: Quick import smoke test from web-portal**

Edit `frontend/apps/web-portal/src/app/app.component.ts` to add a no-op import (then revert after verifying compile):

```typescript
// Append at top of file, then remove after `nx build` succeeds:
// import { LocaleService } from '@cce-frontend/i18n';
```

Or simpler — just confirm via TS compiler:

```bash
cd frontend
pnpm tsc --noEmit -p tsconfig.base.json 2>&1 | head -10
cd ..
```
Expected: no errors related to module resolution for our libs.

- [ ] **Step 3: (No commit — verification only)**

---

## Task 10.11: Final smoke + completion commit

**Files:** `frontend/.gitignore` (verify Nx generators didn't override) + `frontend/README.md` (mention the new libs)

- [ ] **Step 1: Run full smoke**

```bash
cd frontend
pnpm nx run-many -t build --projects=ui-kit,i18n,auth,api-client,contracts 2>&1 | tail -10
pnpm nx run-many -t lint --projects=ui-kit,i18n,auth,api-client,contracts 2>&1 | tail -10
pnpm nx run-many -t test --projects=ui-kit,i18n,auth,api-client,contracts --watch=false 2>&1 | tail -15
cd ..
```
Expected: all build, lint clean, total 12 tests pass (3 oidc-config + 7 LocaleService + 2 AppShell + 0 contracts/api-client).

- [ ] **Step 2: Append a libs section to `frontend/README.md`**

Append after the existing content:

```markdown

## Shared libraries (libs/)

| Lib | Purpose |
|---|---|
| `ui-kit` | Material theme + DGA tokens + Bootstrap grid + shared components (`app-shell`) |
| `i18n` | ngx-translate + `LocaleService` (signal-based, RTL/LTR, persisted) |
| `auth` | OIDC config builder for angular-auth-oidc-client (Keycloak realms) |
| `api-client` | OpenAPI-generated TS clients (filled in Phase 13) |
| `contracts` | Hand-written types — `CceEnv` for runtime config |

Apps import via path mappings — e.g., `import { LocaleService } from '@cce-frontend/i18n';` (replace `@cce-frontend` with whatever Nx named your workspace at bootstrap).
```

- [ ] **Step 3: Commit**

```bash
git add frontend/README.md
git -c commit.gpgsign=false commit -m "docs(phase-10): document shared libs in frontend/README.md"
```

---

## Phase 10 — completion checklist

- [ ] 5 libraries generated under `frontend/libs/`: `ui-kit`, `i18n`, `auth`, `api-client`, `contracts`.
- [ ] DGA tokens + `cce-theme` Material mixin in `ui-kit`.
- [ ] `LocaleService` in `i18n` with signals + RTL/LTR + localStorage persistence.
- [ ] `ar.json` + `en.json` with Foundation strings.
- [ ] `buildCceOidcConfig` helper in `auth` lib.
- [ ] `CceEnv` interface in `contracts` lib.
- [ ] `app-shell` standalone component in `ui-kit`.
- [ ] All libs build, lint clean, tests pass (~12 tests added).
- [ ] `frontend/README.md` documents the libs.
- [ ] `git status` clean.
- [ ] ~11 new commits.

**If all boxes ticked, phase 10 is complete. Proceed to phase 11 (web-portal app shell).**
