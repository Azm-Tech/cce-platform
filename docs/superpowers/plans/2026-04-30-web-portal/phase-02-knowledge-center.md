# Phase 02 — Knowledge Center

> Parent: [`../2026-04-30-web-portal.md`](../2026-04-30-web-portal.md) · Spec: [`../../specs/2026-04-30-web-portal-design.md`](../../specs/2026-04-30-web-portal-design.md) §5 (`/api/resources`, `/api/resources/{id}`, `/api/resources/{id}/download`, `/api/categories`)

**Phase goal:** Public users browse the resource library, filter by category / country / type, view resource details, and trigger file downloads. After Phase 02, `/knowledge-center` is fully functional with pagination, filtering, and download flows. Categories tree browse lives at `/knowledge-center/categories` (or as a left rail widget — implementation detail below).

**Tasks:** 6
**Working directory:** `/Users/m/CCE/`
**Preconditions:**
- Phase 01 closed (`f9348a3`).
- Layout shell + filter rail primitive ready.
- `<cce-paged-table>` available from `@frontend/ui-kit` (Phase 0.1).

---

## Endpoint coverage

| Endpoint | Method | Phase 02 surface | Anonymous |
|---|---|---|---|
| `/api/categories` | GET | Task 2.2 (categories tree) | ✓ |
| `/api/resources` | GET (paged, filterable) | Task 2.3 (list page) | ✓ |
| `/api/resources/{id}` | GET | Task 2.4 (detail page) | ✓ |
| `/api/resources/{id}/download` | GET (binary) | Task 2.5 (download flow) | ✓ (rate-limited) |

## Hand-defined DTOs

```ts
// frontend/apps/web-portal/src/app/features/knowledge-center/knowledge.types.ts
import type { PagedResult } from './shared.types';

export type ResourceType = 'Pdf' | 'Video' | 'Image' | 'Link' | 'Document';

export interface ResourceCategory {
  id: string;
  nameAr: string;
  nameEn: string;
  slug: string;
  parentId: string | null;
  orderIndex: number;
}

export interface ResourceListItem {
  id: string;
  titleAr: string;
  titleEn: string;
  resourceType: ResourceType;
  categoryId: string;
  countryId: string | null;
  publishedOn: string | null;
  viewCount: number;
}

export interface Resource extends ResourceListItem {
  descriptionAr: string;
  descriptionEn: string;
  uploadedById: string;
  assetFileId: string;
  isCenterManaged: boolean;
}

export type { PagedResult };
```

```ts
// frontend/apps/web-portal/src/app/features/knowledge-center/shared.types.ts
// Re-used by Phases 02-08.
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}
```

## Folder structure

```
apps/web-portal/src/app/features/knowledge-center/
├── shared.types.ts                          # PagedResult<T> (used by other phases too)
├── knowledge.types.ts
├── knowledge-api.service.{ts,spec.ts}        # listResources, getResource, listCategories, download
├── resources-list.page.{ts,html,scss,spec.ts} # Task 2.3: filterable list
├── resource-detail.page.{ts,html,scss,spec.ts} # Task 2.4: detail + download
├── categories-tree.component.{ts,html,scss,spec.ts} # Task 2.2: filter widget
├── resource-card.component.{ts,html,scss}    # Task 2.3: card cell for the list
└── routes.ts                                # KNOWLEDGE_CENTER_ROUTES
```

---

## Task 2.1: KnowledgeApiService + types

**Files:**
- `frontend/apps/web-portal/src/app/features/knowledge-center/shared.types.ts`
- `frontend/apps/web-portal/src/app/features/knowledge-center/knowledge.types.ts`
- `frontend/apps/web-portal/src/app/features/knowledge-center/knowledge-api.service.{ts,spec.ts}`

### Step 1: Types (full content from above)

### Step 2: KnowledgeApiService

```ts
// knowledge-api.service.ts
import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { toFeatureError, type FeatureError } from '@frontend/ui-kit';
import type {
  PagedResult,
  Resource,
  ResourceCategory,
  ResourceListItem,
  ResourceType,
} from './knowledge.types';

export type Result<T> = { ok: true; value: T } | { ok: false; error: FeatureError };

@Injectable({ providedIn: 'root' })
export class KnowledgeApiService {
  private readonly http = inject(HttpClient);

  async listCategories(): Promise<Result<ResourceCategory[]>> {
    return this.run(() => firstValueFrom(this.http.get<ResourceCategory[]>('/api/categories')));
  }

  async listResources(opts: {
    page?: number;
    pageSize?: number;
    categoryId?: string;
    countryId?: string;
    resourceType?: ResourceType;
  } = {}): Promise<Result<PagedResult<ResourceListItem>>> {
    let params = new HttpParams();
    if (opts.page !== undefined) params = params.set('page', opts.page);
    if (opts.pageSize !== undefined) params = params.set('pageSize', opts.pageSize);
    if (opts.categoryId) params = params.set('categoryId', opts.categoryId);
    if (opts.countryId) params = params.set('countryId', opts.countryId);
    if (opts.resourceType) params = params.set('resourceType', opts.resourceType);
    return this.run(() =>
      firstValueFrom(this.http.get<PagedResult<ResourceListItem>>('/api/resources', { params })),
    );
  }

  async getResource(id: string): Promise<Result<Resource>> {
    return this.run(() =>
      firstValueFrom(this.http.get<Resource>(`/api/resources/${id}`)),
    );
  }

  /**
   * Returns a Blob for the SPA to materialize as a download. Caller saves it to
   * a hidden <a download> link (same pattern as admin-cms reports).
   */
  async download(id: string): Promise<Result<Blob>> {
    try {
      const value = await firstValueFrom(
        this.http.get(`/api/resources/${id}/download`, { responseType: 'blob' }),
      );
      return { ok: true, value };
    } catch (err) {
      return { ok: false, error: toFeatureError(err as HttpErrorResponse) };
    }
  }

  private async run<T>(fn: () => Promise<T>): Promise<Result<T>> {
    try {
      return { ok: true, value: await fn() };
    } catch (err) {
      return { ok: false, error: toFeatureError(err as HttpErrorResponse) };
    }
  }
}
```

### Step 3: Tests (5 tests)

1. `listCategories()` GETs `/api/categories`.
2. `listResources({ categoryId: 'c1', resourceType: 'Pdf' })` builds query string with both params.
3. `getResource('r1')` GETs `/api/resources/r1`.
4. `download('r1')` GETs `/api/resources/r1/download` with `responseType: 'blob'`; returns `{ ok: true, value: Blob }`.
5. `getResource` returns `{ kind: 'not-found' }` on 404.

### Step 4: Commit

```bash
git add frontend/apps/web-portal/src/app/features/knowledge-center/{shared.types.ts,knowledge.types.ts,knowledge-api.service.ts,knowledge-api.service.spec.ts}
git -c commit.gpgsign=false commit -m "feat(web-portal): KnowledgeApiService + DTOs (Phase 2.1)"
```

---

## Task 2.2: CategoriesTreeComponent (filter widget for the rail)

**Files:**
- `categories-tree.component.{ts,html,scss,spec.ts}`

Recursive list rendering parent → children with click selecting a categoryId (emitted via `(selectionChange)`). Used inside the FilterRail on the resources list.

```ts
@Component({
  selector: 'cce-categories-tree',
  standalone: true,
  imports: [CommonModule, MatButtonModule, TranslateModule],
  templateUrl: './categories-tree.component.html',
  styleUrl: './categories-tree.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CategoriesTreeComponent {
  @Input({ required: true }) categories: readonly ResourceCategory[] = [];
  @Input() selectedId: string | null = null;
  @Input() locale: 'ar' | 'en' = 'en';
  @Output() readonly selectionChange = new EventEmitter<string | null>();

  // Computed: build tree from flat list (parentId = null → root)
  get roots(): ResourceCategory[] {
    return this.categories.filter((c) => c.parentId === null).sort((a, b) => a.orderIndex - b.orderIndex);
  }
  childrenOf(parentId: string): ResourceCategory[] {
    return this.categories.filter((c) => c.parentId === parentId).sort((a, b) => a.orderIndex - b.orderIndex);
  }
  labelOf(c: ResourceCategory): string {
    return this.locale === 'ar' ? c.nameAr : c.nameEn;
  }
  select(id: string | null): void {
    this.selectionChange.emit(id);
  }
}
```

Template renders nested `<ul>` lists with click handlers; "All categories" reset button at top emits null.

3 tests:
- Renders root categories correctly.
- Renders nested children under their parent.
- Click emits the id; "All" emits null.

Commit: `feat(web-portal): CategoriesTreeComponent for knowledge center filter rail (Phase 2.2)`

---

## Task 2.3: ResourcesListPage (paged + filtered)

**Files:**
- `resources-list.page.{ts,html,scss,spec.ts}`
- `resource-card.component.{ts,html,scss}`

ResourcesListPage uses signals for `page`, `pageSize`, `categoryId`, `countryId`, `resourceType`, `rows`, `total`, `loading`, `errorKind`. Renders `<cce-filter-rail>` with categories tree + country (free-text input for v0.1.0, picker in Phase 04) + resource-type select. Cards rendered via `<cce-resource-card>` for each row. Pagination via `<mat-paginator>` (or the generic `<cce-paged-table>` — but cards aren't tabular; just use mat-paginator directly).

ResourceCardComponent (inputs: `resource: ResourceListItem`, `locale: 'ar' | 'en'`): clickable card showing title in selected locale, type badge, view count, link to `/knowledge-center/:id`.

URL syncing: write filters to query params (`?page=2&category=...&type=Pdf`), restore on navigation. Use `ActivatedRoute.queryParamMap` + `Router.navigate(['./'], { queryParams: ..., queryParamsHandling: 'merge', replaceUrl: true })`.

8 tests for the list page:
- Loads on init with default paging.
- Reads query params on init (page=2, categoryId, type).
- Changing category filter resets page to 1 and reloads.
- onPage updates page + size and reloads.
- Updates URL query params on filter change.
- Renders error banner when api fails.
- Empty result shows "no resources" message (i18n `resources.empty`).
- Clicking a card navigates to `/knowledge-center/:id` (verify routerLink).

Commit: `feat(web-portal): ResourcesListPage with filter rail + cards (Phase 2.3)`

---

## Task 2.4: ResourceDetailPage

**Files:**
- `resource-detail.page.{ts,html,scss,spec.ts}`

Reads `:id` from route, calls `getResource`. Renders title, description, type, view count, published date, "Download" button (gated by no permission — public). Back-to-list link. Localized via LocaleService.

5 tests:
- Loads resource on init from route.id.
- Sets errorKind on 404.
- Title/description computed from locale signal.
- Download button calls api + materializes blob.
- Anonymous users see the download button (no auth gate).

Commit: `feat(web-portal): ResourceDetailPage with download flow (Phase 2.4)`

---

## Task 2.5: Wire into app.routes.ts + i18n keys

**Files:**
- Modify: `app.routes.ts` (add `/knowledge-center` lazy route)
- Modify: `routes.ts` in features/knowledge-center
- Modify: `frontend/libs/i18n/src/lib/i18n/{en,ar}.json` (add resources.* keys: list title, filter labels, empty state, type labels, download CTA, view count)

```ts
// routes.ts
export const KNOWLEDGE_CENTER_ROUTES: Routes = [
  { path: '', loadComponent: () => import('./resources-list.page').then((m) => m.ResourcesListPage) },
  { path: ':id', loadComponent: () => import('./resource-detail.page').then((m) => m.ResourceDetailPage) },
];
```

```ts
// app.routes.ts (add)
{
  path: 'knowledge-center',
  loadChildren: () =>
    import('./features/knowledge-center/routes').then((m) => m.KNOWLEDGE_CENTER_ROUTES),
  title: 'CCE — Knowledge Center',
},
```

i18n: add to `en.json` + `ar.json`:

```json
"resources": {
  "title": "Knowledge Center",
  "empty": "No resources match your filters.",
  "filter": {
    "category": "Category",
    "country": "Country",
    "resourceType": "Type",
    "allCategories": "All categories"
  },
  "type": { "Pdf": "PDF", "Video": "Video", "Image": "Image", "Link": "Link", "Document": "Document" },
  "viewCount": "{{count}} views",
  "publishedOn": "Published on",
  "download": { "openButton": "Download", "toast": "Download started." },
  "back": "Back to all resources"
}
```

(Arabic mirror: التقارير → الموارد → etc.)

Commit: `feat(web-portal): /knowledge-center route + i18n keys (Phase 2.5)`

---

## Task 2.6: E2E smoke for knowledge-center happy path

**Files:**
- Create: `frontend/apps/web-portal-e2e/src/knowledge-center.spec.ts`

Playwright test (no actual API run — defer to Phase 9 full-stack run):

```ts
import { test, expect } from '@playwright/test';

test.describe('knowledge center smoke', () => {
  test('navigates from header → /knowledge-center', async ({ page }) => {
    await page.goto('/');
    await page.getByRole('link', { name: /knowledge center|مركز المعرفة/i }).click();
    await expect(page).toHaveURL(/\/knowledge-center/);
    await expect(page.locator('cce-filter-rail')).toBeAttached();
  });
});
```

Lint only — actual run deferred to Phase 9.

Commit: `feat(web-portal-e2e): knowledge-center navigation smoke (Phase 2.6)`

---

## Phase 02 — completion checklist

- [ ] Task 2.1 — KnowledgeApiService + types (5 tests).
- [ ] Task 2.2 — CategoriesTreeComponent (3 tests).
- [ ] Task 2.3 — ResourcesListPage with filter rail + cards (8 tests).
- [ ] Task 2.4 — ResourceDetailPage with download (5 tests).
- [ ] Task 2.5 — Routes + i18n keys.
- [ ] Task 2.6 — E2E smoke spec.
- [ ] All Jest tests passing.
- [ ] admin-cms still 218/218.
- [ ] `pnpm nx build web-portal` clean.
- [ ] `pnpm nx lint web-portal-e2e` clean.

**If all boxes ticked, Phase 02 is complete. Proceed to Phase 03 (News + Events).**
