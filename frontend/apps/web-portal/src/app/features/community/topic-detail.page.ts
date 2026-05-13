import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, type PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslateModule } from '@ngx-translate/core';
import { LocaleService } from '@frontend/i18n';
import { AuthService } from '../../core/auth/auth.service';
import { FollowDirective } from '../follows/follow.directive';
import { CommunityApiService } from './community-api.service';
import {
  ComposePostDialogComponent,
  type ComposePostDialogData,
  type ComposePostDialogResult,
} from './compose-post-dialog.component';
import { PostSummaryComponent } from './post-summary.component';
import { SignInCtaComponent } from './sign-in-cta.component';
import type { PublicPost, PublicTopic } from './community.types';

@Component({
  selector: 'cce-topic-detail-page',
  standalone: true,
  imports: [
    CommonModule, RouterLink,
    MatButtonModule, MatIconModule, MatPaginatorModule, MatProgressBarModule,
    TranslateModule,
    FollowDirective, PostSummaryComponent, SignInCtaComponent,
  ],
  templateUrl: './topic-detail.page.html',
  styleUrl: './topic-detail.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TopicDetailPage implements OnInit {
  private readonly api = inject(CommunityApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly localeService = inject(LocaleService);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);

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
    const ref = this.dialog.open<
      ComposePostDialogComponent,
      ComposePostDialogData,
      ComposePostDialogResult
    >(ComposePostDialogComponent, {
      data: { topicId: t.id },
      autoFocus: 'first-tabbable',
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
