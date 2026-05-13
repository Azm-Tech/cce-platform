# Phase 07 — List view (a11y)

> Parent: [`../2026-05-01-sub-7.md`](../2026-05-01-sub-7.md) · Spec: [`../../specs/2026-05-01-sub-7-design.md`](../../specs/2026-05-01-sub-7-design.md) §8 (UX: dual-view a11y), §9 (locale toggle + view toggle user flows)

**Phase goal:** Add an accessible alternative to the Cytoscape canvas — a structured `<ul>` tree of nodes grouped by NodeType. Visual graph is for sighted users; the list view is keyboard- and screen-reader-friendly. Toggle between them via a view-mode button on the page; the choice rides the URL `?view=graph|list` param (already plumbed in Phase 1.3). Selecting a node in either view targets the same `store.selectNode(id)` action so toggling preserves selection. After Phase 07, the dual-view contract from §8 is complete.

**Tasks:** 3
**Working directory:** `/Users/m/CCE/`
**Preconditions:**
- Phase 06 closed (`50930aa`).
- web-portal: 355/355 Jest tests passing; lint + build clean.
- Store already exposes `viewMode` signal + `setViewMode` action (Phase 1.1).
- URL state already plumbs `?view=` (Phase 1.3) and hydrates it on init (Phase 1.4).

---

## Task 7.1: `ListViewComponent`

**Files (new):**
- `viewer/list-view.component.{ts,html,scss,spec.ts}`

Signal inputs:
- `nodes: input.required<KnowledgeMapNode[]>()`
- `edges: input.required<KnowledgeMapEdge[]>()`
- `selectedId: input<string | null>(null)`
- `dimmedIds: input<ReadonlySet<string>>(new Set())`
- `locale: input<'ar' | 'en'>('en')`

Outputs:
- `nodeSelected = output<string>()`

Renders:
- A top `<nav>` with three `<section>` blocks — one per NodeType (Technology, Sector, SubTopic).
- Each section header shows the localized type name + node count badge.
- Inside each section, a `<ul>` of node buttons. Each `<li>` contains a button labeled with the node's localized name + an outbound-edge count.
- The selected node row gets `aria-current="true"` + a visual highlight (golden left border, same color as the Cytoscape selection state).
- Dimmed nodes get reduced opacity (matches the graph view's filter dim).
- Within each section, sections with zero matching nodes after filter render an empty-state message ("No nodes of this type match the current filter.").

Keyboard:
- Each `<button>` is natively focusable.
- Tab/Shift-Tab walks through nodes in document order.
- Enter/Space activates the button → fires `(nodeSelected)`.

```ts
// list-view.component.ts (sketch)
readonly grouped = computed(() => {
  const byType: Record<NodeType, KnowledgeMapNode[]> = { Technology: [], Sector: [], SubTopic: [] };
  for (const n of this.nodes()) byType[n.nodeType].push(n);
  return NODE_TYPES.map((t) => ({ type: t, nodes: byType[t] }));
});

readonly outboundCounts = computed<ReadonlyMap<string, number>>(() => {
  const map = new Map<string, number>();
  for (const e of this.edges()) {
    map.set(e.fromNodeId, (map.get(e.fromNodeId) ?? 0) + 1);
  }
  return map;
});

nameOf(n: KnowledgeMapNode): string {
  return this.locale() === 'ar' ? n.nameAr : n.nameEn;
}
```

Tests (~6):
1. Renders one section per NodeType.
2. Each section's count badge matches the number of nodes of that type.
3. Locale toggle switches all node names between nameAr and nameEn.
4. Clicking a node emits `(nodeSelected)` with that id.
5. The node matching `selectedId` has `aria-current="true"`.
6. Nodes in `dimmedIds` get the dim class.

Commit: `feat(web-portal): ListViewComponent (Phase 7.1)`

---

## Task 7.2: View-mode toggle button

**Files (modify):**
- `map-viewer.page.{ts,html,scss}` — add a view-mode toggle button in the header action row beside the export menu.

Behavior:
- Two-button toggle (graph icon + list icon). Active button is highlighted.
- Clicking the inactive button calls `store.setViewMode('graph' | 'list')`. The URL-sync effect from Phase 4.4 will need to know about `view` — extend `buildUrlPatch` call to include `view: this.store.viewMode()`.

The store's `viewMode` signal already exists (Phase 1.1). The URL hydration also exists (Phase 1.4). All we need here is the UI control and one more line in the URL-sync `effect()`.

```html
<div class="cce-map-viewer__view-toggle" role="group">
  <button mat-icon-button
    [class.cce-map-viewer__view-toggle--active]="store.viewMode() === 'graph'"
    [attr.aria-label]="'knowledgeMaps.viewMode.graph' | translate"
    (click)="onSetViewMode('graph')"
  >
    <mat-icon>account_tree</mat-icon>
  </button>
  <button mat-icon-button
    [class.cce-map-viewer__view-toggle--active]="store.viewMode() === 'list'"
    [attr.aria-label]="'knowledgeMaps.viewMode.list' | translate"
    (click)="onSetViewMode('list')"
  >
    <mat-icon>list</mat-icon>
  </button>
</div>
```

Page additions:
```ts
onSetViewMode(mode: ViewMode): void {
  this.store.setViewMode(mode);
}
```

Extend the URL-sync effect to read viewMode:
```ts
const view = this.store.viewMode();
const patch = buildUrlPatch({ q, filters, open: otherIds, view });
```

Commit: `feat(web-portal): view-mode toggle button + URL sync (Phase 7.2)`

---

## Task 7.3: Wire `<cce-list-view>` into MapViewerPage + spec + i18n

**Files (modify):**
- `map-viewer.page.{ts,html}` — render `<cce-list-view>` when `store.viewMode() === 'list'`; render `<cce-graph-canvas>` when `'graph'`. Both bind to the same store signals so toggling preserves selection + dim state.
- `map-viewer.page.spec.ts` — add 1 spec confirming the toggle changes which view mounts.
- i18n keys: `knowledgeMaps.viewMode.{graph,list}` in en + ar.

Template change (within active-tab branch):
```html
@if (store.viewMode() === 'graph') {
  <cce-graph-canvas
    class="cce-map-viewer__canvas"
    ...existing bindings...
  />
} @else {
  <cce-list-view
    class="cce-map-viewer__canvas"
    [nodes]="tab.nodes"
    [edges]="tab.edges"
    [selectedId]="store.selectedNodeId()"
    [dimmedIds]="store.dimmedIds()"
    [locale]="locale()"
    (nodeSelected)="onNodeClick($event)"
  />
}
```

Commit: `feat(web-portal): wire ListView into MapViewerPage + view toggle + i18n (Phase 7.3)`

---

## Phase 07 — completion checklist

- [ ] Task 7.1 — ListViewComponent (~6 tests).
- [ ] Task 7.2 — View-mode toggle button + URL sync extension.
- [ ] Task 7.3 — Wire into page + spec (~1 test) + i18n (en/ar).
- [ ] All web-portal Jest tests passing.
- [ ] admin-cms still 218/218.
- [ ] Lint + build clean.
- [ ] axe-core finds zero critical/serious issues on the list view.

**If all boxes ticked, Phase 07 complete. Proceed to Phase 08 (Close-out: ADRs + tag + Lighthouse).**
