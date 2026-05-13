import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, type PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { LocaleService } from '@frontend/i18n';
import { WorkbenchHeroComponent } from '@frontend/ui-kit';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { NewsApiService } from './news-api.service';
import { NewsCardComponent } from './news-card.component';
import type { NewsArticle } from './news.types';

type StatusFilter = 'all' | 'featured' | 'draft';
type SortOrder = 'newest' | 'oldest';

interface ActiveChip {
  /** Stable id used by `track` and the remove handler. */
  id: string;
  /** Already-translated label shown in the chip. */
  label: string;
}

/**
 * Public news list page. Polished UX:
 *  - Toolbar: search · "Filters" button (with badge) · view toggle · count
 *  - Grouped filter panel: Status, Date range, Sort
 *  - Active-filter chip strip with one-click remove + clear-all
 *  - Skeleton placeholders, branded empty / filtered-empty / error states
 *  - All filter state hydrated to / from the URL
 */
@Component({
  selector: 'cce-news-list',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatIconModule, MatPaginatorModule, MatProgressBarModule,
    TranslateModule, NewsCardComponent, WorkbenchHeroComponent,
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
  private readonly t = inject(TranslateService);

  // ─── Search / view ──────────────────────────────────────
  readonly query = signal('');
  readonly viewMode = signal<'grid' | 'list'>('grid');

  // ─── Grouped filter state ───────────────────────────────
  readonly status = signal<StatusFilter>('all');
  readonly dateFrom = signal('');
  readonly dateTo = signal('');
  readonly sortOrder = signal<SortOrder>('newest');
  readonly filtersOpen = signal(false);

  // ─── Pagination + load state ────────────────────────────
  readonly page = signal(1);
  readonly pageSize = signal(12);
  readonly rows = signal<NewsArticle[]>([]);
  readonly total = signal(0);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);

  readonly locale = this.localeService.locale;

  /** Server-side filter shorthand for the API call. */
  private readonly featuredOnly = computed(() => this.status() === 'featured');
  /** Drafts are surfaced client-side because the API only returns
   *  published articles unless `isFeatured` is set. We can still
   *  filter the visible list by `publishedOn === null` for clarity. */
  private readonly draftsOnly = computed(() => this.status() === 'draft');

  /** Apply search + date range + status + sort to `rows()`. */
  readonly filtered = computed<NewsArticle[]>(() => {
    const q = this.query().trim().toLowerCase();
    const from = this.dateFrom() ? new Date(this.dateFrom()).getTime() : null;
    const to = this.dateTo() ? new Date(this.dateTo() + 'T23:59:59').getTime() : null;
    const drafts = this.draftsOnly();

    let out = this.rows().filter((a) => {
      // Status: drafts only
      if (drafts && a.publishedOn) return false;
      // Date range
      if ((from !== null || to !== null) && a.publishedOn) {
        const t = new Date(a.publishedOn).getTime();
        if (from !== null && t < from) return false;
        if (to !== null && t > to) return false;
      }
      // Search
      if (q) {
        const title = this.locale() === 'ar' ? a.titleAr : a.titleEn;
        const content = this.locale() === 'ar' ? a.contentAr : a.contentEn;
        const hay = [title, content.replace(/<[^>]*>/g, ''), a.slug].join(' ').toLowerCase();
        if (!hay.includes(q)) return false;
      }
      return true;
    });

    // Sort
    const sort = this.sortOrder();
    out = [...out].sort((a, b) => {
      const ta = a.publishedOn ? new Date(a.publishedOn).getTime() : 0;
      const tb = b.publishedOn ? new Date(b.publishedOn).getTime() : 0;
      return sort === 'newest' ? tb - ta : ta - tb;
    });
    return out;
  });

  readonly empty = computed(
    () => !this.loading() && this.rows().length === 0 && !this.errorKind(),
  );
  readonly filteredEmpty = computed(
    () =>
      !this.loading() &&
      this.rows().length > 0 &&
      this.filtered().length === 0,
  );

  /** Surface the *non-default* filters in the active-chips strip. */
  readonly activeChips = computed<ActiveChip[]>(() => {
    const chips: ActiveChip[] = [];
    if (this.status() !== 'all') {
      chips.push({
        id: 'status',
        label: this.t.instant(
          this.status() === 'featured'
            ? 'news.filters.statusFeatured'
            : 'news.filters.statusDraft',
        ),
      });
    }
    if (this.dateFrom()) chips.push({ id: 'from', label: `${this.t.instant('news.filters.from')}: ${this.dateFrom()}` });
    if (this.dateTo()) chips.push({ id: 'to', label: `${this.t.instant('news.filters.to')}: ${this.dateTo()}` });
    if (this.sortOrder() !== 'newest') {
      chips.push({ id: 'sort', label: this.t.instant('news.filters.sortOldest') });
    }
    return chips;
  });

  /** Skeleton placeholder array used during the initial load. */
  readonly skeletons = Array.from({ length: 6 });

  ngOnInit(): void {
    const qp = this.route.snapshot.queryParamMap;
    const p = Number(qp.get('page') ?? 1);
    this.page.set(Number.isFinite(p) && p >= 1 ? p : 1);
    const s = qp.get('status');
    this.status.set(s === 'featured' || s === 'draft' ? s : 'all');
    this.dateFrom.set(qp.get('from') ?? '');
    this.dateTo.set(qp.get('to') ?? '');
    this.sortOrder.set(qp.get('sort') === 'oldest' ? 'oldest' : 'newest');
    this.query.set(qp.get('q') ?? '');
    this.viewMode.set(qp.get('view') === 'list' ? 'list' : 'grid');
    void this.load();
  }

  // ─── Handlers ───────────────────────────────────────────
  setViewMode(mode: 'grid' | 'list'): void {
    this.viewMode.set(mode);
    this.syncUrl();
  }

  toggleFilters(): void {
    this.filtersOpen.update((v) => !v);
  }

  setStatus(s: StatusFilter): void {
    this.status.set(s);
    this.page.set(1);
    void this.load();
    this.syncUrl();
  }

  setSort(s: SortOrder): void {
    this.sortOrder.set(s);
    this.syncUrl();
  }

  setDateFrom(v: string): void {
    this.dateFrom.set(v);
    this.syncUrl();
  }

  setDateTo(v: string): void {
    this.dateTo.set(v);
    this.syncUrl();
  }

  removeChip(id: string): void {
    switch (id) {
      case 'status': this.status.set('all'); void this.load(); break;
      case 'from': this.dateFrom.set(''); break;
      case 'to': this.dateTo.set(''); break;
      case 'sort': this.sortOrder.set('newest'); break;
    }
    this.syncUrl();
  }

  clearAllFilters(): void {
    this.status.set('all');
    this.dateFrom.set('');
    this.dateTo.set('');
    this.sortOrder.set('newest');
    this.query.set('');
    this.page.set(1);
    void this.load();
    this.syncUrl();
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

  onQueryChange(v: string): void {
    this.query.set(v);
    this.syncUrl();
  }

  clearQuery(): void {
    this.query.set('');
    this.syncUrl();
  }

  retry(): void {
    void this.load();
  }

  private syncUrl(): void {
    void this.router.navigate(['./'], {
      relativeTo: this.route,
      queryParams: {
        page: this.page() === 1 ? null : this.page(),
        status: this.status() === 'all' ? null : this.status(),
        from: this.dateFrom() || null,
        to: this.dateTo() || null,
        sort: this.sortOrder() === 'newest' ? null : this.sortOrder(),
        q: this.query() || null,
        view: this.viewMode() === 'list' ? 'list' : null,
      },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }
}
