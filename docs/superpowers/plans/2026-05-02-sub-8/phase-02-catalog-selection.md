# Phase 02 — Catalog + selection (Sub-8)

> Parent: [`../2026-05-02-sub-8.md`](../2026-05-02-sub-8.md) · Spec: [`../../specs/2026-05-02-sub-8-design.md`](../../specs/2026-05-02-sub-8-design.md) §5 (components)

**Phase goal:** Fill the three layout slots from Phase 01 with real components: header strip (name + city-type + target-year), technology catalog (search + group + click-to-toggle), selected list (cart with remove + clear). Add a placeholder totals strip showing client-side live numbers — Phase 03 swaps it for the real `TotalsBarComponent` with Run + Save buttons.

**Tasks:** 4
**Working directory:** `/Users/m/CCE/`
**Preconditions:** Phase 01 closed (commit `ca32d82` or later); 76 suites · 406 tests green.

---

## Task 2.1: `ScenarioHeaderComponent`

**Files:**
- Modify: `frontend/apps/web-portal/src/app/features/interactive-city/builder/scenario-header.component.{ts,html,scss}`.
- Create: `frontend/apps/web-portal/src/app/features/interactive-city/builder/scenario-header.component.spec.ts`.

**Behaviour:**
- Three Material inputs in a horizontal flex strip: name (`mat-input`), city-type (`mat-select`), target-year (`mat-input type="number"`).
- Reactive `FormGroup` two-way bound to store via:
  - One effect that mirrors store signals → form patch on hydrate (so URL-driven changes show up in inputs).
  - Form `valueChanges.subscribe` calling the matching store action on each value change.
- Year clamp on blur (delegated to native `min`/`max` attrs; the store also clamps in `setTargetYear` if needed).
- City-type select uses the i18n keys `interactiveCity.cityType.{Coastal,Industrial,Mixed,Residential}`.
- Localized labels via `interactiveCity.builder.{name,cityType,targetYear,namePlaceholder}`.

**Tests:**
- Renders three labelled inputs.
- Editing the name input updates `store.name()`.
- Selecting a city-type calls `store.setCityType` with the right value.
- Editing year updates `store.targetYear()`.
- Initial form values mirror current store signals (so URL-loaded scenarios show up in inputs).

## Task 2.2: `TechnologyCatalogComponent`

**Files:**
- Modify: `frontend/apps/web-portal/src/app/features/interactive-city/builder/technology-catalog.component.{ts,html,scss}`.
- Create: `frontend/apps/web-portal/src/app/features/interactive-city/builder/technology-catalog.component.spec.ts`.

**Behaviour:**
- Header: search box (Reactive `FormControl`, 200ms `debounceTime`) + category filter chips (Sub-7 pattern).
- Body: list of `mat-card` per technology. Group cards by category — render a `<h3>` per category and the cards underneath.
- Card content: name (locale-aware via `LocaleService`), category chip, carbon impact (color-coded green for negative, orange for positive), cost in USD.
- Click anywhere on the card calls `store.toggle(t.id)`. Selected cards get `aria-pressed="true"` + a `cce-catalog-card--selected` class (visible checkmark + raised border).
- Empty state when search yields no matches: `interactiveCity.catalog.empty`.

**Tests:**
- Renders a card per technology.
- Search filter narrows the list.
- Clicking a card toggles `store.selectedIds()`.
- Selected card has `aria-pressed="true"`.

## Task 2.3: `SelectedListComponent`

**Files:**
- Modify: `frontend/apps/web-portal/src/app/features/interactive-city/builder/selected-list.component.{ts,html,scss}`.
- Create: `frontend/apps/web-portal/src/app/features/interactive-city/builder/selected-list.component.spec.ts`.

**Behaviour:**
- Header: `interactiveCity.selected.title` with count interpolation; "Clear all" button (disabled when empty).
- Body: `mat-list` of compact items (name + per-row carbon/cost + remove × button).
- Remove button calls `store.toggle(id)` (toggle removes when present).
- Clear-all button calls `store.clear()`. (No confirm dialog at this size — wait for Phase 04 if it becomes a problem.)
- Empty state: `interactiveCity.selected.empty`.

**Tests:**
- Renders one row per selected technology.
- Remove × button calls `store.toggle`.
- Clear-all button calls `store.clear`.
- Empty state shows the localized message.

## Task 2.4: Wire components into the page + placeholder totals strip

**Files:**
- Modify: `frontend/apps/web-portal/src/app/features/interactive-city/scenario-builder.page.{ts,html,scss}`.

**Behaviour:**
- Replace the three placeholder slot `<div>`s with `<cce-scenario-header />`, `<cce-technology-catalog />`, `<cce-selected-list />`.
- All three sub-components receive nothing via inputs — they all `inject(ScenarioBuilderStore)` directly so the page is just the layout host.
- Add a placeholder totals strip below the three columns: shows `liveTotals().totalCarbonImpactKgPerYear` + `liveTotals().totalCostUsd`. Phase 03 replaces this with `TotalsBarComponent` (with Run/Save buttons + server summary).
- Layout: header takes the full width; catalog (left, ~60%) and selected list (right, ~40%) flex side-by-side on desktop; stack on mobile (single column when viewport ≤ 720px).

## Phase 02 close-out

- [ ] Full `nx test web-portal --watch=false` passes.
- [ ] `nx run web-portal:lint` clean (no Sub-8 warnings).
- [ ] `nx build web-portal` succeeds.
- [ ] Browser smoke at `/interactive-city`: catalog renders 4 cards, search filters them, clicking a card adds to selected list, totals update live, URL `?t=…` updates. Locale switch flips inputs and labels to Arabic.
- [ ] Phase 02 done when test count grows by ~20 to ~426.
