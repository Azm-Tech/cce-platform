import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { TranslateModule } from '@ngx-translate/core';
import type { NewsArticle } from './news.types';

@Component({
  selector: 'cce-news-card',
  standalone: true,
  imports: [CommonModule, DatePipe, RouterLink, MatCardModule, TranslateModule],
  template: `
    <a class="cce-news-card" [routerLink]="['/news', article().slug]">
      <mat-card>
        @if (article().featuredImageUrl) {
          <img mat-card-image [src]="article().featuredImageUrl" [alt]="title()" class="cce-news-card__image" />
        }
        <mat-card-header>
          <mat-card-title>{{ title() }}</mat-card-title>
          @if (article().publishedOn) {
            <mat-card-subtitle>
              {{ article().publishedOn | date:'mediumDate' }}
            </mat-card-subtitle>
          }
        </mat-card-header>
        <mat-card-content>
          <p class="cce-news-card__excerpt">{{ excerpt() }}</p>
        </mat-card-content>
      </mat-card>
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
    return stripped.length > 160 ? stripped.slice(0, 160) + '…' : stripped;
  });
}
