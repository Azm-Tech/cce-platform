import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { toFeatureError, type FeatureError } from '@frontend/ui-kit';
import type {
  ApproveCountryResourceRequestBody,
  AssetFile,
  CountryResourceRequest,
  CreateResourceBody,
  PagedResult,
  RejectCountryResourceRequestBody,
  Resource,
  UpdateResourceBody,
} from './content.types';

export type Result<T> = { ok: true; value: T } | { ok: false; error: FeatureError };

/**
 * Per-feature wrapper around resource + asset + country-resource-request endpoints.
 * Multipart upload for assets (POST /api/admin/assets) is implemented as FormData
 * on the body of the HttpClient.post call — Angular sets the Content-Type header
 * automatically (with the multipart boundary) when the body is a FormData instance.
 */
@Injectable({ providedIn: 'root' })
export class ContentApiService {
  private readonly http = inject(HttpClient);

  // --- Resources ---

  async listResources(opts: {
    page?: number;
    pageSize?: number;
    search?: string;
    categoryId?: string;
    countryId?: string;
    isPublished?: boolean;
  } = {}): Promise<Result<PagedResult<Resource>>> {
    let params = new HttpParams();
    if (opts.page !== undefined) params = params.set('page', opts.page);
    if (opts.pageSize !== undefined) params = params.set('pageSize', opts.pageSize);
    if (opts.search) params = params.set('search', opts.search);
    if (opts.categoryId) params = params.set('categoryId', opts.categoryId);
    if (opts.countryId) params = params.set('countryId', opts.countryId);
    if (opts.isPublished !== undefined)
      params = params.set('isPublished', String(opts.isPublished));
    return this.run(() =>
      firstValueFrom(this.http.get<PagedResult<Resource>>('/api/admin/resources', { params })),
    );
  }

  async createResource(body: CreateResourceBody): Promise<Result<Resource>> {
    return this.run(() =>
      firstValueFrom(this.http.post<Resource>('/api/admin/resources', body)),
    );
  }

  async updateResource(id: string, body: UpdateResourceBody): Promise<Result<Resource>> {
    return this.run(() =>
      firstValueFrom(this.http.put<Resource>(`/api/admin/resources/${id}`, body)),
    );
  }

  async publishResource(id: string): Promise<Result<Resource>> {
    return this.run(() =>
      firstValueFrom(this.http.post<Resource>(`/api/admin/resources/${id}/publish`, {})),
    );
  }

  // --- Assets (multipart upload) ---

  async uploadAsset(file: File): Promise<Result<AssetFile>> {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.run(() =>
      firstValueFrom(this.http.post<AssetFile>('/api/admin/assets', form)),
    );
  }

  async getAsset(id: string): Promise<Result<AssetFile>> {
    return this.run(() =>
      firstValueFrom(this.http.get<AssetFile>(`/api/admin/assets/${id}`)),
    );
  }

  // --- Country resource requests (approve/reject only — list endpoint not exposed) ---

  async approveCountryResourceRequest(
    id: string,
    body: ApproveCountryResourceRequestBody = {},
  ): Promise<Result<CountryResourceRequest>> {
    return this.run(() =>
      firstValueFrom(
        this.http.post<CountryResourceRequest>(
          `/api/admin/country-resource-requests/${id}/approve`,
          body,
        ),
      ),
    );
  }

  async rejectCountryResourceRequest(
    id: string,
    body: RejectCountryResourceRequestBody,
  ): Promise<Result<CountryResourceRequest>> {
    return this.run(() =>
      firstValueFrom(
        this.http.post<CountryResourceRequest>(
          `/api/admin/country-resource-requests/${id}/reject`,
          body,
        ),
      ),
    );
  }

  private async run<T>(fn: () => Promise<T>): Promise<Result<T>> {
    try {
      return { ok: true, value: await fn() };
    } catch (err) {
      const error = toFeatureError(err as HttpErrorResponse);
      return { ok: false, error };
    }
  }
}
