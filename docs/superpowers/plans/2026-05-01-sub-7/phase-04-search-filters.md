# Phase 04 — Search + filters

> Parent: [`../2026-05-01-sub-7.md`](../2026-05-01-sub-7.md) · Spec: [`../../specs/2026-05-01-sub-7-design.md`](../../specs/2026-05-01-sub-7-design.md) §8 (UX: highlight + dim), §9 (search + filter user flow)

**Phase goal:** Add a search input + NodeType filter chips above the graph. Matching nodes light up; non-matching nodes dim to 30% opacity. Filter + search compose: a node must match BOTH the search term (substring match against either localized name) AND any active type filter (empty filter set = "all types match"). URL syncs `?q=` + `?type=` with `replaceUrl: true` so deep-linking preserves the filter state. After Phase 04, "find a concept and see what it connects to" works.

**Tasks:** 4
**Working directory:** `/Users/m/CCE/`
**Preconditions:**
- Phase 03 closed (`05c7ff9`).
- web-portal: 322/322 Jest tests passing; lint + build clean.

---

## Task 4.1: Search predicate helper

**Files (new):**
- `frontend/apps/web-portal/src/app/features/knowledge-maps/lib/search.ts`
- `frontend/apps/web-portal/src/app/features/knowledge-maps/lib/search.spec.ts`

Pure function `nodeMatches(node, term, filters)` returns true when:
- `term` matches a substring of `nameAr` OR `nameEn` (case-insensitive), AND
- `filters` is empty OR contains `node.nodeType`.

Empty term + empty filters means "everything matches" (every-true short circuit). Whitespace term is treated as empty.

```ts
// search.ts
import type { KnowledgeMapNode, NodeType } from '../knowledge-maps.types';

export function nodeMatches(
  node: KnowledgeMapNode,
  term: string,
  filters: ReadonlySet<NodeType>,
): boolean {
  // Empty filter set = no type filter
  if (filters.size > 0 && !filters.has(node.nodeType)) return false;
  const trimmed = term.trim();
  if (!trimmed) return true; // No search term — type filter alone gates
  const needle = trimmed.toLocaleLowerCase();
  return (
    node.nameAr.toLocaleLowerCase().includes(needle) ||
    node.nameEn.toLocaleLowerCase().includes(needle)
  );
}
```

Tests (~5):
1. Empty term + empty filters → all nodes match.
2. Term matches case-insensitive substring of nameEn.
3. Term matches substring of nameAr (Unicode-safe).
4. Filter set excludes nodes whose nodeType isn't in the set.
5. Term + filter compose with AND semantics.

Commit: `feat(web-portal): nodeMatches search predicate (Phase 4.1)`

---

## Task 4.2: Extend `MapViewerStore` with `matchedIds` + `dimmedIds`

**Files (modify):**
- `viewer/map-viewer-store.service.ts` — add two computed signals.
- `viewer/map-viewer-store.service.spec.ts` — add tests.

```ts
readonly matchedIds = computed<ReadonlySet<string>>(() => {
  const tab = this.activeTab();
  if (!tab) return new Set();
  const term = this._searchTerm();
  const filters = this._filters();
  const matched = new Set<string>();
  for (const n of tab.nodes) {
    if (nodeMatches(n, term, filters)) matched.add(n.id);
  }
  return matched;
});

readonly dimmedIds = computed<ReadonlySet<string>>(() => {
  const tab = this.activeTab();
  if (!tab) return new Set();
  // No active filter (empty term + empty filters) means nothing dims.
  if (!this._searchTerm().trim() && this._filters().size === 0) return new Set();
  const matched = this.matchedIds();
  const dimmed = new Set<string>();
  for (const n of tab.nodes) {
    if (!matched.has(n.id)) dimmed.add(n.id);
  }
  return dimmed;
});
```

Tests (~3 new, joining the 10 existing):
- Empty term + empty filters → `dimmedIds` is empty (nothing dims).
- Search term matches one node → `dimmedIds` contains the others.
- Filter excludes one type → nodes of that type are dimmed.

Commit: `feat(web-portal): MapViewerStore matchedIds + dimmedIds computed signals (Phase 4.2)`

---

## Task 4.3: `SearchAndFiltersComponent`

**Files (new):**
- `viewer/search-and-filters.component.{ts,html,scss,spec.ts}`

Signal inputs:
- `searchTerm: input<string>('')`
- `filters: input<ReadonlySet<NodeType>>(new Set())`
- `nodeTypes: input<readonly NodeType[]>(NODE_TYPES)`

Outputs:
- `searchTermChange = output<string>()` — fires after debounce.
- `filtersChange = output<ReadonlySet<NodeType>>()` — fires on chip toggle.

Internals:
- Local `searchInput = signal('')` synced from input + emits debounced changes.
- Material `mat-form-field` + `mat-input` for the search box.
- Material chip-listbox with one chip per NodeType; aria-selected reflects membership in `filters()`.
- 200ms debounce on the search input via `rxjs.debounceTime` or a manual `setTimeout` cleanup.

For simplicity + signal-first style, use a manual `setTimeout` debounce in the input handler — no RxJS for one signal. Cleanup the timer on input changes + on destroy.

Tests (~5):
1. Renders one chip per NodeType.
2. Typing in the search input emits `(searchTermChange)` after 200ms debounce.
3. Clicking an inactive chip emits `(filtersChange)` with the type added.
4. Clicking an active chip emits `(filtersChange)` with the type removed.
5. searchTerm input updates the visible input value.

Commit: `feat(web-portal): SearchAndFiltersComponent (Phase 4.3)`

---

## Task 4.4: Wire into `MapViewerPage` + URL sync + i18n

**Files (modify + new):**
- `map-viewer.page.{ts,html,scss}` — render `<cce-search-and-filters>` above the graph; pass store signals; on `(searchTermChange)` call `store.setSearch(...)`; on `(filtersChange)` call `store.setFilters(...)`. Pass `store.dimmedIds()` to `<cce-graph-canvas>`.
- New: URL-sync `effect()` inside MapViewerPage that watches `store.searchTerm()` + `store.filters()` and calls `router.navigate(...)` with `queryParamsHandling: 'merge'` + `replaceUrl: true` using Phase 1.3's `buildUrlPatch`.
- `map-viewer.page.spec.ts` — add 2 tests for URL sync + filter wiring.
- i18n keys: `knowledgeMaps.search.placeholder`, `knowledgeMaps.filter.title`.

URL sync effect:

```ts
constructor() {
  effect(() => {
    const q = this.store.searchTerm();
    const filters = Array.from(this.store.filters());
    const patch = buildUrlPatch({ q, filters });
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: patch,
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  });
}
```

Guard: skip the URL sync on the initial frame (when both q is '' and filters is empty) so we don't immediately overwrite the URL the user navigated to.

Tests:
1. Search term change syncs `?q=` to the URL.
2. Filter chip toggle syncs `?type=` to the URL.

Commit: `feat(web-portal): wire SearchAndFilters into MapViewerPage + URL sync (Phase 4.4)`

---

## Phase 04 — completion checklist

- [ ] Task 4.1 — `nodeMatches` predicate (~5 tests).
- [ ] Task 4.2 — Store `matchedIds` + `dimmedIds` (~3 new tests, 13 total).
- [ ] Task 4.3 — `SearchAndFiltersComponent` (~5 tests).
- [ ] Task 4.4 — Wire into page + URL sync + i18n + page-spec extension.
- [ ] All web-portal Jest tests passing.
- [ ] admin-cms still 218/218.
- [ ] Lint + build clean.

**If all boxes ticked, Phase 04 complete. Proceed to Phase 05 (Multi-map tabs).**
