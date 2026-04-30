import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatButtonModule } from '@angular/material/button';
import { TranslateModule } from '@ngx-translate/core';
import { LocaleService } from '@frontend/i18n';
import { CountriesApiService } from './countries-api.service';
import { CountryCardComponent } from './country-card.component';
import type { Country } from './country.types';

interface RegionGroup {
  region: string;
  countries: Country[];
}

@Component({
  selector: 'cce-countries-grid',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatFormFieldModule, MatInputModule, MatProgressBarModule, MatButtonModule,
    TranslateModule, CountryCardComponent,
  ],
  templateUrl: './countries-grid.page.html',
  styleUrl: './countries-grid.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CountriesGridPage implements OnInit {
  private readonly api = inject(CountriesApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly localeService = inject(LocaleService);

  readonly searchTerm = signal('');
  readonly rows = signal<Country[]>([]);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);

  readonly locale = this.localeService.locale;

  readonly empty = computed(
    () => !this.loading() && this.rows().length === 0 && !this.errorKind(),
  );

  /** Groups countries by localized region, sorted alphabetically. */
  readonly groups = computed<RegionGroup[]>(() => {
    const loc = this.locale();
    const buckets = new Map<string, Country[]>();
    for (const c of this.rows()) {
      const region = loc === 'ar' ? c.regionAr : c.regionEn;
      const list = buckets.get(region);
      if (list) list.push(c);
      else buckets.set(region, [c]);
    }
    return Array.from(buckets.entries())
      .sort(([a], [b]) => a.localeCompare(b, loc))
      .map(([region, countries]) => ({ region, countries }));
  });

  ngOnInit(): void {
    const q = this.route.snapshot.queryParamMap.get('q') ?? '';
    this.searchTerm.set(q);
    void this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.errorKind.set(null);
    const term = this.searchTerm().trim();
    const res = await this.api.listCountries(term ? { search: term } : {});
    this.loading.set(false);
    if (res.ok) this.rows.set(res.value);
    else this.errorKind.set(res.error.kind);
  }

  onSearchSubmit(): void {
    void this.load();
    this.syncUrl();
  }

  retry(): void {
    void this.load();
  }

  private syncUrl(): void {
    const term = this.searchTerm().trim();
    void this.router.navigate(['./'], {
      relativeTo: this.route,
      queryParams: { q: term || null },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }
}
