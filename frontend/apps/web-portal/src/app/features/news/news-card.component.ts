import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { TranslateModule } from '@ngx-translate/core';
import type { NewsArticle } from './news.types';

/**
 * Public news article card. Modern + simple horizontal layout:
 * 75% content (left in LTR, right in RTL) and 25% media (right in
 * LTR, left in RTL). Image fallback uses a brand gradient with the
 * `newspaper` icon. FEATURED articles show a gold pill on the media.
 */
@Component({
  selector: 'cce-news-card',
  standalone: true,
  imports: [CommonModule, DatePipe, RouterLink, MatIconModule, TranslateModule],
  template: `
    <a class="cce-news-card" [routerLink]="['/news', article().slug]"
       [attr.aria-label]="title()">
      <!-- Media on the leading edge (left in LTR, right in RTL). -->
      <div class="cce-news-card__media"
           [class.cce-news-card__media--placeholder]="!article().featuredImageUrl">
        @if (article().featuredImageUrl; as src) {
          <img [src]="src" [alt]="title()" loading="lazy" referrerpolicy="no-referrer" />
        } @else {
          <span class="cce-news-card__media-icon" aria-hidden="true">
            <mat-icon>newspaper</mat-icon>
          </span>
        }
      </div>

      <div class="cce-news-card__content">
        <span class="cce-news-card__eyebrow"
              [class.cce-news-card__eyebrow--featured]="article().isFeatured">
          @if (article().isFeatured) {
            <mat-icon aria-hidden="true">star</mat-icon>
            {{ 'news.featured.tag' | translate }}
          } @else {
            {{ 'news.tag' | translate }}
          }
        </span>

        <h3 class="cce-news-card__title">{{ title() }}</h3>
        <p class="cce-news-card__excerpt">{{ excerpt() }}</p>

        <div class="cce-news-card__foot">
          <span class="cce-news-card__meta">
            @if (article().publishedOn) {
              <mat-icon aria-hidden="true">schedule</mat-icon>
              {{ article().publishedOn | date:'mediumDate' }}
            } @else {
              <mat-icon aria-hidden="true">edit_note</mat-icon>
              {{ 'news.draft' | translate }}
            }
          </span>
          <span class="cce-news-card__cta">
            {{ 'news.readArticle' | translate }}
            <mat-icon aria-hidden="true">arrow_forward</mat-icon>
          </span>
        </div>
      </div>
    </a>
  `,
  styleUrl: './news-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NewsCardComponent {
  readonly article = input.required<NewsArticle>();
  readonly locale = input<'ar' | 'en'>('en');

  readonly title = computed(() => {
    const a = this.article();
    return this.locale() === 'ar' ? a.titleAr : a.titleEn;
  });

  readonly excerpt = computed(() => {
    const a = this.article();
    const content = this.locale() === 'ar' ? a.contentAr : a.contentEn;
    const stripped = content.replace(/<[^>]*>/g, '').trim();
    return stripped.length > 180 ? stripped.slice(0, 180) + '…' : stripped;
  });
}
