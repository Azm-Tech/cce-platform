# Phase 00 — Cross-cutting (Sub-7)

> Parent: [`../2026-05-01-sub-7.md`](../2026-05-01-sub-7.md) · Spec: [`../../specs/2026-05-01-sub-7-design.md`](../../specs/2026-05-01-sub-7-design.md) §5 (architecture), §6 (DTOs), §7 (endpoint coverage)

**Phase goal:** Lay the foundation for the Knowledge Maps full UX without surfacing any user-visible UI changes. Add the 3 heavy dependencies (lazy-loaded only on the Maps route), build the dynamic-import loader for Cytoscape, define the visual style registry for the 3 NodeTypes + 3 RelationshipTypes, and extend `KnowledgeMapsApiService` with `getMap` / `getNodes` / `getEdges`. Phase 01 starts wiring these into a real page.

**Tasks:** 4
**Working directory:** `/Users/m/CCE/`
**Preconditions:**
- Sub-6 closed (`web-portal-v0.1.0` tag exists; main branch at `6d196d2` or later).
- web-portal: 265/265 Jest tests passing; lint + build clean.

---

## Task 0.1: New dependencies + extended types

**Files:**
- Modify: `frontend/package.json` — add 3 production deps + 1 dev dep.
- Modify: `frontend/apps/web-portal/src/app/features/knowledge-maps/knowledge-maps.types.ts` — add `NodeType`, `RelationshipType`, `KnowledgeMapNode`, `KnowledgeMapEdge`.

**`package.json` additions** (under `dependencies`):
```json
"cytoscape": "^3.30.0",
"cytoscape-svg": "^0.4.0",
"jspdf": "^2.5.1"
```
Under `devDependencies`:
```json
"@types/cytoscape": "^3.21.0"
```

**`knowledge-maps.types.ts` final state:**
```ts
// Existing (unchanged from Sub-6 Phase 9.1)
export interface KnowledgeMap {
  id: string;
  nameAr: string;
  nameEn: string;
  descriptionAr: string;
  descriptionEn: string;
  slug: string;
  isActive: boolean;
}

// NEW
export type NodeType = 'Technology' | 'Sector' | 'SubTopic';
export const NODE_TYPES: readonly NodeType[] = ['Technology', 'Sector', 'SubTopic'] as const;

export type RelationshipType = 'ParentOf' | 'RelatedTo' | 'RequiredBy';
export const RELATIONSHIP_TYPES: readonly RelationshipType[] = ['ParentOf', 'RelatedTo', 'RequiredBy'] as const;

export interface KnowledgeMapNode {
  id: string;
  mapId: string;
  nameAr: string;
  nameEn: string;
  nodeType: NodeType;
  descriptionAr: string | null;
  descriptionEn: string | null;
  iconUrl: string | null;
  layoutX: number;
  layoutY: number;
  orderIndex: number;
}

export interface KnowledgeMapEdge {
  id: string;
  mapId: string;
  fromNodeId: string;
  toNodeId: string;
  relationshipType: RelationshipType;
  orderIndex: number;
}
```

**Steps:**

- [ ] **1. Install deps**
  ```bash
  cd /Users/m/CCE/frontend && pnpm add cytoscape@^3.30.0 cytoscape-svg@^0.4.0 jspdf@^2.5.1 && pnpm add -D @types/cytoscape@^3.21.0
  ```
  Expected: `package.json` + `pnpm-lock.yaml` updated; no other side effects.

- [ ] **2. Extend `knowledge-maps.types.ts`** — replace file contents with the snippet above.

- [ ] **3. Verify type compile**
  ```bash
  cd /Users/m/CCE/frontend && npx tsc --noEmit -p apps/web-portal/tsconfig.app.json
  ```
  Expected: zero errors.

- [ ] **4. Run web-portal lint to confirm no regressions**
  ```bash
  cd /Users/m/CCE/frontend && npx nx lint web-portal
  ```
  Expected: "Successfully ran target lint for project web-portal".

- [ ] **5. Run web-portal jest sweep**
  ```bash
  cd /Users/m/CCE/frontend && npx jest --selectProjects web-portal
  ```
  Expected: all 265 prior tests still pass; no new tests yet.

- [ ] **6. Commit**
  ```bash
  cd /Users/m/CCE && git add frontend/package.json frontend/pnpm-lock.yaml frontend/apps/web-portal/src/app/features/knowledge-maps/knowledge-maps.types.ts && git -c commit.gpgsign=false commit -m "chore(web-portal): add cytoscape + cytoscape-svg + jspdf deps + extended Map types (Phase 0.1)"
  ```

---

## Task 0.2: Cytoscape lazy-loader

**Files (all new):**
- `frontend/apps/web-portal/src/app/features/knowledge-maps/lib/cytoscape-loader.ts`
- `frontend/apps/web-portal/src/app/features/knowledge-maps/lib/cytoscape-loader.spec.ts`

The loader is a small module-level singleton. Public API:

```ts
// cytoscape-loader.ts
import type { Core, ElementDefinition } from 'cytoscape';

let cytoscapeModulePromise: Promise<typeof import('cytoscape')> | null = null;
let svgPluginRegistered = false;

/** Lazy-imports cytoscape on first call. Subsequent calls reuse the same Promise. */
export async function loadCytoscape(): Promise<typeof import('cytoscape')> {
  if (!cytoscapeModulePromise) {
    cytoscapeModulePromise = import('cytoscape');
  }
  return cytoscapeModulePromise;
}

/** Lazy-imports cytoscape-svg + registers the plugin. Idempotent. */
export async function ensureSvgPlugin(): Promise<void> {
  if (svgPluginRegistered) return;
  const [cytoscape, svgModule] = await Promise.all([
    loadCytoscape(),
    import('cytoscape-svg'),
  ]);
  // cytoscape.use takes the plugin's default export
  cytoscape.default.use(svgModule.default);
  svgPluginRegistered = true;
}

/** Public mount helper. Tests can spy on this. */
export interface MountOptions {
  container: HTMLElement;
  elements: ElementDefinition[];
  style: cytoscape.Stylesheet[];
  zoom?: number;
  pan?: { x: number; y: number };
  boxSelectionEnabled?: boolean;
}

export async function mountCytoscape(opts: MountOptions): Promise<Core> {
  const { default: cytoscape } = await loadCytoscape();
  return cytoscape({
    container: opts.container,
    elements: opts.elements,
    style: opts.style,
    layout: { name: 'preset' },
    zoom: opts.zoom ?? 1,
    pan: opts.pan ?? { x: 0, y: 0 },
    boxSelectionEnabled: opts.boxSelectionEnabled ?? true,
    minZoom: 0.25,
    maxZoom: 4,
    wheelSensitivity: 0.5,
  });
}

/** Test-only: reset module-level state between tests. */
export function _resetForTest(): void {
  cytoscapeModulePromise = null;
  svgPluginRegistered = false;
}
```

**`cytoscape-loader.spec.ts` (test mode mocks the dynamic import):**
```ts
import { _resetForTest, ensureSvgPlugin, loadCytoscape } from './cytoscape-loader';

// jest auto-mocks for dynamic ESM imports — we use jest.doMock
jest.mock(
  'cytoscape',
  () => ({
    __esModule: true,
    default: Object.assign(jest.fn(), { use: jest.fn() }),
  }),
  { virtual: true },
);
jest.mock(
  'cytoscape-svg',
  () => ({ __esModule: true, default: { name: 'svg-plugin' } }),
  { virtual: true },
);

describe('cytoscape-loader', () => {
  beforeEach(() => _resetForTest());

  it('loadCytoscape memoizes the import promise', async () => {
    const a = await loadCytoscape();
    const b = await loadCytoscape();
    expect(a).toBe(b);
  });

  it('ensureSvgPlugin registers the plugin exactly once across N calls', async () => {
    const cy = await loadCytoscape();
    const useSpy = cy.default.use as jest.Mock;
    useSpy.mockClear();
    await ensureSvgPlugin();
    await ensureSvgPlugin();
    await ensureSvgPlugin();
    expect(useSpy).toHaveBeenCalledTimes(1);
  });

  it('_resetForTest clears the singleton state (useful for isolation)', async () => {
    await loadCytoscape();
    _resetForTest();
    const cy = await loadCytoscape();
    expect(cy).toBeDefined();
  });
});
```

**Steps:**

- [ ] **1. Write the spec file** above.
- [ ] **2. Run failing test** — `npx jest --testPathPatterns='cytoscape-loader'` should fail with "module not found" or similar.
- [ ] **3. Write `cytoscape-loader.ts`** above.
- [ ] **4. Re-run** — all 3 tests pass.
- [ ] **5. Lint** — `npx nx lint web-portal` clean.
- [ ] **6. Commit**
  ```bash
  cd /Users/m/CCE && git add frontend/apps/web-portal/src/app/features/knowledge-maps/lib/cytoscape-loader.{ts,spec.ts} && git -c commit.gpgsign=false commit -m "feat(web-portal): cytoscape lazy-loader with SVG plugin (Phase 0.2)"
  ```

---

## Task 0.3: Cytoscape style registry

**Files (all new):**
- `frontend/apps/web-portal/src/app/features/knowledge-maps/lib/cytoscape-styles.ts`
- `frontend/apps/web-portal/src/app/features/knowledge-maps/lib/cytoscape-styles.spec.ts`

Per-NodeType + per-RelationshipType visual styles, returned as a Cytoscape `Stylesheet[]`. Pure function — easy to test.

```ts
// cytoscape-styles.ts
import type { Stylesheet } from 'cytoscape';

/**
 * Cytoscape stylesheet for the knowledge-maps viewer.
 * Distinguishes 3 NodeTypes (Technology / Sector / SubTopic) and
 * 3 RelationshipTypes (ParentOf / RelatedTo / RequiredBy) by color
 * and shape. Selectors use `data(nodeType)` / `data(relationshipType)`.
 */
export function buildStylesheet(): Stylesheet[] {
  return [
    // Base node
    {
      selector: 'node',
      style: {
        'label': 'data(label)',
        'text-valign': 'center',
        'text-halign': 'center',
        'color': '#fff',
        'font-size': 12,
        'text-outline-width': 1,
        'text-outline-color': 'rgba(0, 0, 0, 0.4)',
        'width': 80,
        'height': 80,
        'border-width': 2,
        'border-color': 'rgba(0, 0, 0, 0.15)',
      },
    },
    // NodeType — Technology (blue, ellipse)
    {
      selector: 'node[nodeType = "Technology"]',
      style: { 'background-color': '#1565c0', 'shape': 'ellipse' },
    },
    // NodeType — Sector (purple, round-rectangle)
    {
      selector: 'node[nodeType = "Sector"]',
      style: { 'background-color': '#6a1b9a', 'shape': 'round-rectangle' },
    },
    // NodeType — SubTopic (teal, diamond)
    {
      selector: 'node[nodeType = "SubTopic"]',
      style: { 'background-color': '#00897b', 'shape': 'diamond' },
    },
    // Selected highlight
    {
      selector: 'node:selected',
      style: { 'border-width': 4, 'border-color': '#fbc02d' },
    },
    // Dimmed (filter / search non-match)
    {
      selector: 'node.cce-dim',
      style: { 'opacity': 0.3 },
    },
    // Base edge
    {
      selector: 'edge',
      style: {
        'width': 2,
        'curve-style': 'bezier',
        'target-arrow-shape': 'triangle',
      },
    },
    // RelationshipType — ParentOf (solid, thick)
    {
      selector: 'edge[relationshipType = "ParentOf"]',
      style: { 'line-color': '#1565c0', 'target-arrow-color': '#1565c0', 'width': 3 },
    },
    // RelationshipType — RelatedTo (dashed)
    {
      selector: 'edge[relationshipType = "RelatedTo"]',
      style: {
        'line-color': '#757575',
        'target-arrow-color': '#757575',
        'line-style': 'dashed',
      },
    },
    // RelationshipType — RequiredBy (dotted, red-orange)
    {
      selector: 'edge[relationshipType = "RequiredBy"]',
      style: {
        'line-color': '#e64a19',
        'target-arrow-color': '#e64a19',
        'line-style': 'dotted',
      },
    },
    // Dimmed edge (when either endpoint is dimmed)
    {
      selector: 'edge.cce-dim',
      style: { 'opacity': 0.15 },
    },
  ];
}
```

**Spec:**
```ts
import { buildStylesheet } from './cytoscape-styles';

describe('buildStylesheet', () => {
  const sheet = buildStylesheet();

  it('returns an array of Cytoscape style entries', () => {
    expect(Array.isArray(sheet)).toBe(true);
    expect(sheet.length).toBeGreaterThan(0);
    sheet.forEach((entry) => {
      expect(typeof entry.selector).toBe('string');
      expect(typeof entry.style).toBe('object');
    });
  });

  it('defines a style for every NodeType', () => {
    const selectors = sheet.map((e) => e.selector);
    expect(selectors).toContain('node[nodeType = "Technology"]');
    expect(selectors).toContain('node[nodeType = "Sector"]');
    expect(selectors).toContain('node[nodeType = "SubTopic"]');
  });

  it('defines a style for every RelationshipType', () => {
    const selectors = sheet.map((e) => e.selector);
    expect(selectors).toContain('edge[relationshipType = "ParentOf"]');
    expect(selectors).toContain('edge[relationshipType = "RelatedTo"]');
    expect(selectors).toContain('edge[relationshipType = "RequiredBy"]');
  });

  it('defines selected + dimmed states for nodes and edges', () => {
    const selectors = sheet.map((e) => e.selector);
    expect(selectors).toContain('node:selected');
    expect(selectors).toContain('node.cce-dim');
    expect(selectors).toContain('edge.cce-dim');
  });
});
```

**Steps:**

- [ ] **1. Write the spec.**
- [ ] **2. Run failing test** — `npx jest --testPathPatterns='cytoscape-styles'` fails (file not found).
- [ ] **3. Write `cytoscape-styles.ts`.**
- [ ] **4. Re-run** — 4 tests pass.
- [ ] **5. Lint** clean.
- [ ] **6. Commit**
  ```bash
  cd /Users/m/CCE && git add frontend/apps/web-portal/src/app/features/knowledge-maps/lib/cytoscape-styles.{ts,spec.ts} && git -c commit.gpgsign=false commit -m "feat(web-portal): cytoscape style registry for 3 NodeTypes + 3 RelationshipTypes (Phase 0.3)"
  ```

---

## Task 0.4: Extended `KnowledgeMapsApiService`

**Files (modify + extend tests):**
- Modify: `frontend/apps/web-portal/src/app/features/knowledge-maps/knowledge-maps-api.service.ts` — add `getMap`, `getNodes`, `getEdges`.
- Modify: `frontend/apps/web-portal/src/app/features/knowledge-maps/knowledge-maps-api.service.spec.ts` — add corresponding tests.

**Service final state:**
```ts
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { toFeatureError, type FeatureError } from '@frontend/ui-kit';
import type { KnowledgeMap, KnowledgeMapEdge, KnowledgeMapNode } from './knowledge-maps.types';

export type Result<T> = { ok: true; value: T } | { ok: false; error: FeatureError };

@Injectable({ providedIn: 'root' })
export class KnowledgeMapsApiService {
  private readonly http = inject(HttpClient);

  async listMaps(): Promise<Result<KnowledgeMap[]>> {
    return this.run(() =>
      firstValueFrom(this.http.get<KnowledgeMap[]>('/api/knowledge-maps')),
    );
  }

  async getMap(id: string): Promise<Result<KnowledgeMap>> {
    return this.run(() =>
      firstValueFrom(
        this.http.get<KnowledgeMap>(`/api/knowledge-maps/${encodeURIComponent(id)}`),
      ),
    );
  }

  async getNodes(id: string): Promise<Result<KnowledgeMapNode[]>> {
    return this.run(() =>
      firstValueFrom(
        this.http.get<KnowledgeMapNode[]>(
          `/api/knowledge-maps/${encodeURIComponent(id)}/nodes`,
        ),
      ),
    );
  }

  async getEdges(id: string): Promise<Result<KnowledgeMapEdge[]>> {
    return this.run(() =>
      firstValueFrom(
        this.http.get<KnowledgeMapEdge[]>(
          `/api/knowledge-maps/${encodeURIComponent(id)}/edges`,
        ),
      ),
    );
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

**New tests added to `knowledge-maps-api.service.spec.ts`** (5 new tests, joining the existing 2):

```ts
const NODE: KnowledgeMapNode = {
  id: 'n1', mapId: 'm1',
  nameAr: 'تقنية', nameEn: 'Technology',
  nodeType: 'Technology',
  descriptionAr: null, descriptionEn: null,
  iconUrl: null,
  layoutX: 100, layoutY: 200,
  orderIndex: 0,
};
const EDGE: KnowledgeMapEdge = {
  id: 'e1', mapId: 'm1',
  fromNodeId: 'n1', toNodeId: 'n2',
  relationshipType: 'ParentOf',
  orderIndex: 0,
};

it('getMap GETs /api/knowledge-maps/{id}', async () => {
  const promise = sut.getMap('m1');
  const req = http.expectOne('/api/knowledge-maps/m1');
  expect(req.request.method).toBe('GET');
  req.flush(SAMPLE);
  const res = await promise;
  expect(res.ok).toBe(true);
  if (res.ok) expect(res.value.id).toBe('m1');
});

it('getMap returns not-found on 404', async () => {
  const promise = sut.getMap('missing');
  http.expectOne('/api/knowledge-maps/missing').flush('', { status: 404, statusText: 'Not Found' });
  const res = await promise;
  expect(res.ok).toBe(false);
  if (!res.ok) expect(res.error.kind).toBe('not-found');
});

it('getNodes GETs /api/knowledge-maps/{id}/nodes', async () => {
  const promise = sut.getNodes('m1');
  const req = http.expectOne('/api/knowledge-maps/m1/nodes');
  expect(req.request.method).toBe('GET');
  req.flush([NODE]);
  const res = await promise;
  expect(res.ok).toBe(true);
  if (res.ok) expect(res.value).toEqual([NODE]);
});

it('getEdges GETs /api/knowledge-maps/{id}/edges', async () => {
  const promise = sut.getEdges('m1');
  const req = http.expectOne('/api/knowledge-maps/m1/edges');
  expect(req.request.method).toBe('GET');
  req.flush([EDGE]);
  const res = await promise;
  expect(res.ok).toBe(true);
});

it('getNodes returns server error on 500', async () => {
  const promise = sut.getNodes('m1');
  http
    .expectOne('/api/knowledge-maps/m1/nodes')
    .flush('', { status: 500, statusText: 'Server Error' });
  const res = await promise;
  expect(res.ok).toBe(false);
  if (!res.ok) expect(res.error.kind).toBe('server');
});
```

The pre-existing `listMaps` tests continue to work unchanged (one for happy path, one for 500). Final test count: **7** in `knowledge-maps-api.service.spec.ts`.

**Steps:**

- [ ] **1. Add the test imports** for `KnowledgeMapNode` + `KnowledgeMapEdge` + new constants at the top of the spec.
- [ ] **2. Add the 5 new tests** above (after the existing `listMaps` tests).
- [ ] **3. Run failing tests** — `npx jest --testPathPatterns='knowledge-maps-api'` — the 5 new tests fail because the methods don't exist yet.
- [ ] **4. Add the `getMap` / `getNodes` / `getEdges` methods** to `knowledge-maps-api.service.ts`.
- [ ] **5. Re-run** — all 7 tests pass.
- [ ] **6. Lint** clean.
- [ ] **7. Run full web-portal sweep** — `npx jest --selectProjects web-portal` — confirm 270/270 (was 265 + 5 new).
- [ ] **8. Commit**
  ```bash
  cd /Users/m/CCE && git add frontend/apps/web-portal/src/app/features/knowledge-maps/knowledge-maps-api.service.{ts,spec.ts} && git -c commit.gpgsign=false commit -m "feat(web-portal): KnowledgeMapsApiService getMap + getNodes + getEdges (Phase 0.4)"
  ```

---

## Phase 00 — completion checklist

- [ ] Task 0.1 — Deps + extended types committed.
- [ ] Task 0.2 — `cytoscape-loader.ts` + spec (3 tests) committed.
- [ ] Task 0.3 — `cytoscape-styles.ts` + spec (4 tests) committed.
- [ ] Task 0.4 — Extended `KnowledgeMapsApiService` + spec (5 new tests, 7 total) committed.
- [ ] All web-portal Jest tests passing (target: 272/272 = 265 prior + 3 + 4 + (5 new = 7 total in service spec, replacing the old 2)).
- [ ] admin-cms still 218/218.
- [ ] Lint + build clean.
- [ ] Initial bundle unchanged (no eager import of cytoscape / cytoscape-svg / jspdf).

**If all boxes ticked, Phase 00 complete. Proceed to Phase 01 (MapViewerStore + route shell).**
