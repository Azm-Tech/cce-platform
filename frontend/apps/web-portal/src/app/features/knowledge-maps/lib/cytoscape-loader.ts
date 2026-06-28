import type cytoscape from 'cytoscape';
import type { Core, ElementDefinition, StylesheetJson } from 'cytoscape';

/**
 * Lazy-loader + mount helper for Cytoscape.
 *
 * Cytoscape (~400KB) and cytoscape-svg (~20KB) are dynamically
 * imported on first use so they ship only on the lazy
 * /knowledge-maps/:id route. Subsequent calls reuse the in-flight
 * Promise — no duplicate downloads.
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

/** A precomputed node-position map, keyed by node id. */
export type PositionMap = Record<string, { x: number; y: number }>;

/**
 * Builds a Cytoscape `preset` layout from precomputed positions (see
 * lib/radial-layout.ts). `preset` simply applies the positions we pass —
 * it does no automatic placement — which is what gives us full control over
 * branch direction and spacing. Falls back to `grid` if no positions exist.
 */
export function buildPresetLayout(positions?: PositionMap) {
  if (!positions || Object.keys(positions).length === 0) {
    return { name: 'grid', fit: true, padding: 40 } as const;
  }
  return {
    name: 'preset',
    positions,
    fit: true,
    padding: 60,
  } as const;
}

/** Mount options for the Cytoscape instance. */
export interface MountOptions {
  container: HTMLElement;
  elements: ElementDefinition[];
  style: StylesheetJson;
  boxSelectionEnabled?: boolean;
  /** Precomputed node positions; when present a `preset` layout is used. */
  positions?: PositionMap;
}

/**
 * Mounts a Cytoscape instance into the given container, applying the
 * precomputed radial positions via a `preset` layout. Returns the `Core`
 * instance so the caller can register listeners and unmount on destroy.
 */
export async function mountCytoscape(opts: MountOptions): Promise<Core> {
  const cy = await loadCytoscape();
  return cy({
    container: opts.container,
    elements: opts.elements,
    style: opts.style,
    layout: buildPresetLayout(opts.positions),
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
