import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { TranslateModule } from '@ngx-translate/core';
import { LocaleService } from '@frontend/i18n';
import type { PublicPost } from './community.types';

@Component({
  selector: 'cce-post-summary',
  standalone: true,
  imports: [CommonModule, DatePipe, RouterLink, MatCardModule, MatIconModule, TranslateModule],
  template: `
    <a class="cce-post-summary" [routerLink]="['/community', 'posts', post().id]">
      <mat-card>
        <mat-card-content>
          <div class="cce-post-summary__row">
            <p class="cce-post-summary__excerpt">{{ excerpt() }}</p>
            <div class="cce-post-summary__meta">
              @if (post().answeredReplyId) {
                <span class="cce-post-summary__answered">
                  <mat-icon aria-hidden="true">check_circle</mat-icon>
                  {{ 'community.acceptedAnswer' | translate }}
                </span>
              }
              @if (showLanguageBadge()) {
                <span class="cce-post-summary__lang">
                  {{ 'community.languageBadge' | translate:{ locale: post().locale } }}
                </span>
              }
              <small class="cce-post-summary__date">
                {{ post().createdOn | date:'mediumDate' }}
              </small>
            </div>
          </div>
        </mat-card-content>
      </mat-card>
    </a>
  `,
  styleUrl: './post-summary.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PostSummaryComponent {
  private readonly localeService = inject(LocaleService);

  readonly post = input.required<PublicPost>();

  readonly excerpt = computed(() => {
    const raw = this.post().content;
    const stripped = raw.replace(/<[^>]*>/g, '').trim();
    return stripped.length > 160 ? stripped.slice(0, 160) + '…' : stripped;
  });

  /** Show "in {locale}" badge when the post locale differs from the active LocaleService. */
  readonly showLanguageBadge = computed(
    () => this.post().locale !== this.localeService.locale(),
  );
}
