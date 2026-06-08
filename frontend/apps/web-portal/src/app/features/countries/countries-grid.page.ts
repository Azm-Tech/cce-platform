import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslocoModule } from '@jsverse/transloco';
import { LocaleService } from '@frontend/i18n';
import { CountriesApiService } from './countries-api.service';
import { KapsarcApiService } from './kapsarc-api.service';
import { CountryCardComponent } from './country-card.component';
import { CountriesWorldMapComponent } from './countries-world-map.component';
import { getMockCardStats, getMockKapsarc } from './testing/countries-mock';
import { flagUrlFor, flagEmojiFor } from './flag-helpers';
import type { Country, CountryCardStats } from './country.types';

type ViewMode = 'grid' | 'map';

const PAGE_SIZE = 12;

@Component({
  selector: 'cce-countries-grid',
  standalone: true,
  imports: [
    FormsModule, RouterLink,
    MatButtonModule, MatFormFieldModule, MatIconModule,
    MatInputModule, MatMenuModule, MatProgressBarModule,
    TranslocoModule,
    CountryCardComponent, CountriesWorldMapComponent,
  ],
  templateUrl: './countries-grid.page.html',
  styleUrl: './countries-grid.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CountriesGridPage implements OnInit {
  private readonly api = inject(CountriesApiService);
  private readonly kapsarcApi = inject(KapsarcApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly localeService = inject(LocaleService);

  readonly locale = this.localeService.locale;
  readonly skeletons = Array.from({ length: 8 });
  readonly searchTerm = signal('');
  readonly viewMode = signal<ViewMode>('grid');
  readonly topPerformersOnly = signal(false);
  readonly regionFilter = signal<string | null>(null);
  readonly currentPage = signal(1);

  readonly rows = signal<Country[]>([]);
  readonly statsMap = signal<Map<string, CountryCardStats>>(new Map());
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);

  // Map selection state
  readonly selectedCountry = signal<Country | null>(null);
  readonly mapFlagFailed = signal(false);

  readonly empty = computed(() => !this.loading() && this.rows().length === 0 && !this.errorKind());

  readonly regions = computed<string[]>(() => {
    const loc = this.locale();
    const seen = new Set<string>();
    const result: string[] = [];
    for (const c of this.rows()) {
      const r = loc === 'ar' ? c.regionAr : c.regionEn;
      if (!seen.has(r)) { seen.add(r); result.push(r); }
    }
    return result.sort((a, b) => a.localeCompare(b, loc));
  });

  readonly filteredRows = computed<Country[]>(() => {
    let list = this.rows();
    const q = this.searchTerm().trim().toLowerCase();
    if (q) {
      list = list.filter(c => c.nameAr.toLowerCase().includes(q) || c.nameEn.toLowerCase().includes(q));
    }
    const regionF = this.regionFilter();
    if (regionF) {
      const loc = this.locale();
      list = list.filter(c => (loc === 'ar' ? c.regionAr : c.regionEn) === regionF);
    }
    if (this.topPerformersOnly()) {
      const stats = this.statsMap();
      list = list
        .filter(c => stats.has(c.id))
        .sort((a, b) => (stats.get(a.id)!.globalRank || 999) - (stats.get(b.id)!.globalRank || 999))
        .slice(0, 12);
    }
    return list;
  });

  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.filteredRows().length / PAGE_SIZE)));

  readonly pagedRows = computed<Country[]>(() => {
    const page = Math.min(this.currentPage(), this.totalPages());
    const start = (page - 1) * PAGE_SIZE;
    return this.filteredRows().slice(start, start + PAGE_SIZE);
  });

  readonly pageNumbers = computed<number[]>(() =>
    Array.from({ length: this.totalPages() }, (_, i) => i + 1),
  );

  readonly selectedName = computed(() => {
    const c = this.selectedCountry();
    if (!c) return '';
    return this.locale() === 'ar' ? c.nameAr : c.nameEn;
  });

  readonly selectedFlagSrc = computed(() => {
    const c = this.selectedCountry();
    return c ? flagUrlFor(c) : '';
  });

  readonly selectedFlagEmoji = computed(() => {
    const c = this.selectedCountry();
    return c ? flagEmojiFor(c.isoAlpha2) : '🏳️';
  });

  readonly selectedStats = computed(() => {
    const c = this.selectedCountry();
    return c ? (this.statsMap().get(c.id) ?? null) : null;
  });

  async ngOnInit(): Promise<void> {
    const qp = this.route.snapshot.queryParamMap;
    this.searchTerm.set(qp.get('q') ?? '');
    if (qp.get('view') === 'map') this.viewMode.set('map');
    await this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.errorKind.set(null);
    const res = await this.api.listCountries({});
    this.loading.set(false);
    if (res.ok) {
      this.rows.set(res.value);
      this.buildStatsMap(res.value);
    } else {
      this.errorKind.set(res.error.kind);
    }
  }

  private buildStatsMap(countries: Country[]): void {
    const map = new Map<string, CountryCardStats>();
    for (const c of countries) {
      map.set(c.id, getMockCardStats(c));
    }
    this.statsMap.set(map);
  }

  setView(mode: ViewMode): void {
    this.viewMode.set(mode);
    this.selectedCountry.set(null);
    this.syncUrl();
  }

  toggleTopPerformers(): void {
    this.topPerformersOnly.update(v => !v);
    this.currentPage.set(1);
  }

  setRegion(region: string | null): void {
    this.regionFilter.set(region);
    this.currentPage.set(1);
  }

  resetFilters(): void {
    this.topPerformersOnly.set(false);
    this.regionFilter.set(null);
    this.currentPage.set(1);
  }

  onSearchSubmit(): void {
    this.currentPage.set(1);
    this.syncUrl();
  }

  setPage(n: number): void {
    this.currentPage.set(n);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  retry(): void {
    void this.load();
  }

  onMapCountrySelected(country: Country): void {
    this.selectedCountry.set(country);
    this.mapFlagFailed.set(false);
  }

  closeMapPanel(): void {
    this.selectedCountry.set(null);
  }

  onMapFlagError(): void {
    this.mapFlagFailed.set(true);
  }

  private syncUrl(): void {
    const term = this.searchTerm().trim();
    const view = this.viewMode() === 'map' ? 'map' : null;
    void this.router.navigate(['./'], {
      relativeTo: this.route,
      queryParams: { q: term || null, view },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }
}
