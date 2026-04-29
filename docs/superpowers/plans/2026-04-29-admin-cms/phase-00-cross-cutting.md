# Phase 00 — Cross-cutting infrastructure

> Parent: [`../2026-04-29-admin-cms.md`](../2026-04-29-admin-cms.md) · Spec: [`../../specs/2026-04-29-admin-cms-design.md`](../../specs/2026-04-29-admin-cms-design.md) §3.1–§3.12

**Phase goal:** Stand up every cross-cutting building block needed by feature phases:
1. HTTP interceptors (auth, server-error, correlation-id).
2. AuthService + PermissionGuard + `[ccePermission]` directive.
3. i18n service + DirectionService for RTL/LTR.
4. ToastService + ConfirmDialogService + ErrorFormatter.
5. Layout shell with Material side-nav + role-gated nav-config.
6. Generic `<cce-paged-table>` shared component.
7. Cypress + axe-core test harness.

After Phase 00, every feature phase can use these building blocks.

**Tasks:** 7
**Working directory:** `/Users/m/CCE/`
**Preconditions:**
- Sub-project 4 closed at `external-api-v0.1.0` tag (`8dc6dd6`).
- Foundation already scaffolded `apps/admin-cms` shell (~128 lines: app shell + auth-toolbar + locale-switcher + profile page + env service + translate-loader).
- `libs/api-client` generated against `contracts/openapi.internal.json`.

---

## Pre-execution sanity checks

1. `git status` clean apart from `.claude/`.
2. `git tag -l | grep external-api-v0.1.0` → present.
3. `cd frontend && pnpm install` (idempotent).
4. `cd frontend && pnpm nx build admin-cms` 0 errors.
5. `cd frontend && pnpm nx test admin-cms` baseline passes.

If any fail, stop and report.

---

## Task 0.1: HTTP interceptors (auth + server-error + correlation-id)

**Files:**
- Create: `frontend/apps/admin-cms/src/app/core/http/auth.interceptor.ts`
- Create: `frontend/apps/admin-cms/src/app/core/http/server-error.interceptor.ts`
- Create: `frontend/apps/admin-cms/src/app/core/http/correlation-id.interceptor.ts`
- Create: `frontend/apps/admin-cms/src/app/core/http/auth.interceptor.spec.ts`
- Create: `frontend/apps/admin-cms/src/app/core/http/server-error.interceptor.spec.ts`
- Create: `frontend/apps/admin-cms/src/app/core/http/correlation-id.interceptor.spec.ts`
- Modify: `frontend/apps/admin-cms/src/app/app.config.ts` (register the 3 interceptors via `withInterceptors([...])` on `provideHttpClient`)

**Rationale:** Foundation set up `provideHttpClient(withFetch())` but no interceptors. We add 3 functional interceptors (Angular 19 idiom — pure functions, not classes).

- [ ] **Step 1: Define `AuthInterceptor`**

```ts
// frontend/apps/admin-cms/src/app/core/http/auth.interceptor.ts
import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  // Always include cookies (BFF cce.session). withCredentials is per-request.
  const cloned = req.clone({ withCredentials: true });
  return next(cloned).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status === 401) {
        // Skip redirect for the /api/me bootstrap call — let the AuthService handle.
        if (!req.url.includes('/api/me')) {
          const returnUrl = encodeURIComponent(router.url);
          window.location.assign(`/auth/login?returnUrl=${returnUrl}`);
        }
      }
      return throwError(() => err);
    }),
  );
};
```

- [ ] **Step 2: Test `AuthInterceptor`**

```ts
// frontend/apps/admin-cms/src/app/core/http/auth.interceptor.spec.ts
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { authInterceptor } from './auth.interceptor';

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: Router, useValue: { url: '/users' } as Partial<Router> },
      ],
    });
    http = TestBed.inject(HttpClient);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  it('adds withCredentials to every request', () => {
    http.get('/api/admin/users').subscribe();
    const req = httpTesting.expectOne('/api/admin/users');
    expect(req.request.withCredentials).toBe(true);
    req.flush({});
  });

  it('does not redirect for /api/me 401', () => {
    const assignSpy = jest.spyOn(window.location, 'assign').mockImplementation();
    http.get('/api/me').subscribe({ error: () => undefined });
    const req = httpTesting.expectOne('/api/me');
    req.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });
    expect(assignSpy).not.toHaveBeenCalled();
    assignSpy.mockRestore();
  });
});
```

- [ ] **Step 3: Define `ServerErrorInterceptor`**

```ts
// frontend/apps/admin-cms/src/app/core/http/server-error.interceptor.ts
import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ToastService } from '../ui/toast.service';

export const serverErrorInterceptor: HttpInterceptorFn = (req, next) => {
  const toast = inject(ToastService);
  return next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status >= 500) {
        toast.error('errors.server');
      } else if (err.status === 403) {
        toast.error('errors.forbidden');
      }
      return throwError(() => err);
    }),
  );
};
```

(Notes: `ToastService` is created in Task 0.4. The interceptor depends on it; tests stub it out.)

- [ ] **Step 4: Define `CorrelationIdInterceptor`**

```ts
// frontend/apps/admin-cms/src/app/core/http/correlation-id.interceptor.ts
import { HttpInterceptorFn } from '@angular/common/http';

const correlationIdHeader = 'X-Correlation-Id';

function newCorrelationId(): string {
  // Use crypto.randomUUID where available (modern browsers + Node 16+).
  if (typeof crypto !== 'undefined' && 'randomUUID' in crypto) {
    return crypto.randomUUID();
  }
  return `cid-${Date.now()}-${Math.random().toString(36).slice(2, 10)}`;
}

export const correlationIdInterceptor: HttpInterceptorFn = (req, next) => {
  if (req.headers.has(correlationIdHeader)) {
    return next(req);
  }
  const cloned = req.clone({ headers: req.headers.set(correlationIdHeader, newCorrelationId()) });
  return next(cloned);
};
```

- [ ] **Step 5: Tests for `ServerError` + `CorrelationId` interceptors**

Each gets a `.spec.ts` with TestBed + HttpTestingController. ServerError test verifies `ToastService.error` is called for 5xx/403 (use `Substitute.For` / `jest.fn()` stubs). CorrelationId test verifies the header is added on every request and not overwritten if already present.

- [ ] **Step 6: Wire interceptors in `app.config.ts`**

```ts
// frontend/apps/admin-cms/src/app/app.config.ts (excerpt)
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { authInterceptor } from './core/http/auth.interceptor';
import { serverErrorInterceptor } from './core/http/server-error.interceptor';
import { correlationIdInterceptor } from './core/http/correlation-id.interceptor';

// In providers array:
provideHttpClient(
  withFetch(),
  withInterceptors([correlationIdInterceptor, authInterceptor, serverErrorInterceptor]),
),
```

(Order matters: correlation-id added first so all later interceptors see it.)

- [ ] **Step 7: Run tests + commit**

```bash
cd /Users/m/CCE/frontend
pnpm nx test admin-cms --testPathPattern="core/http" 2>&1 | tail -10
```

Expected: 6+ tests passing.

```bash
cd /Users/m/CCE
git add frontend/apps/admin-cms/src/app/core/http/ \
        frontend/apps/admin-cms/src/app/app.config.ts
git -c commit.gpgsign=false commit -m "feat(admin-cms): HTTP interceptors (auth + server-error + correlation-id) (Phase 0.1)"
```

---

## Task 0.2: AuthService + PermissionGuard + `[ccePermission]` directive

**Files:**
- Create: `frontend/apps/admin-cms/src/app/core/auth/auth.service.ts`
- Create: `frontend/apps/admin-cms/src/app/core/auth/auth.service.spec.ts`
- Create: `frontend/apps/admin-cms/src/app/core/auth/permission.guard.ts`
- Create: `frontend/apps/admin-cms/src/app/core/auth/permission.guard.spec.ts`
- Create: `frontend/apps/admin-cms/src/app/core/auth/permission.directive.ts`
- Create: `frontend/apps/admin-cms/src/app/core/auth/permission.directive.spec.ts`

**Rationale:** Three units that work together. AuthService exposes signals; Guard uses signals to gate routes; Directive uses signals to gate UI elements.

- [ ] **Step 1: AuthService (signals-first)**

```ts
// frontend/apps/admin-cms/src/app/core/auth/auth.service.ts
import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface CurrentUser {
  id: string;
  email: string;
  userName: string;
  permissions: readonly string[];
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly _currentUser = signal<CurrentUser | null>(null);
  readonly currentUser = this._currentUser.asReadonly();
  readonly isAuthenticated = computed(() => this._currentUser() !== null);

  /** Bootstraps the user from /api/me. Call from APP_INITIALIZER. */
  async refresh(): Promise<void> {
    try {
      const me = await firstValueFrom(this.http.get<CurrentUser>('/api/me'));
      this._currentUser.set(me);
    } catch {
      this._currentUser.set(null);
    }
  }

  hasPermission(permission: string): boolean {
    const u = this._currentUser();
    return u?.permissions.includes(permission) ?? false;
  }

  signOut(): void {
    this._currentUser.set(null);
    window.location.assign('/auth/logout');
  }
}
```

- [ ] **Step 2: AuthService tests** — TestBed + HttpTestingController. Verify `refresh()` populates the signal on 200; sets null on 401; `hasPermission` reflects the loaded user.

- [ ] **Step 3: PermissionGuard (`CanMatch`)**

```ts
// frontend/apps/admin-cms/src/app/core/auth/permission.guard.ts
import { CanMatchFn, Route, UrlSegment } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from './auth.service';

export const permissionGuard: CanMatchFn = (route: Route, _segments: UrlSegment[]) => {
  const auth = inject(AuthService);
  const required = route.data?.['permission'] as string | undefined;
  if (!required) return true;
  return auth.hasPermission(required);
};
```

- [ ] **Step 4: PermissionGuard tests** — stub AuthService; assert returns true when permission present, false otherwise.

- [ ] **Step 5: `[ccePermission]` structural directive**

```ts
// frontend/apps/admin-cms/src/app/core/auth/permission.directive.ts
import { Directive, EmbeddedViewRef, Input, TemplateRef, ViewContainerRef, effect, inject } from '@angular/core';
import { AuthService } from './auth.service';

@Directive({
  selector: '[ccePermission]',
  standalone: true,
})
export class PermissionDirective {
  private readonly auth = inject(AuthService);
  private readonly tpl = inject(TemplateRef<unknown>);
  private readonly vcr = inject(ViewContainerRef);
  private viewRef: EmbeddedViewRef<unknown> | null = null;
  private requiredPermission: string | null = null;

  constructor() {
    effect(() => {
      // Re-evaluate whenever currentUser changes.
      this.auth.currentUser();
      this.update();
    });
  }

  @Input({ required: true }) set ccePermission(value: string) {
    this.requiredPermission = value;
    this.update();
  }

  private update(): void {
    const allowed = this.requiredPermission !== null && this.auth.hasPermission(this.requiredPermission);
    if (allowed && !this.viewRef) {
      this.viewRef = this.vcr.createEmbeddedView(this.tpl);
    } else if (!allowed && this.viewRef) {
      this.vcr.clear();
      this.viewRef = null;
    }
  }
}
```

- [ ] **Step 6: Directive tests** — TestBed renders a template with `*ccePermission="'X.Y'"`; assert visible when permission present, hidden otherwise. Toggle the AuthService signal mid-test to verify reactivity.

- [ ] **Step 7: Commit**

```bash
git add frontend/apps/admin-cms/src/app/core/auth/
git -c commit.gpgsign=false commit -m "feat(admin-cms): AuthService (signals) + PermissionGuard + [ccePermission] directive (Phase 0.2)"
```

---

## Task 0.3: i18n + RTL/LTR direction service

**Files:**
- Create: `frontend/apps/admin-cms/src/app/core/i18n/i18n.service.ts`
- Create: `frontend/apps/admin-cms/src/app/core/i18n/direction.service.ts`
- Create: `frontend/apps/admin-cms/src/app/core/i18n/i18n.service.spec.ts`
- Create: `frontend/apps/admin-cms/src/app/core/i18n/direction.service.spec.ts`
- Modify: `frontend/apps/admin-cms/src/assets/i18n/ar.json`, `en.json` (or wherever ngx-translate's loader points — Foundation may have set this up)

**Rationale:** ngx-translate is already imported via `libs/i18n`. We add a thin service over it that exposes a signal + automatically toggles `<html dir>` via `DirectionService`.

- [ ] **Step 1: I18nService**

```ts
// frontend/apps/admin-cms/src/app/core/i18n/i18n.service.ts
import { Injectable, inject, signal, effect } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { DirectionService } from './direction.service';

export type SupportedLocale = 'ar' | 'en';
const STORAGE_KEY = 'cce.locale';

@Injectable({ providedIn: 'root' })
export class I18nService {
  private readonly translate = inject(TranslateService);
  private readonly direction = inject(DirectionService);
  private readonly _locale = signal<SupportedLocale>(this.initial());
  readonly locale = this._locale.asReadonly();

  constructor() {
    effect(() => {
      const l = this._locale();
      this.translate.use(l);
      this.direction.set(l === 'ar' ? 'rtl' : 'ltr');
      try { localStorage.setItem(STORAGE_KEY, l); } catch { /* no-op */ }
    });
  }

  setLocale(locale: SupportedLocale): void {
    this._locale.set(locale);
  }

  private initial(): SupportedLocale {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      if (stored === 'ar' || stored === 'en') return stored;
    } catch { /* no-op */ }
    const browser = (navigator?.language ?? 'ar').toLowerCase();
    return browser.startsWith('ar') ? 'ar' : (browser.startsWith('en') ? 'en' : 'ar');
  }
}
```

- [ ] **Step 2: DirectionService**

```ts
// frontend/apps/admin-cms/src/app/core/i18n/direction.service.ts
import { DOCUMENT } from '@angular/common';
import { Injectable, inject } from '@angular/core';

export type Direction = 'rtl' | 'ltr';

@Injectable({ providedIn: 'root' })
export class DirectionService {
  private readonly doc = inject(DOCUMENT);

  set(dir: Direction): void {
    const html = this.doc.documentElement;
    html.setAttribute('dir', dir);
    html.setAttribute('lang', dir === 'rtl' ? 'ar' : 'en');
  }
}
```

- [ ] **Step 3: Tests**

For `DirectionService`: TestBed with a document stub; verify `<html dir="rtl">` and `<html lang="ar">` after `set('rtl')`.

For `I18nService`: stub TranslateService + DirectionService; verify the signal-effect chain wires through (calling `setLocale('en')` triggers `translate.use('en')` and `direction.set('ltr')`).

- [ ] **Step 4: ar.json + en.json bootstrap content**

Add these keys (extend later as features land):

```json
{
  "errors.server": "Server error. Try again later.",
  "errors.forbidden": "You don't have permission for this action.",
  "errors.network": "Network error. Check your connection.",
  "common.save": "Save",
  "common.cancel": "Cancel",
  "common.delete": "Delete",
  "common.edit": "Edit",
  "common.create": "Create"
}
```

(English values literal; Arabic values translated by a human or a stub. For v0.1.0 the en.json is canonical; ar.json mirrors keys.)

- [ ] **Step 5: Commit**

```bash
git add frontend/apps/admin-cms/src/app/core/i18n/ \
        frontend/apps/admin-cms/src/assets/i18n/
git -c commit.gpgsign=false commit -m "feat(admin-cms): I18nService + DirectionService (signals + RTL/LTR) (Phase 0.3)"
```

---

## Task 0.4: ToastService + ConfirmDialogService + ErrorFormatter

**Files:**
- Create: `frontend/apps/admin-cms/src/app/core/ui/toast.service.ts`
- Create: `frontend/apps/admin-cms/src/app/core/ui/confirm-dialog.service.ts`
- Create: `frontend/apps/admin-cms/src/app/core/ui/error-formatter.ts`
- Create: tests for each.

- [ ] **Step 1: ToastService (wraps MatSnackBar)**

```ts
// frontend/apps/admin-cms/src/app/core/ui/toast.service.ts
import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslateService } from '@ngx-translate/core';

@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly snack = inject(MatSnackBar);
  private readonly translate = inject(TranslateService);

  success(messageKey: string, params?: Record<string, unknown>): void {
    this.show(messageKey, params, 'cce-toast-success');
  }

  error(messageKey: string, params?: Record<string, unknown>): void {
    this.show(messageKey, params, 'cce-toast-error');
  }

  private show(key: string, params: Record<string, unknown> | undefined, panelClass: string): void {
    const text = this.translate.instant(key, params);
    this.snack.open(text, undefined, { duration: 4000, panelClass });
  }
}
```

- [ ] **Step 2: ConfirmDialogService**

```ts
// frontend/apps/admin-cms/src/app/core/ui/confirm-dialog.service.ts
import { Injectable, inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { firstValueFrom } from 'rxjs';
import { ConfirmDialogComponent, ConfirmDialogData } from './confirm-dialog.component';

@Injectable({ providedIn: 'root' })
export class ConfirmDialogService {
  private readonly dialog = inject(MatDialog);

  async confirm(data: ConfirmDialogData): Promise<boolean> {
    const ref = this.dialog.open(ConfirmDialogComponent, { data, width: '480px' });
    const result = await firstValueFrom(ref.afterClosed());
    return result === true;
  }
}
```

(`ConfirmDialogComponent` is a simple Material dialog with title + message + Confirm/Cancel buttons. Standalone component. Add it as part of this task — straightforward template, ~30 lines.)

- [ ] **Step 3: ErrorFormatter**

```ts
// frontend/apps/admin-cms/src/app/core/ui/error-formatter.ts
import { HttpErrorResponse } from '@angular/common/http';

/** Map FluentValidation 400 ProblemDetails to a per-field error map. */
export function toFieldErrors(err: HttpErrorResponse): Record<string, string[]> {
  if (err.status !== 400) return {};
  const body = err.error;
  if (!body || typeof body !== 'object') return {};
  // ASP.NET Core ValidationProblemDetails carries an `errors` map.
  const errors = (body as { errors?: Record<string, string[]> }).errors;
  if (!errors) return {};
  const normalized: Record<string, string[]> = {};
  for (const [key, msgs] of Object.entries(errors)) {
    // FluentValidation field names are dotted (e.g. "Name", "Address.Line1"); the form-control
    // names are camelCase first-letter — lowercase first char to align.
    const camelKey = key.charAt(0).toLowerCase() + key.slice(1);
    normalized[camelKey] = Array.isArray(msgs) ? msgs : [String(msgs)];
  }
  return normalized;
}

/** Shape of a feature-domain error after wrapper mapping. */
export type FeatureError =
  | { kind: 'concurrency'; message?: string }
  | { kind: 'duplicate'; message?: string }
  | { kind: 'validation'; fieldErrors: Record<string, string[]> }
  | { kind: 'not-found' }
  | { kind: 'forbidden' }
  | { kind: 'server' }
  | { kind: 'network' }
  | { kind: 'unknown'; status: number };

/** Map an HttpErrorResponse to a FeatureError. */
export function toFeatureError(err: HttpErrorResponse): FeatureError {
  if (err.status === 0) return { kind: 'network' };
  if (err.status === 400) return { kind: 'validation', fieldErrors: toFieldErrors(err) };
  if (err.status === 404) return { kind: 'not-found' };
  if (err.status === 403) return { kind: 'forbidden' };
  if (err.status === 409) {
    const type = (err.error as { type?: string })?.type ?? '';
    return type.includes('/duplicate') ? { kind: 'duplicate' } : { kind: 'concurrency' };
  }
  if (err.status >= 500) return { kind: 'server' };
  return { kind: 'unknown', status: err.status };
}
```

- [ ] **Step 4: Tests** — `toFieldErrors` test covers ASP.NET ValidationProblemDetails shape; `toFeatureError` test covers each branch (0/400/404/403/409 with type=concurrency, 409 with type=duplicate, 5xx, unknown).

- [ ] **Step 5: Commit**

```bash
git add frontend/apps/admin-cms/src/app/core/ui/
git -c commit.gpgsign=false commit -m "feat(admin-cms): ToastService + ConfirmDialogService + ErrorFormatter (Phase 0.4)"
```

---

## Task 0.5: `<cce-shell>` layout + role-gated side nav

**Files:**
- Create: `frontend/apps/admin-cms/src/app/core/layout/shell.component.ts/.html/.scss`
- Create: `frontend/apps/admin-cms/src/app/core/layout/side-nav.component.ts/.html/.scss`
- Create: `frontend/apps/admin-cms/src/app/core/layout/nav-config.ts` (signals-driven nav definition)
- Create: `frontend/apps/admin-cms/src/app/core/layout/shell.component.spec.ts`
- Create: `frontend/apps/admin-cms/src/app/core/layout/side-nav.component.spec.ts`
- Modify: `frontend/apps/admin-cms/src/app/app.component.ts` (mount `<cce-shell>`)
- Modify: `frontend/apps/admin-cms/src/app/app.routes.ts` (root route loads ShellComponent → router-outlet)

**Rationale:** Shell wraps all features. Side nav reads from a `nav-config.ts` exporting an array of `{ label, route, permission, icon }`. Each link rendered only when the user has the permission (`[ccePermission]`).

- [ ] **Step 1: Define nav-config**

```ts
// frontend/apps/admin-cms/src/app/core/layout/nav-config.ts
export interface NavItem {
  labelKey: string;
  route: string;
  permission: string;
  icon: string;
}

export const NAV_ITEMS: readonly NavItem[] = [
  { labelKey: 'nav.users', route: '/users', permission: 'User.Read', icon: 'people' },
  { labelKey: 'nav.experts', route: '/experts', permission: 'Community.Expert.ApproveRequest', icon: 'school' },
  { labelKey: 'nav.resources', route: '/resources', permission: 'Resource.Center.Upload', icon: 'description' },
  { labelKey: 'nav.news', route: '/news', permission: 'News.Update', icon: 'feed' },
  { labelKey: 'nav.events', route: '/events', permission: 'Event.Manage', icon: 'event' },
  { labelKey: 'nav.pages', route: '/pages', permission: 'Page.Edit', icon: 'web' },
  { labelKey: 'nav.taxonomies', route: '/taxonomies', permission: 'Resource.Center.Upload', icon: 'category' },
  { labelKey: 'nav.community', route: '/community-moderation', permission: 'Community.Post.Moderate', icon: 'forum' },
  { labelKey: 'nav.countries', route: '/countries', permission: 'Country.Profile.Update', icon: 'public' },
  { labelKey: 'nav.notifications', route: '/notifications', permission: 'Notification.TemplateManage', icon: 'notifications' },
  { labelKey: 'nav.reports', route: '/reports', permission: 'Report.UserRegistrations', icon: 'assessment' },
  { labelKey: 'nav.audit', route: '/audit', permission: 'Audit.Read', icon: 'history' },
];
```

- [ ] **Step 2: ShellComponent template** — `mat-sidenav-container` with `<cce-side-nav>` in the drawer + `<mat-toolbar>` + `<router-outlet>` in main. The existing `auth-toolbar.component.ts` and `locale-switcher.component.ts` from Foundation slot into the toolbar.

- [ ] **Step 3: SideNavComponent template** — iterates `NAV_ITEMS`, each rendered with `*ccePermission="item.permission"`. `routerLink="{{item.route}}"` + Material list-item.

- [ ] **Step 4: Wire app.component.ts to mount `<cce-shell>`** + update root routes so feature routes render INSIDE the shell.

- [ ] **Step 5: Tests** — TestBed renders Shell + SideNav with stubbed AuthService. Verify nav links appear/disappear based on permissions.

- [ ] **Step 6: Commit**

```bash
git -c commit.gpgsign=false commit -m "feat(admin-cms): <cce-shell> layout + role-gated side nav (Phase 0.5)"
```

---

## Task 0.6: Generic `<cce-paged-table>` shared component

**Files:**
- Create: `frontend/apps/admin-cms/src/app/core/ui/paged-table.component.ts/.html/.scss`
- Create: `frontend/apps/admin-cms/src/app/core/ui/paged-table.component.spec.ts`

**Rationale:** Every list page in feature phases (Users, Experts, Resources, News, etc.) renders a paginated Material table. Centralize the pattern.

```ts
// frontend/apps/admin-cms/src/app/core/ui/paged-table.component.ts
import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output, ChangeDetectionStrategy } from '@angular/core';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslateModule } from '@ngx-translate/core';

export interface PagedTableColumn<T> {
  key: string;
  labelKey: string;
  cell: (row: T) => string | number;
}

@Component({
  selector: 'cce-paged-table',
  standalone: true,
  imports: [CommonModule, MatTableModule, MatPaginatorModule, MatProgressBarModule, TranslateModule],
  templateUrl: './paged-table.component.html',
  styleUrls: ['./paged-table.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PagedTableComponent<T> {
  @Input({ required: true }) columns: PagedTableColumn<T>[] = [];
  @Input({ required: true }) rows: T[] = [];
  @Input({ required: true }) total = 0;
  @Input() page = 1;
  @Input() pageSize = 20;
  @Input() loading = false;
  @Output() readonly pageChange = new EventEmitter<{ page: number; pageSize: number }>();

  get displayedColumns(): string[] {
    return this.columns.map(c => c.key);
  }

  onPage(event: PageEvent): void {
    this.pageChange.emit({ page: event.pageIndex + 1, pageSize: event.pageSize });
  }
}
```

- [ ] **Step 1: Component + template** as above.

- [ ] **Step 2: Tests** — TestBed renders rows + clicks paginator → emits pageChange.

- [ ] **Step 3: Commit**

```bash
git -c commit.gpgsign=false commit -m "feat(admin-cms): generic <cce-paged-table> component (Phase 0.6)"
```

---

## Task 0.7: Cypress + axe-core test harness

**Files:**
- Modify: `frontend/apps/admin-cms-e2e/cypress.config.ts`
- Modify: `frontend/apps/admin-cms-e2e/src/support/e2e.ts` (import cypress-axe; expose `cy.injectAxe()` + `cy.checkA11y()`)
- Create: `frontend/apps/admin-cms-e2e/src/e2e/smoke.cy.ts` — minimal smoke test that opens the app, runs axe, asserts no critical/serious findings.
- Modify: `frontend/apps/admin-cms-e2e/package.json` (add `cypress-axe` if not present; usually it's already in `frontend/package.json`)

**Rationale:** ADR-0012 mandates axe-clean E2E. Wire it now so every feature phase has a smoke test that runs axe.

- [ ] **Step 1: Install cypress-axe (if not already)**

```bash
cd frontend
pnpm install --save-dev cypress-axe axe-core
```

- [ ] **Step 2: Wire in support/e2e.ts**

```ts
// frontend/apps/admin-cms-e2e/src/support/e2e.ts
import 'cypress-axe';
import './commands';
```

- [ ] **Step 3: Smoke test**

```ts
// frontend/apps/admin-cms-e2e/src/e2e/smoke.cy.ts
describe('admin-cms smoke', () => {
  it('app shell renders without critical/serious a11y violations', () => {
    cy.visit('/');
    cy.injectAxe();
    cy.checkA11y(undefined, {
      includedImpacts: ['critical', 'serious'],
    });
  });
});
```

- [ ] **Step 4: Run E2E**

```bash
cd frontend
pnpm nx e2e admin-cms-e2e --headless 2>&1 | tail -10
```

Expected: 1/1 pass.

- [ ] **Step 5: Commit**

```bash
git -c commit.gpgsign=false commit -m "feat(admin-cms-e2e): Cypress + axe-core harness + smoke test (Phase 0.7)"
```

---

## Phase 00 — completion checklist

- [ ] 3 HTTP interceptors registered in app.config (Task 0.1).
- [ ] AuthService + PermissionGuard + `[ccePermission]` directive shipped (Task 0.2).
- [ ] I18nService + DirectionService for RTL/LTR (Task 0.3).
- [ ] ToastService + ConfirmDialogService + ErrorFormatter (Task 0.4).
- [ ] `<cce-shell>` layout + role-gated side nav (Task 0.5).
- [ ] `<cce-paged-table>` shared component (Task 0.6).
- [ ] Cypress + axe-core harness with smoke test passing (Task 0.7).
- [ ] All Jest tests passing for the new core/ folders.
- [ ] Build clean (`pnpm nx build admin-cms`).
- [ ] 7 atomic commits.

**If all boxes ticked, Phase 00 is complete. Proceed to Phase 01 (identity admin).**
