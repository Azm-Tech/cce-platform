import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { TranslocoModule } from '@jsverse/transloco';
import { LocaleService } from '@frontend/i18n';
import { AuthService } from '../../core/auth/auth.service';
import { FollowDirective } from '../follows/follow.directive';
import { timeAgo } from './lib/social-helpers';
import { CommunityApiService } from './community-api.service';
import { CommunityStateService } from './community-state.service';
import { PostSummaryComponent } from './post-summary.component';
import { ComposePostDialogComponent } from './compose-post-dialog.component';
import { CommunityFollowingDialogComponent } from './community-following-dialog.component';
import type { CommunityTopicSummary, CommunityUserProfile, MentionItem, PostType, PublicPost, PublicTopic } from './community.types';

/** Matches PostFeedSort backend enum: Hot=0, Newest=1, TopVoted=2, MostCommented=3 */
type FeedSort = 0 | 1 | 2 | 3;
type ProfileTab = 'posts' | 'followed-posts' | 'followed-topics' | 'my-mentions';

@Component({
  selector: 'cce-community-my-profile-page',
  standalone: true,
  imports: [RouterLink, MatIconModule, MatMenuModule, TranslocoModule, FollowDirective, PostSummaryComponent],
  templateUrl: './community-my-profile.page.html',
  styleUrl: './community-my-profile.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CommunityMyProfilePage implements OnInit {
  private readonly api = inject(CommunityApiService);
  private readonly auth = inject(AuthService);
  private readonly communityState = inject(CommunityStateService);
  private readonly localeService = inject(LocaleService);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);

  // ── Profile ────────────────────────────────────────────────────────────────
  readonly profile = signal<CommunityUserProfile | null>(null);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly locale = this.localeService.locale;
  readonly currentUserId = computed(() => this.auth.currentUser()?.id ?? null);

  readonly displayName = computed(() => {
    const p = this.profile();
    if (!p) return '';
    return [p.firstName, p.lastName].filter(Boolean).join(' ');
  });

  readonly initial = computed(() => this.displayName().charAt(0).toUpperCase() || '؟');

  /** Expert biography in the active language ("السيرة الذاتية"). */
  readonly expertBio = computed(() => {
    const p = this.profile();
    if (!p) return null;
    const bio = this.locale() === 'ar' ? p.expertBioAr : p.expertBioEn;
    return bio?.trim() || null;
  });

  /** Country name in the active language. */
  readonly country = computed(() => {
    const p = this.profile();
    if (!p) return null;
    const c = this.locale() === 'ar' ? p.countryNameAr : p.countryNameEn;
    return c?.trim() || p.location?.trim() || null;
  });

  readonly joinedOnFormatted = computed(() => {
    const d = this.profile()?.joinedOn ?? this.profile()?.joinedDate;
    if (!d) return null;
    try {
      const loc = this.locale() === 'ar' ? 'ar-SA' : 'en-US';
      return new Intl.DateTimeFormat(loc, { month: 'long', year: 'numeric' }).format(new Date(d));
    } catch {
      return null;
    }
  });

  // ── Tabs ──────────────────────────────────────────────────────────────────
  readonly activeTab = signal<ProfileTab>('posts');

  // ── My Posts tab ──────────────────────────────────────────────────────────
  readonly posts = signal<PublicPost[]>([]);
  readonly topicsMap = signal<Map<string, PublicTopic>>(new Map());
  readonly postsLoading = signal(false);
  readonly postsFeedError = signal<string | null>(null);
  readonly sort = signal<FeedSort>(1);
  readonly searchQuery = signal('');
  readonly topicFilter = signal<string | null>(null);
  readonly typeFilter = signal<PostType | null>(null);
  readonly currentPage = signal(1);
  readonly totalItems = signal(0);
  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.totalItems() / 10)));
  readonly pageNumbers = computed(() => this.buildPageNums(this.totalPages(), this.currentPage()));

  // ── Followed Posts tab ────────────────────────────────────────────────────
  readonly feedPosts = signal<PublicPost[]>([]);
  readonly feedLoading = signal(false);
  readonly feedError = signal<string | null>(null);
  readonly feedPage = signal(1);
  readonly feedTotal = signal(0);
  readonly feedTotalPages = computed(() => Math.max(1, Math.ceil(this.feedTotal() / 10)));
  readonly feedPageNums = computed(() => this.buildPageNums(this.feedTotalPages(), this.feedPage()));

  // ── Followed Topics tab ───────────────────────────────────────────────────
  // Lists ALL community topics (with post counts); each row carries a follow
  // toggle whose state comes from the FollowsStoreService via [cceFollow].
  readonly communityTopics = signal<CommunityTopicSummary[]>([]);
  readonly topicsLoading = signal(false);
  readonly topicsError = signal<string | null>(null);

  // ── My Mentions tab ───────────────────────────────────────────────────────
  readonly mentions = signal<MentionItem[]>([]);
  readonly mentionsLoading = signal(false);
  readonly mentionsError = signal<string | null>(null);
  readonly mentionsPage = signal(1);
  readonly mentionsTotal = signal(0);
  readonly mentionsTotalPages = computed(() => Math.max(1, Math.ceil(this.mentionsTotal() / 20)));
  readonly mentionsPageNums = computed(() => this.buildPageNums(this.mentionsTotalPages(), this.mentionsPage()));

  // ── Shared ────────────────────────────────────────────────────────────────
  readonly topicsList = computed(() => Array.from(this.topicsMap().values()).slice(0, 20));
  readonly postSkeletons = Array.from({ length: 4 });

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

  readonly typeOptions: { value: PostType | null; labelKey: string }[] = [
    { value: null, labelKey: 'community.filter.allTypes' },
    { value: 0, labelKey: 'community.postType.informational' },
    { value: 1, labelKey: 'community.postType.question' },
    { value: 2, labelKey: 'community.postType.poll' },
  ];

  readonly currentTypeLabel = computed(() => {
    const t = this.typeFilter();
    return (this.typeOptions.find((o) => o.value === t)?.labelKey) ?? 'community.filter.allTypes';
  });

  readonly currentTopicLabel = computed(() => {
    const id = this.topicFilter();
    if (!id) return 'community.filter.allTopics';
    const t = this.topicsMap().get(id);
    if (!t) return 'community.filter.allTopics';
    return this.locale() === 'ar' ? (t.nameAr ?? t.nameEn ?? '') : (t.nameEn ?? t.nameAr ?? '');
  });

  readonly isTopicFilterAll = computed(() => !this.topicFilter());

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    const uid = this.currentUserId();
    if (!uid) {
      this.errorKind.set('unauthorized');
      return;
    }
    this.loading.set(true);
    this.errorKind.set(null);
    const [profileRes] = await Promise.all([
      this.api.getCommunityUser(uid),
      this.loadTopics(),
    ]);
    this.loading.set(false);
    if (profileRes.ok) {
      this.profile.set(profileRes.value);
      void this.loadMyPosts();
    } else {
      this.errorKind.set(profileRes.error.kind);
    }
  }

  private async loadTopics(): Promise<void> {
    const res = await this.api.listTopics();
    if (res.ok) {
      const m = new Map<string, PublicTopic>();
      for (const t of res.value) m.set(t.id, t);
      this.topicsMap.set(m);
    }
  }

  setTab(tab: ProfileTab): void {
    if (this.activeTab() === tab) return;
    this.activeTab.set(tab);
    if (tab === 'followed-posts' && this.feedPosts().length === 0 && !this.feedLoading()) {
      void this.loadFollowedPosts();
    }
    if (tab === 'followed-topics' && this.communityTopics().length === 0 && !this.topicsLoading()) {
      void this.loadFollowedTopics();
    }
    if (tab === 'my-mentions' && this.mentions().length === 0 && !this.mentionsLoading()) {
      void this.loadMyMentions();
    }
  }

  // ── My Posts ──────────────────────────────────────────────────────────────
  async loadMyPosts(): Promise<void> {
    const authorId = this.currentUserId();
    if (!authorId) return;
    this.postsLoading.set(true);
    this.postsFeedError.set(null);
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
      authorId,
    });
    this.postsLoading.set(false);
    if (res.ok) {
      this.posts.set(res.value.items);
      this.totalItems.set(res.value.total);
    } else {
      this.postsFeedError.set(res.error.kind);
    }
  }

  setSort(s: FeedSort): void {
    this.sort.set(s);
    this.currentPage.set(1);
    void this.loadMyPosts();
  }

  setTopicFilter(id: string | null): void {
    this.topicFilter.set(id);
    this.currentPage.set(1);
    void this.loadMyPosts();
  }

  setTypeFilter(t: PostType | null): void {
    this.typeFilter.set(t);
    this.currentPage.set(1);
    void this.loadMyPosts();
  }

  setPage(n: number): void {
    this.currentPage.set(n);
    void this.loadMyPosts();
  }

  private searchTimer: ReturnType<typeof setTimeout> | null = null;

  onSearchChange(v: string): void {
    this.searchQuery.set(v);
    if (this.searchTimer) clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => {
      this.currentPage.set(1);
      void this.loadMyPosts();
    }, 400);
  }

  clearSearch(): void {
    this.searchQuery.set('');
    this.currentPage.set(1);
    void this.loadMyPosts();
  }

  // ── Followed Posts ────────────────────────────────────────────────────────
  async loadFollowedPosts(): Promise<void> {
    this.feedLoading.set(true);
    this.feedError.set(null);
    await this.communityState.ensureLoaded();
    const communityId = this.communityState.communityId() ?? '';
    const res = await this.api.listFeedPosts(communityId, {
      sort: '1',
      page: this.feedPage(),
      pageSize: 10,
      isWatchlisted: true,
    });
    this.feedLoading.set(false);
    if (res.ok) {
      this.feedPosts.set(res.value.items);
      this.feedTotal.set(res.value.total);
    } else {
      this.feedError.set(res.error.kind);
    }
  }

  setFeedPage(n: number): void {
    this.feedPage.set(n);
    void this.loadFollowedPosts();
  }

  // ── Followed Topics ───────────────────────────────────────────────────────
  async loadFollowedTopics(): Promise<void> {
    this.topicsLoading.set(true);
    this.topicsError.set(null);
    const res = await this.api.listCommunityTopics({ pageSize: 100 });
    this.topicsLoading.set(false);
    if (res.ok) {
      this.communityTopics.set(res.value.items);
    } else {
      this.topicsError.set(res.error.kind);
    }
  }

  topicSummaryName(t: CommunityTopicSummary): string {
    return (this.locale() === 'ar' ? t.nameAr ?? t.nameEn : t.nameEn ?? t.nameAr) ?? '';
  }

  // ── My Mentions ───────────────────────────────────────────────────────────
  async loadMyMentions(): Promise<void> {
    this.mentionsLoading.set(true);
    this.mentionsError.set(null);
    const res = await this.api.getMyMentions({ page: this.mentionsPage(), pageSize: 20 });
    this.mentionsLoading.set(false);
    if (res.ok) {
      this.mentions.set(res.value.items);
      this.mentionsTotal.set(res.value.total);
    } else {
      this.mentionsError.set(res.error.kind);
    }
  }

  setMentionsPage(n: number): void {
    this.mentionsPage.set(n);
    void this.loadMyMentions();
  }

  navigateToMention(item: MentionItem): void {
    const fragment = item.sourceType === 'Reply' ? `reply-${item.sourceId}` : `post-${item.postId}`;
    void this.router.navigate(['/community/posts', item.postId], { fragment });
  }

  mentionTimeAgo(iso: string): string { return timeAgo(iso, this.locale()); }

  // ── Shared helpers ────────────────────────────────────────────────────────
  getTopicName(post: PublicPost): string | null {
    return this.locale() === 'ar'
      ? (post.topicNameAr ?? post.topicNameEn ?? null)
      : (post.topicNameEn ?? post.topicNameAr ?? null);
  }

  topicLabel(t: PublicTopic): string {
    return (this.locale() === 'ar' ? t.nameAr ?? t.nameEn : t.nameEn ?? t.nameAr) ?? '';
  }

  openCreatePost(): void {
    const ref = ComposePostDialogComponent.open(this.dialog, {
      topics: this.topicsList(),
    });
    ref.afterClosed().subscribe((result) => {
      if (result?.submitted) {
        this.currentPage.set(1);
        void this.loadMyPosts();
      }
    });
  }

  /** Open the dialog listing the members the current user follows. */
  openFollowing(): void {
    CommunityFollowingDialogComponent.open(this.dialog);
  }

  private buildPageNums(total: number, current: number): number[] {
    if (total <= 7) return Array.from({ length: total }, (_, i) => i + 1);
    const pages = new Set<number>([1, total]);
    for (let i = Math.max(2, current - 2); i <= Math.min(total - 1, current + 2); i++) {
      pages.add(i);
    }
    return Array.from(pages).sort((a, b) => a - b);
  }
}
