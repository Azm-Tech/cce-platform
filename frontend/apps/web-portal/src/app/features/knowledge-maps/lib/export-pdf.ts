import type { Core } from 'cytoscape';

export interface PdfExportOptions {
  full: boolean;
}

interface JsPdfModule {
  jsPDF: new (opts: { orientation?: 'portrait' | 'landscape'; unit?: 'pt' | 'mm'; format?: string }) => {
    addImage: (
      data: string,
      format: string,
      x: number,
      y: number,
      w: number,
      h: number,
      alias?: string,
      compression?: string,
    ) => void;
    output: (type: 'blob') => Blob;
    internal: {
      pageSize: { getWidth: () => number; getHeight: () => number };
    };
  };
}

/**
 * PDF export — lazy-imports jspdf on first invocation, wraps a
 * 2x-scaled PNG dataUri inside a landscape A4 PDF page with 24pt
 * margins. Returns a Blob.
 *
 * v0.1.0 design choice (per spec §4): raster PDF (PNG-inside-PDF)
 * rather than vector PDF (svg2pdf.js + font-fidelity issues for
 * Arabic). 2x scale keeps the embedded raster sharp at print sizes.
 */
export async function exportPdf(cy: Core, opts: PdfExportOptions): Promise<Blob> {
  const mod = (await import('jspdf')) as unknown as JsPdfModule;
  const dataUri = cy.png({ scale: 2, full: opts.full });
  const doc = new mod.jsPDF({ orientation: 'landscape', unit: 'pt', format: 'a4' });
  const margin = 24;
  const pageW = doc.internal.pageSize.getWidth();
  const pageH = doc.internal.pageSize.getHeight();
  doc.addImage(
    dataUri,
    'PNG',
    margin,
    margin,
    pageW - 2 * margin,
    pageH - 2 * margin,
    'cce-graph',
    'FAST',
  );
  return doc.output('blob');
}
