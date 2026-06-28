import type { StylesheetJson } from 'cytoscape';

/**
 * Cytoscape stylesheet for the knowledge-maps viewer.
 *
 * Brand-aligned (deep green / mid green / gold) so the in-graph nodes
 * read consistently with the list view, detail panel, and the rest of
 * the portal. Three NodeTypes are distinguished by colour AND shape so
 * the graph stays readable in greyscale + colour-blind contexts:
 *
 *   - Technology → deep brand-green ellipse        (primary)
 *   - Sector     → gold round-rectangle            (secondary brand accent)
 *   - SubTopic   → mid brand-green diamond         (softer green variant)
 *
 * RelationshipTypes are distinguished by colour + line-style:
 *
 *   - ParentOf   → deep-green solid, thick         (strong containment)
 *   - RelatedTo  → neutral grey dashed             (weaker peer link)
 *   - RequiredBy → gold-brown dotted               (prerequisite)
 *
 * `node.cce-dim` / `edge.cce-dim` classes are toggled at runtime by
 * GraphCanvasComponent when a node falls outside the current search/
 * filter match set; the selected ring uses the portal's gold accent.
 */
/**
 * Cytoscape renders to canvas and does NOT resolve CSS custom properties,
 * so we read the design-system tokens off :root at build time and feed
 * cytoscape concrete values. This keeps colors driven by _palette.scss
 * (no hardcoded literals) while staying compatible with canvas rendering.
 */
function token(name: string, fallback: string): string {
  if (typeof getComputedStyle === 'undefined' || typeof document === 'undefined') {
    return fallback;
  }
  const v = getComputedStyle(document.documentElement).getPropertyValue(name).trim();
  return v || fallback;
}
/** rgba() from an `--x-rgb` channel token. */
function tokenAlpha(rgbName: string, alpha: number, fallback: string): string {
  const channels = token(rgbName, '');
  return channels ? `rgba(${channels}, ${alpha})` : fallback;
}

export function buildStylesheet(): StylesheetJson {
  const cBrand       = token('--color-brand', '#4285f2');
  const cBrandAccent = token('--color-brand-accent', '#235bb0');
  const cAccent      = token('--color-accent', '#459ed9');
  const cWhite       = token('--white', '#ffffff');
  const cNeutral400  = token('--neutrals--400', '#98a2b0');

  return [
    // ─── Base node ───
    {
      selector: 'node',
      style: {
        'label': 'data(label)',
        'text-valign': 'center',
        'text-halign': 'center',
        'color': cWhite,
        'font-size': 12,
        'font-weight': 600,
        'text-outline-width': 1.5,
        'text-outline-color': tokenAlpha('--color-brand-shadow-rgb', 0.55, 'rgba(0,30,22,0.55)'),
        'width': 84,
        'height': 84,
        'border-width': 2,
        'border-color': tokenAlpha('--color-brand-shadow-rgb', 0.18, 'rgba(0,60,44,0.18)'),
      },
    },
    // ─── NodeType: Technology — deep brand-green ellipse (primary) ───
    {
      selector: 'node[nodeType = "Technology"]',
      style: {
        'background-color': cBrand,
        'shape': 'ellipse',
      },
    },
    // ─── NodeType: Sector — gold round-rectangle (secondary accent) ───
    {
      selector: 'node[nodeType = "Sector"]',
      style: {
        'background-color': cAccent,
        'shape': 'round-rectangle',
      },
    },
    // ─── NodeType: SubTopic — mid brand-green diamond ───
    {
      selector: 'node[nodeType = "SubTopic"]',
      style: {
        'background-color': cBrandAccent,
        'shape': 'diamond',
        'width': 92,
        'height': 92,
      },
    },
    // ─── Hover halo — subtle brand-green ring on mouseover ───
    {
      selector: 'node.cce-hover',
      style: {
        'overlay-color': cBrandAccent,
        'overlay-opacity': 0.16,
        'overlay-padding': 8,
        'border-width': 3,
        'border-color': tokenAlpha('--color-brand-accent-rgb', 0.55, 'rgba(20,184,143,0.55)'),
      },
    },
    // ─── Selected highlight — gold ring (matches portal selection) ───
    {
      selector: 'node:selected',
      style: {
        'border-width': 5,
        'border-color': cAccent,
        'overlay-color': cAccent,
        'overlay-opacity': 0.12,
        'overlay-padding': 8,
      },
    },
    // ─── Dimmed (non-matching node from search/filter) ───
    {
      selector: 'node.cce-dim',
      style: { 'opacity': 0.28 },
    },
    // ─── Base edge ───
    {
      selector: 'edge',
      style: {
        'width': 2,
        'curve-style': 'bezier',
        'target-arrow-shape': 'triangle',
        'arrow-scale': 1.1,
        'opacity': 0.85,
      },
    },
    // ─── RelationshipType: ParentOf — solid deep-green, thick ───
    {
      selector: 'edge[relationshipType = "ParentOf"]',
      style: {
        'line-color': cBrand,
        'target-arrow-color': cBrand,
        'width': 3,
      },
    },
    // ─── RelationshipType: RelatedTo — dashed neutral ───
    {
      selector: 'edge[relationshipType = "RelatedTo"]',
      style: {
        'line-color': cNeutral400,
        'target-arrow-color': cNeutral400,
        'line-style': 'dashed',
      },
    },
    // ─── RelationshipType: RequiredBy — dotted gold-brown ───
    {
      selector: 'edge[relationshipType = "RequiredBy"]',
      style: {
        'line-color': cAccent,
        'target-arrow-color': cAccent,
        'line-style': 'dotted',
        'width': 2.5,
      },
    },
    // ─── Dimmed edge (when host node is dimmed) ───
    {
      selector: 'edge.cce-dim',
      style: { 'opacity': 0.12 },
    },
  ];
}
