import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { toFeatureError, type FeatureError } from '@frontend/ui-kit';
import type {
  CommunityDto,
  CommunityRole,
  CommunityTopicSummary,
  CommunityUserProfile,
  CreatePostPayload,
  CreateReplyPayload,
  EditReplyPayload,
  FeaturedPost,
  MarkAnswerPayload,
  MentionableUser,
  MyCommentItem,
  PagedResult,
  PollInputPayload,
  PollResults,
  PostActivity,
  PostShareLink,
  PostType,
  PublicPost,
  PublicPostReply,
  PublicTopic,
  UpdateDraftPayload,
  VoteDirection,
} from './community.types';

export type Result<T> = { ok: true; value: T } | { ok: false; error: FeatureError };

/** Web-portal responses are wrapped in { data: T }. This unwraps them. */
function unwrap<T>(res: { data?: T } | T | null | undefined): T {
  if (res && typeof res === 'object' && 'data' in (res as object)) {
    const inner = (res as { data?: T }).data;
    if (inner !== undefined && inner !== null) return inner;
  }
  return res as T;
}

function unwrapPaged<T>(res: { data?: PagedResult<T> } | PagedResult<T> | null | undefined): PagedResult<T> {
  const inner = unwrap<PagedResult<T>>(res);
  if (inner && Array.isArray((inner as PagedResult<T>).items)) return inner as PagedResult<T>;
  return { items: [], total: 0, page: 1, pageSize: 0 };
}

@Injectable({ providedIn: 'root' })
export class CommunityApiService {
  private readonly http = inject(HttpClient);

  // ── Communities ───────────────────────────────────────────────────────────

  async listCommunities(opts: { page?: number; pageSize?: number } = {}): Promise<Result<PagedResult<CommunityDto>>> {
    let params = new HttpParams();
    if (opts.page !== undefined) params = params.set('page', opts.page);
    if (opts.pageSize !== undefined) params = params.set('pageSize', opts.pageSize);
    return this.run(async () =>
      unwrapPaged<CommunityDto>(
        await firstValueFrom(this.http.get<{ data?: PagedResult<CommunityDto> }>('/api/community/communities', { params })),
      ),
    );
  }

  async getCommunityBySlug(slug: string): Promise<Result<CommunityDto>> {
    return this.run(async () =>
      unwrap<CommunityDto>(
        await firstValueFrom(
          this.http.get<{ data?: CommunityDto }>(`/api/community/communities/${encodeURIComponent(slug)}`),
        ),
      ),
    );
  }

  async joinCommunity(id: string): Promise<Result<void>> {
    return this.run(async () => {
      await firstValueFrom(this.http.post(`/api/community/communities/${encodeURIComponent(id)}/join`, {}));
    });
  }

  async leaveCommunity(id: string): Promise<Result<void>> {
    return this.run(async () => {
      await firstValueFrom(this.http.post(`/api/community/communities/${encodeURIComponent(id)}/leave`, {}));
    });
  }

  async followCommunity(id: string): Promise<Result<void>> {
    return this.run(async () => {
      await firstValueFrom(this.http.post(`/api/community/communities/${encodeURIComponent(id)}/follow`, {}));
    });
  }

  async unfollowCommunity(id: string): Promise<Result<void>> {
    return this.run(async () => {
      await firstValueFrom(this.http.delete(`/api/community/communities/${encodeURIComponent(id)}/follow`));
    });
  }

  // ── Roles ─────────────────────────────────────────────────────────────────

  async listRoles(): Promise<Result<CommunityRole[]>> {
    return this.run(async () => {
      const res = await firstValueFrom(
        this.http.get<{ data?: CommunityRole[] } | CommunityRole[]>('/api/community/roles'),
      );
      return unwrap<CommunityRole[]>(res) ?? [];
    });
  }

  // ── Topics ────────────────────────────────────────────────────────────────

  async listTopics(): Promise<Result<PublicTopic[]>> {
    return this.run(async () => {
      const res = await firstValueFrom(
        this.http.get<{ data?: PublicTopic[] } | PublicTopic[]>('/api/topics'),
      );
      return unwrap<PublicTopic[]>(res) ?? [];
    });
  }

  /**
   * GET /api/community/topics — community topics with post counts (paged).
   * Used by the profile "followed topics" tab; follow state is layered on
   * client-side via the FollowsStoreService.
   */
  async listCommunityTopics(
    opts: { page?: number; pageSize?: number } = {},
  ): Promise<Result<PagedResult<CommunityTopicSummary>>> {
    let params = new HttpParams();
    if (opts.page !== undefined) params = params.set('page', opts.page);
    if (opts.pageSize !== undefined) params = params.set('pageSize', opts.pageSize);
    return this.run(async () =>
      unwrapPaged<CommunityTopicSummary>(
        await firstValueFrom(
          this.http.get<{ data?: PagedResult<CommunityTopicSummary> }>('/api/community/topics', { params }),
        ),
      ),
    );
  }

  async getTopicBySlug(slug: string): Promise<Result<PublicTopic>> {
    return this.run(async () =>
      unwrap<PublicTopic>(
        await firstValueFrom(
          this.http.get<{ data?: PublicTopic }>(`/api/community/topics/${encodeURIComponent(slug)}`),
        ),
      ),
    );
  }

  // ── Posts ─────────────────────────────────────────────────────────────────

  async listPosts(
    topicId: string,
    opts: { page?: number; pageSize?: number } = {},
  ): Promise<Result<PagedResult<PublicPost>>> {
    let params = new HttpParams();
    if (opts.page !== undefined) params = params.set('page', opts.page);
    if (opts.pageSize !== undefined) params = params.set('pageSize', opts.pageSize);
    return this.run(async () =>
      unwrapPaged<PublicPost>(
        await firstValueFrom(
          this.http.get<{ data?: PagedResult<PublicPost> }>(
            `/api/community/topics/${encodeURIComponent(topicId)}/posts`,
            { params },
          ),
        ),
      ),
    );
  }

  async getPost(id: string): Promise<Result<PublicPost>> {
    return this.run(async () =>
      unwrap<PublicPost>(
        await firstValueFrom(
          this.http.get<{ data?: PublicPost }>(`/api/community/posts/${encodeURIComponent(id)}`),
        ),
      ),
    );
  }

  async createPost(payload: CreatePostPayload): Promise<Result<{ id: string }>> {
    return this.run(async () => {
      // The API returns `data` as either the new id string or a { id } object.
      const data = unwrap<string | { id: string }>(
        await firstValueFrom(
          this.http.post<{ data?: string | { id: string } }>('/api/community/posts', payload),
        ),
      );
      return typeof data === 'string' ? { id: data } : data;
    });
  }

  async updateDraft(postId: string, payload: UpdateDraftPayload): Promise<Result<void>> {
    return this.run(async () => {
      await firstValueFrom(
        this.http.put(`/api/community/posts/${encodeURIComponent(postId)}/draft`, payload),
      );
    });
  }

  async deleteDraft(postId: string): Promise<Result<void>> {
    return this.run(async () => {
      await firstValueFrom(this.http.delete(`/api/community/posts/${encodeURIComponent(postId)}/draft`));
    });
  }

  async publishPost(postId: string): Promise<Result<void>> {
    return this.run(async () => {
      await firstValueFrom(
        this.http.post(`/api/community/posts/${encodeURIComponent(postId)}/publish`, {}),
      );
    });
  }

  async listMyDrafts(opts: { page?: number; pageSize?: number } = {}): Promise<Result<PagedResult<PublicPost>>> {
    let params = new HttpParams();
    if (opts.page !== undefined) params = params.set('page', opts.page);
    if (opts.pageSize !== undefined) params = params.set('pageSize', opts.pageSize);
    return this.run(async () =>
      unwrapPaged<PublicPost>(
        await firstValueFrom(this.http.get<{ data?: PagedResult<PublicPost> }>('/api/me/posts/drafts', { params })),
      ),
    );
  }

  async votePost(postId: string, direction: VoteDirection): Promise<Result<void>> {
    return this.run(async () => {
      await firstValueFrom(
        this.http.post(`/api/community/posts/${encodeURIComponent(postId)}/vote`, { direction }),
      );
    });
  }

  /** Rate a post 1–5 stars (used by the rate-post control). */
  async ratePost(postId: string, stars: number): Promise<Result<void>> {
    return this.run(async () => {
      await firstValueFrom(
        this.http.post(`/api/community/posts/${encodeURIComponent(postId)}/rate`, { stars }),
      );
    });
  }

  async sharePost(postId: string): Promise<Result<PostShareLink>> {
    return this.run(async () =>
      unwrap<PostShareLink>(
        await firstValueFrom(
          this.http.get<{ data?: PostShareLink }>(`/api/community/posts/${encodeURIComponent(postId)}/share`),
        ),
      ),
    );
  }

  async markAnswer(postId: string, replyId: string): Promise<Result<void>> {
    return this.run(async () => {
      const payload: MarkAnswerPayload = { replyId };
      await firstValueFrom(
        this.http.post(`/api/community/posts/${encodeURIComponent(postId)}/mark-answer`, payload),
      );
    });
  }

  // ── Post follow ───────────────────────────────────────────────────────────

  async followPost(postId: string): Promise<Result<void>> {
    return this.run(async () => {
      await firstValueFrom(
        this.http.put(`/api/me/follows/posts/${encodeURIComponent(postId)}`, { status: 1 }),
      );
    });
  }

  async unfollowPost(postId: string): Promise<Result<void>> {
    return this.run(async () => {
      await firstValueFrom(
        this.http.put(`/api/me/follows/posts/${encodeURIComponent(postId)}`, { status: 0 }),
      );
    });
  }

  // ── Replies ───────────────────────────────────────────────────────────────

  async listReplies(
    postId: string,
    opts: { page?: number; pageSize?: number } = {},
  ): Promise<Result<PagedResult<PublicPostReply>>> {
    let params = new HttpParams();
    if (opts.page !== undefined) params = params.set('page', opts.page);
    if (opts.pageSize !== undefined) params = params.set('pageSize', opts.pageSize);
    return this.run(async () =>
      unwrapPaged<PublicPostReply>(
        await firstValueFrom(
          this.http.get<{ data?: PagedResult<PublicPostReply> }>(
            `/api/community/posts/${encodeURIComponent(postId)}/replies`,
            { params },
          ),
        ),
      ),
    );
  }

  async getReplyThread(
    replyId: string,
    opts: { page?: number; pageSize?: number } = {},
  ): Promise<Result<PagedResult<PublicPostReply>>> {
    let params = new HttpParams();
    if (opts.page !== undefined) params = params.set('page', opts.page);
    if (opts.pageSize !== undefined) params = params.set('pageSize', opts.pageSize);
    return this.run(async () =>
      unwrapPaged<PublicPostReply>(
        await firstValueFrom(
          this.http.get<{ data?: PagedResult<PublicPostReply> }>(
            `/api/community/replies/${encodeURIComponent(replyId)}/thread`,
            { params },
          ),
        ),
      ),
    );
  }

  async createReply(postId: string, payload: CreateReplyPayload): Promise<Result<{ id: string }>> {
    return this.run(async () =>
      unwrap<{ id: string }>(
        await firstValueFrom(
          this.http.post<{ data?: { id: string } }>(
            `/api/community/posts/${encodeURIComponent(postId)}/replies`,
            payload,
          ),
        ),
      ),
    );
  }

  async editReply(replyId: string, content: string): Promise<Result<void>> {
    return this.run(async () => {
      const payload: EditReplyPayload = { content };
      await firstValueFrom(
        this.http.put(`/api/community/replies/${encodeURIComponent(replyId)}`, payload),
      );
    });
  }

  async voteReply(replyId: string, direction: VoteDirection): Promise<Result<void>> {
    return this.run(async () => {
      await firstValueFrom(
        this.http.post(`/api/community/replies/${encodeURIComponent(replyId)}/vote`, { direction }),
      );
    });
  }

  // ── Polls ─────────────────────────────────────────────────────────────────

  async getPollResults(pollId: string): Promise<Result<PollResults>> {
    return this.run(async () =>
      unwrap<PollResults>(
        await firstValueFrom(
          this.http.get<{ data?: PollResults }>(`/api/community/polls/${encodeURIComponent(pollId)}/results`),
        ),
      ),
    );
  }

  /** Reconnect catch-up delta — events missed on a post since `since` (AllowAnonymous). */
  async getPostActivity(postId: string, since: string | null): Promise<Result<PostActivity>> {
    let params = new HttpParams();
    if (since) params = params.set('since', since);
    return this.run(async () =>
      unwrap<PostActivity>(
        await firstValueFrom(
          this.http.get<{ data?: PostActivity }>(
            `/api/community/posts/${encodeURIComponent(postId)}/activity`,
            { params },
          ),
        ),
      ),
    );
  }

  async votePoll(pollId: string, optionIds: string[]): Promise<Result<void>> {
    return this.run(async () => {
      await firstValueFrom(
        this.http.post(`/api/community/polls/${encodeURIComponent(pollId)}/vote`, { optionIds }),
      );
    });
  }

  // ── Community user profile ────────────────────────────────────────────────

  async getMentionableUsers(
    communityId: string,
    q: string,
    limit = 10,
  ): Promise<Result<MentionableUser[]>> {
    const params = new HttpParams().set('q', q).set('limit', limit);
    return this.run(async () =>
      unwrap<MentionableUser[]>(
        await firstValueFrom(
          this.http.get<{ data?: MentionableUser[] }>(
            `/api/community/communities/${encodeURIComponent(communityId)}/mentionable-users`,
            { params },
          ),
        ),
      ) ?? [],
    );
  }

  async getCommunityUser(userId: string): Promise<Result<CommunityUserProfile>> {
    return this.run(async () =>
      unwrap<CommunityUserProfile>(
        await firstValueFrom(
          this.http.get<{ data?: CommunityUserProfile }>(`/api/community/users/${encodeURIComponent(userId)}`),
        ),
      ),
    );
  }

  async getMyComments(
    opts: { page?: number; pageSize?: number; sort?: 'newest' | 'oldest' } = {},
  ): Promise<Result<PagedResult<MyCommentItem>>> {
    let params = new HttpParams();
    if (opts.page !== undefined) params = params.set('page', opts.page);
    if (opts.pageSize !== undefined) params = params.set('pageSize', opts.pageSize);
    params = params.set('sort', opts.sort ?? 'newest');
    return this.run(async () =>
      unwrapPaged<MyCommentItem>(
        await firstValueFrom(
          this.http.get<{ data?: PagedResult<MyCommentItem> }>('/api/me/comments', { params }),
        ),
      ),
    );
  }

  // ── Feed ──────────────────────────────────────────────────────────────────

  /** Posts authored by users the current user follows. */
  async getMyFeed(
    opts: { page?: number; pageSize?: number } = {},
  ): Promise<Result<PagedResult<PublicPost>>> {
    let params = new HttpParams();
    if (opts.page !== undefined) params = params.set('page', opts.page);
    if (opts.pageSize !== undefined) params = params.set('pageSize', opts.pageSize);
    return this.run(async () =>
      unwrapPaged<PublicPost>(
        await firstValueFrom(
          this.http.get<{ data?: PagedResult<PublicPost> }>('/api/me/feed', { params }),
        ),
      ),
    );
  }

  async getFeaturedPosts(opts: { page?: number; pageSize?: number } = {}): Promise<Result<PagedResult<FeaturedPost>>> {
    let params = new HttpParams();
    if (opts.page !== undefined) params = params.set('page', opts.page);
    if (opts.pageSize !== undefined) params = params.set('pageSize', opts.pageSize);
    return this.run(async () =>
      unwrapPaged<FeaturedPost>(
        await firstValueFrom(
          this.http.get<{ data?: PagedResult<FeaturedPost> }>('/api/feed/featured-posts', { params }),
        ),
      ),
    );
  }

  async listFeedPosts(
    communityId: string,
    opts: { page?: number; pageSize?: number; sort?: string; topicId?: string; type?: PostType; search?: string; authorId?: string; isWatchlisted?: boolean } = {},
  ): Promise<Result<PagedResult<PublicPost>>> {
    let params = new HttpParams();
    if (communityId) params = params.set('communityId', communityId);
    if (opts.page !== undefined) params = params.set('page', opts.page);
    if (opts.pageSize !== undefined) params = params.set('pageSize', opts.pageSize);
    if (opts.sort) params = params.set('sort', opts.sort);
    if (opts.topicId) params = params.set('topicId', opts.topicId);
    if (opts.type !== undefined && opts.type !== null) params = params.set('postType', opts.type);
    if (opts.search?.trim()) params = params.set('searchTerm', opts.search.trim());
    if (opts.authorId) params = params.set('authorId', opts.authorId);
    if (opts.isWatchlisted !== undefined) params = params.set('isWatchlisted', opts.isWatchlisted);
    return this.run(async () =>
      unwrapPaged<PublicPost>(
        await firstValueFrom(
          this.http.get<{ data?: PagedResult<PublicPost> }>('/api/community/feed', { params }),
        ),
      ),
    );
  }

  // ── Poll payload builder (convenience) ───────────────────────────────────

  static buildPollPayload(opts: {
    deadline: string;
    optionLabels: string[];
    allowMultiple?: boolean;
    isAnonymous?: boolean;
    showResultsBeforeClose?: boolean;
  }): PollInputPayload {
    return {
      deadline: opts.deadline,
      optionLabels: opts.optionLabels,
      allowMultiple: opts.allowMultiple ?? false,
      isAnonymous: opts.isAnonymous ?? false,
      showResultsBeforeClose: opts.showResultsBeforeClose ?? false,
    };
  }

  // ── Private ───────────────────────────────────────────────────────────────

  private async run<T>(fn: () => Promise<T>): Promise<Result<T>> {
    try {
      return { ok: true, value: await fn() };
    } catch (err) {
      return { ok: false, error: toFeatureError(err as HttpErrorResponse) };
    }
  }
}
