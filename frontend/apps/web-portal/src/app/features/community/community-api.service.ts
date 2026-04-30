import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { toFeatureError, type FeatureError } from '@frontend/ui-kit';
import type {
  CreatePostPayload,
  CreateReplyPayload,
  PagedResult,
  PublicPost,
  PublicPostReply,
  PublicTopic,
} from './community.types';

export type Result<T> = { ok: true; value: T } | { ok: false; error: FeatureError };

@Injectable({ providedIn: 'root' })
export class CommunityApiService {
  private readonly http = inject(HttpClient);

  async listTopics(): Promise<Result<PublicTopic[]>> {
    return this.run(() => firstValueFrom(this.http.get<PublicTopic[]>('/api/topics')));
  }

  async getTopicBySlug(slug: string): Promise<Result<PublicTopic>> {
    return this.run(() =>
      firstValueFrom(
        this.http.get<PublicTopic>(`/api/community/topics/${encodeURIComponent(slug)}`),
      ),
    );
  }

  async listPosts(
    topicId: string,
    opts: { page?: number; pageSize?: number } = {},
  ): Promise<Result<PagedResult<PublicPost>>> {
    let params = new HttpParams();
    if (opts.page !== undefined) params = params.set('page', opts.page);
    if (opts.pageSize !== undefined) params = params.set('pageSize', opts.pageSize);
    return this.run(() =>
      firstValueFrom(
        this.http.get<PagedResult<PublicPost>>(
          `/api/community/topics/${encodeURIComponent(topicId)}/posts`,
          { params },
        ),
      ),
    );
  }

  async getPost(id: string): Promise<Result<PublicPost>> {
    return this.run(() =>
      firstValueFrom(
        this.http.get<PublicPost>(`/api/community/posts/${encodeURIComponent(id)}`),
      ),
    );
  }

  async listReplies(
    postId: string,
    opts: { page?: number; pageSize?: number } = {},
  ): Promise<Result<PagedResult<PublicPostReply>>> {
    let params = new HttpParams();
    if (opts.page !== undefined) params = params.set('page', opts.page);
    if (opts.pageSize !== undefined) params = params.set('pageSize', opts.pageSize);
    return this.run(() =>
      firstValueFrom(
        this.http.get<PagedResult<PublicPostReply>>(
          `/api/community/posts/${encodeURIComponent(postId)}/replies`,
          { params },
        ),
      ),
    );
  }

  async createPost(payload: CreatePostPayload): Promise<Result<{ id: string }>> {
    return this.run(() =>
      firstValueFrom(
        this.http.post<{ id: string }>('/api/community/posts', payload),
      ),
    );
  }

  async createReply(
    postId: string,
    payload: CreateReplyPayload,
  ): Promise<Result<{ id: string }>> {
    return this.run(() =>
      firstValueFrom(
        this.http.post<{ id: string }>(
          `/api/community/posts/${encodeURIComponent(postId)}/replies`,
          payload,
        ),
      ),
    );
  }

  async ratePost(postId: string, stars: number): Promise<Result<void>> {
    return this.run(async () => {
      await firstValueFrom(
        this.http.post(
          `/api/community/posts/${encodeURIComponent(postId)}/rate`,
          { stars },
        ),
      );
    });
  }

  async markAnswer(postId: string, replyId: string): Promise<Result<void>> {
    return this.run(async () => {
      await firstValueFrom(
        this.http.post(
          `/api/community/posts/${encodeURIComponent(postId)}/mark-answer`,
          { replyId },
        ),
      );
    });
  }

  async editReply(replyId: string, content: string): Promise<Result<void>> {
    return this.run(async () => {
      await firstValueFrom(
        this.http.put(
          `/api/community/replies/${encodeURIComponent(replyId)}`,
          { content },
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
