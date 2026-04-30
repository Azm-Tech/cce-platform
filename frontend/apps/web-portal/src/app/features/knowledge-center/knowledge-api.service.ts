import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { toFeatureError, type FeatureError } from '@frontend/ui-kit';
import type {
  PagedResult,
  Resource,
  ResourceCategory,
  ResourceListItem,
  ResourceType,
} from './knowledge.types';

export type Result<T> = { ok: true; value: T } | { ok: false; error: FeatureError };

@Injectable({ providedIn: 'root' })
export class KnowledgeApiService {
  private readonly http = inject(HttpClient);

  async listCategories(): Promise<Result<ResourceCategory[]>> {
    return this.run(() => firstValueFrom(this.http.get<ResourceCategory[]>('/api/categories')));
  }

  async listResources(opts: {
    page?: number;
    pageSize?: number;
    categoryId?: string;
    countryId?: string;
    resourceType?: ResourceType;
  } = {}): Promise<Result<PagedResult<ResourceListItem>>> {
    let params = new HttpParams();
    if (opts.page !== undefined) params = params.set('page', opts.page);
    if (opts.pageSize !== undefined) params = params.set('pageSize', opts.pageSize);
    if (opts.categoryId) params = params.set('categoryId', opts.categoryId);
    if (opts.countryId) params = params.set('countryId', opts.countryId);
    if (opts.resourceType) params = params.set('resourceType', opts.resourceType);
    return this.run(() =>
      firstValueFrom(this.http.get<PagedResult<ResourceListItem>>('/api/resources', { params })),
    );
  }

  async getResource(id: string): Promise<Result<Resource>> {
    return this.run(() =>
      firstValueFrom(this.http.get<Resource>(`/api/resources/${id}`)),
    );
  }

  /**
   * Returns a Blob for the SPA to materialize as a download. Caller saves it to
   * a hidden `<a download>` link (same pattern as admin-cms reports).
   */
  async download(id: string): Promise<Result<Blob>> {
    try {
      const value = await firstValueFrom(
        this.http.get(`/api/resources/${id}/download`, { responseType: 'blob' }),
      );
      return { ok: true, value };
    } catch (err) {
      return { ok: false, error: toFeatureError(err as HttpErrorResponse) };
    }
  }

  private async run<T>(fn: () => Promise<T>): Promise<Result<T>> {
    try {
      return { ok: true, value: await fn() };
    } catch (err) {
      return { ok: false, error: toFeatureError(err as HttpErrorResponse) };
    }
  }
}
