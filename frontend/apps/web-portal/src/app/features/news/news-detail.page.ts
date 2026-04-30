import { CommonModule, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { LocaleService } from '@frontend/i18n';
import { TranslateModule } from '@ngx-translate/core';
import { NewsApiService } from './news-api.service';
import type { NewsArticle } from './news.types';

@Component({
  selector: 'cce-news-detail',
  standalone: true,
  imports: [
    CommonModule, DatePipe, RouterLink,
    MatButtonModule, MatIconModule, TranslateModule,
  ],
  templateUrl: './news-detail.page.html',
  styleUrl: './news-detail.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NewsDetailPage implements OnInit {
  private readonly api = inject(NewsApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly localeService = inject(LocaleService);

  readonly article = signal<NewsArticle | null>(null);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);

  readonly locale = this.localeService.locale;

  readonly title = computed(() => {
    const a = this.article();
    if (!a) return '';
    return this.locale() === 'ar' ? a.titleAr : a.titleEn;
  });

  readonly content = computed(() => {
    const a = this.article();
    if (!a) return '';
    return this.locale() === 'ar' ? a.contentAr : a.contentEn;
  });

  async ngOnInit(): Promise<void> {
    const slug = this.route.snapshot.paramMap.get('slug');
    if (!slug) {
      this.errorKind.set('not-found');
      return;
    }
    this.loading.set(true);
    this.errorKind.set(null);
    const res = await this.api.getBySlug(slug);
    this.loading.set(false);
    if (res.ok) this.article.set(res.value);
    else this.errorKind.set(res.error.kind);
  }
}
