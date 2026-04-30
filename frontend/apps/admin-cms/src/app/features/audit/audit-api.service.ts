import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { toFeatureError, type FeatureError } from '../../core/ui/error-formatter';
import type { AuditEvent, PagedResult } from './audit.types';

export type Result<T> = { ok: true; value: T } | { ok: false; error: FeatureError };

@Injectable({ providedIn: 'root' })
export class AuditApiService {
  private readonly http = inject(HttpClient);

  async list(opts: {
    page?: number;
    pageSize?: number;
    actor?: string;
    actionPrefix?: string;
    resourceType?: string;
    correlationId?: string;
    from?: string;
    to?: string;
  } = {}): Promise<Result<PagedResult<AuditEvent>>> {
    let params = new HttpParams();
    if (opts.page !== undefined) params = params.set('page', opts.page);
    if (opts.pageSize !== undefined) params = params.set('pageSize', opts.pageSize);
    if (opts.actor) params = params.set('actor', opts.actor);
    if (opts.actionPrefix) params = params.set('actionPrefix', opts.actionPrefix);
    if (opts.resourceType) params = params.set('resourceType', opts.resourceType);
    if (opts.correlationId) params = params.set('correlationId', opts.correlationId);
    if (opts.from) params = params.set('from', opts.from);
    if (opts.to) params = params.set('to', opts.to);
    try {
      const value = await firstValueFrom(
        this.http.get<PagedResult<AuditEvent>>('/api/admin/audit-events', { params }),
      );
      return { ok: true, value };
    } catch (err) {
      return { ok: false, error: toFeatureError(err as HttpErrorResponse) };
    }
  }
}
