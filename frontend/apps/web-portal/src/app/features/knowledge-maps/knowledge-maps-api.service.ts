import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { toFeatureError, type FeatureError } from '@frontend/ui-kit';
import type { KnowledgeMap } from './knowledge-maps.types';

export type Result<T> = { ok: true; value: T } | { ok: false; error: FeatureError };

@Injectable({ providedIn: 'root' })
export class KnowledgeMapsApiService {
  private readonly http = inject(HttpClient);

  async listMaps(): Promise<Result<KnowledgeMap[]>> {
    try {
      const value = await firstValueFrom(
        this.http.get<KnowledgeMap[]>('/api/knowledge-maps'),
      );
      return { ok: true, value };
    } catch (err) {
      return { ok: false, error: toFeatureError(err as HttpErrorResponse) };
    }
  }
}
