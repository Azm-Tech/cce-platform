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
  const lineBrand = tokenAlpha('--color-brand-rgb', 0.35, 'rgba(66,133,242,0.35)');

  return [
    // ─── Base node — dark circle, blue ring, blue glow, inner icon ────
    {
      selector: 'node',
      style: {
        'shape': 'ellipse',
        'background-color': cNodeFill,
        'border-width': 1.5,
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
        'background-width': '46%',
        'background-height': '46%',
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
        'font-size': 13,
        'font-weight': 600,
        'text-outline-width': 0,
        'text-wrap': 'wrap' as const,
        'text-max-width': 110,
        'width': 64,
        'height': 64,
      },
    },

    // ─── Inner icon scales up slightly on the small leaf nodes ───────
    {
      selector: 'node[level = 2]',
      style: {
        'width': 64,
        'height': 64,
        'font-size': 12,
        'font-weight': 500,
        'text-max-width': 96,
        'text-margin-y': 9,
      },
    },

    // ─── Level 1 — category ("4 R's"): large prominent node ──────────
    {
      selector: 'node[level = 1]',
      style: {
        'width': 104,
        'height': 104,
        'font-size': 15,
        'font-weight': 700,
        'text-max-width': 130,
        'text-margin-y': 12,
        'shadow-blur': 26,
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

    // ─── Level -1 — synthetic central map root (hero node) ───────────
    {
      selector: 'node[level = -1]',
      style: {
        'width': 150,
        'height': 150,
        'background-color': cSurfaceDark,
        'border-width': 2,
        'border-color': cBrand,
        'shadow-blur': 40,
        'shadow-color': cBrand,
        'shadow-opacity': 0.7,
        'background-width': '46%',
        'background-height': '46%',
        'font-size': 18,
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

    // ─── Edge — thin SOLID faint blue parent→child connector ─────────
    {
      selector: 'edge',
      style: {
        'width': 1.5,
        'curve-style': 'bezier' as const,
        'line-color': lineBrand,
        'line-style': 'solid' as const,
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
