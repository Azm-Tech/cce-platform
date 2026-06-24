import { CommonModule, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatMenuModule } from '@angular/material/menu';
import { MatPaginatorModule, type PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { TranslocoModule } from '@jsverse/transloco';
import { LocaleService } from '@frontend/i18n';
import { ConfirmDialogService, ToastService } from '@frontend/ui-kit';
import { RealtimeEvent, RealtimeHubService, type ContentModeratedPayload } from '@frontend/real-time';
import { AuthService } from '../../core/auth/auth.service';
import { EnvService } from '../../core/env.service';
import { CommunityModerationApiService, type TopicLite } from './community-moderation-api.service';
import {
  PostDetailDialogComponent,
  type PostDetailDialogData,
  type PostDetailDialogResult,
} from './post-detail-dialog.component';
import {
  ADMIN_POST_TYPE_FILTERS,
  POST_TYPE_PARAM,
  type AdminPostRow,
  type AdminPostTypeFilter,
  type PostTypeKind,
} from './admin-post.types';

const GUID_RE = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

/**
 * Admin → Community moderation list.
 *
 * Lists every community post (active + soft-deleted) with filters for
 * topic, locale, status, and free-text search over `content`. Each row
 * shows status chips (QUESTION / ANSWERED / DELETED), reply count, and
 * action buttons for soft-deleting the post. A quick-action panel at
 * the top lets moderators paste a reply id to soft-delete it directly.
 *
 * Requires `Community.Post.Moderate` permission (gated at the route).
 */
@Component({
  selector: 'cce-community-moderation',
  standalone: true,
  imports: [
    CommonModule, DatePipe, FormsModule,
    MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule,
    MatMenuModule, MatPaginatorModule, MatProgressBarModule, MatSelectModule,
    MatTooltipModule,
    TranslocoModule,
  ],
  templateUrl: './community-moderation.page.html',
  styleUrl: './community-moderation.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CommunityModerationPage implements OnInit {
  private readonly api = inject(CommunityModerationApiService);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmDialogService);
  private readonly localeService = inject(LocaleService);
  private readonly envService = inject(EnvService);
  private readonly dialog = inject(MatDialog);
  private readonly hub = inject(RealtimeHubService);
  private readonly auth = inject(AuthService);
  private readonly destroyRef = inject(DestroyRef);

  readonly typeFilters = ADMIN_POST_TYPE_FILTERS;
  readonly locale = this.localeService.locale;

  // ─── List state ──────────────────────────────────────────
  readonly rows = signal<AdminPostRow[]>([]);
  readonly total = signal(0);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly topics = signal<TopicLite[]>([]);

  // ─── Filters ─────────────────────────────────────────────
  readonly search = signal('');
  readonly typeFilter = signal<AdminPostTypeFilter>('all');
  readonly localeFilter = signal<'' | 'ar' | 'en'>('');
  readonly topicId = signal<string>('');
  readonly page = signal(1);
  readonly pageSize = signal(20);

  // ─── Quick-action panel (delete reply by id) ─────────────
  readonly quickReplyId = signal<string>('');
  readonly quickReplyError = signal<boolean>(false);
  readonly busy = signal(false);

  readonly empty = computed(
    () => !this.loading() && this.rows().length === 0 && !this.errorKind(),
  );

  async ngOnInit(): Promise<void> {
    // Load topics for the filter dropdown (best-effort — silent on failure).
    void this.api.listTopicsLite().then((res) => {
      if (res.ok) this.topics.set(res.value);
    });
    this.listenForModeration();
    await this.load();
  }

  /** Live moderation channel: another moderator acted → toast + refresh the list.
   *  (The hub auto-joins the `moderation` room for users with the moderator claim.) */
  private listenForModeration(): void {
    this.hub
      .on<ContentModeratedPayload>(RealtimeEvent.ContentModerated)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((ev) => {
        // Skip our own action — the local delete flow already toasted + reloaded.
        if (ev.moderatorId && ev.moderatorId === this.auth.currentUser()?.id) return;
        this.toast.success('communityModeration.toastModerated');
        void this.load();
      });
  }

  topicNameOf(row: AdminPostRow): string {
    return this.locale() === 'ar' ? row.topicNameAr : row.topicNameEn;
  }

  /** Post title for the table cell, falling back to a content excerpt
   *  when the row carries no title (e.g. plain discussion posts). */
  titleOf(row: AdminPostRow): string {
    const title = row.title?.trim();
    if (title) return title;
    return this.excerptOf(row);
  }

  /** First 140 chars of `content` — used as a fallback title and the row tooltip. */
  excerptOf(row: AdminPostRow): string {
    const stripped = (row.content ?? '').replace(/\s+/g, ' ').trim();
    return stripped.length > 140 ? stripped.slice(0, 140) + '…' : stripped;
  }

  /** Normalized post type for chip rendering — derived from `type`,
   *  falling back to the answerable heuristic when the field is absent. */
  postTypeKind(row: AdminPostRow): PostTypeKind {
    const t = (row.type ?? '').toLowerCase();
    if (t.includes('poll')) return 'poll';
    if (t.includes('quest') || (!t && (row.isAnswerable || row.isAnswered))) return 'question';
    return 'info';
  }

  postTypeIcon(kind: PostTypeKind): string {
    switch (kind) {
      case 'question': return 'help_outline';
      case 'poll': return 'bar_chart';
      default: return 'info';
    }
  }

  /**
   * Build the deep-link URL to the post on the public web-portal.
   * Returns `null` when `webPortalUrl` is not configured (e.g. an
   * env where the public site isn't reachable from the admin host).
   * The template hides the "Open in portal" affordance when this
   * returns null so we don't dangle a broken link.
   */
  publicPostUrl(row: AdminPostRow): string | null {
    const base = this.envService.env.webPortalUrl;
    if (!base) return null;
    // Strip a trailing slash if the operator added one — be lenient.
    const origin = base.replace(/\/+$/, '');
    return `${origin}/community/posts/${row.id}`;
  }

  /** Open the public post in a new tab. Uses `noopener,noreferrer`
   *  so the popup can't navigate the admin tab back. Soft-deleted
   *  posts open too — admins may need to inspect the public-facing
   *  tombstone the user portal renders. */
  openInPortal(row: AdminPostRow, event?: MouseEvent): void {
    if (event) {
      event.preventDefault();
      event.stopPropagation();
    }
    const url = this.publicPostUrl(row);
    if (!url) return;
    window.open(url, '_blank', 'noopener,noreferrer');
  }

  /** Open the public post's replies section directly. Uses the
   *  `#replies` anchor that the user-portal's post-detail page
   *  exposes on its replies `<section>`. */
  openReplies(row: AdminPostRow): void {
    const url = this.publicPostUrl(row);
    if (!url) return;
    window.open(`${url}#replies`, '_blank', 'noopener,noreferrer');
  }

  /** Copy the raw post id (the GUID) to the clipboard. Useful for
   *  pasting into the by-ID quick-action panel or into a support
   *  ticket. */
  async copyId(row: AdminPostRow): Promise<void> {
    await this.copyToClipboard(row.id, 'communityModeration.action.copyIdToast');
  }

  /** Copy the public deep-link to the post (when configured). */
  async copyLink(row: AdminPostRow): Promise<void> {
    const url = this.publicPostUrl(row);
    if (!url) return;
    await this.copyToClipboard(url, 'communityModeration.action.copyLinkToast');
  }

  private async copyToClipboard(text: string, successKey: string): Promise<void> {
    try {
      await navigator.clipboard.writeText(text);
      this.toast.success(successKey);
    } catch {
      // Clipboard API blocked (insecure context or permissions denied).
      // Fall back to the prompt so the operator can copy manually.
      window.prompt('Copy', text);
    }
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.errorKind.set(null);
    const type = this.typeFilter();
    const res = await this.api.listPosts({
      page: this.page(),
      pageSize: this.pageSize(),
      topicId: this.topicId() || undefined,
      search: this.search().trim() || undefined,
      postType: type === 'all' ? undefined : POST_TYPE_PARAM[type],
      locale: this.localeFilter() || undefined,
    });
    this.loading.set(false);
    if (res.ok) {
      this.rows.set(res.value.items);
      this.total.set(res.value.total);
    } else {
      this.errorKind.set(res.error.kind);
    }
  }

  // ─── Filter handlers ────────────────────────────────────
  onSearch(value: string): void {
    this.search.set(value);
    this.page.set(1);
    void this.load();
  }
  onType(value: AdminPostTypeFilter): void {
    this.typeFilter.set(value);
    this.page.set(1);
    void this.load();
  }
  onLocale(value: '' | 'ar' | 'en'): void {
    this.localeFilter.set(value);
    this.page.set(1);
    void this.load();
  }
  onTopic(value: string): void {
    this.topicId.set(value);
    this.page.set(1);
    void this.load();
  }
  onPage(e: PageEvent): void {
    this.page.set(e.pageIndex + 1);
    this.pageSize.set(e.pageSize);
    void this.load();
  }
  clearFilters(): void {
    this.search.set('');
    this.typeFilter.set('all');
    this.localeFilter.set('');
    this.topicId.set('');
    this.page.set(1);
    void this.load();
  }

  // ─── Row actions ────────────────────────────────────────
  async deletePost(row: AdminPostRow): Promise<void> {
    if (row.isDeleted) return;
    if (!(await this.confirm.confirm({
      titleKey: 'communityModeration.post.title',
      messageKey: 'communityModeration.post.message',
      confirmKey: 'communityModeration.post.confirm',
      cancelKey: 'common.actions.cancel',
    }))) return;
    this.busy.set(true);
    const res = await this.api.softDeletePost(row.id);
    this.busy.set(false);
    if (res.ok) {
      this.toast.success('communityModeration.post.toast');
      await this.load();
    } else {
      this.toast.error(`errors.${res.error.kind}`);
    }
  }

  // ─── Quick reply-delete panel ───────────────────────────
  async deleteReplyById(): Promise<void> {
    const id = this.quickReplyId().trim();
    if (!GUID_RE.test(id)) {
      this.quickReplyError.set(true);
      return;
    }
    this.quickReplyError.set(false);
    if (!(await this.confirm.confirm({
      titleKey: 'communityModeration.reply.title',
      messageKey: 'communityModeration.reply.message',
      confirmKey: 'communityModeration.reply.confirm',
      cancelKey: 'common.actions.cancel',
    }))) return;
    this.busy.set(true);
    const res = await this.api.softDeleteReply(id);
    this.busy.set(false);
    if (res.ok) {
      this.toast.success('communityModeration.reply.toast');
      this.quickReplyId.set('');
    } else {
      this.toast.error(`errors.${res.error.kind}`);
    }
  }

  openDetail(row: AdminPostRow): void {
    const ref = this.dialog.open<
      PostDetailDialogComponent,
      PostDetailDialogData,
      PostDetailDialogResult
    >(PostDetailDialogComponent, {
      data: { row },
      width: '720px',
      maxWidth: '96vw',
      maxHeight: '90vh',
      autoFocus: false,
      panelClass: 'cce-post-detail-dialog',
    });
    ref.afterClosed().subscribe((result) => {
      if (result?.postDeleted) void this.load();
    });
  }

  retry(): void { void this.load(); }
}
