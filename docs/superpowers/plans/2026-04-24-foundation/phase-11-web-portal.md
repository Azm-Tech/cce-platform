# Phase 11 — web-portal App Shell

> Parent: [../2026-04-24-foundation.md](../2026-04-24-foundation.md) · Spec: [../../specs/2026-04-24-foundation-design.md](../../specs/2026-04-24-foundation-design.md)

**Phase goal:** Replace the Nx-generated welcome stub in `web-portal` with a real shell using `cce-app-shell`, wire ngx-translate against the i18n lib's translations, add a runtime-loaded `env.json`, and ship one `/health` page that actually calls the External API. After Phase 11, `pnpm nx serve web-portal` opens a browser at `http://localhost:4200/` showing a Material toolbar with locale switcher and a navigable health page.

**Tasks in this phase:** 6
**Working directory:** `/Users/m/CCE/`
**Preconditions:** Phase 10 complete; backend services healthy (`docker compose ps` shows 5 healthy); `cd frontend && pnpm nx run-many -t build,lint,test` all green.

---

## Pre-execution sanity checks

1. `git status` clean.
2. `cd frontend && pnpm nx run-many -t build --projects=web-portal,ui-kit,i18n,auth,contracts 2>&1 | tail -5` → all build.
3. `docker compose ps` — sqlserver, redis, keycloak, maildev, clamav all healthy.
4. External API runnable: `curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5001/health 2>&1` — start API in background if not running. (We'll need this for manual smoke in Task 11.6 but tests use mocked HTTP.)
5. Path-mapping prefix from Phase 10 is `@frontend/...`.

---

## Task 11.1: Apply DGA theme to web-portal SCSS

**Files:**

- Modify: `frontend/apps/web-portal/src/styles.scss`

**Rationale:** Phase 09 set up a placeholder Material theme inline in `styles.scss`. Replace it with a `@use` of `@frontend/ui-kit/styles/dga-theme` (the mixin from Phase 10). This locks the visual look across both apps via a single token source.

- [ ] **Step 1: Replace `frontend/apps/web-portal/src/styles.scss` content**

```scss
// CCE web-portal — global styles
// Theme + Bootstrap grid imports use the shared ui-kit lib.

// SCSS path is relative to the app's project root, so reach into libs/ui-kit/src/lib/styles.
// Nx's tsconfig path mappings DON'T apply to SCSS — Sass uses the `loadPaths` config from
// project.json. Phase 09 already set loadPaths to include node_modules; we add libs path here.
@use "../../../libs/ui-kit/src/lib/styles/dga-theme" as theme;

@include theme.cce-theme;

// Bootstrap 5 — grid + utilities ONLY (per ADR-0003).
// Per Phase 09 deviation, we use legacy @import inside a partial to side-step Sass module isolation.
@import "../../../libs/ui-kit/src/lib/styles/bootstrap-grid";

// Document-level resets
html,
body {
  margin: 0;
  padding: 0;
  height: 100%;
  background: #fafafa;
}
```

This assumes Phase 09 created `libs/ui-kit/src/lib/styles/bootstrap-grid.scss` with the legacy-@import workaround. If it lives at a different path, adjust.

- [ ] **Step 2: Verify build**

```bash
cd frontend
pnpm nx build web-portal --skip-nx-cache 2>&1 | tail -8
cd ..
```

Expected: build succeeds. CSS bundle size check optional — Material + grid utilities should be under 200 KB.

- [ ] **Step 3: Commit**

```bash
git add frontend/apps/web-portal/src/styles.scss
git -c commit.gpgsign=false commit -m "feat(phase-11): apply DGA Material theme + Bootstrap grid to web-portal global styles"
```

---

## Task 11.2: Add `/assets/env.json` + `EnvService` runtime loader

**Files:**

- Create: `frontend/apps/web-portal/src/assets/env.json`
- Create: `frontend/apps/web-portal/src/app/core/env.service.ts`
- Create: `frontend/apps/web-portal/src/app/core/env.service.spec.ts`

**Rationale:** Per ADR (Phase 10 contracts lib), runtime config lives in `/assets/env.json`. Same compiled bundle deploys to dev/staging/prod by swapping the file. Apps load it at bootstrap before anything else.

- [ ] **Step 1: Write `frontend/apps/web-portal/src/assets/env.json`**

```json
{
  "environment": "development",
  "apiBaseUrl": "http://localhost:5001",
  "oidcAuthority": "http://localhost:8080/realms/cce-external",
  "oidcClientId": "cce-web-portal",
  "sentryDsn": ""
}
```

- [ ] **Step 2: Write the failing test**

`frontend/apps/web-portal/src/app/core/env.service.spec.ts`:

```typescript
import { provideHttpClient } from "@angular/common/http";
import { provideHttpClientTesting, HttpTestingController } from "@angular/common/http/testing";
import { TestBed } from "@angular/core/testing";
import type { CceEnv } from "@frontend/contracts";
import { EnvService } from "./env.service";

describe("EnvService", () => {
  let service: EnvService;
  let httpMock: HttpTestingController;

  const fixture: CceEnv = {
    environment: "test",
    apiBaseUrl: "http://api.test",
    oidcAuthority: "http://oidc.test",
    oidcClientId: "test-client",
    sentryDsn: "",
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), EnvService],
    });
    service = TestBed.inject(EnvService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it("throws when accessed before load", () => {
    expect(() => service.env).toThrow();
  });

  it("loads env.json once and exposes the typed config", async () => {
    const promise = service.load();
    httpMock.expectOne("/assets/env.json").flush(fixture);
    await promise;

    expect(service.env).toEqual(fixture);
  });

  it("rejects on HTTP error", async () => {
    const promise = service.load();
    httpMock.expectOne("/assets/env.json").error(new ProgressEvent("error"), { status: 500, statusText: "fail" });

    await expect(promise).rejects.toThrow();
  });
});
```

- [ ] **Step 3: Run — expect compile error**

```bash
cd frontend
pnpm nx test web-portal --watch=false 2>&1 | tail -8
cd ..
```

- [ ] **Step 4: Write `frontend/apps/web-portal/src/app/core/env.service.ts`**

```typescript
import { HttpClient } from "@angular/common/http";
import { Injectable, inject } from "@angular/core";
import type { CceEnv } from "@frontend/contracts";
import { firstValueFrom } from "rxjs";

/**
 * Loads /assets/env.json once at app bootstrap so runtime config is available
 * before any other service needs it. Apps call <code>load()</code> from an
 * APP_INITIALIZER provider; consumers read <code>env</code> synchronously
 * thereafter.
 */
@Injectable({ providedIn: "root" })
export class EnvService {
  private readonly http = inject(HttpClient);
  private cached: CceEnv | null = null;

  async load(): Promise<void> {
    this.cached = await firstValueFrom(this.http.get<CceEnv>("/assets/env.json"));
  }

  get env(): CceEnv {
    if (!this.cached) {
      throw new Error("EnvService.env accessed before load() resolved.");
    }
    return this.cached;
  }
}
```

- [ ] **Step 5: Run + commit**

```bash
cd frontend
pnpm nx test web-portal --watch=false 2>&1 | tail -8
# Expected: 5 passed (2 prior AppComponent + 3 EnvService)
cd ..
git add frontend/apps/web-portal/src/assets/env.json frontend/apps/web-portal/src/app/core
git -c commit.gpgsign=false commit -m "feat(phase-11): add /assets/env.json + EnvService runtime config loader (3 TDD tests)"
```

---

## Task 11.3: Wire ngx-translate with HTTP loader pointed at the i18n lib's JSON files

**Files:**

- Modify: `frontend/apps/web-portal/project.json` (add asset glob for translations)
- Modify: `frontend/apps/web-portal/src/app/app.config.ts` (provideTranslateService)
- Create: `frontend/apps/web-portal/src/app/core/translate-loader.factory.ts`

**Rationale:** Translations live in `libs/i18n/src/lib/i18n/{ar,en}.json`. Build-time asset config copies them to `/assets/i18n/` so the runtime HTTP loader can fetch them from the deployed app.

- [ ] **Step 1: Add asset glob to `frontend/apps/web-portal/project.json`**

Find the `targets.build.options.assets` array. Add:

```jsonc
{
  "glob": "**/*.json",
  "input": "libs/i18n/src/lib/i18n",
  "output": "assets/i18n"
}
```

The path is relative to the **workspace root** (`frontend/`), not the app. This block makes Nx copy `libs/i18n/src/lib/i18n/*.json` → `dist/apps/web-portal/assets/i18n/` at build, and serve from `/assets/i18n/` at dev time.

- [ ] **Step 2: Write the http-loader factory**

`frontend/apps/web-portal/src/app/core/translate-loader.factory.ts`:

```typescript
import type { HttpClient } from "@angular/common/http";
import { TranslateHttpLoader } from "@ngx-translate/http-loader";

export function ngxTranslateHttpLoaderFactory(http: HttpClient): TranslateHttpLoader {
  return new TranslateHttpLoader(http, "/assets/i18n/", ".json");
}
```

- [ ] **Step 3: Update `frontend/apps/web-portal/src/app/app.config.ts`**

Replace the existing `app.config.ts` with one that wires translate, http, env loading:

```typescript
import { provideHttpClient, withInterceptorsFromDi, HttpClient } from "@angular/common/http";
import { ApplicationConfig, provideAppInitializer, provideZoneChangeDetection, inject } from "@angular/core";
import { provideRouter } from "@angular/router";
import { TranslateLoader, TranslateModule } from "@ngx-translate/core";
import { LocaleService } from "@frontend/i18n";
import { firstValueFrom } from "rxjs";
import { appRoutes } from "./app.routes";
import { EnvService } from "./core/env.service";
import { ngxTranslateHttpLoaderFactory } from "./core/translate-loader.factory";
import { TranslateService } from "@ngx-translate/core";

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(appRoutes),
    provideHttpClient(withInterceptorsFromDi()),
    ...(TranslateModule.forRoot({
      loader: {
        provide: TranslateLoader,
        useFactory: ngxTranslateHttpLoaderFactory,
        deps: [HttpClient],
      },
      defaultLanguage: "ar",
    }).providers ?? []),
    provideAppInitializer(async () => {
      const env = inject(EnvService);
      await env.load();
      const translate = inject(TranslateService);
      const locale = inject(LocaleService);
      translate.setDefaultLang("ar");
      await firstValueFrom(translate.use(locale.locale()));
    }),
  ],
};
```

- [ ] **Step 4: Verify build**

```bash
cd frontend
pnpm nx build web-portal --skip-nx-cache 2>&1 | tail -8
cd ..
```

Expected: build succeeds. `dist/apps/web-portal/assets/i18n/ar.json` and `en.json` should now exist.

```bash
ls frontend/dist/apps/web-portal/assets/i18n/ 2>/dev/null
```

Expected: `ar.json en.json`.

- [ ] **Step 5: Commit**

```bash
git add frontend/apps/web-portal/
git -c commit.gpgsign=false commit -m "feat(phase-11): wire ngx-translate with HTTP loader + APP_INITIALIZER for env + locale bootstrap"
```

---

## Task 11.4: Replace `AppComponent` with `cce-app-shell` + locale switcher

**Files:**

- Modify: `frontend/apps/web-portal/src/app/app.component.ts`
- Modify: `frontend/apps/web-portal/src/app/app.component.html`
- Modify: `frontend/apps/web-portal/src/app/app.component.scss`
- Modify: `frontend/apps/web-portal/src/app/app.component.spec.ts`
- Create: `frontend/apps/web-portal/src/app/locale-switcher/locale-switcher.component.ts`
- Create: `frontend/apps/web-portal/src/app/locale-switcher/locale-switcher.component.html`
- Create: `frontend/apps/web-portal/src/app/locale-switcher/locale-switcher.component.spec.ts`

- [ ] **Step 1: Write the locale-switcher tests first (TDD)**

```typescript
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { TranslateService } from "@ngx-translate/core";
import { LocaleService } from "@frontend/i18n";
import { LocaleSwitcherComponent } from "./locale-switcher.component";

describe("LocaleSwitcherComponent", () => {
  let component: LocaleSwitcherComponent;
  let fixture: ComponentFixture<LocaleSwitcherComponent>;
  let locale: LocaleService;
  let translate: TranslateService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LocaleSwitcherComponent],
      providers: [LocaleService, { provide: TranslateService, useValue: { use: jest.fn(() => ({ subscribe: (cb: () => void) => cb() })) } }],
    }).compileComponents();

    locale = TestBed.inject(LocaleService);
    translate = TestBed.inject(TranslateService);
    fixture = TestBed.createComponent(LocaleSwitcherComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it("switches locale + invokes translate.use on click", () => {
    component.toggle();

    expect(translate.use).toHaveBeenCalledWith("en");
    expect(locale.locale()).toBe("en");
  });

  it("toggles back to ar after en", () => {
    component.toggle();
    component.toggle();

    expect(locale.locale()).toBe("ar");
  });
});
```

- [ ] **Step 2: Write the component**

`frontend/apps/web-portal/src/app/locale-switcher/locale-switcher.component.ts`:

```typescript
import { ChangeDetectionStrategy, Component, computed, inject } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import { TranslateModule, TranslateService } from "@ngx-translate/core";
import { LocaleService, type SupportedLocale } from "@frontend/i18n";

@Component({
  selector: "cce-locale-switcher",
  standalone: true,
  imports: [MatButtonModule, TranslateModule],
  templateUrl: "./locale-switcher.component.html",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LocaleSwitcherComponent {
  private readonly localeService = inject(LocaleService);
  private readonly translate = inject(TranslateService);

  readonly current = this.localeService.locale;
  readonly nextLabel = computed<SupportedLocale>(() => (this.current() === "ar" ? "en" : "ar"));

  toggle(): void {
    const next: SupportedLocale = this.nextLabel();
    this.localeService.setLocale(next);
    this.translate.use(next);
  }
}
```

`frontend/apps/web-portal/src/app/locale-switcher/locale-switcher.component.html`:

```html
<button type="button" mat-button shellHeaderEnd (click)="toggle()" [attr.aria-label]="('common.locale.switchTo' | translate) + ' ' + ('common.locale.' + nextLabel() | translate)">{{ "common.locale." + nextLabel() | translate }}</button>
```

- [ ] **Step 3: Replace `app.component.ts` to use the shell**

```typescript
import { ChangeDetectionStrategy, Component, inject } from "@angular/core";
import { RouterOutlet } from "@angular/router";
import { TranslateModule, TranslateService } from "@ngx-translate/core";
import { AppShellComponent } from "@frontend/ui-kit";
import { LocaleSwitcherComponent } from "./locale-switcher/locale-switcher.component";

@Component({
  selector: "cce-root",
  standalone: true,
  imports: [RouterOutlet, AppShellComponent, LocaleSwitcherComponent, TranslateModule],
  templateUrl: "./app.component.html",
  styleUrl: "./app.component.scss",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppComponent {
  private readonly translate = inject(TranslateService);
  readonly title = this.translate.instant("common.appName") || "CCE";
}
```

- [ ] **Step 4: Replace `app.component.html`**

```html
<cce-app-shell [appTitle]="title">
  <cce-locale-switcher shellHeaderEnd />

  <router-outlet />
</cce-app-shell>
```

- [ ] **Step 5: Replace `app.component.scss` (empty for now)**

```scss
:host {
  display: block;
}
```

- [ ] **Step 6: Update `app.component.spec.ts` to reflect new shell**

The Nx-generated default test asserts the title text. Update to check that the shell + locale switcher render.

```typescript
import { TestBed } from "@angular/core/testing";
import { provideHttpClient } from "@angular/common/http";
import { provideHttpClientTesting } from "@angular/common/http/testing";
import { provideRouter } from "@angular/router";
import { TranslateModule } from "@ngx-translate/core";
import { AppComponent } from "./app.component";

describe("AppComponent", () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AppComponent, TranslateModule.forRoot()],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();
  });

  it("creates the app", () => {
    const fixture = TestBed.createComponent(AppComponent);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it("renders the cce-app-shell", () => {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    const shell = fixture.nativeElement.querySelector("cce-app-shell");
    expect(shell).toBeTruthy();
  });

  it("renders the cce-locale-switcher in the header end slot", () => {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    const switcher = fixture.nativeElement.querySelector("cce-locale-switcher");
    expect(switcher).toBeTruthy();
  });
});
```

- [ ] **Step 7: Run + commit**

```bash
cd frontend
pnpm nx test web-portal --watch=false 2>&1 | tail -8
# Expected: 8 passed (3 EnvService + 2 LocaleSwitcher + 3 AppComponent)
cd ..
git add frontend/apps/web-portal/src/app
git -c commit.gpgsign=false commit -m "feat(phase-11): replace web-portal AppComponent with cce-app-shell + locale switcher (5 TDD tests)"
```

---

## Task 11.5: Add `HealthClient` service + `/health` page

**Files:**

- Create: `frontend/apps/web-portal/src/app/health/health.client.ts`
- Create: `frontend/apps/web-portal/src/app/health/health.client.spec.ts`
- Create: `frontend/apps/web-portal/src/app/health/health.page.ts`
- Create: `frontend/apps/web-portal/src/app/health/health.page.html`
- Modify: `frontend/apps/web-portal/src/app/app.routes.ts`

**Rationale:** A reachable page that proves the wire-up: route, component, HTTP client, env config, translate keys, RTL behavior. The page calls External API's `GET /health` (set up in Phase 08) and renders the response.

- [ ] **Step 1: Write the failing test for HealthClient**

```typescript
import { provideHttpClient } from "@angular/common/http";
import { provideHttpClientTesting, HttpTestingController } from "@angular/common/http/testing";
import { TestBed } from "@angular/core/testing";
import { firstValueFrom } from "rxjs";
import type { CceEnv } from "@frontend/contracts";
import { EnvService } from "../core/env.service";
import { HealthClient } from "./health.client";

describe("HealthClient", () => {
  let client: HealthClient;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: EnvService,
          useValue: {
            env: {
              environment: "test",
              apiBaseUrl: "http://api.test",
              oidcAuthority: "",
              oidcClientId: "",
              sentryDsn: "",
            } satisfies CceEnv,
          },
        },
        HealthClient,
      ],
    });
    client = TestBed.inject(HealthClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it("GETs apiBaseUrl + /health", async () => {
    const promise = firstValueFrom(client.fetch());
    const req = httpMock.expectOne("http://api.test/health");
    expect(req.request.method).toBe("GET");
    req.flush({ status: "ok", version: "0.1.0", locale: "ar", utcNow: "2026-04-25T00:00:00Z" });

    const result = await promise;
    expect(result.status).toBe("ok");
  });
});
```

- [ ] **Step 2: Write the client**

```typescript
import { HttpClient } from "@angular/common/http";
import { Injectable, inject } from "@angular/core";
import { Observable } from "rxjs";
import { EnvService } from "../core/env.service";

export interface HealthResponse {
  status: string;
  version: string;
  locale: string;
  utcNow: string;
}

@Injectable({ providedIn: "root" })
export class HealthClient {
  private readonly http = inject(HttpClient);
  private readonly env = inject(EnvService);

  fetch(): Observable<HealthResponse> {
    return this.http.get<HealthResponse>(`${this.env.env.apiBaseUrl}/health`);
  }
}
```

- [ ] **Step 3: Write the page component**

`frontend/apps/web-portal/src/app/health/health.page.ts`:

```typescript
import { ChangeDetectionStrategy, Component, inject, signal } from "@angular/core";
import { CommonModule } from "@angular/common";
import { MatCardModule } from "@angular/material/card";
import { MatButtonModule } from "@angular/material/button";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { TranslateModule } from "@ngx-translate/core";
import { HealthClient, type HealthResponse } from "./health.client";

@Component({
  selector: "cce-health-page",
  standalone: true,
  imports: [CommonModule, MatCardModule, MatButtonModule, MatProgressSpinnerModule, TranslateModule],
  templateUrl: "./health.page.html",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HealthPage {
  private readonly client = inject(HealthClient);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly data = signal<HealthResponse | null>(null);

  refresh(): void {
    this.loading.set(true);
    this.error.set(null);
    this.client.fetch().subscribe({
      next: (resp) => {
        this.data.set(resp);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err?.message ?? "unknown error");
        this.loading.set(false);
      },
    });
  }

  ngOnInit(): void {
    this.refresh();
  }
}
```

`frontend/apps/web-portal/src/app/health/health.page.html`:

```html
<mat-card class="health-card">
  <mat-card-header>
    <mat-card-title>{{ "health.title" | translate }}</mat-card-title>
  </mat-card-header>
  <mat-card-content>
    @if (loading()) {
    <mat-spinner diameter="32"></mat-spinner>
    <span>{{ "common.loading" | translate }}</span>
    } @else if (error(); as err) {
    <p role="alert">{{ "common.error.serverError" | translate }}: {{ err }}</p>
    } @else if (data(); as h) {
    <dl>
      <dt>{{ "health.status" | translate }}</dt>
      <dd>{{ h.status }}</dd>
      <dt>{{ "health.version" | translate }}</dt>
      <dd>{{ h.version }}</dd>
    </dl>
    }
  </mat-card-content>
  <mat-card-actions>
    <button type="button" mat-button (click)="refresh()" [disabled]="loading()">{{ "common.actions.retry" | translate }}</button>
  </mat-card-actions>
</mat-card>
```

- [ ] **Step 4: Update `frontend/apps/web-portal/src/app/app.routes.ts`**

```typescript
import { Route } from "@angular/router";
import { HealthPage } from "./health/health.page";

export const appRoutes: Route[] = [
  { path: "", pathMatch: "full", redirectTo: "health" },
  { path: "health", component: HealthPage, title: "CCE — Health" },
];
```

- [ ] **Step 5: Run + commit**

```bash
cd frontend
pnpm nx test web-portal --watch=false 2>&1 | tail -8
# Expected: 9 passed (8 prior + 1 HealthClient)
cd ..
git add frontend/apps/web-portal/src/app
git -c commit.gpgsign=false commit -m "feat(phase-11): add /health page + HealthClient calling External API (1 TDD test)"
```

---

## Task 11.6: Final smoke (build, lint, test) + manual dev-server verification

**Files:** None (verification + smoke).

- [ ] **Step 1: Run all targets**

```bash
cd frontend
pnpm nx run-many -t build,lint,test --projects=web-portal --watch=false 2>&1 | tail -15
cd ..
```

Expected: 3 green runs. Test count: 9.

- [ ] **Step 2: Manual dev server smoke (optional but recommended)**

Start the External API (if not running) and the dev server:

```bash
# Backend in one shell
cd backend
dotnet run --project src/CCE.Api.External --urls http://localhost:5001 > /tmp/api-external.log 2>&1 &
API_PID=$!
sleep 3
curl -s http://localhost:5001/health | head -1
cd ..

# Frontend dev server
cd frontend
pnpm nx serve web-portal --port=4200 > /tmp/web-portal.log 2>&1 &
FE_PID=$!
sleep 8
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:4200/
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:4200/health
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:4200/assets/env.json
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:4200/assets/i18n/ar.json
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:4200/assets/i18n/en.json
cd ..

# Cleanup
kill $FE_PID 2>/dev/null; kill $API_PID 2>/dev/null
wait $FE_PID 2>/dev/null; wait $API_PID 2>/dev/null
```

Expected: all HTTP responses are `200`. Failures here are smoke regressions — STOP and inspect logs.

- [ ] **Step 3: (Verification only — no commit)**

---

## Phase 11 — completion checklist

- [ ] `web-portal` styles use `@frontend/ui-kit/styles/dga-theme`.
- [ ] `/assets/env.json` exists; `EnvService` loads it via APP_INITIALIZER.
- [ ] ngx-translate wired with HTTP loader; build copies `libs/i18n/.../*.json` → `dist/.../assets/i18n/`.
- [ ] `AppComponent` uses `cce-app-shell` with locale switcher in header end slot.
- [ ] `LocaleSwitcherComponent` toggles ar↔en + invokes `TranslateService.use`.
- [ ] `/health` route renders `HealthPage` calling External API.
- [ ] `pnpm nx run-many -t build,lint,test --projects=web-portal` green.
- [ ] Test count ≥ 9 in web-portal.
- [ ] Dev server serves `/`, `/health`, `/assets/env.json`, `/assets/i18n/ar.json`, `/assets/i18n/en.json` all `200`.
- [ ] `git status` clean.
- [ ] ~5 new commits.

**If all boxes ticked, phase 11 is complete. Proceed to phase 12 (admin-cms shell + Keycloak login).**
