import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { ShareMenuComponent } from '../../shared/share-menu/share-menu.component';
import type { NewsArticle } from './news.types';

/**
 * Public news article card — vertical layout matching the unified News &
 * Events design:
 *   [ image header with type-badge overlay ]
 *   [ meta row: published date + read-time ]
 *   [ title ]
 *   [ excerpt ]
 *   [ footer: share menu + "نُشر بواسطة: …" attribution ]
 *
 * The image and title are individually wrapped in `<a>` for navigation
 * so the share button in the footer doesn't trigger card navigation.
 */
@Component({
  selector: 'cce-news-card',
  standalone: true,
  imports: [CommonModule, DatePipe, RouterLink, MatIconModule, TranslocoModule, ShareMenuComponent],
  template: `
    <article class="cce-news-card">
      <a class="cce-news-card__media"
         [routerLink]="['/news', article().id]"
         [attr.aria-label]="title()">
        @if (article().featuredImageUrl; as src) {
          <img [src]="src" [alt]="title()" loading="lazy" referrerpolicy="no-referrer" />
        } @else {
          <span class="cce-news-card__media-icon" aria-hidden="true">
            <mat-icon>image</mat-icon>
          </span>
        }
        <span class="cce-news-card__badge"
              [class.cce-news-card__badge--featured]="article().isFeatured">
          <mat-icon aria-hidden="true">article</mat-icon>
          {{ (article().isFeatured ? 'news.featured.tag' : 'news.tag') | transloco }}
        </span>
      </a>

      <div class="cce-news-card__content">
        <div class="cce-news-card__meta">
          @if (article().publishedOn) {
            <span class="cce-news-card__meta-item">
              <mat-icon aria-hidden="true">calendar_today</mat-icon>
              {{ article().publishedOn | date:'longDate' }}
            </span>
            <span class="cce-news-card__meta-sep" aria-hidden="true">•</span>
          }
          <span class="cce-news-card__meta-item">{{ readingTimeLabel() }}</span>
        </div>

        <a class="cce-news-card__title-link"
           [routerLink]="['/news', article().id]"
           [attr.aria-label]="title()">
          <h3 class="cce-news-card__title">{{ title() }}</h3>
        </a>

        @if (topicLabel(); as topic) {
          <span class="cce-news-card__topic">{{ topic }}</span>
        }
        @if (excerpt()) {
          <p class="cce-news-card__excerpt">{{ excerpt() }}</p>
        }
      </div>

      <footer class="cce-news-card__foot">
        <cce-share-menu [title]="title()" [url]="absoluteUrl()" />
        @if (publisher()) {
          <span class="cce-news-card__author">
            {{ 'news.publishedBy' | transloco }}: {{ publisher() }}
          </span>
        }
      </footer>
    </article>
  `,
  styleUrl: './news-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NewsCardComponent {
  private readonly router = inject(Router);
  private readonly transloco = inject(TranslocoService);
  readonly article = input.required<NewsArticle>();
  readonly locale = input<'ar' | 'en'>('en');

  readonly title = computed(() => {
    const a = this.article();
    return this.locale() === 'ar' ? a.titleAr : a.titleEn;
  });

  readonly excerpt = computed(() => {
    const a = this.article();
    const content = this.locale() === 'ar' ? a.contentAr : a.contentEn;
    const stripped = (content ?? '').replace(/<[^>]*>/g, '').trim();
    return stripped.length > 160 ? stripped.slice(0, 160) + '…' : stripped;
  });

  readonly publisher = computed<string | null>(() => {
    const a = this.article() as NewsArticle & { authorName?: string; publishedBy?: string };
    return a.authorName ?? a.publishedBy ?? null;
  });

  readonly topicLabel = computed<string | null>(() => {
    const a = this.article();
    const label = this.locale() === 'ar' ? a.topicNameAr : a.topicNameEn;
    return label ?? null;
  });

  /** Approximate read time based on word count, ~200 wpm, min 1 min. */
  readonly readingTimeLabel = computed(() => {
    this.locale(); // reactive dependency for language switch
    const a = this.article();
    const content = (this.locale() === 'ar' ? a.contentAr : a.contentEn) ?? '';
    const words = content.replace(/<[^>]*>/g, '').trim().split(/\s+/).filter(Boolean).length;
    const minutes = Math.max(1, Math.round(words / 200));
    return `${minutes} ${this.transloco.translate('news.detail.minRead')}`;
  });

  readonly absoluteUrl = computed<string | null>(() => {
    if (typeof window === 'undefined') return null;
    const tree = this.router.createUrlTree(['/news', this.article().id]);
    return new URL(tree.toString(), window.location.origin).toString();
  });
}
