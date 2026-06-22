import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { TranslocoModule } from '@jsverse/transloco';
import { LocaleService } from '@frontend/i18n';
import { FollowDirective } from '../follows/follow.directive';
import { AuthService } from '../../core/auth/auth.service';
import { CommunityApiService } from './community-api.service';
import { CommunityAuthPromptService } from './community-auth-prompt.service';
import { CommunityStateService } from './community-state.service';
import { PostSummaryComponent } from './post-summary.component';
import type { CommunityUserProfile, PostType, PublicPost, PublicTopic } from './community.types';

/** Matches PostFeedSort backend enum: Hot=0, Newest=1, TopVoted=2 */
type FeedSort = 0 | 1 | 2;

@Component({
  selector: 'cce-community-user-profile-page',
  standalone: true,
  imports: [FormsModule, RouterLink, MatIconModule, MatMenuModule, TranslocoModule, FollowDirective, PostSummaryComponent],
  templateUrl: './community-user-profile.page.html',
  styleUrl: './community-user-profile.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CommunityUserProfilePage implements OnInit {
  private readonly api = inject(CommunityApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly auth = inject(AuthService);
  private readonly authPrompt = inject(CommunityAuthPromptService);
  private readonly communityState = inject(CommunityStateService);
  private readonly localeService = inject(LocaleService);

  // ── Profile ───────────────────────────────────────────────────────────────
  readonly profile = signal<CommunityUserProfile | null>(null);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly isAuthenticated = this.auth.isAuthenticated;
  readonly locale = this.localeService.locale;

  readonly userId = computed(() => this.route.snapshot.paramMap.get('id'));
  readonly currentUserId = computed(() => this.auth.currentUser()?.id ?? null);
  readonly isOwnProfile = computed(() => !!this.currentUserId() && this.currentUserId() === this.userId());

  /** Opens the login/register dialog for an anonymous visitor. */
  promptSignIn(messageKey = 'community.authDialog.messageFollow'): void {
    this.authPrompt.requireAuth(messageKey);
  }

  readonly displayName = computed(() => {
    const p = this.profile();
    if (!p) return '';
    return [p.firstName, p.lastName].filter(Boolean).join(' ');
  });

  readonly initial = computed(() =>
    this.displayName().charAt(0).toUpperCase() || '؟',
  );

  readonly joinedOnFormatted = computed(() => {
    const d = this.profile()?.joinedOn;
    if (!d) return null;
    try {
      const loc = this.locale() === 'ar' ? 'ar-SA' : 'en-US';
      return new Intl.DateTimeFormat(loc, { month: 'long', year: 'numeric' }).format(new Date(d));
    } catch {
      return null;
    }
  });


  // ── Posts feed ────────────────────────────────────────────────────────────
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

  readonly topicsList = computed(() => Array.from(this.topicsMap().values()).slice(0, 20));

  readonly postSkeletons = Array.from({ length: 4 });

  readonly sortOptions: { key: FeedSort; labelKey: string }[] = [
    { key: 1, labelKey: 'community.filter.sortNewest' },
    { key: 2, labelKey: 'community.filter.sortTop' },
    { key: 0, labelKey: 'community.filter.sortMostReplied' },
  ];

  readonly sortIcons: Record<FeedSort, string> = {
    0: 'messages-square',
    1: 'calendar',
    2: 'chevron-up',
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

  readonly isTopicFilterKey = computed(() => !this.topicFilter());

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    const id = this.userId();
    if (!id) return;
    this.loading.set(true);
    this.errorKind.set(null);
    const [profileRes] = await Promise.all([
      this.api.getCommunityUser(id),
      this.loadTopics(),
    ]);
    this.loading.set(false);
    if (profileRes.ok) {
      this.profile.set(profileRes.value);
      void this.loadUserPosts();
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

  async loadUserPosts(): Promise<void> {
    const authorId = this.userId();
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
    void this.loadUserPosts();
  }

  setTopicFilter(topicId: string | null): void {
    this.topicFilter.set(topicId);
    this.currentPage.set(1);
    void this.loadUserPosts();
  }

  setTypeFilter(t: PostType | null): void {
    this.typeFilter.set(t);
    this.currentPage.set(1);
    void this.loadUserPosts();
  }

  setPage(n: number): void {
    this.currentPage.set(n);
    void this.loadUserPosts();
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
      void this.loadUserPosts();
    }, 400);
  }

  clearSearch(): void {
    this.searchQuery.set('');
    this.currentPage.set(1);
    void this.loadUserPosts();
  }

  topicLabel(t: PublicTopic): string {
    return (this.locale() === 'ar' ? t.nameAr ?? t.nameEn : t.nameEn ?? t.nameAr) ?? '';
  }
}
