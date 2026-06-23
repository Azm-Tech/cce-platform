import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnDestroy,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { TranslocoModule } from '@jsverse/transloco';
import { LocaleService } from '@frontend/i18n';
import { ToastService } from '@frontend/ui-kit';
import {
  RealtimeEvent,
  RealtimeHubService,
  type NewPostPayload,
  type PostModeratedPayload,
} from '@frontend/real-time';
import { AuthService } from '../../core/auth/auth.service';
import { FollowsApiService } from '../follows/follows-api.service';
import { CommunityApiService } from './community-api.service';
import { CommunityStateService } from './community-state.service';
import { PostSummaryComponent } from './post-summary.component';
import {
  ComposePostDialogComponent,
  type ComposePostDialogData,
  type ComposePostDialogResult,
} from './compose-post-dialog.component';
import type { CommunityRole, CommunityTopicSummary, CommunityUserProfile, PostType, PublicPost, PublicTopic } from './community.types';

/** Matches PostFeedSort backend enum: Hot=0, Newest=1, TopVoted=2, MostCommented=3 */
type FeedSort = 0 | 1 | 2 | 3;

@Component({
  selector: 'cce-topics-list-page',
  standalone: true,
  imports: [FormsModule, RouterLink, MatButtonModule, MatIconModule, MatMenuModule, TranslocoModule, PostSummaryComponent],
  templateUrl: './topics-list.page.html',
  styleUrl: './topics-list.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TopicsListPage implements OnInit, OnDestroy {
  private readonly api = inject(CommunityApiService);
  private readonly auth = inject(AuthService);
  private readonly communityState = inject(CommunityStateService);
  private readonly followsApi = inject(FollowsApiService);
  private readonly toast = inject(ToastService);
  private readonly dialog = inject(MatDialog);
  private readonly hub = inject(RealtimeHubService);
  private readonly destroyRef = inject(DestroyRef);
  private subscribedCommunityId: string | null = null;
  /** Count of posts published in the community since load (drives the pill). */
  readonly newPostCount = signal(0);
  readonly locale = inject(LocaleService).locale;

  readonly posts = signal<PublicPost[]>([]);
  readonly topicsMap = signal<Map<string, PublicTopic>>(new Map());
  readonly roles = signal<CommunityRole[]>([]);
  readonly expandedRoleKeys = signal<Set<string>>(new Set());
  readonly userProfile = signal<CommunityUserProfile | null>(null);
  readonly topicsLoading = signal(true);
  readonly postsLoading = signal(true);
  readonly feedError = signal<string | null>(null);
  readonly sort = signal<FeedSort>(1);
  readonly searchQuery = signal('');
  readonly topicFilter = signal<string | null>(null);
  readonly typeFilter = signal<PostType | null>(null);

  // ── Pagination ────────────────────────────────────────────────────────────
  readonly currentPage = signal(1);
  readonly totalItems = signal(0);
  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.totalItems() / 10)));

  readonly pageNumbers = computed(() => {
    const total = this.totalPages();
    const current = this.currentPage();
    if (total <= 7) return Array.from({ length: total }, (_, i) => i + 1);
    const pages = new Set<number>([1, total]);
    for (let i = Math.max(2, current - 2); i <= Math.min(total - 1, current + 2); i++) {
      pages.add(i);
    }
    return Array.from(pages).sort((a, b) => a - b);
  });

  // ── Auth ──────────────────────────────────────────────────────────────────
  readonly isAuthenticated = this.auth.isAuthenticated;

  // ── Current user display ──────────────────────────────────────────────────
  readonly userFullName = computed(() => {
    const u = this.auth.currentUser();
    if (!u) return '';
    return `${u.firstName ?? ''} ${u.lastName ?? ''}`.trim();
  });

  readonly userInitial = computed(() => this.userFullName().charAt(0).toUpperCase() || '؟');
  readonly isExpert = computed(() => this.userProfile()?.isExpert ?? false);
  readonly userPostCount = computed(() => this.userProfile()?.postCount ?? 0);

  // All filtering is done server-side; this is just an alias kept for template compatibility
  readonly filteredPosts = this.posts.asReadonly();

  // ── Type filter options ───────────────────────────────────────────────────
  readonly typeOptions: { value: PostType | null; labelKey: string }[] = [
    { value: null, labelKey: 'community.filter.allTypes' },
    { value: 0, labelKey: 'community.postType.informational' },
    { value: 1, labelKey: 'community.postType.question' },
    { value: 2, labelKey: 'community.postType.poll' },
  ];

  readonly currentTypeLabel = computed(() => {
    const t = this.typeFilter();
    const opt = this.typeOptions.find((o) => o.value === t);
    return opt?.labelKey ?? 'community.filter.allTypes';
  });

  /** Community topics with post counts (sidebar list with follow toggles). */
  readonly topicSummaries = signal<CommunityTopicSummary[]>([]);
  /** Followed topic ids — seeded from `isFollowed` on GET /api/community/topics. */
  readonly followedTopicIds = signal<Set<string>>(new Set());
  /** Total posts across all topics — shown on the "All" entry. */
  readonly allTopicsCount = computed(() =>
    this.topicSummaries().reduce((sum, t) => sum + (t.postsCount ?? 0), 0),
  );

  topicSummaryName(t: CommunityTopicSummary): string {
    return (this.locale() === 'ar' ? t.nameAr ?? t.nameEn : t.nameEn ?? t.nameAr) ?? '';
  }

  isTopicFollowed(id: string): boolean {
    return this.followedTopicIds().has(id);
  }

  /** Optimistic follow/unfollow for a sidebar topic (writes via /api/me/follows). */
  async toggleTopicFollow(id: string): Promise<void> {
    if (!this.auth.isAuthenticated()) return;
    const wasFollowing = this.isTopicFollowed(id);
    this.setTopicFollowed(id, !wasFollowing);
    const res = wasFollowing
      ? await this.followsApi.unfollow('topic', id)
      : await this.followsApi.follow('topic', id);
    if (!res.ok) {
      this.setTopicFollowed(id, wasFollowing); // revert
      this.toast.error('errors.ERR012');
    } else if (!wasFollowing) {
      this.toast.success('confirmations.CON010');
    }
  }

  private setTopicFollowed(id: string, value: boolean): void {
    this.followedTopicIds.update((s) => {
      const next = new Set(s);
      value ? next.add(id) : next.delete(id);
      return next;
    });
  }

  // ── Sort + skeleton config ────────────────────────────────────────────────
  readonly postSkeletons = Array.from({ length: 5 });
  readonly topicSkeletons = Array.from({ length: 7 });

  readonly sortOptions: { key: FeedSort; labelKey: string }[] = [
    { key: 0, labelKey: 'community.filter.sortHot' },
    { key: 1, labelKey: 'community.filter.sortNewest' },
    { key: 2, labelKey: 'community.filter.sortTop' },
    { key: 3, labelKey: 'community.filter.sortMostReplied' },
  ];

  readonly sortIcons: Record<FeedSort, string> = {
    0: 'flame',
    1: 'calendar',
    2: 'chevron-up',
    3: 'messages-square',
  };

  readonly topicsList = computed(() => Array.from(this.topicsMap().values()).slice(0, 10));

  ngOnInit(): void {
    void this.loadTopics();
    void this.loadFeed();
    void this.loadUserProfile();
    void this.loadRoles();
    void this.initRealtime();
  }

  ngOnDestroy(): void {
    if (this.subscribedCommunityId) this.hub.unsubscribeCommunity(this.subscribedCommunityId);
  }

  /** Join the community group and react to live posts / moderation. */
  private async initRealtime(): Promise<void> {
    await this.communityState.ensureLoaded();
    const communityId = this.communityState.communityId();
    if (!communityId) return;
    this.subscribedCommunityId = communityId;

    this.hub
      .on<NewPostPayload>(RealtimeEvent.NewPost)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((ev) => {
        if (ev.communityId !== communityId) return;
        const topicFilter = this.topicFilter();
        if (topicFilter && ev.topicId !== topicFilter) return;
        this.newPostCount.update((n) => n + 1);
      });

    this.hub
      .on<PostModeratedPayload>(RealtimeEvent.PostModerated)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((ev) => {
        if (ev.replyId) return; // reply deletions don't affect the feed list
        if (!this.posts().some((p) => p.id === ev.postId)) return;
        this.posts.update((ps) => ps.filter((p) => p.id !== ev.postId));
        this.totalItems.update((t) => Math.max(0, t - 1));
      });

    this.hub.subscribeCommunity(communityId);
  }

  /** Refresh the feed to surface posts that arrived live since load. */
  showNewPosts(): void {
    this.newPostCount.set(0);
    this.currentPage.set(1);
    void this.loadFeed();
  }

  private async loadRoles(): Promise<void> {
    const res = await this.api.listRoles();
    if (res.ok) this.roles.set(res.value);
  }

  toggleRole(key: string): void {
    this.expandedRoleKeys.update((prev) => {
      const next = new Set(prev);
      if (next.has(key)) next.delete(key);
      else next.add(key);
      return next;
    });
  }

  roleName(role: CommunityRole): string {
    return (this.locale() === 'ar' ? role.nameAr ?? role.nameEn : role.nameEn ?? role.nameAr) ?? role.key;
  }

  roleDescription(role: CommunityRole): string {
    return (this.locale() === 'ar' ? role.descriptionAr ?? role.descriptionEn : role.descriptionEn ?? role.descriptionAr) ?? '';
  }

  private async loadUserProfile(): Promise<void> {
    const userId = this.auth.currentUser()?.id;
    if (!userId) return;
    const res = await this.api.getCommunityUser(userId);
    if (res.ok) this.userProfile.set(res.value);
  }

  private async loadTopics(): Promise<void> {
    this.topicsLoading.set(true);
    const [topicsRes, summariesRes] = await Promise.all([
      this.api.listTopics(),
      this.api.listCommunityTopics({ pageSize: 100 }),
    ]);
    if (topicsRes.ok) {
      const m = new Map<string, PublicTopic>();
      for (const t of topicsRes.value) m.set(t.id, t);
      this.topicsMap.set(m);
    }
    if (summariesRes.ok) {
      this.topicSummaries.set(summariesRes.value.items);
      this.followedTopicIds.set(
        new Set(summariesRes.value.items.filter((t) => t.isFollowed).map((t) => t.id)),
      );
    }
    this.topicsLoading.set(false);
  }

  setSort(s: FeedSort): void {
    this.sort.set(s);
    this.currentPage.set(1);
    void this.loadFeed();
  }

  setTopicFilter(topicId: string | null): void {
    this.topicFilter.set(topicId);
    this.currentPage.set(1);
    void this.loadFeed();
  }

  setTypeFilter(t: PostType | null): void {
    this.typeFilter.set(t);
    this.currentPage.set(1);
    void this.loadFeed();
  }

  setPage(n: number): void {
    this.currentPage.set(n);
    void this.loadFeed();
  }

  async loadFeed(): Promise<void> {
    this.postsLoading.set(true);
    this.feedError.set(null);
    await this.communityState.ensureLoaded();
    const communityId = this.communityState.communityId() ?? '';
    const typeVal = this.typeFilter();
    const res = await this.api.listFeedPosts(communityId, {
      sort: String(this.sort()),
      topicId: this.topicFilter() ?? undefined,
      type: typeVal !== null ? typeVal : undefined,
      search: this.searchQuery().trim() || undefined,
      page: this.currentPage(),
      pageSize: 10,
    });
    this.postsLoading.set(false);
    if (res.ok) {
      this.posts.set(res.value.items);
      this.totalItems.set(res.value.total);
    } else {
      this.feedError.set(res.error.kind);
    }
  }

  getTopicName(post: PublicPost): string | null {
    if (this.locale() === 'ar') return post.topicNameAr ?? post.topicNameEn ?? null;
    return post.topicNameEn ?? post.topicNameAr ?? null;
  }

  private searchTimer: ReturnType<typeof setTimeout> | null = null;

  onSearchChange(v: string): void {
    this.searchQuery.set(v);
    if (this.searchTimer) clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => {
      this.currentPage.set(1);
      void this.loadFeed();
    }, 400);
  }

  clearSearch(): void {
    this.searchQuery.set('');
    this.currentPage.set(1);
    void this.loadFeed();
  }

  openNewPost(): void {
    const ref = this.dialog.open<
      ComposePostDialogComponent,
      ComposePostDialogData,
      ComposePostDialogResult
    >(ComposePostDialogComponent, {
      data: {
        topics: this.topicsList(),
        preselectedTopicId: this.topicFilter(),
      },
      width: '600px',
      maxWidth: '95vw',
      autoFocus: 'first-tabbable',
    });
    ref.afterClosed().subscribe((result) => {
      if (result?.submitted) void this.loadFeed();
    });
  }

  retry(): void {
    void this.loadTopics();
    void this.loadFeed();
    void this.loadUserProfile();
    void this.loadRoles();
  }
}
