import type { KnowledgeMapEdge, KnowledgeMapNode } from '../knowledge-maps.types';

/** Subgraph payload shape (the JSON written to disk). */
export interface JsonExportPayload {
  map: {
    id: string;
    nameAr: string;
    nameEn: string;
    slug: string;
  };
  nodes: KnowledgeMapNode[];
  edges: KnowledgeMapEdge[];
  exportedAt: string;
}

/**
 * Synchronously serializes a JsonExportPayload as a pretty-printed
 * Blob with type 'application/json'.
 */
export function exportJson(payload: JsonExportPayload): Blob {
  const text = JSON.stringify(payload, null, 2);
  return new Blob([text], { type: 'application/json' });
}
