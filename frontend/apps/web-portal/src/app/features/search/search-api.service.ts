import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { toFeatureError, type FeatureError } from '@frontend/ui-kit';
import type { PagedResult, SearchHit, SearchableType } from './search.types';

export type Result<T> = { ok: true; value: T } | { ok: false; error: FeatureError };

@Injectable({ providedIn: 'root' })
export class SearchApiService {
  private readonly http = inject(HttpClient);

  async search(opts: {
    q: string;
    type?: SearchableType;
    page?: number;
    pageSize?: number;
  }): Promise<Result<PagedResult<SearchHit>>> {
    let params = new HttpParams().set('q', opts.q);
    if (opts.type) params = params.set('type', opts.type);
    if (opts.page !== undefined) params = params.set('page', opts.page);
    if (opts.pageSize !== undefined) params = params.set('pageSize', opts.pageSize);

    try {
      const value = await firstValueFrom(
        this.http.get<PagedResult<SearchHit>>('/api/search', { params }),
      );
      return { ok: true, value };
    } catch (err) {
      return { ok: false, error: toFeatureError(err as HttpErrorResponse) };
    }
  }
}
