import type { ElementDefinition } from 'cytoscape';
import type { KnowledgeMapEdge, KnowledgeMapNode } from '../knowledge-maps.types';

export interface BuildElementsOptions {
  /** Active locale; selects nameAr vs nameEn for the visual label. */
  locale: 'ar' | 'en';
  /** When true, negate each node's LayoutX (RTL mirroring). */
  mirrored: boolean;
}

/**
 * Pure converter from typed KnowledgeMapNode[] + KnowledgeMapEdge[]
 * to Cytoscape's ElementDefinition[].
 *
 * Selectors in cytoscape-styles.ts read from each node's `data` block
 * (data(nodeType), data(label)) and each edge's `data` block
 * (data(relationshipType)). Positions go in `position: {x, y}` where
 * Cytoscape's `preset` layout reads them.
 *
 * RTL mirroring lives here at build time: when `mirrored: true`,
 * each node's `x` is negated. Edges don't need mirroring — they
 * reference nodes by id and positions are derived.
 */
export function buildElements(
  nodes: KnowledgeMapNode[],
  edges: KnowledgeMapEdge[],
  opts: BuildElementsOptions,
): ElementDefinition[] {
  const nodeElements: ElementDefinition[] = nodes.map((n) => ({
    group: 'nodes',
    data: {
      id: n.id,
      label: opts.locale === 'ar' ? n.nameAr : n.nameEn,
      nodeType: n.nodeType,
    },
    position: {
      x: opts.mirrored ? -n.layoutX : n.layoutX,
      y: n.layoutY,
    },
  }));
  const edgeElements: ElementDefinition[] = edges.map((e) => ({
    group: 'edges',
    data: {
      id: e.id,
      source: e.fromNodeId,
      target: e.toNodeId,
      relationshipType: e.relationshipType,
    },
  }));
  return [...nodeElements, ...edgeElements];
}
