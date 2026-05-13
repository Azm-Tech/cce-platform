# Phase 01 — Store + page shell (Sub-8)

> Parent: [`../2026-05-02-sub-8.md`](../2026-05-02-sub-8.md) · Spec: [`../../specs/2026-05-02-sub-8-design.md`](../../specs/2026-05-02-sub-8-design.md) §3 (state shape), §4 (data flow), §6 (URL state)

**Phase goal:** Bring the `ScenarioBuilderStore` to life. Wire `init()` (load catalog + saved scenarios), implement every action the later phases need (toggle / clear / setName / setCityType / setTargetYear / loadFromSaved / run / save / delete), build the computed signals (`liveTotals`, `selectedTechnologies`, `dirty`, `canRun`, `canSave`), and add the URL hydrate ↔ sync effect. Then wire `ScenarioBuilderPage` to call `store.init()` + URL hydrate on entry and render layout slots ready for Phase 02 to fill.

**Tasks:** 3
**Working directory:** `/Users/m/CCE/`
**Preconditions:** Phase 00 closed (commit `79cead0` or later); 74 suites · 381 tests green.

---

## Task 1.1: Complete `ScenarioBuilderStore`

**Files:**
- Modify: `frontend/apps/web-portal/src/app/features/interactive-city/builder/scenario-builder-store.service.ts` — replace the Phase 00 stub with the full implementation.
- Create: `frontend/apps/web-portal/src/app/features/interactive-city/builder/scenario-builder-store.service.spec.ts` — store unit tests.

**Final store responsibilities:**

- **State:** the 11 signals declared in the Phase 00 stub (kept).
- **Computed:**
  - `liveTotals` — sum of `carbonImpactKgPerYear` and `costUsd` over selected technologies.
  - `selectedTechnologies` — joins `selectedIds` against `technologies()` for display.
  - `dirty` — true if state diverges from the last `loadFromSaved` baseline (or initial-empty state if never loaded). False during URL hydration so the unsaved-changes dialog doesn't fire on page load.
  - `canRun` — `selectedIds().size > 0 && !running()`.
  - `canSave` — `selectedIds().size > 0 && name().trim() !== '' && !saving()`.
- **Actions:**
  - `init()` — fires `listTechnologies` and (when `auth.isAuthenticated()`) `listMyScenarios`; sets corresponding loading/error/data signals.
  - `setCityType(c)`, `setTargetYear(y)`, `setName(n)`, `toggle(id)`, `clear()`.
  - `loadFromSaved(scenario)` — parses `configurationJson`, populates state, clears `serverResult`, sets the dirty baseline.
  - `run()` — guards on `canRun()`, sets `running:true`, posts `runScenario`, on success swaps `serverResult`, returns `Result<RunResult>`.
  - `save()` — guards on `canSave()`, sets `saving:true`, posts `saveScenario`, on success prepends to `savedScenarios()` and updates the dirty baseline. Returns `Result<SavedScenario>`.
  - `delete(id)` — calls `deleteMyScenario`, drops the row from `savedScenarios()`. Returns `Result<void>`.
  - `applyUrlState(s)` / `toUrlState()` — translate between `UrlState` and the editable signals; called by the page's URL effect.
  - `markHydrating(true|false)` — toggles a `hydrating` flag the `dirty` computed reads.

**Strict TDD:** write the spec first; keep tests at the action + computed level (mock `InteractiveCityApiService` and `AuthService` via `useValue`).

Test list (the spec covers these as separate `it()` blocks):

1. `liveTotals` sums carbon + cost over selected, returns zero on empty selection.
2. `liveTotals` ignores selected ids that aren't in the catalog (defensive).
3. `selectedTechnologies` returns the catalog rows in catalog order, not selection order.
4. `canRun` reflects `selectedIds.size > 0` AND `!running()`.
5. `canSave` reflects size + non-empty name + `!saving()`.
6. `dirty` is false on first init, becomes true after a toggle, resets to false after `loadFromSaved`.
7. `dirty` stays false when `markHydrating(true)` wraps a setter.
8. `init()` calls `listTechnologies` + populates `technologies()` + sets `catalogLoading` correctly across success.
9. `init()` sets `catalogError` to the FeatureError kind on failure.
10. `init()` does NOT call `listMyScenarios` when not authenticated.
11. `init()` calls `listMyScenarios` when authenticated and populates `savedScenarios()`.
12. `toggle(id)` adds when absent, removes when present.
13. `clear()` empties `selectedIds`.
14. `setName / setCityType / setTargetYear` update the right signal.
15. `loadFromSaved` parses `configurationJson` and sets `cityType`, `targetYear`, `name`, `selectedIds`. Drops invalid GUIDs in the config.
16. `run()` posts `RunRequest` built from current state and stores `serverResult` on success.
17. `run()` short-circuits when `selectedIds` is empty (returns ok with zero totals — no network call).
18. `run()` clears `serverResult` then refills on success.
19. `save()` posts `SaveRequest` with both `nameAr` and `nameEn` set to `name()`, prepends the returned scenario to `savedScenarios`, updates dirty baseline.
20. `delete(id)` removes from `savedScenarios`.
21. `applyUrlState` populates state but stays inside `markHydrating` so `dirty` stays false.
22. `toUrlState` returns the current editable state in `UrlState` shape.

- [ ] **Step 1: Write the spec** with all 22 tests.
- [ ] **Step 2:** Run it; it should fail (most methods don't exist yet).
- [ ] **Step 3: Replace the stub store** with the full implementation.
- [ ] **Step 4:** Run the spec again; expect 22 passing.
- [ ] **Step 5:** Commit (`feat(interactive-city): implement ScenarioBuilderStore`).

---

## Task 1.2: URL hydrate ↔ sync effect inside the page

**Files:**
- Modify: `frontend/apps/web-portal/src/app/features/interactive-city/scenario-builder.page.ts` — add ActivatedRoute injection + two effects: hydrate-from-URL (on init) and sync-to-URL (debounced).

**Behavior:**
- On `ngOnInit`: read `route.snapshot.queryParamMap` → `parseUrlState` → `store.markHydrating(true)` → `store.applyUrlState(s)` → `store.markHydrating(false)`. Then call `store.init()` (catalog + saved-if-auth load).
- Single `effect()` listens to the editable signals and writes back via `Router.navigate` with `queryParamsHandling: 'merge'` and `replaceUrl: true`. 200ms debounce via `rxjs` `Subject` + `debounceTime` is overkill — simpler: a small `setTimeout`-backed debounce keeps Phase 01 dependency-free.
- The sync effect must be created AFTER hydration to avoid an immediate write-back of the parsed values.

- [ ] **Step 1: Replace `scenario-builder.page.ts`** with the wired version (code shown below).
- [ ] **Step 2: Add a page integration spec** (`scenario-builder.page.spec.ts`) covering URL hydration + load call.
- [ ] **Step 3:** Run tests.
- [ ] **Step 4:** Commit (`feat(interactive-city): wire URL hydration in scenario-builder page`).

```ts
// scenario-builder.page.ts (final state)
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, effect, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { ScenarioBuilderStore } from './builder/scenario-builder-store.service';
import { buildUrlPatch, parseUrlState } from './lib/url-state';

@Component({
  selector: 'cce-scenario-builder-page',
  standalone: true,
  imports: [TranslateModule],
  providers: [ScenarioBuilderStore],
  templateUrl: './scenario-builder.page.html',
  styleUrl: './scenario-builder.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScenarioBuilderPage implements OnInit {
  readonly store = inject(ScenarioBuilderStore);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  ngOnInit(): void {
    // Hydrate from URL before init() so the load doesn't race the first render.
    const urlState = parseUrlState(this.route.snapshot.queryParamMap);
    this.store.markHydrating(true);
    this.store.applyUrlState(urlState);
    this.store.markHydrating(false);

    // Sync editable state → URL with a tiny debounce.
    let pending: ReturnType<typeof setTimeout> | null = null;
    effect(() => {
      const patch = buildUrlPatch(this.store.toUrlState());
      if (pending) clearTimeout(pending);
      pending = setTimeout(() => {
        this.router.navigate([], {
          queryParams: patch,
          queryParamsHandling: 'merge',
          replaceUrl: true,
        });
      }, 200);
    });

    // Kick off the data loads.
    void this.store.init();
  }
}
```

```html
<!-- scenario-builder.page.html (final state for Phase 01) -->
<section class="cce-scenario-builder">
  <h1>{{ 'interactiveCity.builder.title' | translate }}</h1>
  <p class="cce-scenario-builder__subtitle">
    {{ 'interactiveCity.builder.subtitle' | translate }}
  </p>

  @if (store.catalogLoading()) {
    <p class="cce-scenario-builder__loading">{{ 'errors.loading' | translate }}</p>
  }

  @if (store.catalogError(); as kind) {
    <div class="cce-scenario-builder__error" role="alert">
      <span>{{ 'interactiveCity.errors.loadCatalog' | translate }}</span>
      <button mat-button type="button" (click)="store.init()">
        {{ 'interactiveCity.errors.retry' | translate }}
      </button>
    </div>
  }

  <!-- Phase 02 fills these slots -->
  <div class="cce-scenario-builder__layout">
    <div class="cce-scenario-builder__header-slot"></div>
    <div class="cce-scenario-builder__catalog-slot"></div>
    <div class="cce-scenario-builder__selected-slot"></div>
  </div>

  <!-- Phase 03 fills the totals bar; Phase 04 mounts the drawer. -->
</section>
```

---

## Task 1.3: Smoke + close-out

- [ ] **Step 1:** Full `nx test web-portal --watch=false` green; `nx run web-portal:lint` clean (no Sub-8 warnings); `nx build web-portal` succeeds.
- [ ] **Step 2:** Browser smoke: `/interactive-city` renders the title + subtitle + an empty layout grid (no errors, no spinner stuck on). With a populated query `?city=Industrial&year=2035&t=guid` the URL round-trips through hydrate ↔ sync without flicker.
- [ ] **Step 3:** Phase 01 done. Phase 02 plan written when we're ready (catalog + selected list + header form).

**Phase 01 done when:**
- Test count grows by ~25 (store: 22; page: ~3) to ~406.
- 2 commits land on `main` with green CI.
- Browser shows the empty-layout shell at `/interactive-city`.
