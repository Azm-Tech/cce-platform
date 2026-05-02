import type { Core } from 'cytoscape';
import { ensureSvgPlugin } from './cytoscape-loader';

export interface SvgExportOptions {
  full: boolean;
}

/**
 * SVG export — lazy-imports the cytoscape-svg plugin on first use,
 * then calls cy.svg() (provided by the plugin). Returns a Blob with
 * type 'image/svg+xml'.
 *
 * Idempotent: repeat calls reuse the registered plugin (handled in
 * ensureSvgPlugin).
 */
export async function exportSvg(cy: Core, opts: SvgExportOptions): Promise<Blob> {
  await ensureSvgPlugin();
  // After ensureSvgPlugin, `cy` gains a `.svg(opts)` method via the plugin.
  const svgString = (cy as unknown as { svg: (o: { scale?: number; full?: boolean }) => string }).svg({
    scale: 2,
    full: opts.full,
  });
  return new Blob([svgString], { type: 'image/svg+xml' });
}
