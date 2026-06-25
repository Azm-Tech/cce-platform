import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { toFeatureError, type FeatureError } from '@frontend/ui-kit';
import type {
  KnowledgeMap,
  KnowledgeMapEdge,
  KnowledgeMapNode,
  NodeType,
  RelationshipType,
} from './knowledge-maps.types';

export type Result<T> = { ok: true; value: T } | { ok: false; error: FeatureError };

// Live API serializes enums as camelCase strings ("sector", "parentOf") while
// the frontend type system uses PascalCase ("Sector", "ParentOf"). Normalise
// at the service boundary so all downstream code stays unchanged.
const NODE_TYPE_MAP: Record<string, NodeType> = {
  technology: 'Technology',
  sector: 'Sector',
  subtopic: 'SubTopic',
};
const REL_TYPE_MAP: Record<string, RelationshipType> = {
  parentof: 'ParentOf',
  relatedto: 'RelatedTo',
  requiredby: 'RequiredBy',
};

function normalizeNode(n: KnowledgeMapNode): KnowledgeMapNode {
  return { ...n, nodeType: NODE_TYPE_MAP[n.nodeType.toLowerCase()] ?? n.nodeType };
}
function normalizeEdge(e: KnowledgeMapEdge): KnowledgeMapEdge {
  return { ...e, relationshipType: REL_TYPE_MAP[e.relationshipType.toLowerCase()] ?? e.relationshipType };
}

@Injectable({ providedIn: 'root' })
export class KnowledgeMapsApiService {
  private readonly http = inject(HttpClient);

  async listMaps(): Promise<Result<KnowledgeMap[]>> {
    return this.run(() =>
      firstValueFrom(this.http.get<{ data: KnowledgeMap[] }>('/api/knowledge-maps'))
        .then((res) => res.data),
    );
  }

  async getMap(id: string): Promise<Result<KnowledgeMap>> {
    return this.run(() =>
      firstValueFrom(
        this.http.get<{ data: KnowledgeMap }>(`/api/knowledge-maps/${encodeURIComponent(id)}`),
      ).then((res) => res.data),
    );
  }

  async getNodes(id: string): Promise<Result<KnowledgeMapNode[]>> {
    return this.run(() =>
      firstValueFrom(
        this.http.get<{ data: KnowledgeMapNode[] }>(
          `/api/knowledge-maps/${encodeURIComponent(id)}/nodes`,
        ),
      ).then((res) => res.data.map(normalizeNode)),
    );
  }

  async getEdges(id: string): Promise<Result<KnowledgeMapEdge[]>> {
    return this.run(() =>
      firstValueFrom(
        this.http.get<{ data: KnowledgeMapEdge[] }>(
          `/api/knowledge-maps/${encodeURIComponent(id)}/edges`,
        ),
      ).then((res) => res.data.map(normalizeEdge)),
    );
  }

  private async run<T>(fn: () => Promise<T>): Promise<Result<T>> {
    try {
      return { ok: true, value: await fn() };
    } catch (err) {
      return { ok: false, error: toFeatureError(err as HttpErrorResponse) };
    }
  }
}
