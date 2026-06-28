import type { Params } from '@angular/router';
import type { ViewMode } from './map-viewer-store.service';

export interface ViewerUrlState {
  /** Tab ids in the URL `?open=` list, excluding the active route :id. */
  open: string[];
  /** Selected node id from `?node=`, or null if absent. */
  node: string | null;
  /** Search term from `?q=`, or '' if absent. */
  q: string;
  /** Comma-separated level filter from `?type=` — validated as 0, 1, or 2. */
  filters: number[];
  /** View mode from `?view=`, falls back to 'graph' for any unknown value. */
  view: ViewMode;
}

const VALID_VIEW_MODES: ReadonlySet<string> = new Set(['graph', 'list']);
const VALID_LEVELS = new Set(['0', '1', '2']);

export function parseUrlState(params: Params): ViewerUrlState {
  const openRaw = (params['open'] as string | undefined) ?? '';
  const open = openRaw
    .split(',')
    .map((s) => s.trim())
    .filter((s) => s.length > 0);

  const node = (params['node'] as string | undefined) ?? null;

  const q = (params['q'] as string | undefined) ?? '';

  const typeRaw = (params['type'] as string | undefined) ?? '';
  const filters = typeRaw
    .split(',')
    .map((s) => s.trim())
    .filter((s) => VALID_LEVELS.has(s))
    .map((s) => parseInt(s, 10));

  const viewRaw = (params['view'] as string | undefined) ?? 'graph';
  const view: ViewMode = VALID_VIEW_MODES.has(viewRaw) ? (viewRaw as ViewMode) : 'graph';

  return { open, node, q, filters, view };
}

export interface UrlPatch {
  open?: string | null;
  node?: string | null;
  q?: string | null;
  type?: string | null;
  view?: string | null;
}

export function buildUrlPatch(opts: Partial<ViewerUrlState>): UrlPatch {
  const patch: UrlPatch = {};
  if (opts.open !== undefined) {
    patch.open = opts.open.length > 0 ? opts.open.join(',') : null;
  }
  if (opts.node !== undefined) {
    patch.node = opts.node;
  }
  if (opts.q !== undefined) {
    patch.q = opts.q.length > 0 ? opts.q : null;
  }
  if (opts.filters !== undefined) {
    patch.type = opts.filters.length > 0 ? opts.filters.map(String).join(',') : null;
  }
  if (opts.view !== undefined) {
    patch.view = opts.view === 'graph' ? null : opts.view;
  }
  return patch;
}
