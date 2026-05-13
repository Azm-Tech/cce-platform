# Phase 03 — NodeDetailPanel + selection

> Parent: [`../2026-05-01-sub-7.md`](../2026-05-01-sub-7.md) · Spec: [`../../specs/2026-05-01-sub-7-design.md`](../../specs/2026-05-01-sub-7-design.md) §8 (UX decisions: persistent right side panel / mobile bottom sheet), §9 (read-a-node user flow)

**Phase goal:** When a user clicks a node in the GraphCanvas, the `NodeDetailPanel` renders the node's name, description, and outbound edges list. Clicking an edge target switches selection to that target. ESC + close button dismisses the panel. After Phase 03, the "click → read → traverse" loop works end-to-end.

**Tasks:** 4
**Working directory:** `/Users/m/CCE/`
**Preconditions:**
- Phase 02 closed (`88265dc`).
- web-portal: 312/312 Jest tests passing; lint + build clean.

---

## Design choice: CSS-driven drawer (no MatBottomSheet)

The spec called out "side panel (desktop) / `MatBottomSheet` (mobile)." Implementation note: a single CSS-driven drawer is simpler than mixing the two patterns. The panel is rendered inline in the template whenever `selectedNode` is non-null; CSS media query at `720px` switches it from a fixed right-rail to a fixed bottom-sheet. Same component, two responsive presentations. `MatBottomSheet`'s service-controlled lifecycle adds complexity without a UX gain here.

---

## Task 3.1: `NodeDetailPanelComponent` skeleton + rendering

**Files (new):**
- `frontend/apps/web-portal/src/app/features/knowledge-maps/viewer/node-detail-panel.component.ts`
- `frontend/apps/web-portal/src/app/features/knowledge-maps/viewer/node-detail-panel.component.html`
- `frontend/apps/web-portal/src/app/features/knowledge-maps/viewer/node-detail-panel.component.scss`

Signal inputs:
- `node: input<KnowledgeMapNode | null>(null)` — when null, panel renders nothing (host has `[hidden]` binding).
- `outboundEdges: input<KnowledgeMapEdge[]>([])` — edges where `fromNodeId === node.id`.
- `outboundTargets: input<KnowledgeMapNode[]>([])` — pre-resolved nodes for those edges' `toNodeId`s (parent provides; keeps the panel pure).
- `locale: input<'ar' | 'en'>('en')`.

Outputs:
- `closed = output<void>()` — fires when user clicks close button or hits ESC.
- `nodeSelected = output<string>()` — fires when user clicks an outbound edge target.

Computed:
- `name`: localized name from input node.
- `description`: localized description (falls back to "—" when null).
- `nodeTypeBadgeKey`: i18n key for the badge text.

Commit: `feat(web-portal): NodeDetailPanelComponent rendering + computed labels (Phase 3.1)`

---

## Task 3.2: Outbound edges list + click-to-re-select output

**Files (modify):**
- `viewer/node-detail-panel.component.{ts,html,scss}` — add the outbound edges list block.

Each edge row displays: relationship-type badge, target-node localized name (from `outboundTargets`). Clicking the row emits `(nodeSelected)` with the target id; parent uses this to call `store.selectNode(targetId)` so the panel re-renders for the new node.

When `outboundEdges.length === 0`, render "No outbound connections" empty-state message.

Commit: `feat(web-portal): outbound edges list with click-to-re-select (Phase 3.2)`

---

## Task 3.3: ESC keyboard shortcut + close button

**Files (modify):**
- `viewer/node-detail-panel.component.ts` — add `@HostListener('document:keydown.escape')` and a close icon button in the header.

ESC handler: if panel is visible (`node()` is non-null), call `closed.emit()`. Close button does the same.

Commit: `feat(web-portal): ESC + close-button shortcut for NodeDetailPanel (Phase 3.3)`

---

## Task 3.4: Wire `<cce-node-detail-panel>` into MapViewerPage + spec

**Files (modify + new):**
- `features/knowledge-maps/map-viewer.page.{ts,html}` — render the panel beside `<cce-graph-canvas>`; compute `outboundEdges` + `outboundTargets` from the active tab + selected node; wire `(closed)` → `store.selectNode(null)`; wire `(nodeSelected)` → `store.selectNode(id)`.
- New: `viewer/node-detail-panel.component.spec.ts` (~6 tests).

Page additions:
```ts
readonly outboundEdges = computed(() => {
  const tab = this.store.activeTab();
  const node = this.store.selectedNode();
  if (!tab || !node) return [];
  return tab.edges.filter((e) => e.fromNodeId === node.id);
});

readonly outboundTargets = computed(() => {
  const tab = this.store.activeTab();
  const edges = this.outboundEdges();
  if (!tab || edges.length === 0) return [];
  return edges
    .map((e) => tab.nodes.find((n) => n.id === e.toNodeId))
    .filter((n): n is KnowledgeMapNode => n !== undefined);
});
```

Spec tests (~6):
1. Renders nothing when `node` input is null.
2. Renders localized name + description when node is provided.
3. Locale toggle updates name/description.
4. Outbound edges list renders one row per edge; clicking emits `(nodeSelected)` with the target id.
5. ESC keydown emits `(closed)`.
6. Close button click emits `(closed)`.

Commit: `feat(web-portal): wire NodeDetailPanel into MapViewerPage + spec (Phase 3.4)`

---

## i18n keys to add

Both `en.json` and `ar.json`:
- `knowledgeMaps.detail.nodeType` — "Node type"
- `knowledgeMaps.detail.description` — "Description"
- `knowledgeMaps.detail.outboundEdges` — "Outbound connections"
- `knowledgeMaps.detail.noEdges` — "No outbound connections."
- `knowledgeMaps.detail.close` — "Close"
- `knowledgeMaps.detail.relationshipType.ParentOf` — "Parent of"
- `knowledgeMaps.detail.relationshipType.RelatedTo` — "Related to"
- `knowledgeMaps.detail.relationshipType.RequiredBy` — "Required by"
- `knowledgeMaps.detail.nodeType.Technology` — "Technology"
- `knowledgeMaps.detail.nodeType.Sector` — "Sector"
- `knowledgeMaps.detail.nodeType.SubTopic` — "Subtopic"

Added in Task 3.4 alongside the page wiring.

---

## Phase 03 — completion checklist

- [ ] Task 3.1 — Panel skeleton + rendering.
- [ ] Task 3.2 — Outbound edges list with click-to-re-select.
- [ ] Task 3.3 — ESC + close button.
- [ ] Task 3.4 — Wire into MapViewerPage + spec (~6 tests) + i18n keys.
- [ ] All web-portal Jest tests passing.
- [ ] admin-cms still 218/218.
- [ ] Lint + build clean.

**If all boxes ticked, Phase 03 complete. Proceed to Phase 04 (Search + filters).**
