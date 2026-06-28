import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { toFeatureError, type FeatureError } from '@frontend/ui-kit';
import type { InteractiveMap, NodeDetails } from './knowledge-maps.types';

export type Result<T> = { ok: true; value: T } | { ok: false; error: FeatureError };

@Injectable({ providedIn: 'root' })
export class KnowledgeMapsApiService {
  private readonly http = inject(HttpClient);

  async listMaps(): Promise<Result<InteractiveMap[]>> {
    return this.run(() =>
      firstValueFrom(this.http.get<{ data: InteractiveMap[] }>('/api/interactive-maps'))
        .then((res) => res.data),
    );
  }

  async getMap(id: string): Promise<Result<InteractiveMap>> {
    return this.run(() =>
      firstValueFrom(
        this.http.get<{ data: InteractiveMap }>(`/api/interactive-maps/${encodeURIComponent(id)}`),
      ).then((res) => res.data),
    );
  }

  async getNodeDetails(nodeId: string): Promise<Result<NodeDetails>> {
    return this.run(() =>
      firstValueFrom(
        this.http.get<{ data: NodeDetails }>(
          `/api/interactive-maps/nodes/${encodeURIComponent(nodeId)}/details`,
        ),
      ).then((res) => res.data),
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
