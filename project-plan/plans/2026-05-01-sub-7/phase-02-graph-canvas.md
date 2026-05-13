# Phase 02 — GraphCanvas

> Parent: [`../2026-05-01-sub-7.md`](../2026-05-01-sub-7.md) · Spec: [`../../specs/2026-05-01-sub-7-design.md`](../../specs/2026-05-01-sub-7-design.md) §5 (architecture), §8 (UX decisions baked in: server-driven layout + RTL mirroring), §9 (critical user flows)

**Phase goal:** Mount Cytoscape inside `MapViewerPage`, render the active tab's nodes + edges using Phase 0.3's stylesheet at server-supplied `LayoutX/Y` positions, emit `(nodeClick)` when the user clicks a node, and re-mirror node positions on locale toggle. After Phase 02, navigating to `/knowledge-maps/:id` shows a real interactive graph.

**Tasks:** 5
**Working directory:** `/Users/m/CCE/`
**Preconditions:**
- Phase 01 closed (`67edc9f`).
- web-portal: 302/302 Jest tests passing; lint + build clean.

---

## Task 2.1: Element-builder helper (`elements.ts`)

**Files (new):**
- `frontend/apps/web-portal/src/app/features/knowledge-maps/lib/elements.ts`
- `frontend/apps/web-portal/src/app/features/knowledge-maps/lib/elements.spec.ts`

Pure function `buildElements(nodes, edges, opts)` converts our typed `KnowledgeMapNode[]` + `KnowledgeMapEdge[]` to Cytoscape's `ElementDefinition[]`. The selectors in Phase 0.3's stylesheet read from each node's `data` block (`data(nodeType)`, `data(label)`) and each edge's `data(relationshipType)`. Positions go in `position: { x, y }`.

Locale mirroring lives here too — when `mirrored: true`, every node's `x` is negated. Edges don't need mirroring (they reference nodes by id; positions are derived).

```ts
// elements.ts
import type { ElementDefinition } from 'cytoscape';
import type { KnowledgeMapEdge, KnowledgeMapNode } from '../knowledge-maps.types';

export interface BuildElementsOptions {
  /** Active locale; selects nameAr vs nameEn for the visual label. */
  locale: 'ar' | 'en';
  /** When true, negate each node's LayoutX (RTL mirroring). */
  mirrored: boolean;
}

export function buildElements(
  nodes: KnowledgeMapNode[],
  edges: KnowledgeMapEdge[],
  opts: BuildElementsOptions,
): ElementDefinition[] {
  const nodeElements: ElementDefinition[] = nodes.map((n) => ({
    group: 'nodes',
    data: {
      id: n.id,
      label: opts.locale === 'ar' ? n.nameAr : n.nameEn,
      nodeType: n.nodeType,
    },
    position: {
      x: opts.mirrored ? -n.layoutX : n.layoutX,
      y: n.layoutY,
    },
  }));
  const edgeElements: ElementDefinition[] = edges.map((e) => ({
    group: 'edges',
    data: {
      id: e.id,
      source: e.fromNodeId,
      target: e.toNodeId,
      relationshipType: e.relationshipType,
    },
  }));
  return [...nodeElements, ...edgeElements];
}
```

**Tests (~5):**
1. `buildElements([], [], opts)` returns empty array.
2. Maps each node to a Cytoscape element with `data.id`, `data.label`, `data.nodeType`.
3. Selects `nameAr` vs `nameEn` based on `locale` opt.
4. Mirrors `x` (negates) when `mirrored: true`; leaves `y` alone.
5. Maps each edge to a Cytoscape element with `data.source`, `data.target`, `data.relationshipType`.

Commit: `feat(web-portal): elements builder for Cytoscape (Phase 2.1)`

---

## Task 2.2: `GraphCanvasComponent` skeleton + Cytoscape mount

**Files (new):**
- `frontend/apps/web-portal/src/app/features/knowledge-maps/viewer/graph-canvas.component.ts`
- `frontend/apps/web-portal/src/app/features/knowledge-maps/viewer/graph-canvas.component.html`
- `frontend/apps/web-portal/src/app/features/knowledge-maps/viewer/graph-canvas.component.scss`

Component renders a single `<div #host>` with `100% × 600px` (responsive). On `ngAfterViewInit`, calls `mountCytoscape(...)` from Phase 0.2's loader with the elements built from inputs + the stylesheet from Phase 0.3.

Signal inputs:
- `nodes: input.required<KnowledgeMapNode[]>()`
- `edges: input.required<KnowledgeMapEdge[]>()`
- `mirrored: input<boolean>(false)`
- `locale: input<'ar' | 'en'>('en')`
- `selectedId: input<string | null>(null)`
- `dimmedIds: input<ReadonlySet<string>>(new Set())`

Outputs (Task 2.3):
- `nodeClick = output<string>()`
- `selectionChange = output<ReadonlySet<string>>()`

```ts
// graph-canvas.component.ts (Task 2.2 cut — events come in 2.3)
import { CommonModule } from '@angular/common';
import {
  AfterViewInit, ChangeDetectionStrategy, Component, ElementRef,
  OnDestroy, ViewChild, computed, effect, inject, input,
} from '@angular/core';
import type { Core } from 'cytoscape';
import { mountCytoscape } from '../lib/cytoscape-loader';
import { buildStylesheet } from '../lib/cytoscape-styles';
import { buildElements } from '../lib/elements';
import type { KnowledgeMapEdge, KnowledgeMapNode } from '../knowledge-maps.types';

@Component({
  selector: 'cce-graph-canvas',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './graph-canvas.component.html',
  styleUrl: './graph-canvas.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GraphCanvasComponent implements AfterViewInit, OnDestroy {
  readonly nodes = input.required<KnowledgeMapNode[]>();
  readonly edges = input.required<KnowledgeMapEdge[]>();
  readonly mirrored = input<boolean>(false);
  readonly locale = input<'ar' | 'en'>('en');
  readonly selectedId = input<string | null>(null);
  readonly dimmedIds = input<ReadonlySet<string>>(new Set());

  @ViewChild('host', { static: true }) hostRef!: ElementRef<HTMLDivElement>;

  private cy: Core | null = null;

  /** Computed elements — recomputes when nodes/edges/locale/mirrored change. */
  private readonly elements = computed(() =>
    buildElements(this.nodes(), this.edges(), {
      locale: this.locale(),
      mirrored: this.mirrored(),
    }),
  );

  constructor() {
    // Re-render when elements change (Tasks 2.4 will add positional re-mirror).
    effect(() => {
      const els = this.elements();
      if (this.cy) {
        this.cy.elements().remove();
        this.cy.add(els);
      }
    });
  }

  async ngAfterViewInit(): Promise<void> {
    this.cy = await mountCytoscape({
      container: this.hostRef.nativeElement,
      elements: this.elements(),
      style: buildStylesheet(),
      boxSelectionEnabled: true,
    });
  }

  ngOnDestroy(): void {
    this.cy?.destroy();
    this.cy = null;
  }
}
```

```html
<!-- graph-canvas.component.html -->
<div #host class="cce-graph-canvas" data-testid="graph-canvas-host"></div>
```

```scss
/* graph-canvas.component.scss */
:host { display: block; }
.cce-graph-canvas {
  width: 100%;
  height: 600px;
  background: #fafafa;
  border-radius: 8px;
  border: 1px solid rgba(0, 0, 0, 0.08);
}
```

Spec coverage in Task 2.5 (after events + mirror land).

Commit: `feat(web-portal): GraphCanvasComponent skeleton + cytoscape mount (Phase 2.2)`

---

## Task 2.3: Click-to-select + selection-change events

**Files (modify):**
- `viewer/graph-canvas.component.ts` — add `nodeClick`, `selectionChange` outputs + listeners.

After mount, attach Cytoscape listeners:
- `cy.on('tap', 'node', (e) => this.nodeClick.emit(e.target.id()))`
- `cy.on('select unselect', 'node', () => this.selectionChange.emit(new Set(cy.nodes(':selected').map((n) => n.id()))))`

Apply `selectedId` input on each change via `effect()`:
- When `selectedId` becomes truthy and matches a node, call `cy.$('#<id>').select()` (and unselect everything else).
- When null, `cy.elements().unselect()`.

Apply `dimmedIds` input via another `effect()`:
- For each node id in `dimmedIds`, add the `cce-dim` class. Remove from others. Same for edges where either endpoint is dimmed.

Commit: `feat(web-portal): click + selection events + selected/dimmed state binding (Phase 2.3)`

---

## Task 2.4: Locale-mirror effect

**Files (modify):**
- `viewer/graph-canvas.component.ts` — add an `effect()` that watches `mirrored` and re-positions live without remounting.

When `mirrored` toggles after mount:
- For each node, set `position({ x: -current.x, y: current.y })`.
- Preserve `cy.zoom()` and `cy.pan()` — capture before, restore after.

This is a separate task from 2.2's "elements rebuild on inputs change" because we want to mirror **without** remounting all elements (which would lose user zoom/pan / selection state).

Commit: `feat(web-portal): live locale mirroring with zoom+pan preservation (Phase 2.4)`

---

## Task 2.5: Wire `<cce-graph-canvas>` into `MapViewerPage` + spec

**Files (modify):**
- `features/knowledge-maps/map-viewer.page.{ts,html}` — replace placeholder with `<cce-graph-canvas>`, pass nodes/edges/mirrored/locale.
- `features/knowledge-maps/viewer/graph-canvas.component.spec.ts` — new spec mocking `mountCytoscape` from `cytoscape-loader`.

Update `MapViewerPage`:

```ts
// map-viewer.page.ts — additions
import { LocaleService } from '@frontend/i18n';
import { computed } from '@angular/core';
import { GraphCanvasComponent } from './viewer/graph-canvas.component';
// imports: [..., GraphCanvasComponent]
private readonly localeService = inject(LocaleService);
readonly locale = this.localeService.locale;
readonly mirrored = computed(() => this.locale() === 'ar');
onNodeClick(id: string): void { this.store.selectNode(id); }
```

Template:
```html
@if (store.activeTab(); as tab) {
  <cce-graph-canvas
    [nodes]="tab.nodes"
    [edges]="tab.edges"
    [locale]="locale()"
    [mirrored]="mirrored()"
    [selectedId]="store.selectedNodeId()"
    (nodeClick)="onNodeClick($event)"
  />
}
```

Remove the placeholder block.

**`graph-canvas.component.spec.ts`** mocks `mountCytoscape` from the loader so jsdom isn't asked to render Cytoscape:

Tests (~6):
1. ngAfterViewInit calls `mountCytoscape` with elements built from inputs + the stylesheet.
2. Locale 'en' produces `data.label === nameEn`.
3. Locale 'ar' produces `data.label === nameAr` and node `position.x` is negated.
4. nodeClick event fires when Cytoscape's `tap` listener triggers (simulate by capturing the registered handler and invoking with a fake `e.target.id()` returning 'n1').
5. selectedId input toggles `cy.$('#n1').select()`.
6. ngOnDestroy calls `cy.destroy()`.

Plus update `map-viewer.page.spec.ts` (existing 5 tests) to mock `mountCytoscape` similarly so its tests still pass without touching Cytoscape.

Commit: `feat(web-portal): wire GraphCanvas into MapViewerPage + spec (Phase 2.5)`

---

## Phase 02 — completion checklist

- [ ] Task 2.1 — Element builder + spec (~5 tests).
- [ ] Task 2.2 — GraphCanvas skeleton + Cytoscape mount.
- [ ] Task 2.3 — Click + selection events + selected/dimmed bindings.
- [ ] Task 2.4 — Locale mirror effect.
- [ ] Task 2.5 — Wire into MapViewerPage + spec (~6 tests).
- [ ] All web-portal Jest tests passing.
- [ ] admin-cms still 218/218.
- [ ] Lint + build clean.
- [ ] Maps lazy chunk grew by ~400KB (cytoscape) + ~5KB component code; **initial bundle still untouched**.

**If all boxes ticked, Phase 02 complete. Proceed to Phase 03 (NodeDetailPanel + selection).**
