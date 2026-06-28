import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { toFeatureError, type FeatureError } from '@frontend/ui-kit';
import type { NewsArticle, PagedResult } from './news.types';

export type Result<T> = { ok: true; value: T } | { ok: false; error: FeatureError };

@Injectable({ providedIn: 'root' })
export class NewsApiService {
  private readonly http = inject(HttpClient);

  async listNews(opts: {
    page?: number;
    pageSize?: number;
    isFeatured?: boolean;
  } = {}): Promise<Result<PagedResult<NewsArticle>>> {
    let params = new HttpParams();
    if (opts.page !== undefined) params = params.set('page', opts.page);
    if (opts.pageSize !== undefined) params = params.set('pageSize', opts.pageSize);
    if (opts.isFeatured !== undefined) params = params.set('isFeatured', String(opts.isFeatured));
    return this.run(async () => {
      const res = await firstValueFrom(
        this.http.get<PagedResult<NewsArticle> | { data?: PagedResult<NewsArticle> }>(
          '/api/news',
          { params },
        ),
      );
      return unwrapPaged<NewsArticle>(res);
    });
  }

  async getById(id: string): Promise<Result<NewsArticle>> {
    return this.run(async () => {
      const res = await firstValueFrom(
        this.http.get<NewsArticle | { data?: NewsArticle }>(`/api/news/${encodeURIComponent(id)}`),
      );
      return unwrapSingle<NewsArticle>(res);
    });
  }

  async getFollowStatus(): Promise<boolean> {
    try {
      await firstValueFrom(this.http.get('/api/news/follow'));
      return true;
    } catch {
      return false;
    }
  }

  async followNews(): Promise<Result<void>> {
    return this.run(async () => {
      await firstValueFrom(this.http.post('/api/news/follow', {}));
    });
  }

  async unfollowNews(): Promise<Result<void>> {
    return this.run(async () => {
      await firstValueFrom(this.http.delete('/api/news/follow'));
    });
  }

  private async run<T>(fn: () => Promise<T>): Promise<Result<T>> {
    try {
      return { ok: true, value: await fn() };
    } catch (err) {
      return { ok: false, error: toFeatureError(err as HttpErrorResponse) };
    }
  }
}

function unwrapPaged<T>(
  res: PagedResult<T> | { data?: PagedResult<T> } | null | undefined,
): PagedResult<T> {
  if (!res) return { items: [], total: 0, page: 1, pageSize: 0 };
  if ('items' in res && Array.isArray((res as PagedResult<T>).items)) return res as PagedResult<T>;
  const inner = (res as { data?: PagedResult<T> }).data;
  if (inner && Array.isArray(inner.items)) return inner;
  return { items: [], total: 0, page: 1, pageSize: 0 };
}

function unwrapSingle<T>(res: T | { data?: T } | null | undefined): T {
  if (res && typeof res === 'object' && 'data' in (res as object)) {
    const inner = (res as { data?: T }).data;
    if (inner !== undefined && inner !== null) return inner;
  }
  return res as T;
}
