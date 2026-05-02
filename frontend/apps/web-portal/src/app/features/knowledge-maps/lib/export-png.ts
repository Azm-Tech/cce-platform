import type { Core } from 'cytoscape';

export interface PngExportOptions {
  /** When true, exports the entire graph; when false, exports the current viewport. */
  full: boolean;
}

/**
 * PNG export — uses Cytoscape's native cy.png({ output: 'blob' }).
 * Returns a Blob with type 'image/png'. 2x scale for retina-quality
 * output suitable for slides and reports.
 */
export async function exportPng(cy: Core, opts: PngExportOptions): Promise<Blob> {
  const blob = cy.png({
    scale: 2,
    full: opts.full,
    output: 'blob',
  });
  return blob;
}
