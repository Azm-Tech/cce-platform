import type { StylesheetJson } from 'cytoscape';

/**
 * Cytoscape stylesheet for the knowledge-maps viewer.
 *
 * Distinguishes 3 NodeTypes (Technology / Sector / SubTopic) and 3
 * RelationshipTypes (ParentOf / RelatedTo / RequiredBy) by color
 * and shape. Selectors use Cytoscape's `data(<key>)` syntax to read
 * fields written into each node/edge `data` block at mount time.
 *
 * `node.cce-dim` / `edge.cce-dim` classes are toggled at runtime by
 * GraphCanvasComponent (Phase 4) when a node falls outside the
 * current search/filter match set.
 */
export function buildStylesheet(): StylesheetJson {
  return [
    // ─── Base node ───
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
    // ─── NodeType: Technology (blue, ellipse) ───
    {
      selector: 'node[nodeType = "Technology"]',
      style: { 'background-color': '#1565c0', 'shape': 'ellipse' },
    },
    // ─── NodeType: Sector (purple, round-rectangle) ───
    {
      selector: 'node[nodeType = "Sector"]',
      style: { 'background-color': '#6a1b9a', 'shape': 'round-rectangle' },
    },
    // ─── NodeType: SubTopic (teal, diamond) ───
    {
      selector: 'node[nodeType = "SubTopic"]',
      style: { 'background-color': '#00897b', 'shape': 'diamond' },
    },
    // ─── Selected highlight ───
    {
      selector: 'node:selected',
      style: { 'border-width': 4, 'border-color': '#fbc02d' },
    },
    // ─── Dimmed (non-matching node from search/filter) ───
    {
      selector: 'node.cce-dim',
      style: { 'opacity': 0.3 },
    },
    // ─── Base edge ───
    {
      selector: 'edge',
      style: {
        'width': 2,
        'curve-style': 'bezier',
        'target-arrow-shape': 'triangle',
      },
    },
    // ─── RelationshipType: ParentOf (solid blue, thick) ───
    {
      selector: 'edge[relationshipType = "ParentOf"]',
      style: {
        'line-color': '#1565c0',
        'target-arrow-color': '#1565c0',
        'width': 3,
      },
    },
    // ─── RelationshipType: RelatedTo (dashed grey) ───
    {
      selector: 'edge[relationshipType = "RelatedTo"]',
      style: {
        'line-color': '#757575',
        'target-arrow-color': '#757575',
        'line-style': 'dashed',
      },
    },
    // ─── RelationshipType: RequiredBy (dotted red-orange) ───
    {
      selector: 'edge[relationshipType = "RequiredBy"]',
      style: {
        'line-color': '#e64a19',
        'target-arrow-color': '#e64a19',
        'line-style': 'dotted',
      },
    },
    // ─── Dimmed edge (when host node is dimmed) ───
    {
      selector: 'edge.cce-dim',
      style: { 'opacity': 0.15 },
    },
  ];
}
