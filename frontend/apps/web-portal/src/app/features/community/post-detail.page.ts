import { CommonModule, DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  OnDestroy,
  OnInit,
  ViewChild,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, type PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslocoModule } from '@jsverse/transloco';
import { LocaleService } from '@frontend/i18n';
import { ToastService } from '@frontend/ui-kit';
import {
  RealtimeEvent,
  RealtimeHubService,
  type NewReplyPayload,
  type PollResultsChangedPayload,
  type PostModeratedPayload,
  type PresenceChangedPayload,
  type TypingChangedPayload,
  type VoteChangedPayload,
} from '@frontend/real-time';
import { AuthService } from '../../core/auth/auth.service';
import { FollowDirective } from '../follows/follow.directive';
import { CommunityApiService } from './community-api.service';
import { CommunityAuthPromptService } from './community-auth-prompt.service';
import { ComposeReplyFormComponent } from './compose-reply-form.component';
import { SharePostDialogComponent, type SharePostDialogData } from './share-post-dialog.component';
import { authorHandle, authorInitial, timeAgo } from './lib/social-helpers';
import { ReplyComponent } from './reply.component';
import type {
  CommunityUserProfile,
  PollOptionResult,
  PostPoll,
  PublicPost,
  PublicPostReply,
  PublicTopic,
  VoteDirection,
} from './community.types';

@Component({
  selector: 'cce-post-detail-page',
  standalone: true,
  imports: [
    CommonModule, DatePipe, RouterLink,
    MatButtonModule, MatIconModule, MatPaginatorModule, MatProgressBarModule,
    TranslocoModule,
    FollowDirective,
    ComposeReplyFormComponent,
    ReplyComponent,
  ],
  templateUrl: './post-detail.page.html',
  styleUrl: './post-detail.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PostDetailPage implements OnInit, OnDestroy {
  private readonly api = inject(CommunityApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly localeService = inject(LocaleService);
  private readonly auth = inject(AuthService);
  private readonly authPrompt = inject(CommunityAuthPromptService);
  private readonly toast = inject(ToastService);
  private readonly dialog = inject(MatDialog);
  private readonly hub = inject(RealtimeHubService);
  private readonly destroyRef = inject(DestroyRef);
  /** Post id this page joined the realtime `post:{id}` group for. */
  private subscribedPostId: string | null = null;

  // ── Presence & typing (realtime) ────────────────────────────────────────
  /** Live distinct-viewer count for this post (includes the current user). */
  readonly viewers = signal(0);
  private readonly typingUserIds = signal<Set<string>>(new Set());
  private readonly typingTimers = new Map<string, ReturnType<typeof setTimeout>>();
  readonly typingCount = computed(() => this.typingUserIds().size);
  private static readonly TYPING_EXPIRY_MS = 6000;

  readonly post = signal<PublicPost | null>(null);
  readonly replies = signal<PublicPostReply[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly pageSize = signal(20);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);
  /** Full author profile from GET /api/community/users/{id} — richer than
   *  post.author (adds job title / organization). Loaded in the background. */
  readonly authorProfile = signal<CommunityUserProfile | null>(null);
  readonly topicsMap = signal<Map<string, PublicTopic>>(new Map());
  readonly myVote = signal<VoteDirection>(0);
  readonly voting = signal(false);

  // ── Poll (type === 'Poll') ──────────────────────────────────────────────
  readonly poll = signal<PostPoll | null>(null);
  /** Staged option ids for multi-choice polls, before the user submits. */
  readonly pollSelection = signal<Set<string>>(new Set());
  readonly pollVoting = signal(false);

  readonly pollHasVoted = computed(() => (this.poll()?.myVotedOptionIds?.length ?? 0) > 0);
  readonly pollShowResults = computed(() => {
    const p = this.poll();
    return !!p && (p.isClosed || this.pollHasVoted() || p.resultsVisible);
  });
  readonly pollCanVote = computed(() => {
    const p = this.poll();
    return !!p && !p.isClosed && !this.pollHasVoted();
  });
  readonly pollClosed = computed(() => !!this.poll()?.isClosed);

  // ── Post follow (seeded from post.isFollowed, not FollowsStoreService) ──
  private readonly _postFollowed = signal<boolean | null>(null);
  readonly isPostFollowed = computed(
    () => this._postFollowed() ?? this.post()?.isFollowed ?? this.post()?.isWatchlisted ?? false,
  );

  readonly replySkeletons = Array.from({ length: 3 });

  readonly locale = this.localeService.locale;
  readonly isAuthenticated = this.auth.isAuthenticated;
  readonly currentUserId = computed(() => this.auth.currentUser()?.id ?? null);

  readonly notFound = computed(() => this.errorKind() === 'not-found');

  /** Author id resolved from the detail response's nested `author` (preferred)
   *  or the feed's flat `authorId` as a fallback. */
  readonly authorId = computed(() => this.post()?.author?.id ?? this.post()?.authorId ?? null);
  readonly authorIsExpert = computed(
    () => this.authorProfile()?.isExpert ?? this.post()?.author?.isExpert ?? this.post()?.isExpert ?? false,
  );
  readonly authorAvatarUrl = computed(
    () => this.authorProfile()?.avatarUrl ?? this.post()?.author?.avatarUrl ?? null,
  );
  readonly authorPostsCount = computed(
    () => this.authorProfile()?.postCount ?? this.post()?.author?.postsCount ?? 0,
  );

  readonly isOwnPost = computed(() => !!this.currentUserId() && this.currentUserId() === this.authorId());

  readonly topicName = computed<string | null>(() => {
    const p = this.post();
    if (!p) return null;
    const t = this.topicsMap().get(p.topicId);
    if (!t) return null;
    return this.locale() === 'ar' ? t.nameAr : t.nameEn;
  });

  readonly authorDisplayName = computed(() => {
    const prof = this.authorProfile();
    if (prof && (prof.firstName || prof.lastName)) {
      return [prof.firstName, prof.lastName].filter(Boolean).join(' ').trim();
    }
    const p = this.post();
    if (!p) return '';
    if (p.author?.name?.trim()) return p.author.name.trim();
    if (p.authorName?.trim()) return p.authorName.trim();
    return '';
  });

  // BC001: only upvotes shown publicly; +1 for an in-flight optimistic upvote
  readonly voteScore = computed(() => {
    const p = this.post();
    if (!p) return 0;
    return p.upvoteCount + (this.myVote() === 1 ? 1 : 0);
  });

  readonly authorDisplayInitial = computed(() => {
    const name = this.authorDisplayName();
    if (name) return name.charAt(0).toUpperCase();
    const id = this.authorId();
    return id ? authorInitial(id) : '?';
  });

  readonly canMarkAnswer = computed(() => {
    const p = this.post();
    if (!p || p.type !== 'Question') return false;
    return this.currentUserId() === this.authorId();
  });

  readonly postShowLangBadge = computed(() => {
    const p = this.post();
    return !!p && p.locale !== this.localeService.locale();
  });

  /** Replies are flat (no reply-on-reply); the accepted answer is hoisted first. */
  readonly orderedReplies = computed(() => {
    const p = this.post();
    const rs = this.replies();
    if (!p?.answeredReplyId) return rs;
    const accepted = rs.find((r) => r.id === p.answeredReplyId);
    if (!accepted) return rs;
    return [accepted, ...rs.filter((r) => r.id !== accepted.id)];
  });

  readonly authorFollowerCount = computed(
    () => this.authorProfile()?.followerCount ?? this.post()?.author?.followerCount ?? 0,
  );
  readonly authorJobTitle = computed(
    () => this.authorProfile()?.jobTitle ?? this.post()?.author?.jobTitle ?? null,
  );
  readonly authorOrganization = computed(
    () => this.authorProfile()?.organizationName ?? this.post()?.author?.organizationName ?? null,
  );

  @ViewChild('composer') composer?: ElementRef<HTMLElement>;

  timeAgo(iso: string): string {
    return timeAgo(iso, this.locale());
  }

  authorHandle(id: string): string {
    return authorHandle(id);
  }

  authorInitial(id: string): string {
    return authorInitial(id);
  }

  formatFileSize(bytes: number | null | undefined): string {
    if (bytes == null || bytes === 0) return '';
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  attachmentIcon(kind: number): string {
    return kind === 1 ? 'image' : 'description';
  }

  async ngOnInit(): Promise<void> {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.errorKind.set('not-found');
      return;
    }
    this.subscribedPostId = id;
    this.listenRealtime(id);
    this.hub.subscribePost(id);
    await this.load(id);
  }

  ngOnDestroy(): void {
    if (this.subscribedPostId) this.hub.unsubscribePost(this.subscribedPostId);
    for (const timer of this.typingTimers.values()) clearTimeout(timer);
    this.typingTimers.clear();
  }

  /** Wire live `post:{id}` events to the page state. */
  private listenRealtime(postId: string): void {
    this.hub
      .on<NewReplyPayload>(RealtimeEvent.NewReply)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((ev) => {
        if (ev.postId === postId) void this.refreshRepliesCurrentPage();
      });

    this.hub
      .on<VoteChangedPayload>(RealtimeEvent.VoteChanged)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((ev) => this.applyVoteChanged(ev, postId));

    this.hub
      .on<PollResultsChangedPayload>(RealtimeEvent.PollResultsChanged)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((ev) => {
        if (ev.postId === postId) this.applyPollResults(ev);
      });

    this.hub
      .on<PostModeratedPayload>(RealtimeEvent.PostModerated)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((ev) => this.applyModeration(ev, postId));

    this.hub
      .on<PresenceChangedPayload>(RealtimeEvent.PresenceChanged)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((ev) => {
        if (ev.postId === postId) this.viewers.set(ev.viewers);
      });

    this.hub
      .on<TypingChangedPayload>(RealtimeEvent.TypingChanged)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((ev) => {
        if (ev.postId === postId) this.applyTyping(ev);
      });

    // After a reconnect, catch up on events missed while disconnected.
    this.hub.reconnected$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => void this.catchUp());
  }

  /** Track which other users are typing; each entry auto-expires as a safety net. */
  private applyTyping(ev: TypingChangedPayload): void {
    if (!ev.userId || ev.userId === this.currentUserId()) return; // server doesn't echo self; guard anyway
    const timer = this.typingTimers.get(ev.userId);
    if (timer) clearTimeout(timer);
    if (ev.isTyping) {
      this.typingTimers.set(
        ev.userId,
        setTimeout(() => this.clearTyping(ev.userId), PostDetailPage.TYPING_EXPIRY_MS),
      );
      this.typingUserIds.update((s) => (s.has(ev.userId) ? s : new Set(s).add(ev.userId)));
    } else {
      this.clearTyping(ev.userId);
    }
  }

  private clearTyping(userId: string): void {
    const timer = this.typingTimers.get(userId);
    if (timer) clearTimeout(timer);
    this.typingTimers.delete(userId);
    this.typingUserIds.update((s) => {
      if (!s.has(userId)) return s;
      const next = new Set(s);
      next.delete(userId);
      return next;
    });
  }

  /** Reconnect catch-up: fetch the delta since `lastEventTime` and apply it. */
  private async catchUp(): Promise<void> {
    const p = this.post();
    if (!p) return;
    const res = await this.api.getPostActivity(p.id, this.hub.lastEventTime);
    if (!res.ok) return;
    const a = res.value;
    // Authoritative count includes the user's own vote — drop the optimistic +1.
    const optimistic = this.myVote() === 1 ? 1 : 0;
    this.post.update((cur) =>
      cur ? { ...cur, upvoteCount: Math.max(0, a.upvoteCount - optimistic) } : cur,
    );
    if (a.poll) this.applyPollResults(a.poll);
    if ((a.newReplies?.length ?? 0) > 0 || a.replyCount !== this.total()) {
      await this.refreshRepliesCurrentPage();
    }
  }

  /** Re-fetch the visible replies page (a new reply arrived from another user). */
  private async refreshRepliesCurrentPage(): Promise<void> {
    const p = this.post();
    if (!p) return;
    const res = await this.api.listReplies(p.id, { page: this.page(), pageSize: this.pageSize() });
    if (res.ok) {
      this.replies.set(res.value.items);
      this.total.set(Number(res.value.total));
    }
  }

  private applyVoteChanged(ev: VoteChangedPayload, postId: string): void {
    if (ev.postId === postId) {
      // The authoritative count already includes the user's own vote, so subtract
      // the optimistic +1 that `voteScore` adds while `myVote === 1`.
      const optimistic = this.myVote() === 1 ? 1 : 0;
      this.post.update((p) =>
        p ? { ...p, upvoteCount: Math.max(0, ev.upvoteCount - optimistic) } : p,
      );
    } else if (ev.replyId) {
      this.replies.update((rs) =>
        rs.map((r) => (r.id === ev.replyId ? { ...r, upvoteCount: ev.upvoteCount } : r)),
      );
    }
  }

  /** Update poll tallies in-place — from the `PollResultsChanged` push or a
   *  catch-up activity delta (both carry `totalVotes` + `options`). No refetch. */
  private applyPollResults(data: {
    totalVotes: number;
    options: { id: string; voteCount: number; percentage: number }[] | null;
  }): void {
    const updates = new Map((data.options ?? []).map((o) => [o.id, o]));
    this.poll.update((cur) => {
      if (!cur) return cur;
      const options = (cur.options ?? []).map((o) => {
        const u = updates.get(o.id);
        return u ? { ...o, voteCount: u.voteCount, percentage: u.percentage } : o;
      });
      return { ...cur, totalVotes: data.totalVotes, options };
    });
  }

  private applyModeration(ev: PostModeratedPayload, postId: string): void {
    if (ev.postId !== postId) return;
    if (ev.replyId) {
      this.replies.update((rs) => rs.filter((r) => r.id !== ev.replyId));
      this.total.update((t) => Math.max(0, t - 1));
    } else {
      // The post itself was moderated — reload to surface the removed state.
      void this.load(postId);
    }
  }

  async togglePostFollow(): Promise<void> {
    const p = this.post();
    if (!p || !this.isAuthenticated()) return;
    const current = this.isPostFollowed();
    this._postFollowed.set(!current);
    const res = current
      ? await this.api.unfollowPost(p.id)
      : await this.api.followPost(p.id);
    if (!res.ok) {
      this._postFollowed.set(current);
      this.toast.error('errors.' + res.error.kind);
    } else if (!current) {
      this.toast.success('confirmations.CON012');
    }
  }

  private async load(id: string): Promise<void> {
    this.loading.set(true);
    this.errorKind.set(null);
    this._postFollowed.set(null); // re-seed from fresh post.isWatchlisted
    this.authorProfile.set(null);
    this.poll.set(null);
    this.pollSelection.set(new Set());
    try {
      const [postRes, repliesRes, topicsRes] = await Promise.all([
        this.api.getPost(id),
        this.api.listReplies(id, { page: this.page(), pageSize: this.pageSize() }),
        this.api.listTopics(),
      ]);
      if (topicsRes.ok) {
        const map = new Map<string, PublicTopic>();
        for (const t of topicsRes.value) map.set(t.id, t);
        this.topicsMap.set(map);
      }
      if (postRes.ok) {
        this.post.set(postRes.value);
        this.poll.set(postRes.value.poll ?? null);
      } else {
        this.errorKind.set(postRes.error.kind);
      }
      if (repliesRes.ok) {
        this.replies.set(repliesRes.value.items);
        this.total.set(Number(repliesRes.value.total));
      }
    } catch {
      this.errorKind.set('server');
    } finally {
      this.loading.set(false);
    }

    // Enrich the sidebar with the full author profile (job title / org / counts).
    // Loaded in the background — post.author already covers the immediate render.
    const authorId = this.post()?.author?.id ?? this.post()?.authorId;
    if (authorId) {
      const profileRes = await this.api.getCommunityUser(authorId);
      if (profileRes.ok) this.authorProfile.set(profileRes.value);
    }
  }

  async onPage(e: PageEvent): Promise<void> {
    const p = this.post();
    if (!p) return;
    this.page.set(e.pageIndex + 1);
    this.pageSize.set(e.pageSize);
    const res = await this.api.listReplies(p.id, { page: this.page(), pageSize: this.pageSize() });
    if (res.ok) {
      this.replies.set(res.value.items);
      this.total.set(Number(res.value.total));
    }
  }

  async onReplyCreated(): Promise<void> {
    const p = this.post();
    if (!p) return;
    this.page.set(1);
    const res = await this.api.listReplies(p.id, { page: 1, pageSize: this.pageSize() });
    if (res.ok) {
      this.replies.set(res.value.items);
      this.total.set(Number(res.value.total));
    }
  }

  async onAnswerMarked(): Promise<void> {
    const p = this.post();
    if (!p) return;
    await this.load(p.id);
  }

  /** Opens the login/register dialog for an anonymous visitor (e.g. the follow button). */
  promptSignIn(messageKey = 'community.authDialog.message'): void {
    this.authPrompt.requireAuth(messageKey);
  }

  async onVote(dir: VoteDirection): Promise<void> {
    if (!this.authPrompt.requireAuth('community.authDialog.messageVote')) return;
    const p = this.post();
    if (!p || this.voting()) return;

    const prev = this.myVote();
    const next: VoteDirection = prev === dir ? 0 : dir;

    this.myVote.set(next);   // optimistic
    this.voting.set(true);

    const res = await this.api.votePost(p.id, next);

    this.voting.set(false);

    if (!res.ok) {
      this.myVote.set(prev);  // revert
      this.toast.error('errors.' + res.error.kind);
    }
  }

  // ── Poll voting ───────────────────────────────────────────────────────────
  isPollOptionSelected(optionId: string): boolean {
    if (this.pollHasVoted()) {
      return this.poll()?.myVotedOptionIds?.includes(optionId) ?? false;
    }
    return this.pollSelection().has(optionId);
  }

  /** Single-choice: votes immediately. Multi-choice: toggles the staged selection. */
  onPollOptionClick(optionId: string): void {
    const p = this.poll();
    if (!p || !this.pollCanVote() || this.pollVoting()) return;
    if (!this.authPrompt.requireAuth('community.authDialog.messageVote')) return;
    if (p.allowMultiple) {
      this.pollSelection.update((s) => {
        const next = new Set(s);
        next.has(optionId) ? next.delete(optionId) : next.add(optionId);
        return next;
      });
    } else {
      void this.castPollVote([optionId]);
    }
  }

  /** Submit the staged options for a multi-choice poll. */
  submitPollVote(): void {
    if (!this.authPrompt.requireAuth('community.authDialog.messageVote')) return;
    const ids = Array.from(this.pollSelection());
    if (ids.length === 0) return;
    void this.castPollVote(ids);
  }

  private async castPollVote(optionIds: string[]): Promise<void> {
    const p = this.poll();
    if (!p || this.pollVoting()) return;
    this.pollVoting.set(true);
    const res = await this.api.votePoll(p.pollId, optionIds);
    if (!res.ok) {
      this.pollVoting.set(false);
      this.toast.error('errors.' + res.error.kind);
      return;
    }
    // Record the user's choice locally, then refresh counts from the server.
    this.poll.update((cur) => (cur ? { ...cur, myVotedOptionIds: optionIds } : cur));
    const fresh = await this.api.getPollResults(p.pollId);
    this.pollVoting.set(false);
    if (fresh.ok) {
      const r = fresh.value;
      this.poll.update((cur) =>
        cur
          ? {
              ...cur,
              isClosed: r.isClosed,
              allowMultiple: r.allowMultiple,
              resultsVisible: r.resultsVisible,
              totalVotes: r.totalVotes,
              options: r.options ?? cur.options,
            }
          : cur,
      );
    }
  }

  pollDeadlineDate(): string | null {
    return this.poll()?.deadline ?? null;
  }

  trackPollOption = (_: number, opt: PollOptionResult): string => opt.id;

  retry(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) void this.load(id);
  }

  focusComposer(): void {
    const el = this.composer?.nativeElement;
    if (!el) return;
    el.scrollIntoView({ behavior: 'smooth', block: 'center' });
    const focusable = el.querySelector<HTMLElement>(
      'textarea, input, [tabindex]:not([tabindex="-1"])',
    );
    if (focusable) {
      setTimeout(() => focusable.focus(), 220);
    }
  }

  openShare(): void {
    const p = this.post();
    if (!p) return;
    this.dialog.open<SharePostDialogComponent, SharePostDialogData>(
      SharePostDialogComponent,
      {
        data: { url: `${window.location.origin}/community/posts/${p.id}`, title: p.title },
        width: '480px',
        maxWidth: '95vw',
        autoFocus: false,
        panelClass: 'cce-share-dialog',
      },
    );
  }
}
