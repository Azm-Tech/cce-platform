import { CommonModule, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, type PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslateModule } from '@ngx-translate/core';
import { LocaleService } from '@frontend/i18n';
import { AuthService } from '../../core/auth/auth.service';
import { FollowDirective } from '../follows/follow.directive';
import { CommunityApiService } from './community-api.service';
import { ComposeReplyFormComponent } from './compose-reply-form.component';
import { RatePostControlComponent } from './rate-post-control.component';
import { ReplyComponent } from './reply.component';
import { SignInCtaComponent } from './sign-in-cta.component';
import type { PublicPost, PublicPostReply } from './community.types';

@Component({
  selector: 'cce-post-detail-page',
  standalone: true,
  imports: [
    CommonModule, DatePipe, RouterLink,
    MatButtonModule, MatIconModule, MatPaginatorModule, MatProgressBarModule,
    TranslateModule,
    FollowDirective,
    ComposeReplyFormComponent,
    RatePostControlComponent,
    ReplyComponent,
    SignInCtaComponent,
  ],
  templateUrl: './post-detail.page.html',
  styleUrl: './post-detail.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PostDetailPage implements OnInit {
  private readonly api = inject(CommunityApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly localeService = inject(LocaleService);
  private readonly auth = inject(AuthService);

  readonly post = signal<PublicPost | null>(null);
  readonly replies = signal<PublicPostReply[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly pageSize = signal(20);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);

  readonly locale = this.localeService.locale;
  readonly isAuthenticated = this.auth.isAuthenticated;
  readonly currentUserId = computed(() => this.auth.currentUser()?.id ?? null);

  readonly notFound = computed(() => this.errorKind() === 'not-found');

  /** True when the active user authored the post — gates the "Mark as answer" button. */
  readonly canMarkAnswer = computed(() => {
    const p = this.post();
    if (!p || !p.isAnswerable) return false;
    return this.currentUserId() === p.authorId;
  });

  /** Show "in {locale}" badge when post locale differs from active LocaleService. */
  readonly postShowLangBadge = computed(() => {
    const p = this.post();
    return !!p && p.locale !== this.localeService.locale();
  });

  /** Replies hoisted: accepted answer first (when present), rest in original order. */
  readonly orderedReplies = computed(() => {
    const p = this.post();
    const rs = this.replies();
    if (!p?.answeredReplyId) return rs;
    const accepted = rs.find((r) => r.id === p.answeredReplyId);
    if (!accepted) return rs;
    const rest = rs.filter((r) => r.id !== accepted.id);
    return [accepted, ...rest];
  });

  async ngOnInit(): Promise<void> {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.errorKind.set('not-found');
      return;
    }
    await this.load(id);
  }

  private async load(id: string): Promise<void> {
    this.loading.set(true);
    this.errorKind.set(null);
    const [postRes, repliesRes] = await Promise.all([
      this.api.getPost(id),
      this.api.listReplies(id, { page: this.page(), pageSize: this.pageSize() }),
    ]);
    this.loading.set(false);
    if (postRes.ok) this.post.set(postRes.value);
    else this.errorKind.set(postRes.error.kind);
    if (repliesRes.ok) {
      this.replies.set(repliesRes.value.items);
      this.total.set(Number(repliesRes.value.total));
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

  /** Refresh on new reply (called by ComposeReplyForm output). */
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

  /** Refresh both post + replies after a "Mark as answer" succeeds. */
  async onAnswerMarked(): Promise<void> {
    const p = this.post();
    if (!p) return;
    await this.load(p.id);
  }

  retry(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) void this.load(id);
  }
}
