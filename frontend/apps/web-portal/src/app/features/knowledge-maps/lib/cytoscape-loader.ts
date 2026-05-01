import type cytoscape from 'cytoscape';
import type { Core, ElementDefinition, StylesheetJson } from 'cytoscape';

/**
 * Lazy-loader + mount helper for Cytoscape.
 *
 * Cytoscape (~400KB) and cytoscape-svg (~20KB) are dynamically
 * imported on first use so they ship only on the lazy
 * /knowledge-maps/:id route. Subsequent calls reuse the in-flight
 * Promise — no duplicate downloads.
 *
 * Cytoscape's module is CommonJS (`export = cytoscape`). With
 * `esModuleInterop`, the dynamic import gives us either the function
 * itself (some bundlers) or `{ default: function }` (others). The
 * loader normalizes both shapes to the cytoscape function.
 */

type CytoscapeFactory = typeof cytoscape;

interface MaybeDefaulted<T> {
  default?: T;
}

let cytoscapePromise: Promise<CytoscapeFactory> | null = null;
let svgPluginRegistered = false;

/** Lazy-imports cytoscape on first call. Subsequent calls reuse the same Promise. */
export async function loadCytoscape(): Promise<CytoscapeFactory> {
  if (!cytoscapePromise) {
    cytoscapePromise = (async () => {
      const mod = (await import('cytoscape')) as unknown as MaybeDefaulted<CytoscapeFactory>;
      return mod.default ?? (mod as unknown as CytoscapeFactory);
    })();
  }
  return cytoscapePromise;
}

/**
 * Lazy-imports cytoscape-svg + registers the plugin. Idempotent —
 * re-calling is a no-op once the plugin has been registered.
 */
export async function ensureSvgPlugin(): Promise<void> {
  if (svgPluginRegistered) return;
  const [cy, svgModule] = await Promise.all([
    loadCytoscape(),
    import('cytoscape-svg') as unknown as Promise<MaybeDefaulted<unknown>>,
  ]);
  const plugin = svgModule.default ?? svgModule;
  cy.use(plugin as never);
  svgPluginRegistered = true;
}

/** Mount options for the Cytoscape instance. */
export interface MountOptions {
  container: HTMLElement;
  elements: ElementDefinition[];
  style: StylesheetJson;
  zoom?: number;
  pan?: { x: number; y: number };
  boxSelectionEnabled?: boolean;
}

/**
 * Mounts a Cytoscape instance into the given container with the
 * `preset` layout (positions read from `node.position`, which the
 * caller derived from server-supplied LayoutX/Y). Returns the
 * `Core` instance so the caller can register listeners and
 * unmount on destroy.
 */
export async function mountCytoscape(opts: MountOptions): Promise<Core> {
  const cy = await loadCytoscape();
  return cy({
    container: opts.container,
    elements: opts.elements,
    style: opts.style,
    layout: { name: 'preset' },
    zoom: opts.zoom ?? 1,
    pan: opts.pan ?? { x: 0, y: 0 },
    boxSelectionEnabled: opts.boxSelectionEnabled ?? true,
    minZoom: 0.25,
    maxZoom: 4,
    wheelSensitivity: 0.5,
  });
}

/** Test-only: reset module-level state between tests. */
export function _resetForTest(): void {
  cytoscapePromise = null;
  svgPluginRegistered = false;
}
