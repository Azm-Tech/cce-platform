/**
 * Browser-side blob download helpers used by the export menu (Phase 6).
 *
 * downloadBlob materializes a Blob as a download via a hidden anchor;
 * buildFilename produces a stable, sanitized filename with date.
 */

/** Triggers a browser download of `blob` with the given filename. */
export function downloadBlob(blob: Blob, filename: string): void {
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  // Free the object URL on the next tick — gives the browser a chance to
  // start the download before we revoke the underlying blob reference.
  setTimeout(() => URL.revokeObjectURL(url), 0);
}

export type SupportedExtension = 'png' | 'svg' | 'json' | 'pdf';

/**
 * Builds a filename like
 *   knowledge-map-circular-economy-2026-05-02.png
 *
 * Slug is sanitized (only [a-z0-9-] preserved; runs of other chars
 * collapse to a single hyphen). Date in yyyy-mm-dd UTC.
 */
export function buildFilename(slug: string, ext: SupportedExtension): string {
  const today = new Date().toISOString().slice(0, 10);
  const safeSlug = slug
    .replace(/[^a-z0-9-]+/gi, '-')
    .toLowerCase()
    .replace(/^-+|-+$/g, '')      // trim leading / trailing hyphens
    .replace(/-{2,}/g, '-');      // collapse multiple hyphens
  const finalSlug = safeSlug || 'map';
  return `knowledge-map-${finalSlug}-${today}.${ext}`;
}
