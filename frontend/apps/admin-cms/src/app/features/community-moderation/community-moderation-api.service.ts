import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { toFeatureError, type FeatureError } from '@frontend/ui-kit';
import type {
  AdminPostDetail,
  AdminPostReply,
  AdminPostRow,
  CommunityLawSectionDto,
  CreateCommunityLawSectionRequest,
  UpdateCommunityLawSectionRequest,
} from './admin-post.types';

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
 * Reads from `/api/admin/community/posts` for the moderation list, the
 * `/api/admin/community/posts/{id}` + `/{id}/replies` endpoints for the
 * detail dialog, and the soft-delete endpoints for row-level actions. Topic
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
    postType?: 0 | 1 | 2;
    locale?: 'ar' | 'en';
  } = {}): Promise<Result<PagedResult<AdminPostRow>>> {
    let params = new HttpParams();
    if (opts.page !== undefined) params = params.set('page', opts.page);
    if (opts.pageSize !== undefined) params = params.set('pageSize', opts.pageSize);
    if (opts.topicId) params = params.set('topicId', opts.topicId);
    if (opts.search) params = params.set('search', opts.search);
    if (opts.postType !== undefined) params = params.set('postType', opts.postType);
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

  /** Full post detail from the admin moderation API.
   *  `/api/admin/*` responses are auto-unwrapped by the envelope
   *  interceptor, so we type the inner shape directly (no `.data`). */
  async getPostDetail(id: string): Promise<Result<AdminPostDetail>> {
    return this.run(async () => {
      const res = await firstValueFrom(
        this.http.get<AdminPostDetail>(`/api/admin/community/posts/${encodeURIComponent(id)}`),
      );
      if (!res) throw new Error('not-found');
      return res;
    });
  }

  /** Replies for a post from the admin moderation API (paged; up to 100).
   *  Auto-unwrapped — accepts a paged `{ items }` or a bare array. */
  async listPostReplies(postId: string): Promise<Result<AdminPostReply[]>> {
    const params = new HttpParams().set('page', 1).set('pageSize', 100);
    return this.run(async () => {
      const res = await firstValueFrom(
        this.http.get<PagedResult<AdminPostReply> | AdminPostReply[]>(
          `/api/admin/community/posts/${encodeURIComponent(postId)}/replies`,
          { params },
        ),
      );
      return Array.isArray(res) ? res : (res?.items ?? []);
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

  // ── Community Laws ──────────────────────────────────────────────────────────
  // `/api/admin/*` responses are auto-unwrapped by the envelope interceptor, so
  // these type the inner shape directly (no `.data`).

  /** Ordered list of community-law sections. */
  async listLaws(): Promise<Result<CommunityLawSectionDto[]>> {
    return this.run(async () => {
      const res = await firstValueFrom(
        this.http.get<CommunityLawSectionDto[]>('/api/admin/community-laws'),
      );
      const laws = res ?? [];
      return [...laws].sort((a, b) => a.orderIndex - b.orderIndex);
    });
  }

  async createSection(body: CreateCommunityLawSectionRequest): Promise<Result<void>> {
    return this.run(async () => {
      await firstValueFrom(this.http.post<void>('/api/admin/community-laws/sections', body));
    });
  }

  async updateSection(id: string, body: UpdateCommunityLawSectionRequest): Promise<Result<void>> {
    return this.run(async () => {
      await firstValueFrom(
        this.http.put<void>(`/api/admin/community-laws/sections/${encodeURIComponent(id)}`, body),
      );
    });
  }

  async deleteSection(id: string): Promise<Result<void>> {
    return this.run(async () => {
      await firstValueFrom(
        this.http.delete<void>(`/api/admin/community-laws/sections/${encodeURIComponent(id)}`),
      );
    });
  }

  async reorderSection(id: string, orderIndex: number): Promise<Result<void>> {
    return this.run(async () => {
      await firstValueFrom(
        this.http.put<void>(
          `/api/admin/community-laws/sections/${encodeURIComponent(id)}/order`,
          { orderIndex },
        ),
      );
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
