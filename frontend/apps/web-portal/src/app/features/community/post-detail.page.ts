import { CommonModule, DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  OnInit,
  ViewChild,
  computed,
  inject,
  signal,
} from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, type PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslocoModule } from '@jsverse/transloco';
import { LocaleService } from '@frontend/i18n';
import { ToastService } from '@frontend/ui-kit';
import { AuthService } from '../../core/auth/auth.service';
import { FollowDirective } from '../follows/follow.directive';
import { CommunityApiService } from './community-api.service';
import { ComposeReplyFormComponent } from './compose-reply-form.component';
import { authorHandle, authorInitial, timeAgo } from './lib/social-helpers';
import { ReplyComponent } from './reply.component';
import { SignInCtaComponent } from './sign-in-cta.component';
import type {
  CommunityUserProfile,
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
  private readonly toast = inject(ToastService);

  readonly post = signal<PublicPost | null>(null);
  readonly replies = signal<PublicPostReply[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly pageSize = signal(20);
  readonly loading = signal(false);
  readonly profileLoading = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly authorProfile = signal<CommunityUserProfile | null>(null);
  readonly topicsMap = signal<Map<string, PublicTopic>>(new Map());
  readonly myVote = signal<VoteDirection>(0);
  readonly voting = signal(false);

  // ── Post follow (seeded from post.isWatchlisted, not FollowsStoreService) ──
  private readonly _postFollowed = signal<boolean | null>(null);
  readonly isPostFollowed = computed(() => this._postFollowed() ?? this.post()?.isWatchlisted ?? false);

  readonly replySkeletons = Array.from({ length: 3 });

  readonly locale = this.localeService.locale;
  readonly isAuthenticated = this.auth.isAuthenticated;
  readonly currentUserId = computed(() => this.auth.currentUser()?.id ?? null);

  readonly notFound = computed(() => this.errorKind() === 'not-found');

  /** Author id resolved from the detail response's nested `author` (preferred)
   *  or the feed's flat `authorId` as a fallback. */
  readonly authorId = computed(() => this.post()?.author?.id ?? this.post()?.authorId ?? null);
  readonly authorIsExpert = computed(
    () => this.post()?.author?.isExpert ?? this.authorProfile()?.isExpert ?? this.post()?.isExpert ?? false,
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
    const p = this.post();
    if (!p) return '';
    if (p.author?.name?.trim()) return p.author.name.trim();
    if (p.authorName?.trim()) return p.authorName.trim();
    const prof = this.authorProfile();
    if (prof?.firstName || prof?.lastName) {
      return [prof.firstName, prof.lastName].filter(Boolean).join(' ');
    }
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

  readonly topLevelReplies = computed(() => {
    const p = this.post();
    const rs = this.replies();
    const tops = rs.filter((r) => !r.parentReplyId);
    if (!p?.answeredReplyId) return tops;
    const accepted = tops.find((r) => r.id === p.answeredReplyId);
    if (!accepted) return tops;
    return [accepted, ...tops.filter((r) => r.id !== accepted.id)];
  });

  readonly childrenByParent = computed(() => {
    const map = new Map<string, PublicPostReply[]>();
    for (const r of this.replies()) {
      if (!r.parentReplyId) continue;
      const list = map.get(r.parentReplyId) ?? [];
      list.push(r);
      map.set(r.parentReplyId, list);
    }
    return map;
  });

  readonly replyingToReplyId = signal<string | null>(null);
  readonly replyingToReply = computed<PublicPostReply | null>(() => {
    const id = this.replyingToReplyId();
    if (!id) return null;
    return this.replies().find((r) => r.id === id) ?? null;
  });
  readonly replyingToHandle = computed<string | null>(() => {
    const r = this.replyingToReply();
    if (!r) return null;
    return authorHandle(r.authorId);
  });

  readonly authorFollowerCount = computed(() => this.post()?.author?.followerCount ?? 0);

  readonly authorJobTitle = computed(() => this.authorProfile()?.jobTitle ?? null);
  readonly authorOrganization = computed(() => this.authorProfile()?.organizationName ?? null);

  childrenOf(id: string): PublicPostReply[] {
    return this.childrenByParent().get(id) ?? [];
  }

  descendantsOf(rootId: string): PublicPostReply[] {
    const map = this.childrenByParent();
    const out: PublicPostReply[] = [];
    const queue: string[] = [rootId];
    while (queue.length > 0) {
      const id = queue.shift()!;
      const kids = map.get(id) ?? [];
      for (const k of kids) {
        out.push(k);
        queue.push(k.id);
      }
    }
    return out;
  }

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
    await this.load(id);
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
    // Load author profile in background — sidebar shows post.author data immediately
    const p = this.post();
    if (!p) return;
    const authorId = p.author?.id ?? p.authorId;
    if (!authorId) return;
    const profileRes = await this.api.getCommunityUser(authorId);
    if (profileRes.ok) this.authorProfile.set(profileRes.value);
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
    this.replyingToReplyId.set(null);
    const p = this.post();
    if (!p) return;
    this.page.set(1);
    const res = await this.api.listReplies(p.id, { page: 1, pageSize: this.pageSize() });
    if (res.ok) {
      this.replies.set(res.value.items);
      this.total.set(Number(res.value.total));
    }
  }

  onReplyToReply(target: PublicPostReply): void {
    this.replyingToReplyId.set(target.id);
  }

  cancelThreadReply(): void {
    this.replyingToReplyId.set(null);
  }

  async onAnswerMarked(): Promise<void> {
    const p = this.post();
    if (!p) return;
    await this.load(p.id);
  }

  async onVote(dir: VoteDirection): Promise<void> {
    if (!this.auth.isAuthenticated()) {
      this.toast.error('community.signInToRate');
      return;
    }
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

  async copyLink(): Promise<void> {
    const url = window.location.href;
    try {
      await navigator.clipboard.writeText(url);
      this.toast.success('community.detail.shareCopiedToast');
    } catch {
      window.prompt('Copy link', url);
    }
  }
}
