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
import { EventsApiService } from './events-api.service';
import { EventCardComponent } from './event-card.component';
import type { Event as EventModel } from './event.types';

type EventTypeFilter = 'all' | 'online' | 'inPerson';
type WhenFilter = 'all' | 'upcoming' | 'today' | 'past';
type SortOrder = 'soonest' | 'latest';

interface ActiveChip {
  id: string;
  label: string;
}

/**
 * Public events list. Polished UX:
 *  - Toolbar: search · "Filters" (with badge) · view toggle · count
 *  - Grouped filter panel: Type, When, Date range, Sort
 *  - Active-filter chip strip with remove + clear-all
 *  - Skeleton placeholders, branded empty / filtered-empty / error states
 */
@Component({
  selector: 'cce-events-list',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatIconModule, MatPaginatorModule, MatProgressBarModule,
    TranslateModule, EventCardComponent, WorkbenchHeroComponent,
  ],
  templateUrl: './events-list.page.html',
  styleUrl: './events-list.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EventsListPage implements OnInit {
  private readonly api = inject(EventsApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly localeService = inject(LocaleService);
  private readonly t = inject(TranslateService);

  // ─── Search / view ──────────────────────────────────────
  readonly query = signal('');
  readonly viewMode = signal<'grid' | 'list'>('grid');

  // ─── Grouped filter state ───────────────────────────────
  readonly typeFilter = signal<EventTypeFilter>('all');
  readonly whenFilter = signal<WhenFilter>('all');
  readonly from = signal('');
  readonly to = signal('');
  readonly sortOrder = signal<SortOrder>('soonest');
  readonly filtersOpen = signal(false);

  // ─── Pagination + load state ────────────────────────────
  readonly page = signal(1);
  readonly pageSize = signal(12);
  readonly rows = signal<EventModel[]>([]);
  readonly total = signal(0);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);

  readonly locale = this.localeService.locale;

  /** Bucket helper used by the When filter and active chip. */
  private timeBucket(e: EventModel): WhenFilter {
    const now = Date.now();
    const start = new Date(e.startsOn).getTime();
    const end = new Date(e.endsOn).getTime();
    if (Number.isNaN(start)) return 'upcoming';
    const da = new Date(start);
    const dn = new Date(now);
    const sameDay =
      da.getFullYear() === dn.getFullYear() &&
      da.getMonth() === dn.getMonth() &&
      da.getDate() === dn.getDate();
    if (sameDay || (start <= now && end >= now)) return 'today';
    if (start < now) return 'past';
    return 'upcoming';
  }

  /** Apply search + type + when + sort. Date range is server-side. */
  readonly filtered = computed<EventModel[]>(() => {
    const q = this.query().trim().toLowerCase();
    const tf = this.typeFilter();
    const wf = this.whenFilter();

    let out = this.rows().filter((e) => {
      // Type filter
      if (tf === 'online' && !e.onlineMeetingUrl) return false;
      if (tf === 'inPerson' && e.onlineMeetingUrl) return false;
      // When filter
      if (wf !== 'all' && this.timeBucket(e) !== wf) return false;
      // Search
      if (q) {
        const title = this.locale() === 'ar' ? e.titleAr : e.titleEn;
        const loc = this.locale() === 'ar' ? e.locationAr : e.locationEn;
        const hay = [title, loc, e.onlineMeetingUrl ? 'online' : '']
          .filter(Boolean).join(' ').toLowerCase();
        if (!hay.includes(q)) return false;
      }
      return true;
    });

    // Sort by start date
    const sort = this.sortOrder();
    out = [...out].sort((a, b) => {
      const ta = new Date(a.startsOn).getTime();
      const tb = new Date(b.startsOn).getTime();
      return sort === 'soonest' ? ta - tb : tb - ta;
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

  readonly activeChips = computed<ActiveChip[]>(() => {
    const chips: ActiveChip[] = [];
    if (this.typeFilter() !== 'all') {
      chips.push({
        id: 'type',
        label: this.t.instant(
          this.typeFilter() === 'online'
            ? 'events.filters.typeOnline'
            : 'events.filters.typeInPerson',
        ),
      });
    }
    if (this.whenFilter() !== 'all') {
      const map = {
        upcoming: 'events.filters.whenUpcoming',
        today: 'events.filters.whenToday',
        past: 'events.filters.whenPast',
      } as const;
      chips.push({ id: 'when', label: this.t.instant(map[this.whenFilter() as Exclude<WhenFilter, 'all'>]) });
    }
    if (this.from()) chips.push({ id: 'from', label: `${this.t.instant('events.filters.from')}: ${this.from()}` });
    if (this.to()) chips.push({ id: 'to', label: `${this.t.instant('events.filters.to')}: ${this.to()}` });
    if (this.sortOrder() !== 'soonest') {
      chips.push({ id: 'sort', label: this.t.instant('events.filters.sortLatest') });
    }
    return chips;
  });

  /** Skeleton placeholder array for the initial load. */
  readonly skeletons = Array.from({ length: 6 });

  ngOnInit(): void {
    const qp = this.route.snapshot.queryParamMap;
    const p = Number(qp.get('page') ?? 1);
    this.page.set(Number.isFinite(p) && p >= 1 ? p : 1);
    this.from.set(qp.get('from') ?? '');
    this.to.set(qp.get('to') ?? '');
    this.query.set(qp.get('q') ?? '');
    this.viewMode.set(qp.get('view') === 'list' ? 'list' : 'grid');
    const tf = qp.get('type');
    this.typeFilter.set(tf === 'online' || tf === 'inPerson' ? tf : 'all');
    const wf = qp.get('when');
    this.whenFilter.set(wf === 'upcoming' || wf === 'today' || wf === 'past' ? wf : 'all');
    this.sortOrder.set(qp.get('sort') === 'latest' ? 'latest' : 'soonest');
    void this.load();
  }

  // ─── Handlers ───────────────────────────────────────────
  toggleFilters(): void { this.filtersOpen.update((v) => !v); }

  setType(v: EventTypeFilter): void {
    this.typeFilter.set(v);
    this.syncUrl();
  }
  setWhen(v: WhenFilter): void {
    this.whenFilter.set(v);
    this.syncUrl();
  }
  setSort(v: SortOrder): void {
    this.sortOrder.set(v);
    this.syncUrl();
  }
  setDateFrom(v: string): void {
    this.from.set(v);
    this.page.set(1);
    void this.load();
    this.syncUrl();
  }
  setDateTo(v: string): void {
    this.to.set(v);
    this.page.set(1);
    void this.load();
    this.syncUrl();
  }

  removeChip(id: string): void {
    switch (id) {
      case 'type': this.typeFilter.set('all'); break;
      case 'when': this.whenFilter.set('all'); break;
      case 'from': this.from.set(''); this.page.set(1); void this.load(); break;
      case 'to': this.to.set(''); this.page.set(1); void this.load(); break;
      case 'sort': this.sortOrder.set('soonest'); break;
    }
    this.syncUrl();
  }

  clearAllFilters(): void {
    this.typeFilter.set('all');
    this.whenFilter.set('all');
    this.from.set('');
    this.to.set('');
    this.sortOrder.set('soonest');
    this.query.set('');
    this.page.set(1);
    void this.load();
    this.syncUrl();
  }

  setViewMode(mode: 'grid' | 'list'): void {
    this.viewMode.set(mode);
    this.syncUrl();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.errorKind.set(null);
    const res = await this.api.listEvents({
      page: this.page(),
      pageSize: this.pageSize(),
      from: this.from() || undefined,
      to: this.to() || undefined,
    });
    this.loading.set(false);
    if (res.ok) {
      this.rows.set(res.value.items);
      this.total.set(Number(res.value.total));
    } else this.errorKind.set(res.error.kind);
  }

  retry(): void { void this.load(); }

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

  private syncUrl(): void {
    void this.router.navigate(['./'], {
      relativeTo: this.route,
      queryParams: {
        page: this.page() === 1 ? null : this.page(),
        type: this.typeFilter() === 'all' ? null : this.typeFilter(),
        when: this.whenFilter() === 'all' ? null : this.whenFilter(),
        from: this.from() || null,
        to: this.to() || null,
        sort: this.sortOrder() === 'soonest' ? null : this.sortOrder(),
        q: this.query() || null,
        view: this.viewMode() === 'list' ? 'list' : null,
      },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }
}
