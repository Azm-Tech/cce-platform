# Phase 06 — Export menu

> Parent: [`../2026-05-01-sub-7.md`](../2026-05-01-sub-7.md) · Spec: [`../../specs/2026-05-01-sub-7-design.md`](../../specs/2026-05-01-sub-7-design.md) §8 (export decision: 4 formats, ~570KB lazy bundle), §9 (export user flow)

**Phase goal:** User multi-selects nodes (rubber-band or shift-click), opens an Export menu, picks a format (PNG / JSON / SVG / PDF), and a file downloads. SVG and PDF formats lazy-load their packages on first invocation. After Phase 06, the explore-and-share workflow is complete.

**Tasks:** 5
**Working directory:** `/Users/m/CCE/`
**Preconditions:**
- Phase 05 closed (`918bb53`).
- web-portal: 343/343 Jest tests passing; lint + build clean.
- GraphCanvas already exposes `selectionChange` output (Phase 2.3) and `boxSelectionEnabled: true` (Phase 0.2 default).

---

## Task 6.1: Wire `selectionChange` output → store

**Files (modify):**
- `map-viewer.page.{ts,html}` — bind GraphCanvas's `(selectionChange)` to a new page handler that calls `store.setSelection(ids)`.
- `map-viewer.page.spec.ts` — verify the binding propagates to the store.

```ts
onSelectionChange(ids: ReadonlySet<string>): void {
  this.store.setSelection(ids);
}
```

```html
<cce-graph-canvas
  ...
  (selectionChange)="onSelectionChange($event)"
/>
```

Tests (~1 new):
- `(selectionChange)` event propagates IDs to `store.setSelection` (set the GraphCanvas spy that triggers it).

Commit: `feat(web-portal): wire GraphCanvas selectionChange → store (Phase 6.1)`

---

## Task 6.2: Download + filename helpers

**Files (new):**
- `frontend/apps/web-portal/src/app/features/knowledge-maps/lib/download.ts`
- `frontend/apps/web-portal/src/app/features/knowledge-maps/lib/download.spec.ts`

```ts
// download.ts
/** Triggers a browser download of a Blob with the given filename. */
export function downloadBlob(blob: Blob, filename: string): void {
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  // Free the object URL on the next frame so the browser has time to consume it.
  setTimeout(() => URL.revokeObjectURL(url), 0);
}

/** Builds a filename like "knowledge-map-circular-economy-2026-05-02.png". */
export function buildFilename(slug: string, ext: 'png' | 'svg' | 'json' | 'pdf'): string {
  const today = new Date().toISOString().slice(0, 10); // yyyy-mm-dd
  const safeSlug = slug.replace(/[^a-z0-9-]+/gi, '-').toLowerCase();
  return `knowledge-map-${safeSlug}-${today}.${ext}`;
}
```

Tests (~3):
1. `downloadBlob` calls `URL.createObjectURL`, sets `a.download`, calls `a.click()`, and revokes the URL.
2. `buildFilename('circular-economy', 'png')` returns the expected pattern.
3. `buildFilename` sanitizes slugs with special characters.

Commit: `feat(web-portal): download + filename helpers (Phase 6.2)`

---

## Task 6.3: Four serializer files

**Files (new):**
- `lib/export-png.ts` + `export-png.spec.ts`
- `lib/export-svg.ts` + `export-svg.spec.ts`
- `lib/export-json.ts` + `export-json.spec.ts`
- `lib/export-pdf.ts` + `export-pdf.spec.ts`

Each file exports a single `exportXxx(cy, opts)` async function returning a Blob:

```ts
// export-png.ts — direct cy.png()
export async function exportPng(cy: Core, opts: { full: boolean }): Promise<Blob> {
  const dataUri = cy.png({ scale: 2, full: opts.full, output: 'blob' });
  // Cytoscape's png() with output: 'blob' returns a Blob synchronously.
  return dataUri as unknown as Blob;
}

// export-svg.ts — lazy ensureSvgPlugin then cy.svg()
import { ensureSvgPlugin } from './cytoscape-loader';
export async function exportSvg(cy: Core, opts: { full: boolean }): Promise<Blob> {
  await ensureSvgPlugin();
  // After the plugin registers, cy gains a .svg() method.
  const svgString = (cy as unknown as { svg: (o: unknown) => string }).svg({
    scale: 2,
    full: opts.full,
  });
  return new Blob([svgString], { type: 'image/svg+xml' });
}

// export-json.ts — extract subgraph; serialize tab metadata + nodes + edges
export interface JsonExportPayload {
  map: { id: string; nameAr: string; nameEn: string; slug: string };
  nodes: KnowledgeMapNode[];
  edges: KnowledgeMapEdge[];
  exportedAt: string;
}
export function exportJson(payload: JsonExportPayload): Blob {
  return new Blob([JSON.stringify(payload, null, 2)], { type: 'application/json' });
}

// export-pdf.ts — lazy import jsPDF + wrap a high-DPI PNG inside a PDF page
import type { Core } from 'cytoscape';
export async function exportPdf(cy: Core, opts: { full: boolean }): Promise<Blob> {
  const { jsPDF } = await import('jspdf');
  // Get a high-DPI PNG dataUri (not Blob — jsPDF.addImage wants base64 dataUri).
  const dataUri = cy.png({ scale: 2, full: opts.full });
  const doc = new jsPDF({ orientation: 'landscape', unit: 'pt', format: 'a4' });
  // Compute aspect-fit dimensions
  const pageW = doc.internal.pageSize.getWidth();
  const pageH = doc.internal.pageSize.getHeight();
  // Some margin
  const margin = 24;
  doc.addImage(dataUri, 'PNG', margin, margin, pageW - 2 * margin, pageH - 2 * margin, '', 'FAST');
  return doc.output('blob');
}
```

Tests for each (~5 total across all 4):
- exportJson: serializes the payload with stable shape.
- exportPng: calls cy.png with scale: 2; returns a Blob.
- exportSvg: calls ensureSvgPlugin first; then cy.svg.
- exportPdf: lazy-imports jspdf; calls cy.png + doc.addImage + doc.output('blob').
- All four return the right MIME types via Blob.

Commit: `feat(web-portal): four export serializers (PNG/SVG/JSON/PDF) (Phase 6.3)`

---

## Task 6.4: `ExportMenuComponent` + spec

**Files (new):**
- `viewer/export-menu.component.{ts,html,scss,spec.ts}`

A Material `mat-menu` with 4 items (PNG / SVG / JSON / PDF). Click an item → emits `(formatChosen)` with the format string. Parent calls the appropriate serializer, then `downloadBlob`.

Inputs:
- `disabled: input<boolean>(false)` — when true, the trigger button is disabled (e.g., active tab not loaded yet).

Outputs:
- `formatChosen = output<'png' | 'svg' | 'json' | 'pdf'>()`

Tests (~4):
1. Renders a trigger button labelled "Export" + a mat-menu with 4 items.
2. Clicking a menu item emits `(formatChosen)` with the right format.
3. Disabled input gates the trigger.
4. i18n keys for each format label.

Commit: `feat(web-portal): ExportMenuComponent (Phase 6.4)`

---

## Task 6.5: Wire into `MapViewerPage` + spec + i18n

**Files (modify):**
- `map-viewer.page.{ts,html}` — render `<cce-export-menu>` in the header action row; on `(formatChosen)`, dispatch to the right serializer + downloadBlob.
- Add 2 new i18n key blocks: `knowledgeMaps.export.{title,png,svg,json,pdf}` (en + ar).
- `map-viewer.page.spec.ts` — 1 spec for the export dispatcher.

The page needs to expose the live Cytoscape `Core` instance to the export functions. `GraphCanvas` keeps the `cy` reference internal — we'll add a small read accessor:

```ts
// graph-canvas.component.ts addition
/** Returns the live Cytoscape Core, or null if not yet mounted. */
getCytoscape(): Core | null { return this.cy; }
```

Page handler:
```ts
async onExportFormat(format: 'png' | 'svg' | 'json' | 'pdf'): Promise<void> {
  const tab = this.store.activeTab();
  if (!tab) return;
  const cy = this.canvas?.getCytoscape();
  if (!cy && format !== 'json') return;
  const filename = buildFilename(tab.metadata.slug, format);
  const selection = this.store.selection();
  const useFull = selection.size === 0; // export everything when nothing selected

  let blob: Blob;
  switch (format) {
    case 'png': blob = await exportPng(cy!, { full: useFull }); break;
    case 'svg': blob = await exportSvg(cy!, { full: useFull }); break;
    case 'pdf': blob = await exportPdf(cy!, { full: useFull }); break;
    case 'json': blob = exportJson({
      map: { id: tab.id, nameAr: tab.metadata.nameAr, nameEn: tab.metadata.nameEn, slug: tab.metadata.slug },
      nodes: useFull ? tab.nodes : tab.nodes.filter((n) => selection.has(n.id)),
      edges: useFull
        ? tab.edges
        : tab.edges.filter((e) => selection.has(e.fromNodeId) && selection.has(e.toNodeId)),
      exportedAt: new Date().toISOString(),
    }); break;
  }
  downloadBlob(blob, filename);
}
```

Use `@ViewChild` for the canvas reference:
```ts
@ViewChild(GraphCanvasComponent) canvas?: GraphCanvasComponent;
```

i18n:
- `knowledgeMaps.export.title` — "Export"
- `knowledgeMaps.export.png` — "PNG image"
- `knowledgeMaps.export.svg` — "SVG vector"
- `knowledgeMaps.export.json` — "JSON data"
- `knowledgeMaps.export.pdf` — "PDF document"

Spec (1 new):
- Calling `onExportFormat('json')` with a fixture tab + zero selection produces a Blob with all nodes + edges + the right shape (verify by parsing the Blob text).

Commit: `feat(web-portal): wire ExportMenu into MapViewerPage + spec + i18n (Phase 6.5)`

---

## Phase 06 — completion checklist

- [ ] Task 6.1 — selectionChange wiring (~1 test).
- [ ] Task 6.2 — download + filename helpers (~3 tests).
- [ ] Task 6.3 — 4 serializer files (~5 tests).
- [ ] Task 6.4 — ExportMenuComponent (~4 tests).
- [ ] Task 6.5 — Wire into page + spec + i18n (~1 test).
- [ ] All web-portal Jest tests passing.
- [ ] admin-cms still 218/218.
- [ ] Lint + build clean.
- [ ] Initial bundle still untouched (cytoscape-svg + jspdf lazy-imported only on first use).

**If all boxes ticked, Phase 06 complete. Proceed to Phase 07 (List view a11y).**
