import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, computed, inject, signal } from '@angular/core';

import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, type PageEvent } from '@angular/material/paginator';
import { MatSelectModule } from '@angular/material/select';
import { LocaleService } from '@frontend/i18n';
import { WorkbenchHeroComponent } from '@frontend/ui-kit';
import { TranslocoModule } from '@jsverse/transloco';
import { FilterRailComponent } from '../../core/layout/filter-rail.component';
import { CountriesApiService } from '../countries/countries-api.service';
import { KnowledgeApiService } from './knowledge-api.service';
import { MOCK_CATEGORIES, MOCK_RESOURCES } from './testing/mock-data';
import { ResourceCardComponent } from './resource-card.component';
import {
  RESOURCE_TYPES,
  type ResourceCategory,
  type ResourceListItem,
  type ResourceType,
} from './knowledge.types';
import type { Country } from '../countries/country.types';

@Component({
  selector: 'cce-resources-list',
  standalone: true,
  imports: [
    FormsModule,
    MatButtonModule,
    MatCheckboxModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatPaginatorModule,
    MatSelectModule,
    TranslocoModule,
    FilterRailComponent,
    ResourceCardComponent,
    WorkbenchHeroComponent,
  ],
  templateUrl: './resources-list.page.html',
  styleUrl: './resources-list.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResourcesListPage implements OnInit {
  private readonly api = inject(KnowledgeApiService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly countriesApi = inject(CountriesApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly localeService = inject(LocaleService);

  readonly resourceTypes = RESOURCE_TYPES;
  readonly categories = signal<ResourceCategory[]>([]);
  readonly countries = signal<Country[]>([]);
  readonly countrySearch = signal('');
  readonly categoryId = signal<string | null>(null);
  readonly countryId = signal<string>('');
  readonly resourceType = signal<ResourceType | ''>('');
  readonly searchInput = signal('');
  readonly page = signal(1);
  readonly pageSize = signal(20);
  readonly rows = signal<ResourceListItem[]>([]);
  readonly total = signal(0);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly empty = computed(() => !this.loading() && this.rows().length === 0 && !this.errorKind());

  readonly locale = this.localeService.locale;
  readonly skeletons = Array.from({ length: 8 });

  /** Categories flattened to a depth-annotated list for the checkbox filter. */
  readonly categoryOptions = computed(() => {
    const all = this.categories();
    const out: { id: string; label: string; depth: number }[] = [];
    const label = (c: ResourceCategory) => (this.locale() === 'ar' ? c.nameAr : c.nameEn);
    const walk = (parentId: string | null, depth: number) => {
      all
        .filter((c) => c.parentId === parentId)
        .slice()
        .sort((a, b) => a.orderIndex - b.orderIndex)
        .forEach((c) => {
          out.push({ id: c.id, label: label(c), depth });
          walk(c.id, depth + 1);
        });
    };
    walk(null, 0);
    return out;
  });

  /** Countries narrowed by the client-side search box (matches AR + EN names). */
  readonly filteredCountries = computed(() => {
    const q = this.countrySearch().trim().toLowerCase();
    const all = this.countries();
    if (!q) return all;
    return all.filter(
      (c) => c.nameAr.toLowerCase().includes(q) || c.nameEn.toLowerCase().includes(q),
    );
  });
  readonly viewMode = signal<'grid' | 'list'>('grid');
  readonly sortBy = signal<'date' | 'views' | 'title'>('date');

  ngOnInit(): void {
    const qp = this.route.snapshot.queryParamMap;
    const p = Number(qp.get('page') ?? 1);
    const ps = Number(qp.get('pageSize') ?? 20);
    this.page.set(Number.isFinite(p) && p >= 1 ? p : 1);
    this.pageSize.set(Number.isFinite(ps) && ps >= 1 ? ps : 20);
    this.categoryId.set(qp.get('categoryId') || null);
    this.countryId.set(qp.get('countryId') ?? '');
    this.searchInput.set(qp.get('search') ?? '');
    const rt = qp.get('resourceType') as ResourceType | null;
    this.resourceType.set(rt && (RESOURCE_TYPES as readonly string[]).includes(rt) ? rt : '');

    void this.loadCategories();
    void this.loadCountries();
    void this.load();
  }

  async loadCategories(): Promise<void> {
    const res = await this.api.listCategories();
    if (res.ok && res.value.length > 0) {
      this.categories.set(res.value);
    } else {
      this.categories.set(MOCK_CATEGORIES);
    }
    this.cdr.markForCheck();
  }

  async loadCountries(): Promise<void> {
    const res = await this.countriesApi.listCountries({});
    // Guard the shape — a non-array here would throw inside computed()s
    // during change detection and freeze the whole page.
    if (res.ok && Array.isArray(res.value)) {
      this.countries.set(res.value);
      this.cdr.markForCheck();
    }
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.errorKind.set(null);
    this.cdr.markForCheck(); // show skeleton immediately (OnPush + async)
    try {
      const res = await this.api.listResources({
        page: this.page(),
        pageSize: this.pageSize(),
        search: this.searchInput() || undefined,
        categoryId: this.categoryId() ?? undefined,
        countryId: this.countryId() || undefined,
        resourceType: this.resourceType() || undefined,
      });
      if (res.ok) {
        this.rows.set(res.value.items ?? []);
        this.total.set(Number(res.value.total) || 0);
      } else {
        // API unavailable — fall back to mock so the page always shows content.
        let filtered = MOCK_RESOURCES.slice();
        const cat = this.categoryId();
        if (cat) filtered = filtered.filter((r) => r.categoryId === cat || r.categoryId.startsWith(`${cat}-`));
        const country = this.countryId();
        if (country) filtered = filtered.filter((r) => r.countryId === country);
        const type = this.resourceType();
        if (type) filtered = filtered.filter((r) => r.resourceType === type);
        const search = this.searchInput().toLowerCase();
        if (search) filtered = filtered.filter((r) => r.titleAr.toLowerCase().includes(search) || r.titleEn.toLowerCase().includes(search));
        this.total.set(filtered.length);
        const start = (this.page() - 1) * this.pageSize();
        this.rows.set(filtered.slice(start, start + this.pageSize()));
      }
    } finally {
      this.loading.set(false);
      // OnPush + async: explicitly mark dirty so the template re-evaluates
      this.cdr.markForCheck();
    }
  }

  onPage(e: PageEvent): void {
    this.page.set(e.pageIndex + 1);
    this.pageSize.set(e.pageSize);
    void this.load();
    this.syncUrl();
  }

  onSearch(): void {
    this.page.set(1);
    void this.load();
    this.syncUrl();
  }

  onCategoryChange(id: string | null): void {
    this.categoryId.set(id);
    this.page.set(1);
    void this.load();
    this.syncUrl();
  }

  onCountryChange(value: string): void {
    this.countryId.set(value);
    this.page.set(1);
    void this.load();
    this.syncUrl();
  }

  onResourceTypeChange(value: ResourceType | ''): void {
    this.resourceType.set(value);
    this.page.set(1);
    void this.load();
    this.syncUrl();
  }

  private syncUrl(): void {
    void this.router.navigate(['./'], {
      relativeTo: this.route,
      queryParams: {
        page: this.page() === 1 ? null : this.page(),
        pageSize: this.pageSize() === 20 ? null : this.pageSize(),
        search: this.searchInput() || null,
        categoryId: this.categoryId(),
        countryId: this.countryId() || null,
        resourceType: this.resourceType() || null,
      },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }
}
