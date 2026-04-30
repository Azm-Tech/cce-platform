import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatPaginatorModule, type PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { LocaleService } from '@frontend/i18n';
import { TranslateModule } from '@ngx-translate/core';
import { NewsApiService } from './news-api.service';
import { NewsCardComponent } from './news-card.component';
import type { NewsArticle } from './news.types';

@Component({
  selector: 'cce-news-list',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatCheckboxModule, MatPaginatorModule, MatProgressBarModule,
    TranslateModule, NewsCardComponent,
  ],
  templateUrl: './news-list.page.html',
  styleUrl: './news-list.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NewsListPage implements OnInit {
  private readonly api = inject(NewsApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly localeService = inject(LocaleService);

  readonly featuredOnly = signal(false);
  readonly page = signal(1);
  readonly pageSize = signal(12);
  readonly rows = signal<NewsArticle[]>([]);
  readonly total = signal(0);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly empty = computed(() => !this.loading() && this.rows().length === 0 && !this.errorKind());

  readonly locale = this.localeService.locale;

  ngOnInit(): void {
    const qp = this.route.snapshot.queryParamMap;
    const p = Number(qp.get('page') ?? 1);
    this.page.set(Number.isFinite(p) && p >= 1 ? p : 1);
    this.featuredOnly.set(qp.get('featured') === 'true');
    void this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.errorKind.set(null);
    const res = await this.api.listNews({
      page: this.page(),
      pageSize: this.pageSize(),
      isFeatured: this.featuredOnly() ? true : undefined,
    });
    this.loading.set(false);
    if (res.ok) {
      this.rows.set(res.value.items);
      this.total.set(Number(res.value.total));
    } else this.errorKind.set(res.error.kind);
  }

  onPage(e: PageEvent): void {
    this.page.set(e.pageIndex + 1);
    this.pageSize.set(e.pageSize);
    void this.load();
    this.syncUrl();
  }

  onFeaturedToggle(value: boolean): void {
    this.featuredOnly.set(value);
    this.page.set(1);
    void this.load();
    this.syncUrl();
  }

  private syncUrl(): void {
    void this.router.navigate(['./'], {
      relativeTo: this.route,
      queryParams: {
        page: this.page() === 1 ? null : this.page(),
        featured: this.featuredOnly() ? 'true' : null,
      },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }
}
