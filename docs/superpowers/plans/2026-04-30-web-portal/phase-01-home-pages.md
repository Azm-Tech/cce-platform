# Phase 01 — Home + static pages

> Parent: [`../2026-04-30-web-portal.md`](../2026-04-30-web-portal.md) · Spec: [`../../specs/2026-04-30-web-portal-design.md`](../../specs/2026-04-30-web-portal-design.md) §4 (Home flow), §5 (`/api/homepage-sections`, `/api/pages/{slug}`)

**Phase goal:** Wire the home page (driven by `/api/homepage-sections`) and the static-page renderer (`/api/pages/{slug}` for About / Privacy / Terms). Both are anonymous-readable. After Phase 01, opening `localhost:4200` shows the homepage chrome with hero/featured sections; footer links resolve to working pages.

**Tasks:** 4
**Working directory:** `/Users/m/CCE/`
**Preconditions:**
- Phase 00 closed (`ffc1127`).
- Layout shell, AuthService, interceptors, dev proxy all in place.

---

## Endpoint coverage (External API → frontend)

| Endpoint | Method | Phase 01 surface | Anonymous |
|---|---|---|---|
| `/api/homepage-sections` | GET | Task 1.1 (HomePage) | ✓ |
| `/api/pages/{slug}` | GET | Task 1.2 (StaticPage) | ✓ |

## Hand-defined DTOs

The External API generated client emits `unknown` response types (Sub-3/4 didn't declare `Produces<T>()`). Mirror the backend records:

```ts
// frontend/apps/web-portal/src/app/features/home/home.types.ts
export type HomepageSectionType = 'Hero' | 'FeaturedNews' | 'FeaturedResources' | 'UpcomingEvents';

export interface HomepageSection {
  id: string;
  sectionType: HomepageSectionType;
  orderIndex: number;
  contentAr: string;
  contentEn: string;
  isActive: boolean;
}
```

```ts
// frontend/apps/web-portal/src/app/features/pages/page.types.ts
export type PageType = 'AboutPlatform' | 'TermsOfService' | 'PrivacyPolicy' | 'Custom';

export interface PublicPage {
  id: string;
  slug: string;
  pageType: PageType;
  titleAr: string;
  titleEn: string;
  contentAr: string;
  contentEn: string;
}
```

## Folder structure

```
apps/web-portal/src/app/features/
├── home/
│   ├── home-api.service.{ts,spec.ts}
│   ├── home.types.ts
│   ├── home.page.{ts,html,scss,spec.ts}
│   └── routes.ts                    # HOME_ROUTES
└── pages/
    ├── pages-api.service.{ts,spec.ts}
    ├── page.types.ts
    ├── static-page.page.{ts,html,scss,spec.ts}
    └── routes.ts                    # STATIC_PAGES_ROUTES
```

---

## Task 1.1: Home page driven by /api/homepage-sections

**Files (all new):**
- `apps/web-portal/src/app/features/home/home.types.ts`
- `apps/web-portal/src/app/features/home/home-api.service.ts`
- `apps/web-portal/src/app/features/home/home-api.service.spec.ts`
- `apps/web-portal/src/app/features/home/home.page.ts`
- `apps/web-portal/src/app/features/home/home.page.html`
- `apps/web-portal/src/app/features/home/home.page.scss`
- `apps/web-portal/src/app/features/home/home.page.spec.ts`
- `apps/web-portal/src/app/features/home/routes.ts`
- Modify: `apps/web-portal/src/app/app.routes.ts` (root '' → home)

### Step 1: Types + ApiService

```ts
// home.types.ts (full content from "Hand-defined DTOs" above)
```

```ts
// home-api.service.ts
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { toFeatureError, type FeatureError } from '@frontend/ui-kit';
import type { HomepageSection } from './home.types';

export type Result<T> = { ok: true; value: T } | { ok: false; error: FeatureError };

@Injectable({ providedIn: 'root' })
export class HomeApiService {
  private readonly http = inject(HttpClient);

  async listSections(): Promise<Result<HomepageSection[]>> {
    try {
      const value = await firstValueFrom(this.http.get<HomepageSection[]>('/api/homepage-sections'));
      return { ok: true, value };
    } catch (err) {
      return { ok: false, error: toFeatureError(err as HttpErrorResponse) };
    }
  }
}
```

### Step 2: Service tests

3 tests (TestBed + HttpTestingController):
- GETs `/api/homepage-sections`, returns `{ ok: true, value: array }`.
- Returns `{ ok: false, error: { kind: 'server' } }` on 500.
- Returns `{ ok: false, error: { kind: 'network' } }` on status 0.

### Step 3: HomePage component (signal-based, OnPush)

```ts
// home.page.ts
import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { LocaleService } from '@frontend/i18n';
import { TranslateModule } from '@ngx-translate/core';
import { HomeApiService } from './home-api.service';
import type { HomepageSection } from './home.types';

@Component({
  selector: 'cce-home',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  templateUrl: './home.page.html',
  styleUrl: './home.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HomePage implements OnInit {
  private readonly api = inject(HomeApiService);
  private readonly locale = inject(LocaleService);

  readonly sections = signal<HomepageSection[]>([]);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly localizedSections = computed(() => {
    const isArabic = this.locale.locale() === 'ar';
    return this.sections()
      .filter((s) => s.isActive)
      .sort((a, b) => a.orderIndex - b.orderIndex)
      .map((s) => ({
        ...s,
        content: isArabic ? s.contentAr : s.contentEn,
      }));
  });

  async ngOnInit(): Promise<void> {
    this.loading.set(true);
    this.errorKind.set(null);
    const res = await this.api.listSections();
    this.loading.set(false);
    if (res.ok) this.sections.set(res.value);
    else this.errorKind.set(res.error.kind);
  }
}
```

```html
<!-- home.page.html -->
<section class="cce-home">
  @if (loading()) {
    <p class="cce-home__loading">{{ 'common.loading' | translate }}</p>
  }
  @if (errorKind(); as kind) {
    <div class="cce-home__error" role="alert">{{ ('errors.' + kind) | translate }}</div>
  }
  @for (section of localizedSections(); track section.id) {
    <article [class]="'cce-home__section cce-home__section--' + section.sectionType.toLowerCase()">
      <div class="cce-home__content" [innerHTML]="section.content"></div>
    </article>
  }
</section>
```

```scss
:host { display: block; }
.cce-home { display: flex; flex-direction: column; }
.cce-home__loading,
.cce-home__error {
  padding: 1.5rem;
  text-align: center;
}
.cce-home__error {
  background: #fdecea;
  color: #b00020;
  margin: 1rem;
  border-radius: 4px;
}
.cce-home__section {
  padding: 2rem 1.5rem;
  border-bottom: 1px solid rgba(0, 0, 0, 0.06);

  &--hero {
    background: linear-gradient(135deg, rgba(0, 0, 0, 0.02), rgba(0, 0, 0, 0.06));
    padding: 4rem 1.5rem;
    text-align: center;
  }
}
.cce-home__content {
  max-width: 1200px;
  margin: 0 auto;
}
```

### Step 4: Page tests

5 tests (TestBed + provideRouter + provideNoopAnimations + LocaleService stub):
- Loads sections on init.
- Sets `errorKind` signal when api returns error.
- Filters out inactive sections.
- Sorts by `orderIndex`.
- `localizedSections` returns Arabic content when locale is 'ar', English when 'en'.

### Step 5: routes.ts

```ts
// routes.ts
import { Routes } from '@angular/router';

export const HOME_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./home.page').then((m) => m.HomePage),
  },
];
```

### Step 6: Wire into app.routes.ts

Replace the existing app.routes.ts with:

```ts
import { Route } from '@angular/router';

export const appRoutes: Route[] = [
  {
    path: '',
    pathMatch: 'full',
    loadChildren: () => import('./features/home/routes').then((m) => m.HOME_ROUTES),
  },
];
```

(Other routes added in subsequent phases.)

### Step 7: Verify + commit

```bash
cd /Users/m/CCE/frontend
pnpm nx test web-portal --testPathPattern="features/home" 2>&1 | tail -10
```

Expected: 8+ tests passing.

```bash
cd /Users/m/CCE
git add frontend/apps/web-portal/src/app/features/home/ frontend/apps/web-portal/src/app/app.routes.ts
git -c commit.gpgsign=false commit -m "feat(web-portal): home page driven by /api/homepage-sections (Phase 1.1)"
```

---

## Task 1.2: Static page renderer

**Files (all new):**
- `apps/web-portal/src/app/features/pages/page.types.ts`
- `apps/web-portal/src/app/features/pages/pages-api.service.{ts,spec.ts}`
- `apps/web-portal/src/app/features/pages/static-page.page.{ts,html,scss,spec.ts}`
- `apps/web-portal/src/app/features/pages/routes.ts`
- Modify: `apps/web-portal/src/app/app.routes.ts` (add `/pages/:slug` route)

### Step 1: Types + ApiService

```ts
// page.types.ts (content from above)
```

```ts
// pages-api.service.ts
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { toFeatureError, type FeatureError } from '@frontend/ui-kit';
import type { PublicPage } from './page.types';

export type Result<T> = { ok: true; value: T } | { ok: false; error: FeatureError };

@Injectable({ providedIn: 'root' })
export class PagesApiService {
  private readonly http = inject(HttpClient);

  async getBySlug(slug: string): Promise<Result<PublicPage>> {
    try {
      const value = await firstValueFrom(this.http.get<PublicPage>(`/api/pages/${encodeURIComponent(slug)}`));
      return { ok: true, value };
    } catch (err) {
      return { ok: false, error: toFeatureError(err as HttpErrorResponse) };
    }
  }
}
```

### Step 2: Service tests

3 tests:
- GETs `/api/pages/about`, returns `{ ok: true, value: page }`.
- Returns `{ kind: 'not-found' }` on 404.
- URL-encodes slugs with special characters (e.g. `/pages/some%2Fslug`).

### Step 3: StaticPagePage component

```ts
// static-page.page.ts
import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { LocaleService } from '@frontend/i18n';
import { TranslateModule } from '@ngx-translate/core';
import { PagesApiService } from './pages-api.service';
import type { PublicPage } from './page.types';

@Component({
  selector: 'cce-static-page',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  templateUrl: './static-page.page.html',
  styleUrl: './static-page.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StaticPagePage implements OnInit {
  private readonly api = inject(PagesApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly locale = inject(LocaleService);

  readonly page = signal<PublicPage | null>(null);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly title = computed(() => {
    const p = this.page();
    if (!p) return '';
    return this.locale.locale() === 'ar' ? p.titleAr : p.titleEn;
  });
  readonly content = computed(() => {
    const p = this.page();
    if (!p) return '';
    return this.locale.locale() === 'ar' ? p.contentAr : p.contentEn;
  });

  async ngOnInit(): Promise<void> {
    const slug = this.route.snapshot.paramMap.get('slug');
    if (!slug) {
      this.errorKind.set('not-found');
      return;
    }
    this.loading.set(true);
    this.errorKind.set(null);
    const res = await this.api.getBySlug(slug);
    this.loading.set(false);
    if (res.ok) this.page.set(res.value);
    else this.errorKind.set(res.error.kind);
  }
}
```

```html
<!-- static-page.page.html -->
<article class="cce-static-page">
  @if (loading()) {
    <p class="cce-static-page__loading">{{ 'common.loading' | translate }}</p>
  }
  @if (errorKind(); as kind) {
    <div class="cce-static-page__error" role="alert">{{ ('errors.' + kind) | translate }}</div>
  }
  @if (page()) {
    <h1 class="cce-static-page__title">{{ title() }}</h1>
    <div class="cce-static-page__content" [innerHTML]="content()"></div>
  }
</article>
```

```scss
:host { display: block; padding: 2rem 1.5rem; max-width: 800px; margin: 0 auto; }
.cce-static-page__title { margin: 0 0 1.5rem; }
.cce-static-page__error {
  background: #fdecea;
  color: #b00020;
  padding: 0.75rem 1rem;
  border-radius: 4px;
}
.cce-static-page__content { line-height: 1.6; }
```

### Step 4: Page tests

5 tests:
- Loads page on init using slug from route.
- Sets `errorKind: 'not-found'` when slug missing.
- Sets `errorKind` from api error.
- `title` computed returns Arabic in 'ar' locale, English in 'en'.
- `content` computed similarly.

### Step 5: routes.ts

```ts
// routes.ts
import { Routes } from '@angular/router';

export const STATIC_PAGES_ROUTES: Routes = [
  {
    path: ':slug',
    loadComponent: () => import('./static-page.page').then((m) => m.StaticPagePage),
  },
];
```

### Step 6: Wire into app.routes.ts

```ts
{
  path: 'pages',
  loadChildren: () => import('./features/pages/routes').then((m) => m.STATIC_PAGES_ROUTES),
},
```

### Step 7: Verify + commit

```bash
cd /Users/m/CCE/frontend
pnpm nx test web-portal --testPathPattern="features/pages" 2>&1 | tail -10
```

Expected: 8+ tests passing.

```bash
cd /Users/m/CCE
git add frontend/apps/web-portal/src/app/features/pages/ frontend/apps/web-portal/src/app/app.routes.ts
git -c commit.gpgsign=false commit -m "feat(web-portal): static page renderer at /pages/:slug (Phase 1.2)"
```

---

## Task 1.3: Header search box wiring + i18n keys for shell

**Files:**
- Modify: `frontend/libs/i18n/src/lib/i18n/en.json` (add nav.*, header.*, footer.*, filter.*, search.*, errors.* keys)
- Modify: `frontend/libs/i18n/src/lib/i18n/ar.json` (mirror with Arabic translations)
- Modify: `apps/web-portal/src/app/app.routes.ts` (add `/search` placeholder route — Phase 5 will replace with the real results page)

### Step 1: Add i18n keys to en.json

The shell already references many keys (e.g. `header.signIn`, `nav.home`, `footer.about`). Add a `web-portal` block to the top-level i18n JSON files. Audit by grepping the SPA for `| translate` and `'header.` etc. — list of keys needed at minimum:

```
nav.{home,knowledgeCenter,news,events,countries,community}
header.{signIn,signOut,profile,notifications,follows,menu}
footer.{about,privacy,terms,contact,ministryAttribution}
filter.{title,openButton}
search.{placeholder}
errors.{server,forbidden,not-found,validation,concurrency,duplicate,network,unknown}
common.{loading}
```

Most of these likely already exist in the i18n files from Sub-5; just verify and add what's missing.

### Step 2: Mirror to ar.json

Provide Arabic translations for every new key. Use the same style as Sub-5's i18n entries.

### Step 3: Placeholder /search route

For now, add a route entry that renders a minimal "Search results coming soon" component, OR just route to home until Phase 5. Recommend the placeholder approach so the header search box doesn't navigate to a 404:

```ts
// In app.routes.ts:
{
  path: 'search',
  loadComponent: () =>
    import('./features/search/search-placeholder.page').then((m) => m.SearchPlaceholderPage),
},
```

```ts
// frontend/apps/web-portal/src/app/features/search/search-placeholder.page.ts
import { ChangeDetectionStrategy, Component } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { inject } from '@angular/core';
import { map } from 'rxjs';

@Component({
  selector: 'cce-search-placeholder',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  template: `
    <div style="padding: 2rem; text-align: center;">
      <h1>{{ 'search.title' | translate }}</h1>
      <p>{{ 'search.coming' | translate }}</p>
      @if (query()) { <p><code>q = {{ query() }}</code></p> }
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SearchPlaceholderPage {
  private readonly route = inject(ActivatedRoute);
  readonly query = toSignal(
    this.route.queryParamMap.pipe(map((p) => p.get('q') ?? '')),
    { initialValue: '' },
  );
}
```

(Phase 5 replaces this with the real `<cce-search-results>` page.)

### Step 4: Verify + commit

```bash
cd /Users/m/CCE/frontend
pnpm nx test web-portal 2>&1 | tail -5  # full suite — should not regress
pnpm nx build web-portal 2>&1 | tail -5
```

```bash
cd /Users/m/CCE
git add frontend/libs/i18n/src/lib/i18n/ frontend/apps/web-portal/src/app/features/search/ frontend/apps/web-portal/src/app/app.routes.ts
git -c commit.gpgsign=false commit -m "feat(web-portal): shell i18n keys + /search placeholder (Phase 1.3)"
```

---

## Task 1.4: Wire about / privacy / terms footer links to working pages

**Approach:** The footer already has the routerLinks (`/pages/about`, `/pages/privacy`, `/pages/terms`, `/pages/contact`). The static-page renderer (Task 1.2) handles these slugs via `/api/pages/{slug}`. Sub-3 backend admin can create these pages; for v0.1.0 web-portal, the External API may return 404 if a page doesn't exist — the renderer shows the not-found error key.

**No new code in this task** — it's a documentation/integration verification task:

- [ ] **Step 1:** Verify the four footer links resolve correctly:

```bash
# Start dev server in one terminal: pnpm nx serve web-portal
# In another: confirm route registration
```

(Manual: open http://localhost:4200/pages/about — should show the static page renderer with an error toast if the page doesn't exist in DB; no 404 from the SPA itself.)

- [ ] **Step 2:** Update web-portal-completion checklist (deferred to Phase 9): note that pages with slugs `about`, `privacy`, `terms`, `contact` are expected to be seeded by admin-cms before launch.

- [ ] **Step 3:** Update i18n if not done in Task 1.3 — verify `errors.not-found` translates to a friendly "Page not found" / "الصفحة غير موجودة" rather than the technical default.

**No commit for this task** — it's pure verification. (Or commit a small docs note.)

---

## Phase 01 — completion checklist

- [ ] Task 1.1 — Home page driven by /api/homepage-sections (8+ tests).
- [ ] Task 1.2 — Static page renderer at /pages/:slug (8+ tests).
- [ ] Task 1.3 — Shell i18n keys + /search placeholder.
- [ ] Task 1.4 — Footer links verified.
- [ ] All Jest tests passing.
- [ ] admin-cms still 218/218.
- [ ] `pnpm nx build web-portal` clean.

**If all boxes ticked, Phase 01 is complete. Proceed to Phase 02 (Knowledge Center).**
