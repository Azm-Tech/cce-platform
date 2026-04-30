# Phase 04 — Country profiles + KAPSARC

> Parent: [`../2026-04-30-web-portal.md`](../2026-04-30-web-portal.md) · Spec: [`../../specs/2026-04-30-web-portal-design.md`](../../specs/2026-04-30-web-portal-design.md) §5 (`/api/countries`, `/api/countries/{id}/profile`, `/api/kapsarc/snapshots/{countryId}`)

**Phase goal:** Public users browse the participating countries (grouped by region), open a country detail page that shows the country profile (description / key initiatives / contact info) plus the latest KAPSARC snapshot block (classification, performance score, total index). After Phase 04, `/countries` and `/countries/:id` are fully functional.

**Tasks:** 4
**Working directory:** `/Users/m/CCE/`
**Preconditions:**
- Phase 03 closed (`bbd3200`).
- Knowledge Center / News / Events patterns established (api service + paged list + detail + i18n + e2e smoke).

---

## Endpoint coverage

| Endpoint | Method | Phase 04 surface | Anonymous |
|---|---|---|---|
| `/api/countries` | GET (`?search=`) | Task 4.2 (CountriesGridPage) | ✓ |
| `/api/countries/{id}/profile` | GET | Task 4.3 (CountryDetailPage) | ✓ |
| `/api/kapsarc/snapshots/{countryId}` | GET | Task 4.3 (KAPSARC block on CountryDetailPage) | ✓ |

Note: `/api/countries` returns a flat list ordered server-side (no paging). KAPSARC's "snapshot" endpoint returns the **latest** snapshot — null/404 means the country has no KAPSARC snapshot yet, which is a render-empty state, not an error.

## Hand-defined DTOs

```ts
// frontend/apps/web-portal/src/app/features/countries/country.types.ts
export interface Country {
  id: string;
  isoAlpha3: string;
  isoAlpha2: string;
  nameAr: string;
  nameEn: string;
  regionAr: string;
  regionEn: string;
  flagUrl: string;
}

export interface CountryProfile {
  id: string;
  countryId: string;
  descriptionAr: string;
  descriptionEn: string;
  keyInitiativesAr: string;
  keyInitiativesEn: string;
  contactInfoAr: string | null;
  contactInfoEn: string | null;
  lastUpdatedOn: string;
}

export interface KapsarcSnapshot {
  id: string;
  countryId: string;
  classification: string;
  performanceScore: number;
  totalIndex: number;
  snapshotTakenOn: string;
  sourceVersion: string | null;
}
```

## Folder structure

```
apps/web-portal/src/app/features/
└── countries/
    ├── country.types.ts
    ├── countries-api.service.{ts,spec.ts}
    ├── kapsarc-api.service.{ts,spec.ts}
    ├── countries-grid.page.{ts,html,scss,spec.ts}
    ├── country-detail.page.{ts,html,scss,spec.ts}
    ├── country-card.component.{ts,scss}        # presentation card (inline template)
    ├── kapsarc-snapshot.component.{ts,scss}    # presentation block (inline template)
    └── routes.ts
```

---

## Task 4.1: CountriesApiService + KapsarcApiService + types

**Files (all new):**
- `features/countries/country.types.ts`
- `features/countries/countries-api.service.{ts,spec.ts}`
- `features/countries/kapsarc-api.service.{ts,spec.ts}`

CountriesApiService methods:
- `listCountries({ search? })` → `Result<Country[]>` (GET `/api/countries`).
- `getProfile(countryId)` → `Result<CountryProfile>` (GET `/api/countries/{id}/profile`); returns `{ kind: 'not-found' }` on 404.

KapsarcApiService method:
- `getLatestSnapshot(countryId)` → `Result<KapsarcSnapshot | null>` (GET `/api/kapsarc/snapshots/{countryId}`). 404 maps to `{ ok: true, value: null }` (no snapshot yet — empty state, not an error).

Both services follow the same `Result<T>` + `toFeatureError` pattern used since Phase 1.

**Tests (~9 total: 4 + 5 split):**

CountriesApiService:
1. `listCountries({})` GETs `/api/countries` (no query string).
2. `listCountries({ search: 'jo' })` builds `?search=jo` query string.
3. `getProfile('c1')` GETs `/api/countries/c1/profile`.
4. `getProfile` returns `{ kind: 'not-found' }` on 404.

KapsarcApiService:
1. `getLatestSnapshot('c1')` GETs `/api/kapsarc/snapshots/c1`.
2. Returns `{ ok: true, value: <dto> }` on 200.
3. Returns `{ ok: true, value: null }` on 404 (no snapshot is a valid empty state).
4. Returns `{ ok: false, error: { kind: 'server' } }` on 500.
5. Returns `{ ok: false, error: { kind: 'network' } }` on transport error.

Commit: `feat(web-portal): CountriesApiService + KapsarcApiService + DTOs (Phase 4.1)`

---

## Task 4.2: CountriesGridPage (grouped by region)

**Files:**
- `features/countries/countries-grid.page.{ts,html,scss,spec.ts}`
- `features/countries/country-card.component.{ts,scss}` (inline template)

CountryCardComponent: signal-input pattern (`input.required<Country>`, `input<'ar'|'en'>('en')`). Renders `flagUrl` (with width/height to avoid CLS), localized country name, ISO alpha-3 badge. Click navigates to `/countries/:id` (id routing).

CountriesGridPage:
- Loads via `countries.listCountries({})` on init. Anonymous-friendly (no auth gate).
- Optional search input wired to a `searchTerm` signal; on submit (Enter key) re-issues `listCountries({ search })` and updates URL `?q=`.
- After load: groups the flat list by **localized region** (uses `regionEn` when locale is `en`, `regionAr` when `ar`); region order is alphabetical.
- Renders one `<section>` per region with an `<h2>` region heading and a Bootstrap grid of `<cce-country-card>`.
- Loading spinner, error banner with retry, empty state ("No countries available.").

Tests (~7):
1. Init load groups countries by region (e.g. 2 regions → 2 sections).
2. Search submit re-issues `listCountries({ search: 'jo' })` and syncs URL `?q=jo`.
3. URL `?q=jo` is read on init and pre-populates the search input + service call.
4. Card title localizes by `LocaleService` signal (toggle ar → renders `nameAr`).
5. Region heading localizes (toggle ar → renders `regionAr`).
6. Error path renders error banner + retry click triggers fresh `listCountries`.
7. Empty path renders empty message.

Commit: `feat(web-portal): CountriesGridPage with region grouping (Phase 4.2)`

---

## Task 4.3: CountryDetailPage with KAPSARC snapshot

**Files:**
- `features/countries/country-detail.page.{ts,html,scss,spec.ts}`
- `features/countries/kapsarc-snapshot.component.{ts,scss}` (inline template)

KapsarcSnapshotComponent: presentation-only card. `input.required<KapsarcSnapshot>`, `input<'ar'|'en'>('en')`. Renders classification (badge), performance score + total index (formatted to 2 decimals via `DecimalPipe`), snapshot date (`DatePipe`), and source-version footnote when present. Pure presentation; no service calls.

CountryDetailPage:
- Reads `:id` from route, calls **both** `countries.getProfile(id)` and `kapsarc.getLatestSnapshot(id)` in parallel via `Promise.all`.
- State: `loading` signal, `profile` signal (`CountryProfile | null`), `snapshot` signal (`KapsarcSnapshot | null`), `errorKind` signal (only set when `getProfile` fails — KAPSARC failure shows an inline "Snapshot unavailable" subtle note but does **not** block the profile render).
- Renders: country header (flag + name from sibling `listCountries` cache *or* hard-render from id only — see below), profile description (HTML `[innerHTML]` — sanitized server-side), key initiatives, contact info (only when non-null), `<cce-kapsarc-snapshot>` when snapshot present, "Latest snapshot not yet published." inline note when null, "Back to countries" link to `/countries`.
- Country header: since `getProfile` returns the profile only (not the country row), the page also resolves the country from `countries.listCountries({})` (cached 5min via signal) to render flag + name. If the lookup is unresolved, fall back to a generic header (no flag, "Country profile" placeholder) — the profile body still renders.
- Localized title/description/initiatives/contact via LocaleService.

Tests (~6):
1. Loads on init from `:id` and renders profile description (en).
2. errorKind on profile 404 → renders "Country not found" message + back link.
3. Locale toggle updates title/description/initiatives.
4. KAPSARC null path renders inline "Snapshot not yet published" message; profile still visible.
5. KAPSARC error path renders inline error note; profile still visible.
6. Back link points to `/countries`.

Commit: `feat(web-portal): CountryDetailPage with KAPSARC snapshot (Phase 4.3)`

---

## Task 4.4: Routes + i18n keys + E2E nav smoke

Add to `app.routes.ts` (after `events`, before `search`):

```ts
{
  path: 'countries',
  loadChildren: () => import('./features/countries/routes').then((m) => m.COUNTRIES_ROUTES),
  title: 'CCE — Countries',
},
```

Routes file:

```ts
// features/countries/routes.ts
import { Routes } from '@angular/router';

export const COUNTRIES_ROUTES: Routes = [
  { path: '', loadComponent: () => import('./countries-grid.page').then((m) => m.CountriesGridPage) },
  { path: ':id', loadComponent: () => import('./country-detail.page').then((m) => m.CountryDetailPage) },
];
```

Header nav: add "Countries" link to `header.component.html` between "Knowledge Center" and "News" (en order; ar mirrors). Bind via `nav.countries` i18n key.

i18n additions (both `en.json` and `ar.json` mirrored):

For `nav`:
- `nav.countries` — "Countries" / "الدول"

New top-level `countries` block:
- `countries.title` — "Countries" / "الدول"
- `countries.search.placeholder` — "Search countries…" / "ابحث في الدول..."
- `countries.empty` — "No countries available." / "لا توجد دول متاحة."
- `countries.back` — "Back to countries" / "العودة إلى الدول"
- `countries.notFound` — "Country not found." / "لم يتم العثور على الدولة."
- `countries.detail.description` — "Description" / "الوصف"
- `countries.detail.keyInitiatives` — "Key initiatives" / "أبرز المبادرات"
- `countries.detail.contactInfo` — "Contact information" / "بيانات التواصل"
- `countries.detail.lastUpdated` — "Last updated" / "آخر تحديث"

New `kapsarc` block:
- `kapsarc.title` — "KAPSARC snapshot" / "لقطة كابسارك"
- `kapsarc.classification` — "Classification" / "التصنيف"
- `kapsarc.performanceScore` — "Performance score" / "درجة الأداء"
- `kapsarc.totalIndex` — "Total index" / "المؤشر الإجمالي"
- `kapsarc.snapshotTakenOn` — "Snapshot taken on" / "تاريخ اللقطة"
- `kapsarc.sourceVersion` — "Source version" / "إصدار المصدر"
- `kapsarc.unavailable` — "Latest snapshot not yet published." / "لم يتم نشر أحدث لقطة بعد."
- `kapsarc.error` — "Could not load KAPSARC snapshot." / "تعذّر تحميل لقطة كابسارك."

E2E nav smoke at `frontend/apps/web-portal-e2e/src/countries.spec.ts`:

```ts
import { test, expect } from '@playwright/test';

/**
 * Phase 04 navigation smoke. Anonymous user clicks Countries from the
 * top nav and lands on the grid page.
 *
 * Full-stack run with the External API + actual data is deferred to
 * Phase 9 close-out; this spec only verifies navigation + DOM structure.
 */
test.describe('countries nav smoke', () => {
  test('navigates from header → /countries', async ({ page }) => {
    await page.goto('/');
    await expect(page.locator('cce-header')).toBeAttached({ timeout: 15_000 });
    await page.getByRole('link', { name: /^countries|الدول/i }).first().click();
    await expect(page).toHaveURL(/\/countries/);
    await expect(page.locator('cce-countries-grid')).toBeAttached({ timeout: 10_000 });
  });
});
```

Lint only (no run). One bundled commit covers routes + header nav + i18n + E2E.

Commit: `feat(web-portal): /countries route + i18n + E2E nav smoke (Phase 4.4)`

---

## Phase 04 — completion checklist

- [ ] Task 4.1 — Countries + Kapsarc ApiServices + DTOs (~9 tests).
- [ ] Task 4.2 — CountriesGridPage with region grouping (~7 tests).
- [ ] Task 4.3 — CountryDetailPage with KAPSARC snapshot (~6 tests).
- [ ] Task 4.4 — Routes + header nav + i18n + E2E smoke.
- [ ] All Jest tests passing.
- [ ] admin-cms still 218/218.
- [ ] Lint + build clean.

**If all boxes ticked, Phase 04 complete. Proceed to Phase 05 (Search).**
