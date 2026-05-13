import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { toFeatureError, type FeatureError } from '@frontend/ui-kit';
import type { AdminPostRow, AdminPostStatus } from './admin-post.types';

interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}

export type Result<T> = { ok: true; value: T } | { ok: false; error: FeatureError };

/** Lightweight Topic row used by the topic filter dropdown. */
export interface TopicLite {
  id: string;
  nameEn: string;
  nameAr: string;
}

/**
 * Admin → community moderation API client.
 *
 * Reads from `/api/admin/community/posts` for the moderation list and
 * the existing soft-delete endpoints for the row-level actions. Topic
 * names are sourced from `/api/admin/topics` (TaxonomyApiService also
 * uses this; we don't import it here to keep the moderation feature
 * self-contained).
 */
@Injectable({ providedIn: 'root' })
export class CommunityModerationApiService {
  private readonly http = inject(HttpClient);

  /** Paginated list of community posts. Filters mirror the backend
   *  query: topicId / search / status / locale, plus paging. */
  async listPosts(opts: {
    page?: number;
    pageSize?: number;
    topicId?: string;
    search?: string;
    status?: AdminPostStatus;
    locale?: 'ar' | 'en';
  } = {}): Promise<Result<PagedResult<AdminPostRow>>> {
    let params = new HttpParams();
    if (opts.page !== undefined) params = params.set('page', opts.page);
    if (opts.pageSize !== undefined) params = params.set('pageSize', opts.pageSize);
    if (opts.topicId) params = params.set('topicId', opts.topicId);
    if (opts.search) params = params.set('search', opts.search);
    if (opts.status && opts.status !== 'all') params = params.set('status', opts.status);
    if (opts.locale) params = params.set('locale', opts.locale);
    return this.run(() =>
      firstValueFrom(
        this.http.get<PagedResult<AdminPostRow>>('/api/admin/community/posts', { params }),
      ),
    );
  }

  /** Soft-delete a post by id. */
  async softDeletePost(id: string): Promise<Result<void>> {
    return this.run(async () => {
      await firstValueFrom(this.http.delete<void>(`/api/admin/community/posts/${id}`));
    });
  }

  /** Soft-delete a reply by id. */
  async softDeleteReply(id: string): Promise<Result<void>> {
    return this.run(async () => {
      await firstValueFrom(this.http.delete<void>(`/api/admin/community/replies/${id}`));
    });
  }

  /** Lightweight topic list — used to populate the topic filter
   *  dropdown. Page-size 100 is enough for the seeded catalog. */
  async listTopicsLite(): Promise<Result<TopicLite[]>> {
    let params = new HttpParams().set('page', 1).set('pageSize', 100);
    return this.run(async () => {
      const res = await firstValueFrom(
        this.http.get<PagedResult<TopicLite>>('/api/admin/topics', { params }),
      );
      return res.items;
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
