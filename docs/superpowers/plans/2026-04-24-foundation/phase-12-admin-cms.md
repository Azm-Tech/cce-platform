# Phase 12 — admin-cms App Shell + Keycloak Login Flow

> Parent: [../2026-04-24-foundation.md](../2026-04-24-foundation.md) · Spec: [../../specs/2026-04-24-foundation-design.md](../../specs/2026-04-24-foundation-design.md)

**Phase goal:** Wire the admin CMS exactly like web-portal (DGA theme, env, i18n, shell, locale switcher) plus the OIDC code-flow + PKCE login against Keycloak's `cce-internal` realm. After Phase 12, `pnpm nx serve admin-cms` opens at `http://localhost:4201/`, clicking "Sign in" redirects to Keycloak, you log in as `admin@cce.local / Admin123!@`, return to `/profile` and see your decoded JWT claims.

**Tasks in this phase:** 7
**Working directory:** `/Users/m/CCE/`
**Preconditions:** Phase 11 complete; web-portal runnable; Docker stack healthy; Keycloak cce-internal realm has the seeded admin user from Phase 02.

---

## Pre-execution sanity checks

1. `git status` clean.
2. `cd frontend && pnpm nx run-many -t build --projects=admin-cms,web-portal,ui-kit,i18n,auth,contracts 2>&1 | tail -3` → all build.
3. `docker compose ps` — sqlserver/redis/keycloak/maildev/clamav healthy.
4. `curl -s http://localhost:8080/realms/cce-internal/.well-known/openid-configuration | jq -r .issuer` → `http://localhost:8080/realms/cce-internal`.
5. `curl -s -X POST "http://localhost:8080/realms/master/protocol/openid-connect/token" -d "grant_type=password&client_id=admin-cli&username=admin&password=admin" | jq -r 'if .access_token then "ADMIN OK" else "ADMIN FAILED" end'` → `ADMIN OK` (proves the seeded master admin from Phase 02 still works).

If any fail, stop and report.

---

## Task 12.1: Apply DGA theme + Bootstrap grid + env.json to admin-cms

**Files:**
- Modify: `frontend/apps/admin-cms/src/styles.scss`
- Create: `frontend/apps/admin-cms/src/_bootstrap-grid.scss`
- Create: `frontend/apps/admin-cms/src/assets/env.json`
- Modify: `frontend/apps/admin-cms/project.json` (asset globs for src/assets + libs/i18n + libs/ui-kit if needed)

**Rationale:** Mirror Phase 11 Task 11.1 + 11.2 in admin-cms. Different `apiBaseUrl` (Internal API on :5002), different OIDC realm (`cce-internal`).

- [ ] **Step 1: Replace `frontend/apps/admin-cms/src/styles.scss`**

```scss
// CCE admin-cms — global styles. Same theme as web-portal via shared mixin.

@use "../../../libs/ui-kit/src/lib/styles/dga-theme" as theme;

@include theme.cce-theme;

@import "./bootstrap-grid";

html, body {
  margin: 0;
  padding: 0;
  height: 100%;
  background: #fafafa;
}
```

- [ ] **Step 2: Create `frontend/apps/admin-cms/src/_bootstrap-grid.scss`**

Mirror the file Phase 09/11 created for web-portal. Likely contents:

```scss
// Bootstrap 5 — grid + utilities ONLY (per ADR-0003).
// Legacy @import to side-step Sass module isolation that breaks Bootstrap's _variables.
@import "bootstrap/scss/functions";
@import "bootstrap/scss/variables";
@import "bootstrap/scss/maps";
@import "bootstrap/scss/mixins";
@import "bootstrap/scss/utilities";
@import "bootstrap/scss/grid";
@import "bootstrap/scss/utilities/api";
```

- [ ] **Step 3: Create `frontend/apps/admin-cms/src/assets/env.json`**

```json
{
  "environment": "development",
  "apiBaseUrl": "http://localhost:5002",
  "oidcAuthority": "http://localhost:8080/realms/cce-internal",
  "oidcClientId": "cce-admin-cms",
  "sentryDsn": ""
}
```

- [ ] **Step 4: Update `frontend/apps/admin-cms/project.json` assets**

Find `targets.build.options.assets` and ensure it has the same shape Phase 11 used for web-portal (just adapted for admin-cms paths):

```jsonc
"assets": [
  { "glob": "**/*", "input": "apps/admin-cms/public", "output": "/" },
  { "glob": "**/*", "input": "apps/admin-cms/src/assets", "output": "/assets" },
  { "glob": "**/*.json", "input": "libs/i18n/src/lib/i18n", "output": "assets/i18n" }
]
```

If Nx 20 wrote a different shape (e.g., target globs only `public/`), keep its existing entries and APPEND the i18n + assets entries.

- [ ] **Step 5: Build to verify**

```bash
cd frontend
pnpm nx build admin-cms --skip-nx-cache 2>&1 | tail -8
ls dist/apps/admin-cms/assets/i18n/ 2>/dev/null
ls dist/apps/admin-cms/assets/ 2>/dev/null | head
cd ..
```
Expected: build green; `assets/i18n/ar.json`, `assets/i18n/en.json`, `assets/env.json` all present in dist.

- [ ] **Step 6: Commit**

```bash
git add frontend/apps/admin-cms/
git -c commit.gpgsign=false commit -m "feat(phase-12): apply DGA theme + Bootstrap grid + assets/env.json to admin-cms"
```

---

## Task 12.2: Add `EnvService` to admin-cms

**Files:**
- Create: `frontend/apps/admin-cms/src/app/core/env.service.ts`
- Create: `frontend/apps/admin-cms/src/app/core/env.service.spec.ts`

**Rationale:** Foundation seeds a per-app `EnvService` (one in web-portal from Phase 11, one here). Future refactor could move it into a shared lib; for Foundation we keep them per-app to stay simple.

- [ ] **Step 1: Copy the EnvService + tests from web-portal verbatim**

```bash
cp frontend/apps/web-portal/src/app/core/env.service.ts frontend/apps/admin-cms/src/app/core/env.service.ts
cp frontend/apps/web-portal/src/app/core/env.service.spec.ts frontend/apps/admin-cms/src/app/core/env.service.spec.ts
```

The implementation is identical — both apps load `/assets/env.json`. Only the JSON content differs (different apiBaseUrl, different realm).

- [ ] **Step 2: Run tests**

```bash
cd frontend
pnpm nx test admin-cms --watch=false 2>&1 | tail -8
# Expected: 5 passed (2 prior AppComponent + 3 EnvService)
cd ..
```

- [ ] **Step 3: Commit**

```bash
git add frontend/apps/admin-cms/src/app/core
git -c commit.gpgsign=false commit -m "feat(phase-12): add EnvService to admin-cms (3 TDD tests, mirrors web-portal)"
```

---

## Task 12.3: Wire OIDC + ngx-translate in admin-cms `app.config.ts`

**Files:**
- Modify: `frontend/apps/admin-cms/src/app/app.config.ts`
- Create: `frontend/apps/admin-cms/src/app/core/translate-loader.factory.ts`

**Rationale:** Apps consume `buildCceOidcConfig` from `@frontend/auth` to produce the `angular-auth-oidc-client` config. The OIDC provider is added to the app's providers tree alongside ngx-translate.

- [ ] **Step 1: Copy translate-loader factory from web-portal**

```bash
cp frontend/apps/web-portal/src/app/core/translate-loader.factory.ts frontend/apps/admin-cms/src/app/core/translate-loader.factory.ts
```

- [ ] **Step 2: Replace `frontend/apps/admin-cms/src/app/app.config.ts`**

```typescript
import { provideHttpClient, withInterceptorsFromDi, HttpClient } from '@angular/common/http';
import { ApplicationConfig, provideAppInitializer, provideZoneChangeDetection, inject } from '@angular/core';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideRouter } from '@angular/router';
import { TranslateLoader, TranslateModule, TranslateService } from '@ngx-translate/core';
import { provideAuth } from 'angular-auth-oidc-client';
import { firstValueFrom } from 'rxjs';
import { LocaleService } from '@frontend/i18n';
import { buildCceOidcConfig } from '@frontend/auth';
import { appRoutes } from './app.routes';
import { EnvService } from './core/env.service';
import { ngxTranslateHttpLoaderFactory } from './core/translate-loader.factory';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(appRoutes),
    provideHttpClient(withInterceptorsFromDi()),
    provideAnimationsAsync(),
    ...TranslateModule.forRoot({
      loader: {
        provide: TranslateLoader,
        useFactory: ngxTranslateHttpLoaderFactory,
        deps: [HttpClient],
      },
      defaultLanguage: 'ar',
    }).providers ?? [],
    // OIDC config is built dynamically AFTER env.json loads, so provideAuth uses a placeholder
    // here and we re-configure it inside provideAppInitializer once env is available.
    provideAuth({
      config: buildCceOidcConfig({
        authority: 'http://localhost:8080/realms/cce-internal',
        clientId: 'cce-admin-cms',
        redirectUri: typeof window !== 'undefined' ? `${window.location.origin}/auth/callback` : 'http://localhost:4201/auth/callback',
        postLogoutRedirectUri: typeof window !== 'undefined' ? window.location.origin : 'http://localhost:4201',
      }),
    }),
    provideAppInitializer(async () => {
      const env = inject(EnvService);
      await env.load();
      const translate = inject(TranslateService);
      const locale = inject(LocaleService);
      translate.setDefaultLang('ar');
      await firstValueFrom(translate.use(locale.locale()));
    }),
  ],
};
```

Note: angular-auth-oidc-client's `provideAuth` doesn't take an async config in v18 — we use the dev hostnames inline. For production, override via env-driven re-init in a guard or use the lib's runtime-config feature.

- [ ] **Step 3: Build**

```bash
cd frontend
pnpm nx build admin-cms 2>&1 | tail -8
cd ..
```
Expected: build succeeds.

- [ ] **Step 4: Commit**

```bash
git add frontend/apps/admin-cms/src/app
git -c commit.gpgsign=false commit -m "feat(phase-12): wire ngx-translate + OIDC (cce-internal realm) in admin-cms app.config"
```

---

## Task 12.4: Login/logout buttons + `cce-app-shell` in `AppComponent`

**Files:**
- Modify: `frontend/apps/admin-cms/src/app/app.component.ts`
- Modify: `frontend/apps/admin-cms/src/app/app.component.html`
- Modify: `frontend/apps/admin-cms/src/app/app.component.spec.ts`
- Create: `frontend/apps/admin-cms/src/app/locale-switcher/locale-switcher.component.ts`
- Create: `frontend/apps/admin-cms/src/app/locale-switcher/locale-switcher.component.html`
- Create: `frontend/apps/admin-cms/src/app/auth-toolbar/auth-toolbar.component.ts`
- Create: `frontend/apps/admin-cms/src/app/auth-toolbar/auth-toolbar.component.html`
- Create: `frontend/apps/admin-cms/src/app/auth-toolbar/auth-toolbar.component.spec.ts`

- [ ] **Step 1: Copy locale-switcher from web-portal**

```bash
cp -r frontend/apps/web-portal/src/app/locale-switcher frontend/apps/admin-cms/src/app/locale-switcher
```

- [ ] **Step 2: Write the auth-toolbar test (TDD)**

`frontend/apps/admin-cms/src/app/auth-toolbar/auth-toolbar.component.spec.ts`:

```typescript
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { of } from 'rxjs';
import { AuthToolbarComponent } from './auth-toolbar.component';

describe('AuthToolbarComponent', () => {
  let fixture: ComponentFixture<AuthToolbarComponent>;
  let component: AuthToolbarComponent;
  let oidc: jest.Mocked<Pick<OidcSecurityService, 'authorize' | 'logoff'>>;

  beforeEach(async () => {
    oidc = { authorize: jest.fn(), logoff: jest.fn().mockReturnValue(of({})) } as any;
    await TestBed.configureTestingModule({
      imports: [AuthToolbarComponent, TranslateModule.forRoot()],
      providers: [{ provide: OidcSecurityService, useValue: oidc }],
    }).compileComponents();
    fixture = TestBed.createComponent(AuthToolbarComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('signIn() invokes oidc.authorize()', () => {
    component.signIn();
    expect(oidc.authorize).toHaveBeenCalledTimes(1);
  });

  it('signOut() invokes oidc.logoff()', () => {
    component.signOut();
    expect(oidc.logoff).toHaveBeenCalledTimes(1);
  });
});
```

- [ ] **Step 3: Write the auth-toolbar component**

`frontend/apps/admin-cms/src/app/auth-toolbar/auth-toolbar.component.ts`:

```typescript
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { TranslateModule } from '@ngx-translate/core';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';

@Component({
  selector: 'cce-auth-toolbar',
  standalone: true,
  imports: [CommonModule, MatButtonModule, TranslateModule],
  templateUrl: './auth-toolbar.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuthToolbarComponent {
  private readonly oidc = inject(OidcSecurityService);

  // isAuthenticated$ is provided by angular-auth-oidc-client; map to a plain bool signal.
  readonly isAuthenticated = toSignal(
    this.oidc.isAuthenticated$.pipe(map((v) => v.isAuthenticated)),
    { initialValue: false },
  );

  signIn(): void {
    this.oidc.authorize();
  }

  signOut(): void {
    this.oidc.logoff().subscribe();
  }
}
```

`frontend/apps/admin-cms/src/app/auth-toolbar/auth-toolbar.component.html`:

```html
@if (isAuthenticated()) {
  <button type="button" mat-button shellHeaderEnd (click)="signOut()">
    {{ "common.actions.signOut" | translate }}
  </button>
} @else {
  <button type="button" mat-raised-button color="accent" shellHeaderEnd (click)="signIn()">
    {{ "common.actions.signIn" | translate }}
  </button>
}
```

- [ ] **Step 4: Replace `frontend/apps/admin-cms/src/app/app.component.ts`**

```typescript
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { AppShellComponent } from '@frontend/ui-kit';
import { LocaleSwitcherComponent } from './locale-switcher/locale-switcher.component';
import { AuthToolbarComponent } from './auth-toolbar/auth-toolbar.component';

@Component({
  selector: 'cce-root',
  standalone: true,
  imports: [RouterOutlet, AppShellComponent, LocaleSwitcherComponent, AuthToolbarComponent, TranslateModule],
  templateUrl: './app.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppComponent {
  private readonly translate = inject(TranslateService);
  readonly title = this.translate.instant('common.appName') || 'CCE Admin';
}
```

`frontend/apps/admin-cms/src/app/app.component.html`:

```html
<cce-app-shell [appTitle]="title">
  <cce-locale-switcher shellHeaderEnd />
  <cce-auth-toolbar shellHeaderEnd />

  <router-outlet />
</cce-app-shell>
```

- [ ] **Step 5: Update `app.component.spec.ts`**

```typescript
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { of } from 'rxjs';
import { AppComponent } from './app.component';

describe('AppComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AppComponent, TranslateModule.forRoot()],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: OidcSecurityService,
          useValue: {
            isAuthenticated$: of({ isAuthenticated: false }),
            authorize: jest.fn(),
            logoff: jest.fn().mockReturnValue(of({})),
          },
        },
      ],
    }).compileComponents();
  });

  it('renders the cce-app-shell', () => {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('cce-app-shell')).toBeTruthy();
  });

  it('renders the cce-auth-toolbar', () => {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('cce-auth-toolbar')).toBeTruthy();
  });
});
```

Also update `frontend/apps/admin-cms/src/index.html` to use `<cce-root></cce-root>` (matching the new selector).

- [ ] **Step 6: Run tests + commit**

```bash
cd frontend
pnpm nx test admin-cms --watch=false 2>&1 | tail -8
# Expected: 9 passed (3 EnvService + 2 LocaleSwitcher + 2 AuthToolbar + 2 AppComponent)
cd ..
git add frontend/apps/admin-cms
git -c commit.gpgsign=false commit -m "feat(phase-12): replace admin-cms shell with cce-app-shell + locale switcher + sign-in/sign-out toolbar (4 TDD tests)"
```

---

## Task 12.5: Add `/profile` page showing claims after login

**Files:**
- Create: `frontend/apps/admin-cms/src/app/profile/profile.page.ts`
- Create: `frontend/apps/admin-cms/src/app/profile/profile.page.html`
- Create: `frontend/apps/admin-cms/src/app/profile/profile.page.spec.ts`
- Modify: `frontend/apps/admin-cms/src/app/app.routes.ts`

**Rationale:** Foundation's only authenticated page in admin-cms. Reads `userData$` from `OidcSecurityService` and renders the claims. Phase 13+ adds real CRUD pages; this is the auth-flow proof.

- [ ] **Step 1: Write the failing test**

```typescript
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { of } from 'rxjs';
import { ProfilePage } from './profile.page';

describe('ProfilePage', () => {
  let fixture: ComponentFixture<ProfilePage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProfilePage, TranslateModule.forRoot()],
      providers: [
        {
          provide: OidcSecurityService,
          useValue: {
            userData$: of({
              userData: {
                preferred_username: 'admin@cce.local',
                email: 'admin@cce.local',
                upn: 'admin@cce.local',
                groups: ['SuperAdmin'],
              },
            }),
          },
        },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(ProfilePage);
    fixture.detectChanges();
  });

  it('renders preferred_username from userData', () => {
    expect(fixture.nativeElement.textContent).toContain('admin@cce.local');
  });

  it('renders SuperAdmin group', () => {
    expect(fixture.nativeElement.textContent).toContain('SuperAdmin');
  });
});
```

- [ ] **Step 2: Write the page**

```typescript
import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { TranslateModule } from '@ngx-translate/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { map } from 'rxjs';

@Component({
  selector: 'cce-profile-page',
  standalone: true,
  imports: [CommonModule, MatCardModule, TranslateModule],
  templateUrl: './profile.page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProfilePage {
  private readonly oidc = inject(OidcSecurityService);

  readonly userData = toSignal(
    this.oidc.userData$.pipe(map((u) => u?.userData ?? null)),
    { initialValue: null },
  );

  readonly preferredUsername = computed(() => this.userData()?.preferred_username ?? '');
  readonly email = computed(() => this.userData()?.email ?? '');
  readonly upn = computed(() => this.userData()?.upn ?? '');
  readonly groups = computed<string[]>(() => {
    const g = this.userData()?.groups;
    return Array.isArray(g) ? g : [];
  });
}
```

```html
<mat-card>
  <mat-card-header>
    <mat-card-title>Profile</mat-card-title>
  </mat-card-header>
  <mat-card-content>
    <dl>
      <dt>preferred_username</dt><dd>{{ preferredUsername() }}</dd>
      <dt>email</dt><dd>{{ email() }}</dd>
      <dt>upn</dt><dd>{{ upn() }}</dd>
      <dt>groups</dt>
      <dd>
        <ul>
          @for (g of groups(); track g) {
            <li>{{ g }}</li>
          }
        </ul>
      </dd>
    </dl>
  </mat-card-content>
</mat-card>
```

- [ ] **Step 3: Update `frontend/apps/admin-cms/src/app/app.routes.ts`**

```typescript
import { Route } from '@angular/router';
import { autoLoginPartialRoutesGuard } from 'angular-auth-oidc-client';
import { ProfilePage } from './profile/profile.page';

export const appRoutes: Route[] = [
  { path: '', pathMatch: 'full', redirectTo: 'profile' },
  { path: 'profile', component: ProfilePage, canActivate: [autoLoginPartialRoutesGuard], title: 'CCE — Profile' },
];
```

- [ ] **Step 4: Run + commit**

```bash
cd frontend
pnpm nx test admin-cms --watch=false 2>&1 | tail -8
# Expected: 11 passed (9 prior + 2 ProfilePage)
cd ..
git add frontend/apps/admin-cms
git -c commit.gpgsign=false commit -m "feat(phase-12): add /profile page rendering claims with autoLoginPartialRoutesGuard (2 TDD tests)"
```

---

## Task 12.6: ESLint a11y verification on the admin-cms shell

**Files:** None — verification only.

- [ ] **Step 1: Run lint with a11y rules**

```bash
cd frontend
pnpm nx lint admin-cms 2>&1 | tail -10
cd ..
```
Expected: 0 warnings/errors. The new templates use `mat-button`, `dl/dt/dd`, and `<ul>` which conform to the `@angular-eslint/template/*` a11y rules wired in Phase 09.

If anything fires (e.g., `interactive-supports-focus`), fix the offending template (it's our scaffold) before continuing.

- [ ] **Step 2: (No commit — verification only)**

---

## Task 12.7: End-to-end smoke including a real Keycloak login round-trip

**Files:** None — manual smoke.

**Rationale:** Final proof admin-cms works against real Keycloak. The dev server stays up while we drive a programmatic check.

- [ ] **Step 1: Run admin-cms dev server in the background**

```bash
cd frontend
pnpm nx serve admin-cms --port=4201 > /tmp/admin-cms.log 2>&1 &
DEV_PID=$!
cd ..

# Wait for dev server
for i in $(seq 1 20); do
  if curl -s -o /dev/null -w "%{http_code}\n" http://localhost:4201/ 2>/dev/null | grep -q 200; then
    break
  fi
  sleep 2
done
```

- [ ] **Step 2: Hit each endpoint and confirm 200**

```bash
echo "Root:"        ; curl -s -o /dev/null -w "%{http_code}\n" http://localhost:4201/
echo "env.json:"    ; curl -s -o /dev/null -w "%{http_code}\n" http://localhost:4201/assets/env.json
echo "ar.json:"     ; curl -s -o /dev/null -w "%{http_code}\n" http://localhost:4201/assets/i18n/ar.json
echo "en.json:"     ; curl -s -o /dev/null -w "%{http_code}\n" http://localhost:4201/assets/i18n/en.json
```
Expected: each prints `200`.

- [ ] **Step 3: Confirm the realm's authorization endpoint returns the Keycloak login HTML when the redirect URL matches a registered client redirect URI**

```bash
curl -s -o /dev/null -w "%{http_code}\n" \
  "http://localhost:8080/realms/cce-internal/protocol/openid-connect/auth?client_id=cce-admin-cms&response_type=code&redirect_uri=http%3A%2F%2Flocalhost%3A4201%2Fauth%2Fcallback&scope=openid+profile&state=test"
```
Expected: `200` (Keycloak renders its login form). If it's `400` or shows "invalid_redirect_uri", the realm config from Phase 02 has a problem — STOP and report.

- [ ] **Step 4: Stop the dev server**

```bash
kill $DEV_PID 2>/dev/null
wait $DEV_PID 2>/dev/null
```

- [ ] **Step 5: Smoke summary**

If all checks pass, manual login round-trip works in a real browser:
1. Open `http://localhost:4201/` in a browser.
2. App redirects to Keycloak login.
3. Enter `admin@cce.local` / `Admin123!@`.
4. Browser returns to `http://localhost:4201/auth/callback?code=...` then to `/profile`.
5. Profile page shows preferred_username, email, upn, groups containing `SuperAdmin`.

Document this in the phase report.

- [ ] **Step 6: (No commit — verification only)**

---

## Phase 12 — completion checklist

- [ ] admin-cms styles use shared DGA theme.
- [ ] admin-cms env.json points at Internal API + cce-internal realm.
- [ ] admin-cms `EnvService` loads via APP_INITIALIZER.
- [ ] admin-cms ngx-translate wired with HTTP loader; assets bundle translations.
- [ ] admin-cms `app.config.ts` has `provideAuth` with `buildCceOidcConfig`.
- [ ] `AppComponent` uses `cce-app-shell` + locale switcher + auth toolbar.
- [ ] `AuthToolbarComponent` calls `oidc.authorize()` on signIn, `oidc.logoff()` on signOut.
- [ ] `/profile` page exists, gated by `autoLoginPartialRoutesGuard`, renders claims.
- [ ] ESLint a11y rules clean on all new templates.
- [ ] Dev server smoke: 4 URLs return 200.
- [ ] Keycloak authorization endpoint returns 200 for the cce-admin-cms client redirect URI.
- [ ] `git status` clean.
- [ ] ~5 new commits.

**If all boxes ticked, phase 12 is complete. Proceed to phase 13 (OpenAPI contract pipeline).**
