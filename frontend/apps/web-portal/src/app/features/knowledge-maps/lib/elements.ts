import type { ElementDefinition } from 'cytoscape';
import type { InteractiveMapNode } from '../knowledge-maps.types';
import { iconDataUri } from './node-icons';

export interface BuildElementsOptions {
  /** Active locale; selects nameAr vs nameEn for the visual label. */
  locale: 'ar' | 'en';
}

/**
 * Converts InteractiveMapNode[] to Cytoscape ElementDefinition[].
 *
 * Edges are derived from `parentId`: each node with a parentId gets
 * an implicit parent→child edge. No explicit edge objects from the API.
 *
 * No position is set — the caller uses a layout algorithm (breadthfirst)
 * to compute positions after mounting.
 */
export function buildElements(
  nodes: InteractiveMapNode[],
  opts: BuildElementsOptions,
): ElementDefinition[] {
  const nodeElements: ElementDefinition[] = nodes.map((n) => ({
    group: 'nodes',
    data: {
      id: n.id,
      label: opts.locale === 'ar' ? n.nameAr : n.nameEn,
      level: n.level,
      // Every node gets a line-icon (fallback dot for unknown keys) so the
      // canvas matches the Figma "iconned circle" treatment.
      iconUrl: iconDataUri(n.iconKey),
    },
  }));

  const edgeElements: ElementDefinition[] = nodes
    .filter((n) => n.parentId)
    .map((n) => ({
      group: 'edges',
      data: {
        id: `edge__${n.parentId}__${n.id}`,
        source: n.parentId as string,
        target: n.id,
      },
    }));

  return [...nodeElements, ...edgeElements];
}
