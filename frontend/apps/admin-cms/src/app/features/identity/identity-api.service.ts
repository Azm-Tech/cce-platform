import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { toFeatureError, type FeatureError } from '../../core/ui/error-formatter';
import type {
  PagedResult,
  StateRepAssignment,
  UserDetail,
  UserListItem,
} from './identity.types';

export type Result<T> = { ok: true; value: T } | { ok: false; error: FeatureError };

/**
 * Per-feature wrapper around the Identity admin endpoints. Maps every HTTP
 * failure through {@link toFeatureError} so callers can render typed errors
 * without re-doing the discrimination per page (ADR-0035 hybrid pattern).
 */
@Injectable({ providedIn: 'root' })
export class IdentityApiService {
  private readonly http = inject(HttpClient);

  async listUsers(opts: {
    page?: number;
    pageSize?: number;
    search?: string;
    role?: string;
  } = {}): Promise<Result<PagedResult<UserListItem>>> {
    let params = new HttpParams();
    if (opts.page !== undefined) params = params.set('page', opts.page);
    if (opts.pageSize !== undefined) params = params.set('pageSize', opts.pageSize);
    if (opts.search) params = params.set('search', opts.search);
    if (opts.role) params = params.set('role', opts.role);
    return this.run(() =>
      firstValueFrom(this.http.get<PagedResult<UserListItem>>('/api/admin/users', { params })),
    );
  }

  async getUser(id: string): Promise<Result<UserDetail>> {
    return this.run(() =>
      firstValueFrom(this.http.get<UserDetail>(`/api/admin/users/${id}`)),
    );
  }

  async assignRoles(id: string, roles: string[]): Promise<Result<UserDetail>> {
    return this.run(() =>
      firstValueFrom(this.http.put<UserDetail>(`/api/admin/users/${id}/roles`, { roles })),
    );
  }

  async listStateRepAssignments(opts: {
    page?: number;
    pageSize?: number;
    userId?: string;
    countryId?: string;
    active?: boolean;
  } = {}): Promise<Result<PagedResult<StateRepAssignment>>> {
    let params = new HttpParams();
    if (opts.page !== undefined) params = params.set('page', opts.page);
    if (opts.pageSize !== undefined) params = params.set('pageSize', opts.pageSize);
    if (opts.userId) params = params.set('userId', opts.userId);
    if (opts.countryId) params = params.set('countryId', opts.countryId);
    if (opts.active !== undefined) params = params.set('active', String(opts.active));
    return this.run(() =>
      firstValueFrom(
        this.http.get<PagedResult<StateRepAssignment>>('/api/admin/state-rep-assignments', { params }),
      ),
    );
  }

  async createStateRepAssignment(body: {
    userId: string;
    countryId: string;
  }): Promise<Result<StateRepAssignment>> {
    return this.run(() =>
      firstValueFrom(
        this.http.post<StateRepAssignment>('/api/admin/state-rep-assignments', body),
      ),
    );
  }

  async revokeStateRepAssignment(id: string): Promise<Result<void>> {
    return this.run(() =>
      firstValueFrom(this.http.delete<void>(`/api/admin/state-rep-assignments/${id}`)),
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
