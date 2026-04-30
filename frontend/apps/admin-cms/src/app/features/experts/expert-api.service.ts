import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { toFeatureError, type FeatureError } from '@frontend/ui-kit';
import type {
  ApproveExpertRequestBody,
  ExpertProfile,
  ExpertRegistrationStatus,
  ExpertRequest,
  PagedResult,
  RejectExpertRequestBody,
} from './expert.types';

export type Result<T> = { ok: true; value: T } | { ok: false; error: FeatureError };

/**
 * Per-feature wrapper around the Expert workflow endpoints.
 */
@Injectable({ providedIn: 'root' })
export class ExpertApiService {
  private readonly http = inject(HttpClient);

  async listRequests(opts: {
    page?: number;
    pageSize?: number;
    status?: ExpertRegistrationStatus;
    requestedById?: string;
  } = {}): Promise<Result<PagedResult<ExpertRequest>>> {
    let params = new HttpParams();
    if (opts.page !== undefined) params = params.set('page', opts.page);
    if (opts.pageSize !== undefined) params = params.set('pageSize', opts.pageSize);
    if (opts.status) params = params.set('status', opts.status);
    if (opts.requestedById) params = params.set('requestedById', opts.requestedById);
    return this.run(() =>
      firstValueFrom(
        this.http.get<PagedResult<ExpertRequest>>('/api/admin/expert-requests', { params }),
      ),
    );
  }

  async approve(id: string, body: ApproveExpertRequestBody): Promise<Result<ExpertRequest>> {
    return this.run(() =>
      firstValueFrom(
        this.http.post<ExpertRequest>(`/api/admin/expert-requests/${id}/approve`, body),
      ),
    );
  }

  async reject(id: string, body: RejectExpertRequestBody): Promise<Result<ExpertRequest>> {
    return this.run(() =>
      firstValueFrom(
        this.http.post<ExpertRequest>(`/api/admin/expert-requests/${id}/reject`, body),
      ),
    );
  }

  async listProfiles(opts: {
    page?: number;
    pageSize?: number;
    search?: string;
  } = {}): Promise<Result<PagedResult<ExpertProfile>>> {
    let params = new HttpParams();
    if (opts.page !== undefined) params = params.set('page', opts.page);
    if (opts.pageSize !== undefined) params = params.set('pageSize', opts.pageSize);
    if (opts.search) params = params.set('search', opts.search);
    return this.run(() =>
      firstValueFrom(
        this.http.get<PagedResult<ExpertProfile>>('/api/admin/expert-profiles', { params }),
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
