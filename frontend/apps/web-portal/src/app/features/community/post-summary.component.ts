import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { NgClass } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { TranslocoModule } from '@jsverse/transloco';
import { ToastService } from '@frontend/ui-kit';
import { LocaleService } from '@frontend/i18n';
import { AuthService } from '../../core/auth/auth.service';
import { CommunityApiService } from './community-api.service';
import { CommunityAuthPromptService } from './community-auth-prompt.service';
import { SharePostDialogComponent, type SharePostDialogData } from './share-post-dialog.component';
import type { PublicPost } from './community.types';

function timeAgo(dateStr: string | null | undefined, locale: string): string {
  if (!dateStr) return '';
  const ms = Date.now() - new Date(dateStr).getTime();
  const sec = Math.floor(ms / 1000);
  const min = Math.floor(sec / 60);
  const hr = Math.floor(min / 60);
  const day = Math.floor(hr / 24);
  if (locale === 'ar') {
    if (day > 365) return `منذ ${Math.floor(day / 365)} سنة`;
    if (day > 30) return `منذ ${Math.floor(day / 30)} شهر`;
    if (day > 6) return `منذ ${Math.floor(day / 7)} أسبوع`;
    if (day > 1) return `منذ ${day} أيام`;
    if (day === 1) return 'منذ يوم';
    if (hr > 1) return `منذ ${hr} ساعات`;
    if (hr === 1) return 'منذ ساعة';
    if (min > 1) return `منذ ${min} دقائق`;
    if (min === 1) return 'منذ دقيقة';
    return 'الآن';
  }
  if (day > 365) return `${Math.floor(day / 365)}y ago`;
  if (day > 30) return `${Math.floor(day / 30)}mo ago`;
  if (day > 6) return `${Math.floor(day / 7)}w ago`;
  if (day > 0) return `${day}d ago`;
  if (hr > 0) return `${hr}h ago`;
  if (min > 0) return `${min}m ago`;
  return 'now';
}

@Component({
  selector: 'cce-post-summary',
  standalone: true,
  imports: [NgClass, RouterLink, MatIconModule, TranslocoModule],
  template: `
    <article class="pc">

      <!-- ── Header ──────────────────────────────────────────────────── -->
      <div class="pc__header">

        <!-- Author: avatar inline-start, meta fills remainder -->
        <div class="pc__author">
          @if (authorId(); as aid) {
            <a
              class="pc__avatar pc__avatar--link"
              [routerLink]="['/community', 'users', aid]"
              tabindex="-1"
              aria-hidden="true"
            >{{ avatarInitial() }}</a>
          } @else {
            <div class="pc__avatar" aria-hidden="true">{{ avatarInitial() }}</div>
          }
          <div class="pc__author-meta">
            <div class="pc__author-name-row">
              @if (authorId(); as aid) {
                <a class="pc__author-name pc__author-name--link" [routerLink]="['/community', 'users', aid]">
                  {{ post().authorName || ('community.anonymousAuthor' | transloco) }}
                </a>
              } @else {
                <span class="pc__author-name">
                  {{ post().authorName || ('community.anonymousAuthor' | transloco) }}
                </span>
              }
              @if (post().isExpert) {
                <mat-icon svgIcon="badge-check" aria-hidden="true"></mat-icon>
              }
            </div>
            <span class="pc__time">
              {{ time() }}
              <span class="pc__time-sep" aria-hidden="true"> • </span>
              {{ readTime() }}
            </span>
          </div>
        </div>

        <!-- Chips: inline-end -->
        <div class="pc__chips">
          @if (topicDisplay()) {
            <span class="pc__chip pc__chip--topic">{{ topicDisplay() }}</span>
          }
          <span class="pc__chip" [ngClass]="postTypeClass()">
            {{ postTypeLabelKey() | transloco }}
          </span>
        </div>

      </div>

      <!-- ── Body: clickable title + excerpt ─────────────────────────── -->
      <a class="pc__body" [routerLink]="['/community', 'posts', post().id]">
        <h3 class="pc__title" dir="auto">{{ title() }}</h3>
        @if (excerpt()) {
          <p class="pc__excerpt" dir="auto">{{ excerpt() }}</p>
        }
      </a>

      <!-- ── Main image (feed thumbnail) ──────────────────────────────── -->
      @if (post().mainImageUrl; as img) {
        <a class="pc__media" [routerLink]="['/community', 'posts', post().id]">
          <img [src]="img" alt="" loading="lazy" />
        </a>
      }

      <hr class="pc__divider" aria-hidden="true" />

      <!-- ── Footer: vote + replies + share (inline-start) | follow (inline-end) ── -->
      <div class="pc__footer">

        <!-- Action group — inline-start (RIGHT in RTL) -->
        <div class="pc__actions">

          <!-- Vote — dir="ltr" keeps ↓ count ↑ order always -->
          <div class="pc__vote" dir="ltr">
            <button
              type="button"
              class="pc__vote-btn"
              [class.pc__vote-btn--active]="voteStatus() === -1"
              aria-label="downvote"
              (click)="vote(-1)"
            >
              <mat-icon
                svgIcon="{{ voteStatus() === -1 ? 'arrow-big-down-fill' : 'arrow-big-down' }}"
                aria-hidden="true"
              ></mat-icon>
            </button>
            <span class="pc__vote-count pc__vote-count--down">{{ displayDownCount() }}</span>
            <span class="pc__vote-sep" aria-hidden="true"></span>
            <span class="pc__vote-count">{{ displayVoteCount() }}</span>
            <button
              type="button"
              class="pc__vote-btn pc__vote-btn--up"
              [class.pc__vote-btn--active]="voteStatus() === 1"
              aria-label="upvote"
              (click)="vote(1)"
            >
              <mat-icon
                svgIcon="{{ voteStatus() === 1 ? 'arrow-big-up-fill' : 'arrow-big-up' }}"
                aria-hidden="true"
              ></mat-icon>
            </button>
          </div>

          <!-- Replies — count + icon only (no label, matches Figma 74px width) -->
          <a class="pc__action" [routerLink]="['/community', 'posts', post().id]" fragment="replies"
             [attr.aria-label]="('community.comment' | transloco) + ' ' + post().commentsCount">
            <span class="pc__action-count">{{ post().commentsCount }}</span>
            <mat-icon svgIcon="messages-square" aria-hidden="true"></mat-icon>
          </a>

          <!-- Share — icon only (matches Figma 47px width) -->
          <button type="button" class="pc__action" [attr.aria-label]="'community.share' | transloco" (click)="openShare()">
            <mat-icon svgIcon="share-2" aria-hidden="true"></mat-icon>
          </button>

        </div>

        <!-- Follow / bookmark — inline-end (LEFT in RTL) -->
        <button
          type="button"
          class="pc__save-btn"
          [class.pc__save-btn--active]="isAuthenticated() && isFollowed()"
          (click)="toggleFollow()"
        >
          {{ (isAuthenticated() && isFollowed()) ? ('community.followingPost' | transloco) : ('community.followPost' | transloco) }}
          <mat-icon svgIcon="bookmark" aria-hidden="true"></mat-icon>
        </button>

      </div>

    </article>
  `,
  styleUrl: './post-summary.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PostSummaryComponent {
  private readonly localeService = inject(LocaleService);
  private readonly auth = inject(AuthService);
  private readonly communityApi = inject(CommunityApiService);
  private readonly authPrompt = inject(CommunityAuthPromptService);
  private readonly toast = inject(ToastService);
  private readonly dialog = inject(MatDialog);

  readonly post = input.required<PublicPost>();
  readonly topicName = input<string | null>(null);

  readonly isAuthenticated = this.auth.isAuthenticated;
  readonly locale = this.localeService.locale;

  /** Author id for the profile link (null when anonymous → name not linked). */
  readonly authorId = computed(() => this.post().authorId ?? this.post().author?.id ?? null);

  // ── Vote state ────────────────────────────────────────────────────────────
  private readonly _voteStatus = signal<number | null>(null);

  readonly voteStatus = computed(() => this._voteStatus() ?? this.post().voteStatus ?? 0);

  /**
   * Displayed count is the UPVOTE count only (BC001) — downvotes never change
   * it. `post().upvoteCount` from the API includes the user's own upvote, so we
   * strip the original self-upvote to get a stable base, then re-add it based
   * on the user's CURRENT vote. Result: upvoting ±1, downvoting/clearing a
   * downvote = no change.
   */
  readonly displayVoteCount = computed(() => {
    const p = this.post();
    const baseWithoutSelf = (p.upvoteCount ?? 0) - (p.voteStatus === 1 ? 1 : 0);
    return baseWithoutSelf + (this.voteStatus() === 1 ? 1 : 0);
  });

  /** Downvote count (shown beside the downvote icon) — same self-exclusion model
   *  as the upvote count; upvotes never change it and vice-versa. */
  readonly displayDownCount = computed(() => {
    const p = this.post();
    const baseWithoutSelf = (p.downvoteCount ?? 0) - (p.voteStatus === -1 ? 1 : 0);
    return baseWithoutSelf + (this.voteStatus() === -1 ? 1 : 0);
  });

  async vote(dir: 1 | -1): Promise<void> {
    if (!this.authPrompt.requireAuth('community.authDialog.messageVote')) return;
    const current = this.voteStatus();
    const newDir = current === dir ? 0 : dir;
    this._voteStatus.set(newDir);   // optimistic; displayVoteCount derives from it
    const res = await this.communityApi.votePost(this.post().id, newDir);
    if (!res.ok) {
      this._voteStatus.set(current);
    }
  }

  // ── Follow state ──────────────────────────────────────────────────────────
  private readonly _followed = signal<boolean | null>(null);

  readonly isFollowed = computed(() => this._followed() ?? this.post().isWatchlisted);

  async toggleFollow(): Promise<void> {
    if (!this.authPrompt.requireAuth('community.authDialog.messageFollow')) return;
    const current = this.isFollowed();
    this._followed.set(!current);
    const res = current
      ? await this.communityApi.unfollowPost(this.post().id)
      : await this.communityApi.followPost(this.post().id);
    if (!res.ok) {
      this._followed.set(current);
    } else if (!current) {
      this.toast.success('confirmations.CON012');
    }
  }

  // ── Display helpers ───────────────────────────────────────────────────────
  readonly avatarInitial = computed(() => {
    const name = this.post().authorName;
    return name ? name.charAt(0).toUpperCase() : '؟';
  });

  readonly time = computed(() => timeAgo(this.post().createdOn, this.locale()));

  readonly title = computed(() => {
    const t = this.post().title;
    if (t) return t.length > 130 ? t.slice(0, 130) + '…' : t;
    const stripped = (this.post().content ?? '').replace(/<[^>]*>/g, '').trim();
    const line = stripped.split(/\n+/)[0] ?? '';
    return line.length > 130 ? line.slice(0, 130) + '…' : line;
  });

  readonly excerpt = computed(() => {
    if (this.post().title) {
      const stripped = (this.post().content ?? '').replace(/<[^>]*>/g, '').trim();
      if (!stripped) return '';
      return stripped.length > 240 ? stripped.slice(0, 240) + '…' : stripped;
    }
    const stripped = (this.post().content ?? '').replace(/<[^>]*>/g, '').trim();
    const rest = stripped.split(/\n+/).slice(1).join(' ').trim();
    if (!rest) return '';
    return rest.length > 240 ? rest.slice(0, 240) + '…' : rest;
  });


  readonly postTypeLabelKey = computed(() => {
    const map: Record<string, string> = {
      'Info': 'community.postType.informational',
      'Question': 'community.postType.question',
      'Poll': 'community.postType.poll',
    };
    return map[this.post().type] ?? 'community.postType.informational';
  });

  readonly postTypeClass = computed(() => {
    const map: Record<string, string> = {
      'Info': 'pc__chip--info',
      'Question': 'pc__chip--question',
      'Poll': 'pc__chip--poll',
    };
    return map[this.post().type] ?? 'pc__chip--info';
  });

  readonly readTime = computed(() => {
    const text = (this.post().content ?? '').replace(/<[^>]*>/g, '').trim();
    const words = text.split(/\s+/).filter(Boolean).length;
    const mins = Math.max(1, Math.round(words / 200));
    return this.locale() === 'ar' ? `${mins} دقيقة قراءة` : `${mins} min read`;
  });

  readonly topicDisplay = computed(() => {
    if (this.topicName()) return this.topicName();
    return this.locale() === 'ar' ? this.post().topicNameAr : this.post().topicNameEn;
  });

  openShare(): void {
    this.dialog.open<SharePostDialogComponent, SharePostDialogData>(
      SharePostDialogComponent,
      {
        data: { url: `${window.location.origin}/community/posts/${this.post().id}`, title: this.title() },
        width: '480px',
        maxWidth: '95vw',
        autoFocus: false,
        panelClass: 'cce-share-dialog',
      }
    );
  }
}
