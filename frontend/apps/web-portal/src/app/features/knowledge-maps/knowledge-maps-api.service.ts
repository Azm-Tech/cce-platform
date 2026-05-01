import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { toFeatureError, type FeatureError } from '@frontend/ui-kit';
import type {
  KnowledgeMap,
  KnowledgeMapEdge,
  KnowledgeMapNode,
} from './knowledge-maps.types';

export type Result<T> = { ok: true; value: T } | { ok: false; error: FeatureError };

@Injectable({ providedIn: 'root' })
export class KnowledgeMapsApiService {
  private readonly http = inject(HttpClient);

  async listMaps(): Promise<Result<KnowledgeMap[]>> {
    return this.run(() =>
      firstValueFrom(this.http.get<KnowledgeMap[]>('/api/knowledge-maps')),
    );
  }

  async getMap(id: string): Promise<Result<KnowledgeMap>> {
    return this.run(() =>
      firstValueFrom(
        this.http.get<KnowledgeMap>(`/api/knowledge-maps/${encodeURIComponent(id)}`),
      ),
    );
  }

  async getNodes(id: string): Promise<Result<KnowledgeMapNode[]>> {
    return this.run(() =>
      firstValueFrom(
        this.http.get<KnowledgeMapNode[]>(
          `/api/knowledge-maps/${encodeURIComponent(id)}/nodes`,
        ),
      ),
    );
  }

  async getEdges(id: string): Promise<Result<KnowledgeMapEdge[]>> {
    return this.run(() =>
      firstValueFrom(
        this.http.get<KnowledgeMapEdge[]>(
          `/api/knowledge-maps/${encodeURIComponent(id)}/edges`,
        ),
      ),
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
