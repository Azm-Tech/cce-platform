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
    return this.run(() =>
      firstValueFrom(this.http.get<PagedResult<NewsArticle>>('/api/news', { params })),
    );
  }

  async getBySlug(slug: string): Promise<Result<NewsArticle>> {
    return this.run(() =>
      firstValueFrom(this.http.get<NewsArticle>(`/api/news/${encodeURIComponent(slug)}`)),
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
