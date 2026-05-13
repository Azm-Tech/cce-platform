# Phase 01 — MapViewerStore + route shell

> Parent: [`../2026-05-01-sub-7.md`](../2026-05-01-sub-7.md) · Spec: [`../../specs/2026-05-01-sub-7-design.md`](../../specs/2026-05-01-sub-7-design.md) §5.3 (state management), §5.4 (URL state), §10 (error handling)

**Phase goal:** Wire the `MapViewerStore` (signals-first state container) and the `/knowledge-maps/:id` route. Land a minimal `MapViewerPage` shell that can mount, fetch a map's metadata + nodes + edges via the Phase 0.4 API, and render either a loading bar, an error banner with retry, a not-found block, or a placeholder where Phase 02's GraphCanvas will live. URL state hydrates from `:id` + `?open=&node=&q=&type=&view=`. After Phase 01, navigating to `/knowledge-maps/:id` works end-to-end without any visual graph yet.

**Tasks:** 4
**Working directory:** `/Users/m/CCE/`
**Preconditions:**
- Phase 00 closed (`88318fe`).
- web-portal: 277/277 Jest tests passing; lint + build clean.

---

## Task 1.1: `MapViewerStore` (signals + actions)

**Files (all new):**
- `frontend/apps/web-portal/src/app/features/knowledge-maps/viewer/map-viewer-store.service.ts`
- `frontend/apps/web-portal/src/app/features/knowledge-maps/viewer/map-viewer-store.service.spec.ts`

The store holds all viewer state in private signals and exposes readonly accessors + computed signals. Actions mutate signals; data fetching delegates to `KnowledgeMapsApiService`.

```ts
// map-viewer-store.service.ts
import { Injectable, computed, inject, signal } from '@angular/core';
import { KnowledgeMapsApiService } from '../knowledge-maps-api.service';
import type {
  KnowledgeMap,
  KnowledgeMapEdge,
  KnowledgeMapNode,
  NodeType,
} from '../knowledge-maps.types';

export interface ViewerTab {
  id: string;
  metadata: KnowledgeMap;
  nodes: KnowledgeMapNode[];
  edges: KnowledgeMapEdge[];
  loadedAt: number;
}

export type ViewMode = 'graph' | 'list';

export interface MapViewerState {
  /** All open tabs keyed by map id. */
  tabsById: ReadonlyMap<string, ViewerTab>;
  /** Currently active tab id (null when no tabs are open). */
  activeId: string | null;
  /** Selected node id within the active tab. */
  selectedNodeId: string | null;
  /** Search term applied to the active tab. */
  searchTerm: string;
  /** NodeType filter applied to the active tab. Empty set means no filter (all types match). */
  filters: ReadonlySet<NodeType>;
  /** Graph or list view. */
  viewMode: ViewMode;
  /** Multi-select for export. */
  selection: ReadonlySet<string>;
}

@Injectable()
export class MapViewerStore {
  private readonly api = inject(KnowledgeMapsApiService);

  private readonly _tabsById = signal<Map<string, ViewerTab>>(new Map());
  private readonly _activeId = signal<string | null>(null);
  private readonly _selectedNodeId = signal<string | null>(null);
  private readonly _searchTerm = signal('');
  private readonly _filters = signal<Set<NodeType>>(new Set());
  private readonly _viewMode = signal<ViewMode>('graph');
  private readonly _selection = signal<Set<string>>(new Set());
  private readonly _loading = signal(false);
  private readonly _errorKind = signal<string | null>(null);

  // ─── Read-only signal accessors ───
  readonly tabsById = this._tabsById.asReadonly();
  readonly activeId = this._activeId.asReadonly();
  readonly selectedNodeId = this._selectedNodeId.asReadonly();
  readonly searchTerm = this._searchTerm.asReadonly();
  readonly filters = this._filters.asReadonly();
  readonly viewMode = this._viewMode.asReadonly();
  readonly selection = this._selection.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly errorKind = this._errorKind.asReadonly();

  // ─── Computed ───
  readonly openTabs = computed(() => Array.from(this._tabsById().values()));
  readonly activeTab = computed<ViewerTab | null>(() => {
    const id = this._activeId();
    return id ? (this._tabsById().get(id) ?? null) : null;
  });
  readonly selectedNode = computed<KnowledgeMapNode | null>(() => {
    const tab = this.activeTab();
    const sid = this._selectedNodeId();
    if (!tab || !sid) return null;
    return tab.nodes.find((n) => n.id === sid) ?? null;
  });
  readonly notFound = computed(() => this._errorKind() === 'not-found');

  // ─── Actions ───

  /** Loads map + nodes + edges in parallel. Adds the tab to the store and sets it active. */
  async openTab(id: string): Promise<void> {
    // Already open? just switch.
    if (this._tabsById().has(id)) {
      this._activeId.set(id);
      return;
    }
    this._loading.set(true);
    this._errorKind.set(null);
    const [mapRes, nodesRes, edgesRes] = await Promise.all([
      this.api.getMap(id),
      this.api.getNodes(id),
      this.api.getEdges(id),
    ]);
    this._loading.set(false);
    if (!mapRes.ok) {
      this._errorKind.set(mapRes.error.kind);
      return;
    }
    if (!nodesRes.ok) {
      this._errorKind.set(nodesRes.error.kind);
      return;
    }
    if (!edgesRes.ok) {
      this._errorKind.set(edgesRes.error.kind);
      return;
    }
    const tab: ViewerTab = {
      id,
      metadata: mapRes.value,
      nodes: nodesRes.value,
      edges: edgesRes.value,
      loadedAt: Date.now(),
    };
    this._tabsById.update((m) => {
      const next = new Map(m);
      next.set(id, tab);
      return next;
    });
    this._activeId.set(id);
  }

  closeTab(id: string): void {
    this._tabsById.update((m) => {
      const next = new Map(m);
      next.delete(id);
      return next;
    });
    if (this._activeId() === id) {
      const remaining = Array.from(this._tabsById().keys());
      this._activeId.set(remaining[remaining.length - 1] ?? null);
    }
  }

  setActive(id: string): void {
    if (!this._tabsById().has(id)) return;
    this._activeId.set(id);
    this._selectedNodeId.set(null);
  }

  selectNode(id: string | null): void {
    this._selectedNodeId.set(id);
  }

  setSearch(term: string): void {
    this._searchTerm.set(term);
  }

  setFilters(types: NodeType[]): void {
    this._filters.set(new Set(types));
  }

  setViewMode(mode: ViewMode): void {
    this._viewMode.set(mode);
  }

  setSelection(ids: ReadonlySet<string>): void {
    this._selection.set(new Set(ids));
  }

  retry(): Promise<void> | void {
    const id = this._activeId();
    if (id) return this.openTab(id);
  }
}
```

**Spec (~10 tests):**
1. `openTab` calls api 3× in parallel and lands tab in store on success.
2. `openTab` failure on `getMap` sets `errorKind` and `notFound` computed to true on 404.
3. `openTab` re-opening an already-open tab just switches active without re-fetching.
4. `closeTab` removes the tab and falls back to the last remaining as active.
5. `closeTab` on the last tab leaves `activeId` null.
6. `selectNode` updates the signal; `selectedNode` computed resolves to the matching node.
7. `setSearch` / `setFilters` / `setViewMode` mutate the corresponding signals.
8. `openTabs` computed mirrors the size of the underlying map.
9. `activeTab` resolves to null when `activeId` is null.
10. `retry()` re-runs `openTab` on the current active id.

Commit: `feat(web-portal): MapViewerStore signal-driven state container (Phase 1.1)`

---

## Task 1.2: Routes — `/knowledge-maps/:id`

**Files:**
- Modify: `frontend/apps/web-portal/src/app/features/knowledge-maps/routes.ts` — add the `/:id` child.
- New: `frontend/apps/web-portal/src/app/features/knowledge-maps/map-viewer.page.ts` (stub component for now; populated in Task 1.4).

```ts
// routes.ts (after edit)
import { Routes } from '@angular/router';

export const KNOWLEDGE_MAPS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./knowledge-maps-list.page').then((m) => m.KnowledgeMapsListPage),
  },
  {
    path: ':id',
    loadComponent: () => import('./map-viewer.page').then((m) => m.MapViewerPage),
  },
];
```

```ts
// map-viewer.page.ts (stub — Task 1.4 fills it out)
import { ChangeDetectionStrategy, Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'cce-map-viewer-page',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  template: `<section class="cce-map-viewer">{{ 'knowledgeMaps.title' | translate }} (viewer scaffold)</section>`,
  styles: [`:host { display: block; padding: 1.5rem; }`],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MapViewerPage {}
```

No new tests for this task — the stub is replaced wholesale in Task 1.4. Only verifying:
- Lint clean.
- `nx build web-portal` clean (the lazy chunk is created).
- E2E navigates to `/knowledge-maps/some-id` without 404 SPA-side.

Commit: `feat(web-portal): /knowledge-maps/:id route + MapViewerPage stub (Phase 1.2)`

---

## Task 1.3: URL-state hydration helpers

**Files (new):**
- `frontend/apps/web-portal/src/app/features/knowledge-maps/viewer/url-state.ts`
- `frontend/apps/web-portal/src/app/features/knowledge-maps/viewer/url-state.spec.ts`

Pure functions to read URL query params into store actions and write store state back into URL params. Live in their own file so the page component stays thin and the parsing is unit-testable without the router.

```ts
// url-state.ts
import type { Params } from '@angular/router';
import { NODE_TYPES, type NodeType } from '../knowledge-maps.types';
import type { ViewMode } from './map-viewer-store.service';

export interface ViewerUrlState {
  open: string[];                  // tab ids excluding the active one (which is the route :id)
  node: string | null;             // selected node id
  q: string;                       // search term
  filters: NodeType[];             // empty = no filter (all types)
  view: ViewMode;
}

const VALID_VIEW_MODES: ReadonlySet<string> = new Set(['graph', 'list']);
const VALID_NODE_TYPES: ReadonlySet<string> = new Set(NODE_TYPES);

/** Parses `Params` (from ActivatedRoute.queryParams snapshot) into a typed shape. */
export function parseUrlState(params: Params): ViewerUrlState {
  const openRaw = (params['open'] as string | undefined) ?? '';
  const open = openRaw
    .split(',')
    .map((s) => s.trim())
    .filter((s) => s.length > 0);

  const node = (params['node'] as string | undefined) ?? null;

  const q = (params['q'] as string | undefined) ?? '';

  const typeRaw = (params['type'] as string | undefined) ?? '';
  const filters = typeRaw
    .split(',')
    .map((s) => s.trim())
    .filter((s): s is NodeType => VALID_NODE_TYPES.has(s));

  const viewRaw = (params['view'] as string | undefined) ?? 'graph';
  const view: ViewMode = VALID_VIEW_MODES.has(viewRaw) ? (viewRaw as ViewMode) : 'graph';

  return { open, node, q, filters, view };
}

/** Serializes a partial state into a query-param patch suitable for `router.navigate`. */
export interface UrlPatch {
  open?: string | null;
  node?: string | null;
  q?: string | null;
  type?: string | null;
  view?: string | null;
}

export function buildUrlPatch(opts: Partial<ViewerUrlState>): UrlPatch {
  const patch: UrlPatch = {};
  if (opts.open !== undefined) patch.open = opts.open.length > 0 ? opts.open.join(',') : null;
  if (opts.node !== undefined) patch.node = opts.node;
  if (opts.q !== undefined) patch.q = opts.q.length > 0 ? opts.q : null;
  if (opts.filters !== undefined) {
    patch.type = opts.filters.length > 0 ? opts.filters.join(',') : null;
  }
  if (opts.view !== undefined) patch.view = opts.view === 'graph' ? null : opts.view;
  return patch;
}
```

**Spec (~6 tests):**
1. `parseUrlState({})` returns sensible defaults (empty arrays, null node, '' q, 'graph' view).
2. `parseUrlState({ open: 'a,b,c' })` parses comma-separated open ids.
3. `parseUrlState({ type: 'Technology,InvalidType,Sector' })` filters out invalid NodeTypes.
4. `parseUrlState({ view: 'something-else' })` falls back to 'graph'.
5. `buildUrlPatch({ filters: [] })` clears the param to null (so URL drops it).
6. `buildUrlPatch({ view: 'graph' })` clears the param (default — no need to write).

Commit: `feat(web-portal): URL-state helpers for the map viewer (Phase 1.3)`

---

## Task 1.4: `MapViewerPage` shell

**Files:**
- Modify: `frontend/apps/web-portal/src/app/features/knowledge-maps/map-viewer.page.ts` — full implementation.
- New: `frontend/apps/web-portal/src/app/features/knowledge-maps/map-viewer.page.html`
- New: `frontend/apps/web-portal/src/app/features/knowledge-maps/map-viewer.page.scss`
- New: `frontend/apps/web-portal/src/app/features/knowledge-maps/map-viewer.page.spec.ts`

The shell mounts `MapViewerStore` at the component level (`providers: [MapViewerStore]`), reads the route `:id`, calls `store.openTab(id)`, and renders one of:
- progress bar (loading)
- not-found block with link back to `/knowledge-maps`
- error banner with retry button
- placeholder `<div class="cce-map-viewer__placeholder">` where Phase 2's GraphCanvas will mount.

URL query params are hydrated into the store on init.

```ts
// map-viewer.page.ts
import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslateModule } from '@ngx-translate/core';
import { MapViewerStore } from './viewer/map-viewer-store.service';
import { parseUrlState } from './viewer/url-state';

@Component({
  selector: 'cce-map-viewer-page',
  standalone: true,
  imports: [
    CommonModule, RouterLink,
    MatButtonModule, MatIconModule, MatProgressBarModule,
    TranslateModule,
  ],
  providers: [MapViewerStore],
  templateUrl: './map-viewer.page.html',
  styleUrl: './map-viewer.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MapViewerPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  readonly store = inject(MapViewerStore);

  async ngOnInit(): Promise<void> {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) return;

    // Hydrate non-route URL state into the store before opening the tab.
    const url = parseUrlState(this.route.snapshot.queryParams);
    this.store.setSearch(url.q);
    this.store.setFilters(url.filters);
    this.store.setViewMode(url.view);
    if (url.node) this.store.selectNode(url.node);

    await this.store.openTab(id);
  }

  retry(): void {
    void this.store.retry();
  }
}
```

```html
<!-- map-viewer.page.html -->
<section class="cce-map-viewer">
  <a class="cce-map-viewer__back" routerLink="/knowledge-maps">
    ← {{ 'knowledgeMaps.title' | translate }}
  </a>

  @if (store.loading()) { <mat-progress-bar mode="indeterminate" /> }

  @if (store.notFound()) {
    <div class="cce-map-viewer__notfound" role="alert">
      <h1>{{ 'knowledgeMaps.notFound' | translate }}</h1>
      <a mat-stroked-button routerLink="/knowledge-maps">
        {{ 'knowledgeMaps.title' | translate }}
      </a>
    </div>
  } @else if (store.errorKind()) {
    <div class="cce-map-viewer__error" role="alert">
      <span>{{ ('errors.' + store.errorKind()) | translate }}</span>
      <button mat-button type="button" (click)="retry()">
        {{ 'errors.retry' | translate }}
      </button>
    </div>
  }

  @if (store.activeTab(); as tab) {
    <header class="cce-map-viewer__header">
      <h1>{{ tab.metadata.nameEn }}</h1>
      <p>{{ tab.metadata.descriptionEn }}</p>
      <small>{{ tab.nodes.length }} nodes · {{ tab.edges.length }} edges</small>
    </header>

    <div class="cce-map-viewer__placeholder" data-testid="graph-placeholder">
      <p>{{ 'knowledgeMaps.viewerPlaceholder' | translate }}</p>
    </div>
  }
</section>
```

```scss
/* map-viewer.page.scss */
:host { display: block; padding: 1.5rem; max-width: 1400px; margin: 0 auto; }

.cce-map-viewer__back {
  display: inline-block;
  margin-bottom: 1rem;
  color: #1565c0;
  text-decoration: none;
  &:hover { text-decoration: underline; }
}

.cce-map-viewer__notfound,
.cce-map-viewer__error {
  background: #fdecea;
  color: #b00020;
  padding: 1rem;
  border-radius: 4px;
  margin: 1rem 0;
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 1rem;
  flex-wrap: wrap;
}

.cce-map-viewer__header {
  background: #fff;
  border-radius: 8px;
  padding: 1rem 1.5rem;
  box-shadow: 0 0 0 1px rgba(0, 0, 0, 0.06);
  margin-bottom: 1rem;

  h1 { margin: 0; }
  p { margin: 0.5rem 0; color: rgba(0, 0, 0, 0.72); }
  small { color: rgba(0, 0, 0, 0.55); }
}

.cce-map-viewer__placeholder {
  background: rgba(0, 0, 0, 0.02);
  border: 2px dashed rgba(0, 0, 0, 0.12);
  border-radius: 8px;
  padding: 4rem 2rem;
  text-align: center;
  color: rgba(0, 0, 0, 0.55);
  min-height: 400px;
  display: flex;
  align-items: center;
  justify-content: center;
}
```

Add 2 i18n keys (`knowledgeMaps.notFound` + `knowledgeMaps.viewerPlaceholder`) to `libs/i18n/src/lib/i18n/{en,ar}.json`.

**Spec (~5 tests):**
1. Init with valid id calls `store.openTab(id)`.
2. URL `?q=carbon&type=Technology&view=list&node=n1` hydrates store before opening tab.
3. 404 renders not-found block + link back to /knowledge-maps.
4. Generic error renders error banner with retry button calling `store.retry()`.
5. Active tab renders the header (name + description + node/edge counts).

Commit: `feat(web-portal): MapViewerPage shell with URL hydration + error states (Phase 1.4)`

---

## Phase 01 — completion checklist

- [ ] Task 1.1 — `MapViewerStore` (~10 tests).
- [ ] Task 1.2 — `/knowledge-maps/:id` route + stub page.
- [ ] Task 1.3 — URL-state helpers (~6 tests).
- [ ] Task 1.4 — `MapViewerPage` shell (~5 tests).
- [ ] All web-portal Jest tests passing.
- [ ] admin-cms still 218/218.
- [ ] Lint + build clean.
- [ ] Initial bundle still within budget (no eager imports of cytoscape / cytoscape-svg / jspdf).

**If all boxes ticked, Phase 01 complete. Proceed to Phase 02 (GraphCanvas).**
