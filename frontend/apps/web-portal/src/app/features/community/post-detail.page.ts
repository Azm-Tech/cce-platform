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
import { TranslateModule } from '@ngx-translate/core';
import { LocaleService } from '@frontend/i18n';
import { ToastService } from '@frontend/ui-kit';
import { AuthService } from '../../core/auth/auth.service';
import { FollowDirective } from '../follows/follow.directive';
import { CommunityApiService } from './community-api.service';
import { ComposeReplyFormComponent } from './compose-reply-form.component';
import { authorHandle, authorInitial, timeAgo } from './lib/social-helpers';
import { LikeDislikeControlComponent } from './like-dislike-control.component';
import { ReplyComponent } from './reply.component';
import { SignInCtaComponent } from './sign-in-cta.component';
import type { PublicPost, PublicPostReply } from './community.types';

/**
 * Public post detail page rendered as a social-feed thread (FB / IG / X
 * style). The post is the OP card at the top with an avatar header,
 * status chip, time-ago, body, and a Like / Reply / Save / Share action
 * bar. Replies render as a threaded feed below an inline composer.
 */
@Component({
  selector: 'cce-post-detail-page',
  standalone: true,
  imports: [
    CommonModule, DatePipe, RouterLink,
    MatButtonModule, MatIconModule, MatPaginatorModule, MatProgressBarModule,
    TranslateModule,
    FollowDirective,
    ComposeReplyFormComponent,
    LikeDislikeControlComponent,
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
  readonly errorKind = signal<string | null>(null);

  /** Skeleton row count rendered before the post arrives. */
  readonly replySkeletons = Array.from({ length: 3 });

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

  /** Top-level replies (parentReplyId === null). The accepted answer
   *  is hoisted to the top; everything else keeps its arrival order. */
  readonly topLevelReplies = computed(() => {
    const p = this.post();
    const rs = this.replies();
    const tops = rs.filter((r) => !r.parentReplyId);
    if (!p?.answeredReplyId) return tops;
    const accepted = tops.find((r) => r.id === p.answeredReplyId);
    if (!accepted) return tops;
    return [accepted, ...tops.filter((r) => r.id !== accepted.id)];
  });

  /** Index from parentReplyId → its direct children, used by the tree
   *  template to render nested threads. */
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

  /** Currently-being-replied-to reply id (drives the inline thread
   *  composer + the highlight ring on the targeted bubble). */
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

  /** Helper used by the template to fetch direct children for a
   *  given reply id. Returns an empty array when the reply has none. */
  childrenOf(id: string): PublicPostReply[] {
    return this.childrenByParent().get(id) ?? [];
  }

  /** Flat BFS of every descendant of `rootId`. Used to render a top-
   *  level reply's whole sub-thread as a single indented list (Twitter-
   *  style flat thread). Replies are ordered breadth-first so direct
   *  children come first. */
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

  /** Anchor to the inline composer for the "Reply" action button. */
  @ViewChild('composer') composer?: ElementRef<HTMLElement>;

  // ─── Social helpers (locale-aware) ───────────────────────
  timeAgo(iso: string): string {
    return timeAgo(iso, this.locale());
  }
  authorHandle(id: string): string {
    return authorHandle(id);
  }
  authorInitial(id: string): string {
    return authorInitial(id);
  }

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

  /** Refresh on new reply (called by ComposeReplyForm output).
   *  Also clears the threaded-reply target so the inline composer
   *  collapses back into the page-level composer. */
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

  /** A child reply emits this when its "Reply" button is clicked. */
  onReplyToReply(target: PublicPostReply): void {
    this.replyingToReplyId.set(target.id);
  }

  /** Cancel the inline thread composer and fall back to the page
   *  composer at the bottom. */
  cancelThreadReply(): void {
    this.replyingToReplyId.set(null);
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

  /** "Reply" action button — scrolls to and focuses the inline composer. */
  focusComposer(): void {
    const el = this.composer?.nativeElement;
    if (!el) return;
    el.scrollIntoView({ behavior: 'smooth', block: 'center' });
    const focusable = el.querySelector<HTMLElement>(
      'textarea, input, [tabindex]:not([tabindex="-1"])',
    );
    if (focusable) {
      // Wait a frame so the smooth-scroll doesn't steal focus.
      setTimeout(() => focusable.focus(), 220);
    }
  }

  /** "Share" action — copies the canonical post URL to the clipboard. */
  async copyLink(): Promise<void> {
    const url = window.location.href;
    try {
      await navigator.clipboard.writeText(url);
      this.toast.success('community.detail.shareCopiedToast');
    } catch {
      // Clipboard API not available (insecure context or denied permission)
      // — fall back to a manual prompt-based copy.
      window.prompt('Copy link', url);
    }
  }
}
