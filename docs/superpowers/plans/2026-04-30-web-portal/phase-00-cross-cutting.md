# Phase 00 — Cross-cutting infrastructure

> Parent: [`../2026-04-30-web-portal.md`](../2026-04-30-web-portal.md) · Spec: [`../../specs/2026-04-30-web-portal-design.md`](../../specs/2026-04-30-web-portal-design.md) §3.1–§3.12, §11

**Phase goal:** Stand up every cross-cutting building block needed by Sub-6 feature phases:

1. Promote re-usable primitives from `apps/admin-cms` to `libs/ui-kit` so both apps share them (paged-table, error-formatter, feedback services).
2. BFF cookie auth: `AuthService` (signals + `/api/me` bootstrap), `authGuard` (`CanMatchFn`), `*ifAuthenticated` directive.
3. HTTP interceptors scoped same-origin from day 1 (BFF-credentials, server-error, correlation-id).
4. Public layout: `<cce-portal-shell>` (top header + footer), `<cce-filter-rail>`, `<cce-search-box>`.
5. Dev proxy.conf so the SPA and BFF endpoints look same-origin in the browser.
6. Playwright + `@axe-core/playwright` harness with smoke + layout regression spec.
7. i18n: extend `libs/i18n` with web-portal keys.

After Phase 00, every feature phase can use these building blocks.

**Tasks:** 8
**Working directory:** `/Users/m/CCE/`
**Preconditions:**
- Sub-5 closed at `admin-cms-v0.1.0` tag.
- `external-api-v0.1.0` provides BFF endpoints at `/auth/{login,callback,refresh,logout}` (verified in `backend/src/CCE.Api.Common/Auth/BffAuthEndpoints.cs`).
- Foundation already scaffolded `apps/web-portal` (health page + locale-switcher + env service).

---

## Pre-execution sanity checks

1. `git status` clean apart from `.claude/`.
2. `git tag -l | grep admin-cms-v0.1.0` → present.
3. `cd frontend && pnpm install` (idempotent).
4. `cd frontend && pnpm nx build web-portal` 0 errors (Foundation baseline).
5. `cd frontend && pnpm nx test web-portal` baseline passes.
6. External API runs locally on port 5001 (or wherever `appsettings.Development.json` puts it) — Phase 0 dev proxy depends on this.

If any fail, stop and report.

---

## Task 0.1: Promote `<cce-paged-table>` from admin-cms to libs/ui-kit

**Files:**
- Move: `frontend/apps/admin-cms/src/app/core/ui/paged-table.component.{ts,html,scss,spec.ts}` → `frontend/libs/ui-kit/src/lib/paged-table/paged-table.component.{ts,html,scss,spec.ts}`
- Modify: `frontend/libs/ui-kit/src/index.ts` (export PagedTableComponent + types)
- Modify: every admin-cms importer of `core/ui/paged-table.component` → import from `@frontend/ui-kit`

**Rationale:** Sub-6 will use the same paged-table pattern as admin-cms. Promoting it to `libs/ui-kit` removes duplication. Component itself is generic (no admin-cms domain references), so the move is mechanical.

- [ ] **Step 1: Move the four files to libs/ui-kit/src/lib/paged-table/**

```bash
cd /Users/m/CCE
mkdir -p frontend/libs/ui-kit/src/lib/paged-table
git mv frontend/apps/admin-cms/src/app/core/ui/paged-table.component.ts \
       frontend/libs/ui-kit/src/lib/paged-table/paged-table.component.ts
git mv frontend/apps/admin-cms/src/app/core/ui/paged-table.component.html \
       frontend/libs/ui-kit/src/lib/paged-table/paged-table.component.html
git mv frontend/apps/admin-cms/src/app/core/ui/paged-table.component.scss \
       frontend/libs/ui-kit/src/lib/paged-table/paged-table.component.scss
git mv frontend/apps/admin-cms/src/app/core/ui/paged-table.component.spec.ts \
       frontend/libs/ui-kit/src/lib/paged-table/paged-table.component.spec.ts
```

- [ ] **Step 2: Add export in libs/ui-kit/src/index.ts**

Append after the existing exports:

```ts
export * from './lib/paged-table/paged-table.component';
```

- [ ] **Step 3: Update admin-cms importers to use `@frontend/ui-kit`**

```bash
cd /Users/m/CCE
grep -rl "core/ui/paged-table.component" frontend/apps/admin-cms/src 2>/dev/null
```

For every match, replace `from '../../core/ui/paged-table.component'` (or whatever relative path) with `from '@frontend/ui-kit'`. Expected files: `users-list.page.ts`, `expert-requests-list.page.ts` and similar — in admin-cms feature folders that import `PagedTableComponent`, `PagedTableColumn`, `PagedTablePageChange`.

- [ ] **Step 4: Run admin-cms tests to confirm imports still work**

```bash
cd /Users/m/CCE/frontend
pnpm nx test admin-cms 2>&1 | tail -10
```

Expected: 238/238 pass (same as `admin-cms-v0.1.0` tag baseline).

- [ ] **Step 5: Run ui-kit tests**

```bash
cd /Users/m/CCE/frontend
pnpm nx test ui-kit 2>&1 | tail -10
```

Expected: 8 paged-table tests pass under the new project.

- [ ] **Step 6: Build ui-kit + admin-cms**

```bash
cd /Users/m/CCE/frontend
pnpm nx build ui-kit 2>&1 | tail -5
pnpm nx build admin-cms 2>&1 | tail -5
```

Expected: both clean.

- [ ] **Step 7: Commit**

```bash
cd /Users/m/CCE
git add frontend/libs/ui-kit/src/lib/paged-table/ frontend/libs/ui-kit/src/index.ts frontend/apps/admin-cms/src
git -c commit.gpgsign=false commit -m "chore(ui-kit): promote PagedTableComponent from admin-cms (Phase 0.1)"
```

---

## Task 0.2: Promote ErrorFormatter to libs/ui-kit

**Files:**
- Move: `frontend/apps/admin-cms/src/app/core/ui/error-formatter.{ts,spec.ts}` → `frontend/libs/ui-kit/src/lib/error-formatter/error-formatter.{ts,spec.ts}`
- Modify: `frontend/libs/ui-kit/src/index.ts` (export `toFeatureError`, `toFieldErrors`, `FeatureError`)
- Modify: admin-cms feature service imports of `core/ui/error-formatter` → `@frontend/ui-kit`

- [ ] **Step 1: Move the two files**

```bash
cd /Users/m/CCE
mkdir -p frontend/libs/ui-kit/src/lib/error-formatter
git mv frontend/apps/admin-cms/src/app/core/ui/error-formatter.ts \
       frontend/libs/ui-kit/src/lib/error-formatter/error-formatter.ts
git mv frontend/apps/admin-cms/src/app/core/ui/error-formatter.spec.ts \
       frontend/libs/ui-kit/src/lib/error-formatter/error-formatter.spec.ts
```

- [ ] **Step 2: Add export in libs/ui-kit/src/index.ts**

```ts
export * from './lib/error-formatter/error-formatter';
```

- [ ] **Step 3: Update admin-cms importers**

```bash
grep -rl "core/ui/error-formatter" frontend/apps/admin-cms/src 2>/dev/null
```

For every match, replace `from '../../core/ui/error-formatter'` (or similar) with `from '@frontend/ui-kit'`. Expected files: every `*-api.service.ts` in admin-cms (identity, experts, content, publishing, taxonomies, countries, notifications, reports, audit).

- [ ] **Step 4: Verify**

```bash
cd /Users/m/CCE/frontend
pnpm nx test admin-cms 2>&1 | tail -5  # 238/238 still pass
pnpm nx test ui-kit 2>&1 | tail -5      # error-formatter tests pass under ui-kit
pnpm nx build admin-cms 2>&1 | tail -3
```

- [ ] **Step 5: Commit**

```bash
cd /Users/m/CCE
git add frontend/libs/ui-kit/src/lib/error-formatter/ frontend/libs/ui-kit/src/index.ts frontend/apps/admin-cms/src
git -c commit.gpgsign=false commit -m "chore(ui-kit): promote ErrorFormatter (toFeatureError + FeatureError) (Phase 0.2)"
```

---

## Task 0.3: Promote ToastService + ConfirmDialogService to libs/ui-kit

**Files:**
- Move: `frontend/apps/admin-cms/src/app/core/ui/toast.service.{ts,spec.ts}`, `confirm-dialog.service.{ts,spec.ts}`, `confirm-dialog.component.ts` → `frontend/libs/ui-kit/src/lib/feedback/`
- Modify: `frontend/libs/ui-kit/src/index.ts` (export both services + `ConfirmDialogData` type)
- Modify: admin-cms importers of these services

- [ ] **Step 1: Move the five files**

```bash
cd /Users/m/CCE
mkdir -p frontend/libs/ui-kit/src/lib/feedback
git mv frontend/apps/admin-cms/src/app/core/ui/toast.service.ts \
       frontend/libs/ui-kit/src/lib/feedback/toast.service.ts
git mv frontend/apps/admin-cms/src/app/core/ui/toast.service.spec.ts \
       frontend/libs/ui-kit/src/lib/feedback/toast.service.spec.ts
git mv frontend/apps/admin-cms/src/app/core/ui/confirm-dialog.service.ts \
       frontend/libs/ui-kit/src/lib/feedback/confirm-dialog.service.ts
git mv frontend/apps/admin-cms/src/app/core/ui/confirm-dialog.service.spec.ts \
       frontend/libs/ui-kit/src/lib/feedback/confirm-dialog.service.spec.ts
git mv frontend/apps/admin-cms/src/app/core/ui/confirm-dialog.component.ts \
       frontend/libs/ui-kit/src/lib/feedback/confirm-dialog.component.ts
```

- [ ] **Step 2: Update libs/ui-kit/src/index.ts**

```ts
export * from './lib/feedback/toast.service';
export * from './lib/feedback/confirm-dialog.service';
```

- [ ] **Step 3: Update admin-cms importers**

```bash
grep -rl "core/ui/toast.service\|core/ui/confirm-dialog.service" frontend/apps/admin-cms/src 2>/dev/null
```

For every match, replace the relative path with `from '@frontend/ui-kit'`. Expected files: every page that injects `ToastService` (most feature pages), every page using `ConfirmDialogService` (state-rep, resources, news, events, etc.).

- [ ] **Step 4: Verify**

```bash
cd /Users/m/CCE/frontend
pnpm nx test admin-cms 2>&1 | tail -5  # 238/238 still pass
pnpm nx test ui-kit 2>&1 | tail -5      # toast + confirm tests pass
pnpm nx build admin-cms 2>&1 | tail -3
```

- [ ] **Step 5: Commit**

```bash
cd /Users/m/CCE
git add frontend/libs/ui-kit/src/lib/feedback/ frontend/libs/ui-kit/src/index.ts frontend/apps/admin-cms/src
git -c commit.gpgsign=false commit -m "chore(ui-kit): promote ToastService + ConfirmDialogService (Phase 0.3)"
```

---

## Task 0.4: HTTP interceptors (BFF-credentials, server-error, correlation-id, all same-origin scoped)

**Files:**
- Create: `frontend/apps/web-portal/src/app/core/http/bff-credentials.interceptor.ts`
- Create: `frontend/apps/web-portal/src/app/core/http/bff-credentials.interceptor.spec.ts`
- Create: `frontend/apps/web-portal/src/app/core/http/server-error.interceptor.ts`
- Create: `frontend/apps/web-portal/src/app/core/http/server-error.interceptor.spec.ts`
- Create: `frontend/apps/web-portal/src/app/core/http/correlation-id.interceptor.ts`
- Create: `frontend/apps/web-portal/src/app/core/http/correlation-id.interceptor.spec.ts`
- Create: `frontend/apps/web-portal/src/app/core/http/is-internal-url.ts` (shared helper)
- Modify: `frontend/apps/web-portal/src/app/app.config.ts` (register the 3 interceptors via `withInterceptors([...])`)

**Rationale:** Same hybrid pattern as Sub-5 admin-cms, but with Sub-5 lessons baked in:
- All interceptors scoped same-origin from day 1 (no Keycloak CORS bug).
- ServerErrorInterceptor uses lazy ToastService injection (no NG0200 cycle).
- BFF replaces Sub-5's Sub-5's auth interceptor — same `withCredentials: true` behavior, no 401 redirect (BFF does that server-side via 302 to `/auth/login`).

- [ ] **Step 1: Define `isInternalUrl` helper**

```ts
// frontend/apps/web-portal/src/app/core/http/is-internal-url.ts
/**
 * Returns true when the URL targets the CCE backend (relative path or
 * absolute URL whose origin matches the SPA). Cross-origin URLs (third-party
 * APIs, Sentry, etc.) MUST NOT receive same-origin headers/credentials —
 * those services do not declare them in `Access-Control-Allow-Headers`,
 * so the browser preflight rejects the request before it fires.
 */
export function isInternalUrl(url: string): boolean {
  if (url.startsWith('/')) return true;
  try {
    return new URL(url).origin === window.location.origin;
  } catch {
    return false;
  }
}
```

- [ ] **Step 2: Define `bffCredentialsInterceptor`**

```ts
// frontend/apps/web-portal/src/app/core/http/bff-credentials.interceptor.ts
import { HttpInterceptorFn } from '@angular/common/http';
import { isInternalUrl } from './is-internal-url';

/**
 * Sets `withCredentials: true` on same-origin requests so the browser sends
 * the BFF session cookie. Cross-origin requests pass through untouched
 * (would force a credentialed CORS preflight which third-party APIs
 * generally do not allow).
 */
export const bffCredentialsInterceptor: HttpInterceptorFn = (req, next) => {
  if (!isInternalUrl(req.url)) return next(req);
  return next(req.clone({ withCredentials: true }));
};
```

- [ ] **Step 3: Test bffCredentialsInterceptor**

```ts
// frontend/apps/web-portal/src/app/core/http/bff-credentials.interceptor.spec.ts
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { bffCredentialsInterceptor } from './bff-credentials.interceptor';

describe('bffCredentialsInterceptor', () => {
  let http: HttpClient;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([bffCredentialsInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('sets withCredentials on relative URLs', () => {
    http.get('/api/me').subscribe();
    const req = httpTesting.expectOne('/api/me');
    expect(req.request.withCredentials).toBe(true);
    req.flush({});
  });

  it('does NOT set withCredentials on cross-origin URLs', () => {
    http.get('http://example.com/api').subscribe();
    const req = httpTesting.expectOne('http://example.com/api');
    expect(req.request.withCredentials).toBe(false);
    req.flush({});
  });

  it('sets withCredentials on absolute same-origin URLs', () => {
    const sameOrigin = `${window.location.origin}/api/x`;
    http.get(sameOrigin).subscribe();
    const req = httpTesting.expectOne(sameOrigin);
    expect(req.request.withCredentials).toBe(true);
    req.flush({});
  });
});
```

- [ ] **Step 4: Define `serverErrorInterceptor` (lazy ToastService)**

```ts
// frontend/apps/web-portal/src/app/core/http/server-error.interceptor.ts
import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { EnvironmentInjector, inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ToastService } from '@frontend/ui-kit';

/**
 * Lazy-resolves ToastService inside catchError to avoid NG0200 cycles
 * (Sub-5 admin-cms hit this when env.json bootstrap was the first request).
 */
export const serverErrorInterceptor: HttpInterceptorFn = (req, next) => {
  const injector = inject(EnvironmentInjector);
  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status >= 500) {
        injector.get(ToastService).error('errors.server');
      } else if (err.status === 403) {
        injector.get(ToastService).error('errors.forbidden');
      }
      return throwError(() => err);
    }),
  );
};
```

- [ ] **Step 5: Test serverErrorInterceptor**

```ts
// frontend/apps/web-portal/src/app/core/http/server-error.interceptor.spec.ts
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ToastService } from '@frontend/ui-kit';
import { serverErrorInterceptor } from './server-error.interceptor';

describe('serverErrorInterceptor', () => {
  let http: HttpClient;
  let httpTesting: HttpTestingController;
  let toast: { error: jest.Mock };

  beforeEach(() => {
    toast = { error: jest.fn() };
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([serverErrorInterceptor])),
        provideHttpClientTesting(),
        { provide: ToastService, useValue: toast },
      ],
    });
    http = TestBed.inject(HttpClient);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('toasts errors.server on 5xx', () => {
    http.get('/x').subscribe({ error: () => undefined });
    httpTesting.expectOne('/x').flush('boom', { status: 500, statusText: 'Server' });
    expect(toast.error).toHaveBeenCalledWith('errors.server');
  });

  it('toasts errors.forbidden on 403', () => {
    http.get('/x').subscribe({ error: () => undefined });
    httpTesting.expectOne('/x').flush('nope', { status: 403, statusText: 'Forbidden' });
    expect(toast.error).toHaveBeenCalledWith('errors.forbidden');
  });

  it('does not toast on 200', () => {
    http.get('/x').subscribe();
    httpTesting.expectOne('/x').flush({});
    expect(toast.error).not.toHaveBeenCalled();
  });
});
```

- [ ] **Step 6: Define `correlationIdInterceptor`**

```ts
// frontend/apps/web-portal/src/app/core/http/correlation-id.interceptor.ts
import { HttpInterceptorFn } from '@angular/common/http';
import { isInternalUrl } from './is-internal-url';

const correlationIdHeader = 'X-Correlation-Id';

function newCorrelationId(): string {
  if (typeof crypto !== 'undefined' && 'randomUUID' in crypto) {
    return crypto.randomUUID();
  }
  return `cid-${Date.now()}-${Math.random().toString(36).slice(2, 10)}`;
}

export const correlationIdInterceptor: HttpInterceptorFn = (req, next) => {
  if (req.headers.has(correlationIdHeader) || !isInternalUrl(req.url)) {
    return next(req);
  }
  return next(req.clone({ headers: req.headers.set(correlationIdHeader, newCorrelationId()) }));
};
```

- [ ] **Step 7: Test correlationIdInterceptor**

Test file mirrors the admin-cms version with cross-origin skip + same-origin stamp + already-set preservation. Three test cases: relative URL gets header, cross-origin URL does NOT, existing header is preserved.

- [ ] **Step 8: Wire interceptors in app.config.ts**

```ts
// frontend/apps/web-portal/src/app/app.config.ts (excerpt — add to providers)
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { bffCredentialsInterceptor } from './core/http/bff-credentials.interceptor';
import { correlationIdInterceptor } from './core/http/correlation-id.interceptor';
import { serverErrorInterceptor } from './core/http/server-error.interceptor';

provideHttpClient(
  withFetch(),
  withInterceptors([correlationIdInterceptor, bffCredentialsInterceptor, serverErrorInterceptor]),
),
```

(Order matters: correlation-id added first so all later interceptors see it; BFF-credentials before server-error so credentials are set before any error fires.)

- [ ] **Step 9: Run tests + commit**

```bash
cd /Users/m/CCE/frontend
pnpm nx test web-portal --testPathPattern="core/http" 2>&1 | tail -10
```

Expected: 9+ tests passing.

```bash
cd /Users/m/CCE
git add frontend/apps/web-portal/src/app/core/http/ frontend/apps/web-portal/src/app/app.config.ts
git -c commit.gpgsign=false commit -m "feat(web-portal): HTTP interceptors (BFF-credentials + server-error + correlation-id, same-origin scoped) (Phase 0.4)"
```

---

## Task 0.5: AuthService + authGuard + `*ifAuthenticated` directive

**Files:**
- Create: `frontend/apps/web-portal/src/app/core/auth/auth.service.ts`
- Create: `frontend/apps/web-portal/src/app/core/auth/auth.service.spec.ts`
- Create: `frontend/apps/web-portal/src/app/core/auth/auth.guard.ts`
- Create: `frontend/apps/web-portal/src/app/core/auth/auth.guard.spec.ts`
- Create: `frontend/apps/web-portal/src/app/core/auth/if-authenticated.directive.ts`
- Create: `frontend/apps/web-portal/src/app/core/auth/if-authenticated.directive.spec.ts`

**Rationale:** Anonymous-first means most pages do not require auth. We only need a binary "is signed in?" check (no role/permission strings — simpler than admin-cms's `hasPermission`). AuthService bootstraps from `/api/me` via `APP_INITIALIZER`; on 401 the BFF middleware will already have cleared the cookie — the SPA just notes "anonymous" and continues.

- [ ] **Step 1: AuthService (signals-first)**

```ts
// frontend/apps/web-portal/src/app/core/auth/auth.service.ts
import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface CurrentUser {
  id: string;
  email: string | null;
  userName: string | null;
  displayNameAr: string | null;
  displayNameEn: string | null;
  avatarUrl: string | null;
  countryId: string | null;
  isExpert: boolean;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly _currentUser = signal<CurrentUser | null>(null);
  readonly currentUser = this._currentUser.asReadonly();
  readonly isAuthenticated = computed(() => this._currentUser() !== null);

  /** Bootstraps from /api/me. Tolerates 401 (anonymous) silently. Call from APP_INITIALIZER. */
  async refresh(): Promise<void> {
    try {
      const me = await firstValueFrom(this.http.get<CurrentUser>('/api/me'));
      this._currentUser.set(me);
    } catch {
      this._currentUser.set(null);
    }
  }

  /** Public test helper. Not for application code. */
  _setUserForTest(user: CurrentUser | null): void {
    this._currentUser.set(user);
  }

  /** Full-page navigation to BFF login. SPA does NOT exchange tokens itself. */
  signIn(returnUrl: string = window.location.pathname + window.location.search): void {
    window.location.assign(`/auth/login?returnUrl=${encodeURIComponent(returnUrl)}`);
  }

  /** Full-page POST is overkill; use a tiny form to satisfy POST + browser redirect. */
  async signOut(): Promise<void> {
    try {
      await firstValueFrom(this.http.post('/auth/logout', {}));
    } finally {
      this._currentUser.set(null);
      window.location.assign('/');
    }
  }
}
```

- [ ] **Step 2: AuthService tests**

TestBed + HttpTestingController. Verify:
- `refresh()` populates `currentUser` on 200.
- `refresh()` keeps `currentUser` null on 401 (no throw).
- `isAuthenticated` reflects the loaded user.
- `signIn(returnUrl)` calls `window.location.assign` with the encoded URL (jsdom: redefine `window.location`).
- `signOut()` POSTs to `/auth/logout`, then sets user to null.

(Pattern matches `admin-cms/src/app/core/auth/auth.service.spec.ts` from Sub-5.)

- [ ] **Step 3: authGuard**

```ts
// frontend/apps/web-portal/src/app/core/auth/auth.guard.ts
import { inject } from '@angular/core';
import { CanMatchFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

/**
 * Boolean-only gate. Public routes do NOT use this — they're left unguarded.
 * On miss, redirects through BFF /auth/login with returnUrl=<originally requested>.
 */
export const authGuard: CanMatchFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.isAuthenticated()) return true;
  auth.signIn(router.url);
  return false;
};
```

- [ ] **Step 4: authGuard tests**

Stub AuthService with `isAuthenticated: signal-readonly`, stub Router with `url`. Assert:
- Returns true when authenticated.
- Returns false + calls `signIn` when not authenticated.

- [ ] **Step 5: `*ifAuthenticated` structural directive**

```ts
// frontend/apps/web-portal/src/app/core/auth/if-authenticated.directive.ts
import { Directive, EmbeddedViewRef, TemplateRef, ViewContainerRef, effect, inject } from '@angular/core';
import { AuthService } from './auth.service';

/**
 * Renders the embedded template only when the user is signed in.
 * Anonymous-friendly counterpart pattern: pages can use *ifAnonymous below,
 * or render the inline "Sign in to continue" affordance inside an *ngIf.
 */
@Directive({
  selector: '[ifAuthenticated]',
  standalone: true,
})
export class IfAuthenticatedDirective {
  private readonly auth = inject(AuthService);
  private readonly tpl = inject(TemplateRef<unknown>);
  private readonly vcr = inject(ViewContainerRef);
  private viewRef: EmbeddedViewRef<unknown> | null = null;

  constructor() {
    effect(() => {
      const allowed = this.auth.isAuthenticated();
      if (allowed && !this.viewRef) {
        this.viewRef = this.vcr.createEmbeddedView(this.tpl);
      } else if (!allowed && this.viewRef) {
        this.vcr.clear();
        this.viewRef = null;
      }
    });
  }
}
```

- [ ] **Step 6: Directive tests**

TestBed renders `<button *ifAuthenticated>edit</button>`; toggle AuthService signal mid-test; assert button appears + disappears reactively.

- [ ] **Step 7: Commit**

```bash
git add frontend/apps/web-portal/src/app/core/auth/
git -c commit.gpgsign=false commit -m "feat(web-portal): AuthService (signals + BFF) + authGuard + *ifAuthenticated (Phase 0.5)"
```

---

## Task 0.6: PortalShellComponent (top header + footer + filter rail + search box)

**Files:**
- Create: `frontend/apps/web-portal/src/app/core/layout/portal-shell.component.{ts,html,scss,spec.ts}`
- Create: `frontend/apps/web-portal/src/app/core/layout/header.component.{ts,html,scss}` (sub-component for the top bar)
- Create: `frontend/apps/web-portal/src/app/core/layout/footer.component.{ts,html,scss}`
- Create: `frontend/apps/web-portal/src/app/core/layout/filter-rail.component.{ts,html,scss,spec.ts}`
- Create: `frontend/apps/web-portal/src/app/core/layout/search-box.component.{ts,html,scss,spec.ts}`
- Create: `frontend/apps/web-portal/src/app/core/layout/nav-config.ts` (primary nav definition — no permission strings, just labels + routes)
- Modify: `frontend/apps/web-portal/src/app/app.component.{ts,html}` (mount `<cce-portal-shell>`)

**Rationale:** Hybrid layout per Section 3.9 of the spec — header (logo + nav + search + locale + sign-in), footer (secondary links + service-rating CTA), filter rail (slot-based; collapsible). Mobile-first: header collapses to hamburger ≤ 768px; rail collapses to "Filters" button on mobile.

- [ ] **Step 1: nav-config.ts**

```ts
// frontend/apps/web-portal/src/app/core/layout/nav-config.ts
export interface PrimaryNavItem {
  labelKey: string;
  route: string;
  icon: string;
}

export const PRIMARY_NAV: readonly PrimaryNavItem[] = [
  { labelKey: 'nav.home', route: '/', icon: 'home' },
  { labelKey: 'nav.knowledgeCenter', route: '/knowledge-center', icon: 'menu_book' },
  { labelKey: 'nav.news', route: '/news', icon: 'feed' },
  { labelKey: 'nav.events', route: '/events', icon: 'event' },
  { labelKey: 'nav.countries', route: '/countries', icon: 'public' },
  { labelKey: 'nav.community', route: '/community', icon: 'forum' },
];
```

- [ ] **Step 2: PortalShellComponent template + class**

```ts
// portal-shell.component.ts
import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { HeaderComponent } from './header.component';
import { FooterComponent } from './footer.component';

@Component({
  selector: 'cce-portal-shell',
  standalone: true,
  imports: [RouterOutlet, HeaderComponent, FooterComponent, TranslateModule],
  templateUrl: './portal-shell.component.html',
  styleUrl: './portal-shell.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PortalShellComponent {}
```

```html
<!-- portal-shell.component.html -->
<cce-header />
<main class="cce-portal-shell__main">
  <router-outlet />
</main>
<cce-footer />
```

```scss
:host { display: flex; flex-direction: column; min-height: 100vh; }
.cce-portal-shell__main { flex: 1; }
```

- [ ] **Step 3: HeaderComponent template + class**

```ts
// header.component.ts
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from '../auth/auth.service';
import { LocaleSwitcherComponent } from '../../locale-switcher/locale-switcher.component';
import { SearchBoxComponent } from './search-box.component';
import { PRIMARY_NAV } from './nav-config';

@Component({
  selector: 'cce-header',
  standalone: true,
  imports: [
    CommonModule, RouterLink, RouterLinkActive,
    MatButtonModule, MatIconModule, MatMenuModule,
    TranslateModule, LocaleSwitcherComponent, SearchBoxComponent,
  ],
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HeaderComponent {
  private readonly auth = inject(AuthService);
  readonly nav = PRIMARY_NAV;
  readonly mobileMenuOpen = signal(false);
  readonly isAuthenticated = this.auth.isAuthenticated;
  readonly userLabel = computed(() => {
    const u = this.auth.currentUser();
    return u?.displayNameEn ?? u?.userName ?? u?.email ?? '';
  });

  toggleMobileMenu(): void {
    this.mobileMenuOpen.update((v) => !v);
  }
  signIn(): void { this.auth.signIn(); }
  async signOut(): Promise<void> { await this.auth.signOut(); }
}
```

```html
<!-- header.component.html -->
<header class="cce-header" role="banner">
  <a routerLink="/" class="cce-header__logo">CCE</a>

  <nav class="cce-header__primary" [class.cce-header__primary--open]="mobileMenuOpen()" role="navigation">
    @for (item of nav; track item.route) {
      <a [routerLink]="item.route" routerLinkActive="cce-header__link--active"
         [routerLinkActiveOptions]="{ exact: item.route === '/' }">
        {{ item.labelKey | translate }}
      </a>
    }
  </nav>

  <cce-search-box class="cce-header__search" />
  <cce-locale-switcher class="cce-header__locale" />

  @if (isAuthenticated()) {
    <button mat-button [matMenuTriggerFor]="userMenu" class="cce-header__user">
      <mat-icon>account_circle</mat-icon>
      <span>{{ userLabel() }}</span>
    </button>
    <mat-menu #userMenu>
      <a mat-menu-item routerLink="/me/profile">{{ 'header.profile' | translate }}</a>
      <a mat-menu-item routerLink="/me/notifications">{{ 'header.notifications' | translate }}</a>
      <a mat-menu-item routerLink="/me/follows">{{ 'header.follows' | translate }}</a>
      <button type="button" mat-menu-item (click)="signOut()">{{ 'header.signOut' | translate }}</button>
    </mat-menu>
  } @else {
    <button type="button" mat-flat-button color="primary" (click)="signIn()" class="cce-header__signin">
      {{ 'header.signIn' | translate }}
    </button>
  }

  <button type="button" mat-icon-button class="cce-header__hamburger" (click)="toggleMobileMenu()"
    [attr.aria-label]="'header.menu' | translate">
    <mat-icon>menu</mat-icon>
  </button>
</header>
```

```scss
.cce-header {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 0.75rem 1.5rem;
  border-bottom: 1px solid rgba(0, 0, 0, 0.08);
  background: #fff;
}
.cce-header__logo {
  font-weight: 700;
  font-size: 1.25rem;
  text-decoration: none;
  color: inherit;
}
.cce-header__primary {
  display: flex;
  gap: 1rem;
  flex: 1;
  a {
    text-decoration: none;
    color: rgba(0, 0, 0, 0.7);
    padding: 0.25rem 0.5rem;
    border-radius: 4px;
    &:hover { background: rgba(0, 0, 0, 0.04); }
  }
  .cce-header__link--active { color: inherit; font-weight: 600; background: rgba(0, 0, 0, 0.06); }
}
.cce-header__search { min-width: 240px; }
.cce-header__hamburger { display: none; }

@media (max-width: 768px) {
  .cce-header__primary {
    display: none;
    &.cce-header__primary--open {
      display: flex;
      flex-direction: column;
      position: absolute; top: 100%; left: 0; right: 0;
      background: #fff; padding: 1rem; gap: 0.5rem;
      box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
    }
  }
  .cce-header__hamburger { display: inline-flex; }
  .cce-header__search { min-width: 0; flex: 1; }
}
```

- [ ] **Step 4: FooterComponent**

```ts
// footer.component.ts
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'cce-footer',
  standalone: true,
  imports: [RouterLink, MatButtonModule, TranslateModule],
  templateUrl: './footer.component.html',
  styleUrl: './footer.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FooterComponent {
  private readonly dialog = inject(MatDialog);
  // openServiceRating() wired in Phase 6 once the dialog component exists
}
```

```html
<footer class="cce-footer" role="contentinfo">
  <nav class="cce-footer__links">
    <a routerLink="/pages/about">{{ 'footer.about' | translate }}</a>
    <a routerLink="/pages/privacy">{{ 'footer.privacy' | translate }}</a>
    <a routerLink="/pages/terms">{{ 'footer.terms' | translate }}</a>
    <a routerLink="/pages/contact">{{ 'footer.contact' | translate }}</a>
  </nav>
  <p class="cce-footer__attrib">{{ 'footer.ministryAttribution' | translate }}</p>
</footer>
```

```scss
.cce-footer {
  padding: 1.5rem;
  border-top: 1px solid rgba(0, 0, 0, 0.08);
  background: rgba(0, 0, 0, 0.02);
  text-align: center;
}
.cce-footer__links {
  display: flex; gap: 1.5rem; justify-content: center; flex-wrap: wrap; margin-bottom: 0.75rem;
  a { color: rgba(0, 0, 0, 0.7); text-decoration: none; &:hover { color: inherit; } }
}
.cce-footer__attrib { color: rgba(0, 0, 0, 0.5); font-size: 0.85em; margin: 0; }
```

- [ ] **Step 5: FilterRailComponent (slot-based, collapsible)**

```ts
// filter-rail.component.ts
import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'cce-filter-rail',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule, TranslateModule],
  template: `
    <button type="button" mat-button class="cce-filter-rail__toggle" (click)="toggle()">
      <mat-icon>filter_list</mat-icon>
      {{ 'filter.openButton' | translate }}
    </button>
    <aside class="cce-filter-rail" [class.cce-filter-rail--open]="open()">
      <h2 class="cce-filter-rail__title">{{ 'filter.title' | translate }}</h2>
      <ng-content />
    </aside>
  `,
  styleUrl: './filter-rail.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FilterRailComponent {
  readonly open = signal(window.innerWidth > 768);
  toggle(): void { this.open.update((v) => !v); }
}
```

```scss
.cce-filter-rail__toggle { display: none; }
.cce-filter-rail {
  width: 240px;
  padding: 1rem;
  border-right: 1px solid rgba(0, 0, 0, 0.08);
}
.cce-filter-rail__title {
  font-size: 1rem; margin: 0 0 1rem;
}

@media (max-width: 768px) {
  .cce-filter-rail__toggle { display: inline-flex; }
  .cce-filter-rail {
    display: none;
    &.cce-filter-rail--open { display: block; }
    width: 100%; border-right: none; border-bottom: 1px solid rgba(0, 0, 0, 0.08);
  }
}
```

Spec: 1 test asserts `open()` initial state matches a stubbed `window.innerWidth`; toggle flips it.

- [ ] **Step 6: SearchBoxComponent**

```ts
// search-box.component.ts
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'cce-search-box',
  standalone: true,
  imports: [FormsModule, MatFormFieldModule, MatIconModule, MatInputModule, TranslateModule],
  template: `
    <mat-form-field appearance="outline" class="cce-search-box">
      <mat-icon matPrefix>search</mat-icon>
      <input matInput type="search"
        [placeholder]="'search.placeholder' | translate"
        [ngModel]="query()" (ngModelChange)="query.set($event)"
        (keyup.enter)="submit()" />
    </mat-form-field>
  `,
  styleUrl: './search-box.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SearchBoxComponent {
  private readonly router = inject(Router);
  readonly query = signal('');
  submit(): void {
    const q = this.query().trim();
    if (q) void this.router.navigate(['/search'], { queryParams: { q } });
  }
}
```

Spec: stub Router, type "abc" + Enter → asserts router.navigate called with `['/search'], { queryParams: { q: 'abc' } }`.

- [ ] **Step 7: Wire app.component to mount the shell**

```ts
// app.component.ts
import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PortalShellComponent } from './core/layout/portal-shell.component';

@Component({
  selector: 'cce-root',
  standalone: true,
  imports: [PortalShellComponent],
  template: '<cce-portal-shell />',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppComponent {}
```

- [ ] **Step 8: Verify + commit**

```bash
cd /Users/m/CCE/frontend
pnpm nx test web-portal --testPathPattern="core/layout" 2>&1 | tail -10
```

Expected: 6+ tests pass.

```bash
cd /Users/m/CCE
git add frontend/apps/web-portal/src/app/core/layout/ frontend/apps/web-portal/src/app/app.component.ts frontend/apps/web-portal/src/app/app.component.html
git -c commit.gpgsign=false commit -m "feat(web-portal): PortalShellComponent (header + footer + filter-rail + search-box) (Phase 0.6)"
```

---

## Task 0.7: Dev proxy.conf (forward `/api/*` + `/auth/*` to External API on :5001)

**Files:**
- Create: `frontend/apps/web-portal/proxy.conf.json`
- Modify: `frontend/apps/web-portal/project.json` (`serve.options.proxyConfig`)

**Rationale:** In dev the SPA runs on `localhost:4200` and the External API on `localhost:5001`. The BFF cookie set by `localhost:5001` will not be sent in XHRs originated by `localhost:4200` cleanly without CORS+credentials gymnastics. Proxying `/api/*` and `/auth/*` through the SPA's dev server makes everything same-origin in the browser — matching prod (where the load balancer fronts both behind the same domain).

- [ ] **Step 1: Create proxy.conf.json**

```json
// frontend/apps/web-portal/proxy.conf.json
{
  "/api": {
    "target": "http://localhost:5001",
    "secure": false,
    "changeOrigin": true,
    "logLevel": "warn"
  },
  "/auth": {
    "target": "http://localhost:5001",
    "secure": false,
    "changeOrigin": true,
    "logLevel": "warn"
  }
}
```

- [ ] **Step 2: Wire into serve target**

In `frontend/apps/web-portal/project.json`, find the `"serve"` block and add `"proxyConfig": "apps/web-portal/proxy.conf.json"` next to `"port": 4200`.

- [ ] **Step 3: Smoke test**

```bash
# In one terminal: external API
cd /Users/m/CCE
dotnet run --project backend/src/CCE.Api.External --urls "http://localhost:5001"

# In another:
cd /Users/m/CCE/frontend
pnpm nx serve web-portal
# Then in a third:
curl -s -o /dev/null -w "/api/me proxied: %{http_code}\n" http://localhost:4200/api/me
```

Expected: `401` (unauthenticated) — proves the proxy forwarded the request and the External API responded. If `404`, the proxy is misconfigured.

- [ ] **Step 4: Commit**

```bash
git add frontend/apps/web-portal/proxy.conf.json frontend/apps/web-portal/project.json
git -c commit.gpgsign=false commit -m "feat(web-portal): dev proxy.conf forwards /api/* + /auth/* to External API (Phase 0.7)"
```

---

## Task 0.8: Playwright + axe E2E harness + smoke + layout regression spec

**Files:**
- Modify: `frontend/apps/web-portal-e2e/playwright.config.ts` (port 4200, dev-server auto-launch)
- Create: `frontend/apps/web-portal-e2e/src/support/axe.ts` (lift identical helper from admin-cms-e2e)
- Create: `frontend/apps/web-portal-e2e/src/smoke.spec.ts` (anonymous land + axe)
- Create: `frontend/apps/web-portal-e2e/src/layout.spec.ts` (asserts shell + filter-rail render)

**Rationale:** Mirror Sub-5's harness (`apps/admin-cms-e2e`). Foundation may have scaffolded this differently; check `frontend/apps/web-portal-e2e/` first and adjust. Smoke: anonymous user opens `/`, sees the header + footer, axe-clean. Layout: the shell renders before any route resolves.

- [ ] **Step 1: Inspect existing scaffold**

```bash
ls /Users/m/CCE/frontend/apps/web-portal-e2e/
cat /Users/m/CCE/frontend/apps/web-portal-e2e/playwright.config.ts
```

Adjust `baseURL` to `http://localhost:4200`; `webServer.command` to `pnpm exec nx run web-portal:serve`.

- [ ] **Step 2: Create support/axe.ts**

```ts
// frontend/apps/web-portal-e2e/src/support/axe.ts
import type { Page, TestInfo } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

export async function expectNoA11yViolations(page: Page, testInfo: TestInfo, scope?: string): Promise<void> {
  const builder = new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa']);
  if (scope) builder.include(scope);
  const results = await builder.analyze();
  await testInfo.attach('axe-results.json', {
    body: JSON.stringify(results, null, 2),
    contentType: 'application/json',
  });
  const blocking = results.violations.filter((v) => v.impact === 'critical' || v.impact === 'serious');
  if (blocking.length > 0) {
    const summary = blocking.map((v) => `${v.id} (${v.impact}): ${v.description}`).join('\n');
    throw new Error(`a11y violations:\n${summary}`);
  }
}
```

- [ ] **Step 3: Smoke spec — anonymous land + axe**

```ts
// frontend/apps/web-portal-e2e/src/smoke.spec.ts
import { test, expect } from '@playwright/test';
import { expectNoA11yViolations } from './support/axe';

test.describe('web-portal smoke', () => {
  test('anonymous land — header + footer render and axe is clean', async ({ page }, testInfo) => {
    await page.goto('/');
    await expect(page.locator('cce-header')).toBeVisible();
    await expect(page.locator('cce-footer')).toBeVisible();
    await expect(page.getByRole('button', { name: /sign in|تسجيل الدخول/i })).toBeVisible();
    await expectNoA11yViolations(page, testInfo);
  });
});
```

- [ ] **Step 4: Layout regression spec**

```ts
// frontend/apps/web-portal-e2e/src/layout.spec.ts
import { test, expect } from '@playwright/test';

test.describe('web-portal layout', () => {
  test('cce-portal-shell renders with header + main + footer', async ({ page }) => {
    await page.goto('/');
    await expect(page.locator('cce-portal-shell')).toBeAttached({ timeout: 15_000 });
    await expect(page.locator('cce-header')).toBeAttached();
    await expect(page.locator('main')).toBeAttached();
    await expect(page.locator('cce-footer')).toBeAttached();
  });
});
```

- [ ] **Step 5: Run E2E**

```bash
cd /Users/m/CCE/frontend
pnpm nx e2e web-portal-e2e 2>&1 | tail -10
```

Expected: 2/2 pass.

- [ ] **Step 6: Commit**

```bash
cd /Users/m/CCE
git add frontend/apps/web-portal-e2e/
git -c commit.gpgsign=false commit -m "feat(web-portal-e2e): Playwright + axe harness + smoke + layout regression spec (Phase 0.8)"
```

---

## Phase 00 — completion checklist

- [ ] Task 0.1 — paged-table promoted to `libs/ui-kit`.
- [ ] Task 0.2 — error-formatter promoted to `libs/ui-kit`.
- [ ] Task 0.3 — toast + confirm-dialog promoted to `libs/ui-kit`.
- [ ] Task 0.4 — 3 HTTP interceptors registered (BFF-credentials + server-error + correlation-id), all same-origin scoped.
- [ ] Task 0.5 — AuthService + authGuard + `*ifAuthenticated` shipped.
- [ ] Task 0.6 — PortalShellComponent (header + footer + filter-rail + search-box) shipped.
- [ ] Task 0.7 — Dev proxy.conf forwards /api + /auth to External API.
- [ ] Task 0.8 — Playwright + axe harness with smoke + layout regression passing.
- [ ] All Jest tests passing for the new core/ folders.
- [ ] admin-cms still 238/238 (after Tasks 0.1–0.3 import migration).
- [ ] Build clean (`pnpm nx build web-portal && pnpm nx build admin-cms && pnpm nx build ui-kit`).
- [ ] 8 atomic commits.

**If all boxes ticked, Phase 00 is complete. Proceed to Phase 01 (home + static pages).**
