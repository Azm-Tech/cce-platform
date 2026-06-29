import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { toFeatureError, type FeatureError } from '@frontend/ui-kit';
import type {
  CreateInteractiveMapNodeRequest,
  InteractiveMapDto,
  InteractiveMapNodeDto,
  PagedResult,
  UpdateInteractiveMapNodeRequest,
  UpdateInteractiveMapRequest,
} from './interactive-maps.types';

export type Result<T> = { ok: true; value: T } | { ok: false; error: FeatureError };

@Injectable({ providedIn: 'root' })
export class InteractiveMapsApiService {
  private readonly http = inject(HttpClient);

  // ── Map (single) ──────────────────────────────────────────────────────────────

  /**
   * The system has exactly one Interactive Map. `GET /api/admin/interactive-maps`
   * returns that single map directly (the backend made this a singleton — there
   * is no id and the old `/{id}` route was removed). Response is auto-unwrapped
   * by the admin envelope interceptor, so the inner DTO is typed directly.
   */
  async getCurrentMap(): Promise<Result<InteractiveMapDto>> {
    return this.run(() =>
      firstValueFrom(this.http.get<InteractiveMapDto>('/api/admin/interactive-maps')),
    );
  }

  /** Updates the single map's metadata — `PUT /api/admin/interactive-maps` (no id). */
  async updateMap(body: UpdateInteractiveMapRequest): Promise<Result<void>> {
    return this.run(() =>
      firstValueFrom(this.http.put<void>('/api/admin/interactive-maps', body)),
    );
  }

  // ── Nodes ───────────────────────────────────────────────────────────────────

  async listNodes(mapId: string, opts: { page?: number; pageSize?: number; isActive?: boolean } = {}): Promise<Result<PagedResult<InteractiveMapNodeDto>>> {
    let params = new HttpParams();
    if (opts.page !== undefined) params = params.set('page', opts.page);
    if (opts.pageSize !== undefined) params = params.set('pageSize', opts.pageSize);
    if (opts.isActive !== undefined) params = params.set('isActive', String(opts.isActive));
    return this.run(() =>
      firstValueFrom(
        this.http.get<PagedResult<InteractiveMapNodeDto>>(
          `/api/admin/interactive-maps/${encodeURIComponent(mapId)}/nodes`,
          { params },
        ),
      ),
    );
  }

  async createNode(mapId: string, body: CreateInteractiveMapNodeRequest): Promise<Result<void>> {
    return this.run(() =>
      firstValueFrom(
        this.http.post<void>(`/api/admin/interactive-maps/${encodeURIComponent(mapId)}/nodes`, body),
      ),
    );
  }

  async updateNode(mapId: string, id: string, body: UpdateInteractiveMapNodeRequest): Promise<Result<void>> {
    return this.run(() =>
      firstValueFrom(
        this.http.put<void>(
          `/api/admin/interactive-maps/${encodeURIComponent(mapId)}/nodes/${encodeURIComponent(id)}`,
          body,
        ),
      ),
    );
  }

  async deleteNode(mapId: string, id: string): Promise<Result<void>> {
    return this.run(() =>
      firstValueFrom(
        this.http.delete<void>(
          `/api/admin/interactive-maps/${encodeURIComponent(mapId)}/nodes/${encodeURIComponent(id)}`,
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
