import type { StylesheetJson } from 'cytoscape';

/**
 * Cytoscape stylesheet — dark-navy "knowledge map" visual aligned with the
 * CCE Knowledge Hub Figma.
 *
 * Node visual language (matches Figma "Knowledge Map Icon"):
 *   - ALL nodes → ellipse (circle), dark-navy fill (#082f59), bright blue
 *     ring (#4285f2) and a soft blue glow (drop-shadow 0 0 20 rgba(blue,.6)).
 *   - A white/light line-icon sits inside each circle (data(iconUrl)).
 *   - Label rendered BELOW the circle in light cyan (#c9e8fb).
 *   - `level` encodes SIZE only:
 *       level -1 → 150 px  central map root (darker fill, strongest glow)
 *       level 0  → 112 px  top category
 *       level 1  → 104 px  category (the "4 R's")
 *       level 2  → 64 px   leaf topic
 *
 * Edge visual language:
 *   - Parent→child connectors, thin SOLID faint blue lines (Figma style).
 *
 * Cytoscape renders to <canvas> and does NOT resolve CSS custom properties.
 * The `token()` helper reads resolved values off :root at call-time so colours
 * stay driven by _palette.scss; the hex fallbacks are for non-browser tests.
 */
function token(name: string, fallback: string): string {
  if (typeof getComputedStyle === 'undefined' || typeof document === 'undefined') {
    return fallback;
  }
  const v = getComputedStyle(document.documentElement).getPropertyValue(name).trim();
  return v || fallback;
}

function tokenAlpha(rgbName: string, alpha: number, fallback: string): string {
  const channels = token(rgbName, '');
  return channels ? `rgba(${channels}, ${alpha})` : fallback;
}

export function buildStylesheet(): StylesheetJson {
  const cSurfaceDark = token('--primary--950',     '#002645'); // page / root fill
  const cNodeFill    = token('--color-brand-dark', '#082f59'); // primary-900 node fill
  const cBrand       = token('--color-brand',      '#4285f2'); // ring + glow
  const cLabel       = token('--tertiary--500',    '#c9e8fb'); // label text

  const glowBrand = tokenAlpha('--color-brand-rgb', 0.60, 'rgba(66,133,242,0.60)');
  const lineBrand = tokenAlpha('--color-brand-rgb', 0.62, 'rgba(66,133,242,0.62)');

  return [
    // ─── Base node — dark circle, blue ring, blue glow, inner icon ────
    {
      selector: 'node',
      style: {
        'shape': 'ellipse',
        'background-color': cNodeFill,
        'border-width': 2,
        'border-color': cBrand,
        'border-opacity': 1,
        // Soft blue halo (Figma drop-shadow 0 0 20 rgba(blue,.6))
        'shadow-blur': 22,
        'shadow-color': cBrand,
        'shadow-opacity': 0.6,
        'shadow-offset-x': 0,
        'shadow-offset-y': 0,
        // Inner line-icon — use 'none' fit so background-width/height (a % of
        // the node) actually applies and centers the icon. With 'contain'
        // Cytoscape IGNORES background-width and the icon fills the whole node.
        'background-image': 'data(iconUrl)',
        'background-fit': 'none' as const,
        // Figma: inner icon is ~50% of the node circle (52/104, 32/62).
        'background-width': '50%',
        'background-height': '50%',
        'background-position-x': '50%',
        'background-position-y': '50%',
        'background-image-opacity': 1,
        'background-clip': 'none' as const,
        // Label below the circle, light cyan
        'label': 'data(label)',
        'text-valign': 'bottom' as const,
        'text-halign': 'center' as const,
        'text-margin-y': 10,
        'color': cLabel,
        'font-size': 14,
        'font-weight': 600,
        'text-outline-width': 0,
        'text-wrap': 'wrap' as const,
        'text-max-width': 120,
        'width': 62,
        'height': 62,
      },
    },

    // ─── Level 2 — leaf topic (Figma: 62 px node, 18 px roman label) ─
    {
      selector: 'node[level = 2]',
      style: {
        'width': 62,
        'height': 62,
        'font-size': 14,
        'font-weight': 500,
        // Narrow wrap so multi-word labels break onto 2 lines (Figma).
        'text-max-width': 86,
        'text-margin-y': 10,
      },
    },

    // ─── Level 1 — main branch (Figma: 104 px node, 20 px bold label) ─
    {
      selector: 'node[level = 1]',
      style: {
        'width': 104,
        'height': 104,
        'font-size': 17,
        'font-weight': 700,
        'text-max-width': 110,
        'text-margin-y': 13,
        'border-width': 2.5,
        'shadow-blur': 42,
        'shadow-opacity': 0.8,
      },
    },

    // ─── Level 0 — top category (when present) ───────────────────────
    {
      selector: 'node[level = 0]',
      style: {
        'width': 112,
        'height': 112,
        'font-size': 15,
        'font-weight': 700,
        'text-max-width': 140,
        'text-margin-y': 13,
        'shadow-blur': 28,
      },
    },

    // ─── Level -1 — synthetic central map root (Figma: 193 px hero) ──
    {
      selector: 'node[level = -1]',
      style: {
        'width': 190,
        'height': 190,
        'background-color': cSurfaceDark,
        // Thicker, prominent ring + strongest halo — the central hero (Figma).
        'border-width': 3.5,
        'border-color': cBrand,
        'shadow-blur': 64,
        'shadow-color': cBrand,
        'shadow-opacity': 0.95,
        // Larger CO₂ glyph filling more of the circle (Figma).
        'background-width': '52%',
        'background-height': '52%',
        'font-size': 20,
        'font-weight': 800,
        'text-max-width': 170,
        'text-margin-y': 16,
      },
    },

    // ─── Hover: brighter ring + stronger glow ────────────────────────
    {
      selector: 'node.cce-hover',
      style: {
        'border-width': 2.5,
        'border-opacity': 1,
        'shadow-blur': 34,
        'shadow-opacity': 0.85,
      },
    },

    // ─── Selected: thick ring + strongest glow ───────────────────────
    {
      selector: 'node:selected',
      style: {
        'border-width': 3,
        'border-opacity': 1,
        'shadow-blur': 40,
        'shadow-opacity': 0.9,
      },
    },

    // ─── Dimmed (outside search/filter match) ────────────────────────
    {
      selector: 'node.cce-dim',
      style: { 'opacity': 0.18 },
    },

    // ─── Edge — dashed, glowing blue parent→child connector (Figma) ──
    {
      selector: 'edge',
      style: {
        'width': 1.75,
        'curve-style': 'bezier' as const,
        'line-color': lineBrand,
        'line-style': 'dashed' as const,
        'line-dash-pattern': [6, 5],
        // Soft blue halo along the connector — the Figma "glowing line" look.
        'shadow-blur': 9,
        'shadow-color': cBrand,
        'shadow-opacity': 0.55,
        'shadow-offset-x': 0,
        'shadow-offset-y': 0,
        'target-arrow-shape': 'none' as const,
        'opacity': 1,
      },
    },

    // ─── Dimmed edge ─────────────────────────────────────────────────
    {
      selector: 'edge.cce-dim',
      style: { 'opacity': 0.06 },
    },
  ] as StylesheetJson;
}
