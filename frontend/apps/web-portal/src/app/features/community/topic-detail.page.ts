
import { ChangeDetectionStrategy, Component, DestroyRef, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, type PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslocoModule } from '@jsverse/transloco';
import { LocaleService } from '@frontend/i18n';
import {
  RealtimeEvent,
  RealtimeHubService,
  type NewPostPayload,
  type PostModeratedPayload,
} from '@frontend/real-time';
import { AuthService } from '../../core/auth/auth.service';
import { FollowDirective } from '../follows/follow.directive';
import { CommunityApiService } from './community-api.service';
import { ComposePostDialogComponent } from './compose-post-dialog.component';
import { PostSummaryComponent } from './post-summary.component';
import { SignInCtaComponent } from './sign-in-cta.component';
import type { PublicPost, PublicTopic } from './community.types';

@Component({
  selector: 'cce-topic-detail-page',
  standalone: true,
  imports: [
    RouterLink,
    MatButtonModule,
    MatIconModule,
    MatPaginatorModule,
    MatProgressBarModule,
    TranslocoModule,
    FollowDirective,
    PostSummaryComponent,
    SignInCtaComponent
],
  templateUrl: './topic-detail.page.html',
  styleUrl: './topic-detail.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TopicDetailPage implements OnInit, OnDestroy {
  private readonly api = inject(CommunityApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly localeService = inject(LocaleService);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly hub = inject(RealtimeHubService);
  private readonly destroyRef = inject(DestroyRef);
  private subscribedTopicId: string | null = null;
  /** Count of posts published in this topic since the page loaded (drives the pill). */
  readonly newPostCount = signal(0);

  readonly topic = signal<PublicTopic | null>(null);
  readonly posts = signal<PublicPost[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly pageSize = signal(20);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);

  /** Skeleton placeholder array used during the initial load. */
  readonly skeletons = Array.from({ length: 4 });

  readonly locale = this.localeService.locale;
  readonly isAuthenticated = this.auth.isAuthenticated;

  readonly notFound = computed(() => this.errorKind() === 'not-found');

  readonly topicName = computed(() => {
    const t = this.topic();
    if (!t) return '';
    return this.locale() === 'ar' ? t.nameAr : t.nameEn;
  });

  readonly topicDescription = computed(() => {
    const t = this.topic();
    if (!t) return '';
    return this.locale() === 'ar' ? t.descriptionAr : t.descriptionEn;
  });

  async ngOnInit(): Promise<void> {
    const slug = this.route.snapshot.paramMap.get('slug');
    if (!slug) {
      this.errorKind.set('not-found');
      return;
    }
    await this.load(slug);
    const t = this.topic();
    if (t) {
      this.subscribedTopicId = t.id;
      this.listenRealtime(t.id);
      this.hub.subscribeTopic(t.id);
    }
  }

  ngOnDestroy(): void {
    if (this.subscribedTopicId) this.hub.unsubscribeTopic(this.subscribedTopicId);
  }

  /** Live `topic:{id}` events — count new posts, drop moderated ones. */
  private listenRealtime(topicId: string): void {
    this.hub
      .on<NewPostPayload>(RealtimeEvent.NewPost)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((ev) => {
        if (ev.topicId === topicId) this.newPostCount.update((n) => n + 1);
      });

    this.hub
      .on<PostModeratedPayload>(RealtimeEvent.PostModerated)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((ev) => {
        if (ev.replyId) return; // reply deletions don't affect the topic feed
        if (!this.posts().some((p) => p.id === ev.postId)) return;
        this.posts.update((ps) => ps.filter((p) => p.id !== ev.postId));
        this.total.update((t) => Math.max(0, t - 1));
      });
  }

  /** Refresh to surface posts that arrived live since load. */
  showNewPosts(): void {
    const t = this.topic();
    if (!t) return;
    this.newPostCount.set(0);
    this.page.set(1);
    void this.loadPosts(t.id);
  }

  private async load(slug: string): Promise<void> {
    this.loading.set(true);
    this.errorKind.set(null);
    const topicRes = await this.api.getTopicBySlug(slug);
    if (!topicRes.ok) {
      this.loading.set(false);
      this.errorKind.set(topicRes.error.kind);
      return;
    }
    this.topic.set(topicRes.value);
    await this.loadPosts(topicRes.value.id);
    this.loading.set(false);
  }

  private async loadPosts(topicId: string): Promise<void> {
    const res = await this.api.listPosts(topicId, {
      page: this.page(),
      pageSize: this.pageSize(),
    });
    if (res.ok) {
      this.posts.set(res.value.items);
      this.total.set(Number(res.value.total));
    } else {
      this.errorKind.set(res.error.kind);
    }
  }

  async onPage(e: PageEvent): Promise<void> {
    this.page.set(e.pageIndex + 1);
    this.pageSize.set(e.pageSize);
    const t = this.topic();
    if (t) await this.loadPosts(t.id);
  }

  openComposeDialog(): void {
    const t = this.topic();
    if (!t) return;
    const ref = ComposePostDialogComponent.open(this.dialog, {
      topics: [t],
      preselectedTopicId: t.id,
    });
    ref.afterClosed().subscribe((result) => {
      if (result?.submitted) {
        this.page.set(1);
        void this.loadPosts(t.id);
      }
    });
  }

  retry(): void {
    const slug = this.route.snapshot.paramMap.get('slug');
    if (slug) void this.load(slug);
  }
}
