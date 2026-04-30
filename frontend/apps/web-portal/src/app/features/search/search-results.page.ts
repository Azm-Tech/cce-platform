import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatPaginatorModule, type PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatRadioModule } from '@angular/material/radio';
import { TranslateModule } from '@ngx-translate/core';
import { Subscription } from 'rxjs';
import { LocaleService } from '@frontend/i18n';
import { SearchApiService } from './search-api.service';
import { SearchHitComponent } from './search-hit.component';
import { SEARCHABLE_TYPES, type SearchHit, type SearchableType } from './search.types';

@Component({
  selector: 'cce-search-results',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule, MatPaginatorModule, MatProgressBarModule, MatRadioModule,
    TranslateModule, SearchHitComponent,
  ],
  templateUrl: './search-results.page.html',
  styleUrl: './search-results.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SearchResultsPage implements OnInit, OnDestroy {
  private readonly api = inject(SearchApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly localeService = inject(LocaleService);

  readonly q = signal('');
  readonly type = signal<SearchableType | null>(null);
  readonly page = signal(1);
  readonly pageSize = signal(20);
  readonly rows = signal<SearchHit[]>([]);
  readonly total = signal(0);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);

  readonly facetTypes = SEARCHABLE_TYPES;
  readonly locale = this.localeService.locale;

  readonly empty = computed(
    () => !!this.q() && !this.loading() && this.rows().length === 0 && !this.errorKind(),
  );
  readonly noQuery = computed(() => !this.q());

  private subscription?: Subscription;

  ngOnInit(): void {
    this.subscription = this.route.queryParamMap.subscribe((qp) => {
      const q = (qp.get('q') ?? '').trim();
      const t = qp.get('type') as SearchableType | null;
      const p = Number(qp.get('page') ?? 1);
      const ps = Number(qp.get('pageSize') ?? 20);
      this.q.set(q);
      this.type.set(this.isValidType(t) ? t : null);
      this.page.set(Number.isFinite(p) && p >= 1 ? p : 1);
      this.pageSize.set(Number.isFinite(ps) && ps >= 1 ? ps : 20);
      void this.load();
    });
  }

  ngOnDestroy(): void {
    this.subscription?.unsubscribe();
  }

  async load(): Promise<void> {
    if (!this.q()) {
      this.rows.set([]);
      this.total.set(0);
      this.loading.set(false);
      this.errorKind.set(null);
      return;
    }
    this.loading.set(true);
    this.errorKind.set(null);
    const res = await this.api.search({
      q: this.q(),
      type: this.type() ?? undefined,
      page: this.page(),
      pageSize: this.pageSize(),
    });
    this.loading.set(false);
    if (res.ok) {
      this.rows.set(res.value.items);
      this.total.set(Number(res.value.total));
    } else {
      this.errorKind.set(res.error.kind);
    }
  }

  toggleType(t: SearchableType): void {
    const next = this.type() === t ? null : t;
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { type: next ?? null, page: null },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }

  clearType(): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { type: null, page: null },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }

  onPage(e: PageEvent): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        page: e.pageIndex + 1 === 1 ? null : e.pageIndex + 1,
        pageSize: e.pageSize === 20 ? null : e.pageSize,
      },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }

  retry(): void {
    void this.load();
  }

  private isValidType(t: string | null): t is SearchableType {
    return !!t && (SEARCHABLE_TYPES as readonly string[]).includes(t);
  }
}
