# Phase 03 — News + Events

> Parent: [`../2026-04-30-web-portal.md`](../2026-04-30-web-portal.md) · Spec: [`../../specs/2026-04-30-web-portal-design.md`](../../specs/2026-04-30-web-portal-design.md) §5 (`/api/news`, `/api/news/{slug}`, `/api/events`, `/api/events/{id}`, `/api/events/{id}.ics`)

**Phase goal:** Public users browse news articles + events. News uses slug routing; events have a list view + detail with `.ics` calendar export. After Phase 03, `/news` and `/events` are fully functional.

**Tasks:** 7
**Working directory:** `/Users/m/CCE/`
**Preconditions:**
- Phase 02 closed (`ad6b91b`).
- Knowledge Center patterns established (api service + paged list + detail + i18n + e2e smoke).

---

## Endpoint coverage

| Endpoint | Method | Phase 03 surface | Anonymous |
|---|---|---|---|
| `/api/news` | GET (paged) | Task 3.2 (NewsListPage) | ✓ |
| `/api/news/{slug}` | GET | Task 3.3 (NewsDetailPage) | ✓ |
| `/api/events` | GET (paged) | Task 3.4 (EventsListPage) | ✓ |
| `/api/events/{id}` | GET | Task 3.5 (EventDetailPage) | ✓ |
| `/api/events/{id}.ics` | GET (binary) | Task 3.5 (export-to-calendar) | ✓ |

## Hand-defined DTOs

```ts
// frontend/apps/web-portal/src/app/features/news/news.types.ts
import type { PagedResult } from '../knowledge-center/shared.types';

export interface NewsArticle {
  id: string;
  titleAr: string;
  titleEn: string;
  contentAr: string;
  contentEn: string;
  slug: string;
  authorId: string;
  featuredImageUrl: string | null;
  publishedOn: string | null;
  isFeatured: boolean;
  isPublished: boolean;
}

export type { PagedResult };
```

```ts
// frontend/apps/web-portal/src/app/features/events/event.types.ts
import type { PagedResult } from '../knowledge-center/shared.types';

export interface Event {
  id: string;
  titleAr: string;
  titleEn: string;
  descriptionAr: string;
  descriptionEn: string;
  startsOn: string;
  endsOn: string;
  locationAr: string | null;
  locationEn: string | null;
  onlineMeetingUrl: string | null;
  featuredImageUrl: string | null;
  iCalUid: string;
}

export type { PagedResult };
```

## Folder structure

```
apps/web-portal/src/app/features/
├── news/
│   ├── news.types.ts
│   ├── news-api.service.{ts,spec.ts}
│   ├── news-list.page.{ts,html,scss,spec.ts}
│   ├── news-detail.page.{ts,html,scss,spec.ts}
│   ├── news-card.component.{ts,scss}        # presentation card (inline template)
│   └── routes.ts
└── events/
    ├── event.types.ts
    ├── events-api.service.{ts,spec.ts}
    ├── events-list.page.{ts,html,scss,spec.ts}
    ├── event-detail.page.{ts,html,scss,spec.ts}
    └── routes.ts
```

---

## Task 3.1: NewsApiService + EventsApiService + types

**Files (all new):**
- `features/news/news.types.ts`
- `features/news/news-api.service.{ts,spec.ts}`
- `features/events/event.types.ts`
- `features/events/events-api.service.{ts,spec.ts}`

NewsApiService methods:
- `listNews({ page?, pageSize?, isFeatured? })` → `Result<PagedResult<NewsArticle>>` (GET `/api/news`).
- `getBySlug(slug)` → `Result<NewsArticle>` (GET `/api/news/{slug}`).

EventsApiService methods:
- `listEvents({ page?, pageSize?, from?, to? })` → `Result<PagedResult<Event>>` (GET `/api/events`).
- `getEvent(id)` → `Result<Event>` (GET `/api/events/{id}`).
- `downloadIcs(id)` → `Result<Blob>` (GET `/api/events/{id}.ics`, `responseType: 'blob'`).

Both services follow the same `Result<T>` + `toFeatureError` pattern used since Phase 1.

**Tests (~10 total: 5 per service):**

NewsApiService:
1. `listNews({ page: 2, pageSize: 50, isFeatured: true })` builds query string with all 3 params.
2. `getBySlug('hello')` GETs `/api/news/hello`; URL-encodes slugs with special chars.
3. `getBySlug` returns `{ kind: 'not-found' }` on 404.

EventsApiService:
1. `listEvents({ page: 1, pageSize: 20 })` GETs `/api/events`.
2. `listEvents({ from: '2026-01-01', to: '2026-12-31' })` builds query string.
3. `getEvent('e1')` GETs `/api/events/e1`.
4. `downloadIcs('e1')` GETs `/api/events/e1.ics` with `responseType: 'blob'`.
5. `getEvent` returns 'not-found' on 404.

Commit: `feat(web-portal): NewsApiService + EventsApiService + DTOs (Phase 3.1)`

---

## Task 3.2: NewsListPage

**Files:**
- `features/news/news-list.page.{ts,html,scss,spec.ts}`
- `features/news/news-card.component.{ts,scss}` (inline template)

NewsCardComponent: signal-input pattern (`input.required<NewsArticle>`, `input<'ar'|'en'>('en')`). Renders title (localized), publishedOn date, featured-image hero (or icon if absent), excerpt of contentAr/contentEn (first 160 chars). Click navigates to `/news/{slug}` (slug routing, not id).

NewsListPage: paged list with optional "featured-only" filter checkbox. Uses NewsCardComponent in a grid. Pagination via mat-paginator. URL query params (`?page=2&featured=true`). Empty + error states.

Tests (~8): init load with default paging, query param read, featured-filter toggle resets page + reloads, paginator updates, error banner, empty state, card routerLink target, card title localization.

Commit: `feat(web-portal): NewsListPage with feature filter + cards (Phase 3.2)`

---

## Task 3.3: NewsDetailPage

**Files:**
- `features/news/news-detail.page.{ts,html,scss,spec.ts}`

Reads `:slug` from route, calls `getBySlug`. Renders title, publishedOn date, featured image (if any), full content (HTML via `[innerHTML]` — content sanitized server-side per Sub-4 ADR-0034 HtmlSanitizer). "Back to news" link to `/news`. Localized title/content via LocaleService.

Tests (~5): loads on init from slug, errorKind on 404, title/content computed by locale, missing slug → not-found, back link present.

Commit: `feat(web-portal): NewsDetailPage with slug routing (Phase 3.3)`

---

## Task 3.4: EventsListPage

**Files:**
- `features/events/events-list.page.{ts,html,scss,spec.ts}`

Paged list of events. Filter rail with from/to date range inputs (datetime-local type) — submit on apply, sync to URL. Each event renders as a card in a grid (start date, title, location, online icon). Click → `/events/:id`.

For v0.1.0 simplicity: no calendar/month-grid view (the spec mentioned "month grid view" as nice-to-have, but flat paged list ships first; calendar view can be a Phase 9 polish task or deferred to v0.2.0).

Tests (~7): init load, from/to filter applies, paginator updates, URL sync, empty/error states, card title localization, click navigation.

Commit: `feat(web-portal): EventsListPage with date-range filter (Phase 3.4)`

---

## Task 3.5: EventDetailPage with .ics export

**Files:**
- `features/events/event-detail.page.{ts,html,scss,spec.ts}`

Reads `:id` from route, calls `getEvent`. Renders title, description, start/end times (DatePipe formatted), location (online URL link or physical address), featured image. "Add to calendar" button calls `events.downloadIcs(id)` and materializes the Blob as a download (`{event-title}-{date}.ics`). Localized title/description/location via LocaleService.

Tests (~5): loads on init, errorKind on 404, ics download materializes blob + toasts success, ics error surfaces via toast.error, locale toggle updates title/description.

Commit: `feat(web-portal): EventDetailPage with .ics calendar export (Phase 3.5)`

---

## Task 3.6: Routes + i18n keys

Add to `app.routes.ts`:

```ts
{
  path: 'news',
  loadChildren: () => import('./features/news/routes').then((m) => m.NEWS_ROUTES),
  title: 'CCE — News',
},
{
  path: 'events',
  loadChildren: () => import('./features/events/routes').then((m) => m.EVENTS_ROUTES),
  title: 'CCE — Events',
},
```

Routes files:

```ts
// features/news/routes.ts
export const NEWS_ROUTES: Routes = [
  { path: '', loadComponent: () => import('./news-list.page').then((m) => m.NewsListPage) },
  { path: ':slug', loadComponent: () => import('./news-detail.page').then((m) => m.NewsDetailPage) },
];
```

```ts
// features/events/routes.ts
export const EVENTS_ROUTES: Routes = [
  { path: '', loadComponent: () => import('./events-list.page').then((m) => m.EventsListPage) },
  { path: ':id', loadComponent: () => import('./event-detail.page').then((m) => m.EventDetailPage) },
];
```

i18n: extend the existing `news` + `events` blocks (admin-cms keys exist) with web-portal additions:

For `news`:
- `news.empty` — "No news articles published yet."
- `news.featured.toggle` — "Show featured only"
- `news.back` — "Back to news"
- `news.publishedOn` — "Published on" (top-level; admin's `news.col.publishedOn` is for a table)

For `events`:
- `events.empty` — "No events scheduled."
- `events.filter.from`, `events.filter.to` — "From", "To" (date range)
- `events.back` — "Back to events"
- `events.startsOn`, `events.endsOn` — "Starts", "Ends"
- `events.location.online` — "Online"
- `events.location.tba` — "Location TBA"
- `events.export.openButton` — "Add to calendar"
- `events.export.toast` — "Calendar invite downloaded."

Both ar.json + en.json mirrored.

Commit: `feat(web-portal): /news + /events routes + i18n keys (Phase 3.6)`

---

## Task 3.7: E2E nav smoke

```ts
// frontend/apps/web-portal-e2e/src/news-events.spec.ts
import { test, expect } from '@playwright/test';

test.describe('news + events smoke', () => {
  test('navigates from header → /news', async ({ page }) => {
    await page.goto('/');
    await page.getByRole('link', { name: /^news|الأخبار/i }).first().click();
    await expect(page).toHaveURL(/\/news/);
  });

  test('navigates from header → /events', async ({ page }) => {
    await page.goto('/');
    await page.getByRole('link', { name: /^events|الفعاليات/i }).first().click();
    await expect(page).toHaveURL(/\/events/);
  });
});
```

Lint only (no run). Commit: `feat(web-portal-e2e): news + events nav smoke (Phase 3.7)`

---

## Phase 03 — completion checklist

- [ ] Task 3.1 — News + Events ApiServices + DTOs (~10 tests).
- [ ] Task 3.2 — NewsListPage (~8 tests).
- [ ] Task 3.3 — NewsDetailPage (~5 tests).
- [ ] Task 3.4 — EventsListPage (~7 tests).
- [ ] Task 3.5 — EventDetailPage with .ics (~5 tests).
- [ ] Task 3.6 — Routes + i18n keys.
- [ ] Task 3.7 — E2E smoke.
- [ ] All Jest tests passing.
- [ ] admin-cms still 218/218.
- [ ] Lint + build clean.

**If all boxes ticked, Phase 03 complete. Proceed to Phase 04 (Country profiles + KAPSARC).**
