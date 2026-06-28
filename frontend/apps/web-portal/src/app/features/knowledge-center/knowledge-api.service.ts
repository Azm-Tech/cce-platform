import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { toFeatureError, type FeatureError } from '@frontend/ui-kit';
import type { PublicResourceDto } from '@frontend/api-client';
import {
  normalizeResourceType,
  type PagedResult,
  type Resource,
  type ResourceCategory,
  type ResourceListItem,
  type ResourceType,
} from './knowledge.types';

/**
 * Compile-time contract tripwire: every field this feature reads must
 * exist on the generated OpenAPI DTO (regenerate via
 * pnpm nx run api-client:generate-types). Note: resourceType is checked
 * by name only — the spec declares it as an integer enum while the live
 * API serializes strings (reported to backend 2026-06-07), so the
 * feature-level string union + normalizeResourceType stay authoritative.
 */
// eslint-disable-next-line @typescript-eslint/no-unused-vars -- compile-time assertion only
type _ResourceContractCheck = keyof Pick<
  PublicResourceDto,
  | 'id' | 'titleAr' | 'titleEn' | 'descriptionAr' | 'descriptionEn'
  | 'resourceType' | 'categoryId' | 'categoryNameAr' | 'categoryNameEn'
  | 'assetFileId' | 'assetFileName' | 'countryIds' | 'countryNames'
  | 'publishedOn' | 'viewCount'
>;

export type Result<T> = { ok: true; value: T } | { ok: false; error: FeatureError };

@Injectable({ providedIn: 'root' })
export class KnowledgeApiService {
  private readonly http = inject(HttpClient);

  async listCategories(): Promise<Result<ResourceCategory[]>> {
    return this.run(async () => {
      const res = await firstValueFrom(this.http.get<{ data: ResourceCategory[] }>('/api/categories'));
      return Array.isArray(res) ? res : (res.data ?? []);
    });
  }

  async listResources(opts: {
    page?: number;
    pageSize?: number;
    search?: string;
    categoryId?: string;
    countryId?: string;
    resourceType?: ResourceType;
  } = {}): Promise<Result<PagedResult<ResourceListItem>>> {
    let params = new HttpParams();
    if (opts.page !== undefined) params = params.set('page', opts.page);
    if (opts.pageSize !== undefined) params = params.set('pageSize', opts.pageSize);
    if (opts.search) params = params.set('search', opts.search);
    if (opts.categoryId) params = params.set('categoryId', opts.categoryId);
    if (opts.countryId) params = params.set('countryId', opts.countryId);
    if (opts.resourceType) params = params.set('resourceType', opts.resourceType);
    return this.runNormalized(async () => {
      const res = await firstValueFrom(
        this.http.get<{ data: PagedResult<ResourceListItem> } | PagedResult<ResourceListItem>>(
          '/api/resources', { params }
        )
      );
      return 'data' in res ? res.data : res;
    });
  }

  async getResource(id: string): Promise<Result<Resource>> {
    return this.runNormalized(async () => {
      const res = await firstValueFrom(
        this.http.get<{ data: Resource } | Resource>(`/api/resources/${id}`)
      );
      return ('data' in res && res.data !== undefined) ? res.data : res as Resource;
    });
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

  /** Like run(), but also normalizes integer resourceType fields in the response. */
  private async runNormalized<T extends object>(fn: () => Promise<T>): Promise<Result<T>> {
    try {
      const value = await fn();
      return { ok: true, value: this.normalizeTypes(value) as T };
    } catch (err) {
      return { ok: false, error: toFeatureError(err as HttpErrorResponse) };
    }
  }

  private normalizeTypes<T>(obj: T): T {
    if (Array.isArray(obj)) return obj.map((item) => this.normalizeTypes(item)) as unknown as T;
    if (obj !== null && typeof obj === 'object') {
      const result = { ...obj } as Record<string, unknown>;
      if ('resourceType' in result) {
        result['resourceType'] = normalizeResourceType(result['resourceType'] as ResourceType | number);
      }
      if ('items' in result && Array.isArray(result['items'])) {
        result['items'] = result['items'].map((item: unknown) => this.normalizeTypes(item as object));
      }
      return result as T;
    }
    return obj;
  }

  private async run<T>(fn: () => Promise<T>): Promise<Result<T>> {
    try {
      return { ok: true, value: await fn() };
    } catch (err) {
      return { ok: false, error: toFeatureError(err as HttpErrorResponse) };
    }
  }
}
