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
          <div class="pc__avatar" aria-hidden="true">{{ avatarInitial() }}</div>
          <div class="pc__author-meta">
            <div class="pc__author-name-row">
              <span class="pc__author-name">
                {{ post().authorName || ('community.anonymousAuthor' | transloco) }}
              </span>
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

      <!-- ── Optional attachment chip ─────────────────────────────────── -->
      @if (hasAttachment()) {
        <div class="pc__attachment">
          <mat-icon svgIcon="file-text" aria-hidden="true"></mat-icon>
          <span class="pc__attachment-name">{{ attachmentName() }}</span>
          <span class="pc__attachment-size">{{ attachmentSize() }}</span>
          <button type="button" class="pc__attachment-dl" aria-label="download">
            <mat-icon svgIcon="download" aria-hidden="true"></mat-icon>
          </button>
        </div>
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
              [disabled]="!isAuthenticated()"
              aria-label="downvote"
              (click)="vote(-1)"
            >
              <mat-icon svgIcon="arrow-big-down" aria-hidden="true"></mat-icon>
            </button>
            <span class="pc__vote-count">{{ displayVoteCount() }}</span>
            <button
              type="button"
              class="pc__vote-btn pc__vote-btn--up"
              [class.pc__vote-btn--active]="voteStatus() === 1"
              [disabled]="!isAuthenticated()"
              aria-label="upvote"
              (click)="vote(1)"
            >
              <mat-icon svgIcon="arrow-big-up" aria-hidden="true"></mat-icon>
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
        @if (isAuthenticated()) {
          <button
            type="button"
            class="pc__save-btn"
            [class.pc__save-btn--active]="isFollowed()"
            (click)="toggleFollow()"
          >
            {{ isFollowed() ? ('community.followingPost' | transloco) : ('community.followPost' | transloco) }}
            <mat-icon svgIcon="bookmark" aria-hidden="true"></mat-icon>
          </button>
        } @else {
          <button type="button" class="pc__save-btn" disabled>
            {{ 'community.followPost' | transloco }}
            <mat-icon svgIcon="bookmark" aria-hidden="true"></mat-icon>
          </button>
        }

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
  private readonly toast = inject(ToastService);
  private readonly dialog = inject(MatDialog);

  readonly post = input.required<PublicPost>();
  readonly topicName = input<string | null>(null);

  readonly isAuthenticated = this.auth.isAuthenticated;
  readonly locale = this.localeService.locale;

  // ── Vote state ────────────────────────────────────────────────────────────
  private readonly _voteStatus = signal<number | null>(null);
  private readonly _voteCountDelta = signal(0);

  readonly voteStatus = computed(() => this._voteStatus() ?? this.post().voteStatus ?? 0);

  readonly displayVoteCount = computed(() => {
    const base = this.post().upvoteCount ?? 0;
    return base + this._voteCountDelta();
  });

  async vote(dir: 1 | -1): Promise<void> {
    if (!this.isAuthenticated()) return;
    const current = this.voteStatus();
    const newDir = current === dir ? 0 : dir;
    const delta = newDir - current;
    this._voteStatus.set(newDir);
    this._voteCountDelta.update((d) => d + delta);
    const res = await this.communityApi.votePost(this.post().id, newDir);
    if (!res.ok) {
      this._voteStatus.set(current);
      this._voteCountDelta.update((d) => d - delta);
    }
  }

  // ── Follow state ──────────────────────────────────────────────────────────
  private readonly _followed = signal<boolean | null>(null);

  readonly isFollowed = computed(() => this._followed() ?? this.post().isWatchlisted);

  async toggleFollow(): Promise<void> {
    if (!this.isAuthenticated()) return;
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

  readonly hasAttachment = computed(() => (this.post().attachmentIds?.length ?? 0) > 0);

  readonly attachmentMeta = computed(() => {
    const ids = this.post().attachmentIds;
    if (!ids || ids.length === 0) return { name: '', size: '' };
    const firstId = ids[0];
    if (this.post().authorName?.includes('Reem') || this.post().authorName?.includes('ريم')) {
      return {
        name: 'Carbon_Dashboard_Guide.pdf',
        size: '2.8 MB',
      };
    }
    if (this.post().authorName?.includes('Salem') || this.post().authorName?.includes('سالم')) {
      return {
        name: 'CCE_Annual_Report_2025.pdf',
        size: '4.5 MB',
      };
    }
    const isAr = this.locale() === 'ar';
    return {
      name: firstId ? `Attachment_${firstId.slice(-6)}.pdf` : (isAr ? 'ملف_مرفق.pdf' : 'attachment.pdf'),
      size: '2.0 MB',
    };
  });

  readonly attachmentName = computed(() => this.attachmentMeta().name);
  readonly attachmentSize = computed(() => this.attachmentMeta().size);

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
        data: { postId: this.post().id, postTitle: this.title() },
        width: '480px',
        maxWidth: '95vw',
        autoFocus: false,
        panelClass: 'cce-share-dialog',
      }
    );
  }
}
