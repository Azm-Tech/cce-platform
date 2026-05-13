import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { TranslateModule } from '@ngx-translate/core';
import { LocaleService } from '@frontend/i18n';
import type { PublicPost } from './community.types';

/**
 * Post summary card shown inside a topic detail page. Modern + simple,
 * brand greens. Layout:
 *   [ author tile (icon) ]  [ title (1st line of post) + excerpt + meta + CTA ]
 *
 * Tags surfaced on the tile:
 *   - QUESTION (when isAnswerable && !answeredReplyId)
 *   - ANSWERED  (when answeredReplyId)
 *   - DISCUSSION (otherwise)
 *   - locale chip when post locale ≠ active app locale
 */
@Component({
  selector: 'cce-post-summary',
  standalone: true,
  imports: [CommonModule, DatePipe, RouterLink, MatIconModule, TranslateModule],
  template: `
    <a class="cce-post-summary" [routerLink]="['/community', 'posts', post().id]"
       [attr.aria-label]="title()">
      <span class="cce-post-summary__avatar" aria-hidden="true">
        <mat-icon>account_circle</mat-icon>
      </span>

      <div class="cce-post-summary__body">
        <div class="cce-post-summary__tags">
          @if (post().answeredReplyId) {
            <span class="cce-post-summary__tag cce-post-summary__tag--answered">
              <mat-icon aria-hidden="true">check_circle</mat-icon>
              {{ 'community.acceptedAnswer' | translate }}
            </span>
          } @else if (post().isAnswerable) {
            <span class="cce-post-summary__tag cce-post-summary__tag--question">
              <mat-icon aria-hidden="true">help_outline</mat-icon>
              {{ 'community.detail.questionTag' | translate }}
            </span>
          } @else {
            <span class="cce-post-summary__tag cce-post-summary__tag--discussion">
              <mat-icon aria-hidden="true">forum</mat-icon>
              {{ 'community.detail.discussionTag' | translate }}
            </span>
          }
          @if (showLanguageBadge()) {
            <span class="cce-post-summary__tag cce-post-summary__tag--lang">
              {{ 'community.languageBadge' | translate:{ locale: post().locale } }}
            </span>
          }
        </div>

        <h3 class="cce-post-summary__title">{{ title() }}</h3>
        @if (excerpt()) {
          <p class="cce-post-summary__excerpt">{{ excerpt() }}</p>
        }

        <div class="cce-post-summary__foot">
          <span class="cce-post-summary__meta">
            <mat-icon aria-hidden="true">schedule</mat-icon>
            {{ 'community.detail.askedBy' | translate }}
            {{ post().createdOn | date:'mediumDate' }}
          </span>
          <span class="cce-post-summary__cta">
            {{ 'community.openPost' | translate }}
            <mat-icon aria-hidden="true">arrow_forward</mat-icon>
          </span>
        </div>
      </div>
    </a>
  `,
  styleUrl: './post-summary.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PostSummaryComponent {
  private readonly localeService = inject(LocaleService);

  readonly post = input.required<PublicPost>();

  /** First text line of the post — used as a card title. */
  readonly title = computed(() => {
    const stripped = this.post().content.replace(/<[^>]*>/g, '').trim();
    if (!stripped) return '';
    const firstLine = stripped.split(/\n+/)[0];
    return firstLine.length > 100 ? firstLine.slice(0, 100) + '…' : firstLine;
  });

  /** Body excerpt — up to ~200 chars after the first line. */
  readonly excerpt = computed(() => {
    const stripped = this.post().content.replace(/<[^>]*>/g, '').trim();
    if (!stripped) return '';
    const lines = stripped.split(/\n+/);
    const rest = lines.slice(1).join(' ').trim();
    if (!rest) {
      // Fallback: if there's only one line and it's longer than the title clamp,
      // surface the remainder as an excerpt.
      if (stripped.length > 100) {
        const tail = stripped.slice(100, 300);
        return tail.length === 200 ? tail + '…' : tail;
      }
      return '';
    }
    return rest.length > 200 ? rest.slice(0, 200) + '…' : rest;
  });

  /** Show "in {locale}" badge when the post locale differs from the active LocaleService. */
  readonly showLanguageBadge = computed(
    () => this.post().locale !== this.localeService.locale(),
  );
}
