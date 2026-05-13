import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { LocaleService } from '@frontend/i18n';
import { WorkbenchHeroComponent } from '@frontend/ui-kit';
import { CommunityApiService } from './community-api.service';
import { TopicCardComponent } from './topic-card.component';
import type { PublicTopic } from './community.types';

type SortOrder = 'default' | 'alpha' | 'alphaReverse';

interface ActiveChip {
  id: string;
  label: string;
}

/**
 * Public community topics list page. Polished UX:
 *  - Toolbar: search · "Filters" (with badge) · view toggle · count
 *  - Grouped filter panel: Sort
 *  - Active-filter chip strip with remove + clear-all
 *  - Skeleton cards, branded empty / filtered-empty / error states
 */
@Component({
  selector: 'cce-topics-list-page',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatIconModule, MatProgressBarModule,
    TranslateModule, TopicCardComponent, WorkbenchHeroComponent,
  ],
  templateUrl: './topics-list.page.html',
  styleUrl: './topics-list.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TopicsListPage implements OnInit {
  private readonly api = inject(CommunityApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly localeService = inject(LocaleService);
  private readonly t = inject(TranslateService);

  readonly rows = signal<PublicTopic[]>([]);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly query = signal('');
  readonly viewMode = signal<'grid' | 'list'>('grid');
  readonly sortOrder = signal<SortOrder>('default');
  readonly filtersOpen = signal(false);

  readonly locale = this.localeService.locale;

  /** Apply sort order. Search filter runs after sort. */
  private readonly sortedRows = computed(() => {
    const all = [...this.rows()];
    const sort = this.sortOrder();
    if (sort === 'default') {
      return all.sort((a, b) => a.orderIndex - b.orderIndex);
    }
    return all.sort((a, b) => {
      const an = (this.locale() === 'ar' ? a.nameAr : a.nameEn) || '';
      const bn = (this.locale() === 'ar' ? b.nameAr : b.nameEn) || '';
      const cmp = an.localeCompare(bn, this.locale());
      return sort === 'alpha' ? cmp : -cmp;
    });
  });

  readonly filtered = computed<PublicTopic[]>(() => {
    const q = this.query().trim().toLowerCase();
    const all = this.sortedRows();
    if (!q) return all;
    return all.filter((t) => {
      const name = this.locale() === 'ar' ? t.nameAr : t.nameEn;
      const desc = this.locale() === 'ar' ? t.descriptionAr : t.descriptionEn;
      const hay = [name, desc.replace(/<[^>]*>/g, ''), t.slug].join(' ').toLowerCase();
      return hay.includes(q);
    });
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

  readonly activeChips = computed<ActiveChip[]>(() => {
    const chips: ActiveChip[] = [];
    if (this.sortOrder() !== 'default') {
      chips.push({
        id: 'sort',
        label: this.t.instant(
          this.sortOrder() === 'alpha'
            ? 'community.filters.sortAlpha'
            : 'community.filters.sortAlphaReverse',
        ),
      });
    }
    return chips;
  });

  readonly skeletons = Array.from({ length: 6 });

  ngOnInit(): void {
    const qp = this.route.snapshot.queryParamMap;
    this.query.set(qp.get('q') ?? '');
    this.viewMode.set(qp.get('view') === 'list' ? 'list' : 'grid');
    const s = qp.get('sort');
    this.sortOrder.set(s === 'alpha' || s === 'alphaReverse' ? s : 'default');
    void this.load();
  }

  setViewMode(mode: 'grid' | 'list'): void {
    this.viewMode.set(mode);
    this.syncUrl();
  }

  toggleFilters(): void {
    this.filtersOpen.update((v) => !v);
  }

  setSort(s: SortOrder): void {
    this.sortOrder.set(s);
    this.syncUrl();
  }

  removeChip(id: string): void {
    if (id === 'sort') this.sortOrder.set('default');
    this.syncUrl();
  }

  clearAllFilters(): void {
    this.sortOrder.set('default');
    this.query.set('');
    this.syncUrl();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.errorKind.set(null);
    const res = await this.api.listTopics();
    this.loading.set(false);
    if (res.ok) this.rows.set(res.value);
    else this.errorKind.set(res.error.kind);
  }

  retry(): void { void this.load(); }

  onQueryChange(v: string): void {
    this.query.set(v);
    this.syncUrl();
  }

  clearQuery(): void {
    this.query.set('');
    this.syncUrl();
  }

  private syncUrl(): void {
    void this.router.navigate(['./'], {
      relativeTo: this.route,
      queryParams: {
        q: this.query() || null,
        view: this.viewMode() === 'list' ? 'list' : null,
        sort: this.sortOrder() === 'default' ? null : this.sortOrder(),
      },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }
}
