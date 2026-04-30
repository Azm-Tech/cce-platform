# Phase 05 — Unified search

> Parent: [`../2026-04-30-web-portal.md`](../2026-04-30-web-portal.md) · Spec: [`../../specs/2026-04-30-web-portal-design.md`](../../specs/2026-04-30-web-portal-design.md) §5 (`/api/search`)

**Phase goal:** Replace the Phase 1 placeholder at `/search` with a real cross-entity results page consuming `/api/search`. Anonymous users type in the header search box, hit Enter, and land on a results page that shows hits from News, Events, Resources, Pages, and Knowledge Maps — paginated and filterable by entity type via a facet rail.

**Tasks:** 3
**Working directory:** `/Users/m/CCE/`
**Preconditions:**
- Phase 04 closed (`56c9bfb`).
- SearchBoxComponent (Phase 1) already navigates to `/search?q=...`.
- SearchPlaceholderPage exists at `app.routes.ts` line 33 — Phase 5 replaces the `loadComponent` reference with the real `SearchResultsPage` and deletes the placeholder file.

---

## Endpoint coverage

| Endpoint | Method | Phase 05 surface | Anonymous |
|---|---|---|---|
| `/api/search` | GET (`?q=`, `?type=`, `?page=`, `?pageSize=`) | Task 5.2 (SearchResultsPage) | ✓ |

**Backend contract** (verified at `backend/src/CCE.Api.External/Endpoints/SearchEndpoints.cs` + `SearchHitDto.cs`):

```csharp
public sealed record SearchHitDto(
    Guid Id,
    SearchableType Type,        // News | Events | Resources | Pages | KnowledgeMaps
    string TitleAr, string TitleEn,
    string ExcerptAr, string ExcerptEn,
    double Score);

// SearchableType enum values: News=0, Events=1, Resources=2, Pages=3, KnowledgeMaps=4
// SearchQuery: q (string, required), type (SearchableType?, nullable), page=1, pageSize=20
// Returns: PagedResult<SearchHitDto>
```

The backend serializes the enum **as a string** by default (System.Text.Json + standard ASP.NET config), so the wire shape is `"type": "News"`. We model the FE type as a string-literal union to match.

## Hand-defined DTOs

```ts
// frontend/apps/web-portal/src/app/features/search/search.types.ts
import type { PagedResult } from '../knowledge-center/shared.types';

export type SearchableType = 'News' | 'Events' | 'Resources' | 'Pages' | 'KnowledgeMaps';

export interface SearchHit {
  id: string;
  type: SearchableType;
  titleAr: string;
  titleEn: string;
  excerptAr: string;
  excerptEn: string;
  score: number;
}

export type { PagedResult };
```

## Folder structure

```
apps/web-portal/src/app/features/search/
├── search.types.ts                              # NEW
├── search-api.service.{ts,spec.ts}              # NEW
├── search-results.page.{ts,html,scss,spec.ts}   # NEW
├── search-hit.component.{ts,scss}               # NEW (presentation card)
└── search-placeholder.page.ts                   # DELETED in Task 5.3
```

---

## Task 5.1: SearchApiService + types

**Files (all new):**
- `features/search/search.types.ts`
- `features/search/search-api.service.{ts,spec.ts}`

SearchApiService method:
- `search({ q, type?, page?, pageSize? })` → `Result<PagedResult<SearchHit>>` (GET `/api/search`).
  - `q` is required. Pass through `type` only when set (URL omits the param when `type === undefined`).
  - Default page/pageSize handled by backend (1, 20); FE only sends them when explicitly overridden.

Follows the same `Result<T>` + `toFeatureError` pattern used since Phase 1.

**Tests (~5):**
1. `search({ q: 'circular' })` GETs `/api/search` with only `q=circular`.
2. `search({ q: 'circ', type: 'News' })` adds `type=News` to query string.
3. `search({ q: 'a', page: 2, pageSize: 50 })` adds `page=2&pageSize=50`.
4. Returns `PagedResult<SearchHit>` on 200.
5. Returns `{ ok: false, error: { kind: 'server' } }` on 500.

Commit: `feat(web-portal): SearchApiService + DTOs (Phase 5.1)`

---

## Task 5.2: SearchResultsPage with entity-type facet rail

**Files:**
- `features/search/search-results.page.{ts,html,scss,spec.ts}`
- `features/search/search-hit.component.{ts,scss}` (inline template)

SearchHitComponent: signal-input pattern (`input.required<SearchHit>`, `input<'ar'|'en'>('en')`). Renders:
- Localized title (computed by locale → `titleAr` | `titleEn`).
- Excerpt (first 200 chars, HTML-stripped).
- Type badge (e.g. "News", "Resource") via `searchType.<key>` i18n key.
- Score (right-aligned, formatted to 2 decimals — collapses on narrow screens, only visible to keyboard/screen-reader users via `aria-label`; the visible badge is the entity type).
- `routerLink` to the appropriate detail route based on `type`:
  - `News` → `/news/<id>` (note: news detail uses **slug**, not id; for the slug-vs-id mismatch, FE links to a stub detail-by-id route fallback handled in 5.3 — see backend SearchHitDto: `Id` is the entity primary key. For News specifically, the search hit's `id` is the article id, **not** the slug. Two clean approaches:
    - **A** (chosen for v0.1.0): Render the link as `/news/<id>` and have the news detail page tolerate id-or-slug — but news-detail currently only accepts slug. So instead, **don't** link news hits to `/news/<id>` directly; render a non-link placeholder badge "News" with the title as plain text, plus a "View in news →" CTA that navigates to `/news?q=<title-snippet>` (URL-syncing the search term to the list page filter, which Phase 3 already supports via the `featured` toggle pattern, but `?q=` is not yet wired into NewsListPage).
    - **B** (deferred to Phase 9 polish): backend extends SearchHitDto with a `slug` field for News.

  For v0.1.0, pick **option A simplified**: Each hit links to `/<area>/<id>` where the area route knows how to resolve. For News, the search hit `id` *is* the article id — but since `/news/:slug` is the only news detail route, navigating to `/news/<id>` would 404. **Decision:** for News hits, link to `/news` (the list page); for all other types, link to the canonical detail route (`/events/<id>`, `/knowledge-center/<id>`, `/pages/<slug>` — but Pages also use slug). For the v0.1.0 scope: **link only Events, Resources, and Knowledge Maps to detail; News and Pages render as non-link cards with a small "Open in News" / "Open in Pages list" CTA**. This keeps the page shippable without a backend SearchHitDto change.

  Pragmatic v0.1.0 implementation:
  ```ts
  readonly detailLink = computed<string | null>(() => {
    const h = this.hit();
    switch (h.type) {
      case 'Events': return `/events/${h.id}`;
      case 'Resources': return `/knowledge-center/${h.id}`;
      case 'KnowledgeMaps': return `/knowledge-maps`;  // skeleton; opens placeholder (Phase 9)
      case 'News': return null;  // see Phase 9 enhancement note
      case 'Pages': return null; // ditto
    }
  });
  ```
  Card renders as `<a [routerLink]="detailLink()">` when non-null, plain `<div>` when null.)

SearchResultsPage:
- Reads `q`, `type`, `page`, `pageSize` from URL via `ActivatedRoute.queryParamMap`. The `q` is **always required** — when empty, render a friendly "Type a query in the search box above" message; do not call the API.
- On URL changes (q, type, page), re-fires `search.search(...)` (use `toSignal` over `route.queryParamMap`).
- Renders:
  - Header: "Results for `q`" (localized) + total count.
  - Facet rail (left): list of entity-type checkboxes (News, Events, Resources, Pages, KnowledgeMaps). The current selection is single-choice (radio-style: `?type=` is one value or absent). Clicking a type sets `?type=` in URL and resets `?page=1`. Clicking the active type clears it.
  - Result list (main): `<cce-search-hit>` per row.
  - Pagination: `mat-paginator` with size options [10, 20, 50, 100].
  - Empty state: "No matches for `q`."
  - Error state: error banner with retry.
  - Loading: progress bar.

Tests (~6):
1. URL `?q=carbon` triggers `search({ q: 'carbon' })` on init.
2. URL `?q=carbon&type=News` triggers `search({ q: 'carbon', type: 'News' })`.
3. Empty `q` (no query param) does **not** call the API; renders "type a query" hint.
4. Clicking a type facet syncs `?type=` to URL + resets `?page=1`.
5. Clicking the **active** type clears `?type=` from URL.
6. `mat-paginator` change syncs `?page=`/`?pageSize=` to URL and re-fires the call.

Commit: `feat(web-portal): SearchResultsPage with entity-type facet rail (Phase 5.2)`

---

## Task 5.3: Wire route + i18n + delete placeholder + E2E smoke

**Files:**
- Modify: `apps/web-portal/src/app/app.routes.ts` — replace `loadComponent: () => import('./features/search/search-placeholder.page').then((m) => m.SearchPlaceholderPage)` with `loadComponent: () => import('./features/search/search-results.page').then((m) => m.SearchResultsPage)`. Title remains `'CCE — Search'`.
- Delete: `apps/web-portal/src/app/features/search/search-placeholder.page.ts`.
- New: `apps/web-portal-e2e/src/search.spec.ts`.

i18n additions (both `en.json` and `ar.json` mirrored):

Update existing `search` block:
- Remove `search.coming` ("Search results land in Phase 5.") — placeholder retired.
- Add `search.resultsFor` — "Results for {{q}}" / "نتائج لـ {{q}}" (use `[innerHTML]` with `translate` parameter binding via `params` input; or use `<ng-container *ngTemplateOutlet>` — simpler: split the i18n key into `search.resultsForPrefix` + render the `q` value separately).
- Add `search.empty` — "No matches for {{q}}." / "لا توجد نتائج لـ {{q}}." (same parameter handling).
- Add `search.typeAQuery` — "Type a query in the search box above." / "اكتب استعلامك في مربع البحث في الأعلى."
- Add `search.totalCount` — "{{count}} results" / "{{count}} نتيجة"
- Add `search.facets.title` — "Filter by type" / "صفية حسب النوع"
- Add `search.facets.clear` — "Clear filter" / "مسح المرشح"

New top-level `searchType` block (entity type labels for badges + facet rail):
- `searchType.News` — "News" / "أخبار"
- `searchType.Events` — "Events" / "فعاليات"
- `searchType.Resources` — "Resources" / "موارد"
- `searchType.Pages` — "Pages" / "صفحات"
- `searchType.KnowledgeMaps` — "Knowledge maps" / "خرائط المعرفة"

E2E smoke at `apps/web-portal-e2e/src/search.spec.ts`:

```ts
import { test, expect } from '@playwright/test';

/**
 * Phase 05 search smoke. Anonymous user types in the header search box,
 * hits Enter, and the results page mounts at /search?q=.
 *
 * The full-stack run (with the External API + Meilisearch + actual
 * indexed data) is deferred to Phase 9 close-out; this spec verifies
 * navigation + DOM mount only.
 */
test.describe('search nav smoke', () => {
  test('header search box → /search?q=', async ({ page }) => {
    await page.goto('/');
    await expect(page.locator('cce-header')).toBeAttached({ timeout: 15_000 });
    const input = page.locator('cce-search-box input[type=search]');
    await input.fill('carbon');
    await input.press('Enter');
    await expect(page).toHaveURL(/\/search\?q=carbon/);
    await expect(page.locator('cce-search-results')).toBeAttached({ timeout: 10_000 });
  });

  test('empty query path renders "type a query" hint', async ({ page }) => {
    await page.goto('/search');
    await expect(page.locator('cce-search-results')).toBeAttached({ timeout: 10_000 });
    await expect(page.getByText(/type a query|اكتب استعلامك/i)).toBeVisible();
  });
});
```

Commit: `feat(web-portal): /search route + i18n + E2E smoke (Phase 5.3)`

---

## Phase 05 — completion checklist

- [ ] Task 5.1 — SearchApiService + DTOs (~5 tests).
- [ ] Task 5.2 — SearchResultsPage with facet rail (~6 tests).
- [ ] Task 5.3 — Route swap + i18n + delete placeholder + E2E smoke.
- [ ] All Jest tests passing.
- [ ] admin-cms still 218/218.
- [ ] Lint + build clean.

**If all boxes ticked, Phase 05 complete. Proceed to Phase 06 (Account: register + /me + expert-request + service-rating).**

---

## Notes for Phase 9 polish backlog

- **News / Pages search hit links** — current v0.1.0 design renders these as non-link cards because:
  - News detail route uses `:slug`, but `SearchHitDto.id` is the article primary key (no slug field on the wire).
  - Pages detail route also uses `:slug`.
  - **Fix in Phase 9:** either extend `SearchHitDto` to include `slug` for News/Pages, or add a tiny `/news/by-id/:id` redirect route on FE that resolves the slug via `getById` and `router.navigate` to the canonical slug URL. Document this decision when Phase 9 polish runs.
- **Knowledge Maps hits** — link to `/knowledge-maps` (skeleton); the placeholder page from Phase 9 lists available maps so the user can find their hit manually until Sub-7 ships the detailed view.
