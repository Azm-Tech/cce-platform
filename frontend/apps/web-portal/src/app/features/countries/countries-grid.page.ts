import { CommonModule, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, HostListener, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslateModule } from '@ngx-translate/core';
import { LocaleService } from '@frontend/i18n';
import { WorkbenchHeroComponent } from '@frontend/ui-kit';
import { CountriesApiService } from './countries-api.service';
import { CountryCardComponent } from './country-card.component';
import { getMockKapsarc, getMockProfile } from './countries-mock';
import { flagEmojiFor, flagUrlFor } from './flag-helpers';
import { KapsarcApiService } from './kapsarc-api.service';
import { KapsarcSnapshotComponent } from './kapsarc-snapshot.component';
import type { Country, CountryProfile, KapsarcSnapshot } from './country.types';

interface RegionGroup {
  region: string;
  countries: Country[];
}

@Component({
  selector: 'cce-countries-grid',
  standalone: true,
  imports: [
    CommonModule, DatePipe, FormsModule, RouterLink,
    MatButtonModule, MatFormFieldModule, MatIconModule,
    MatInputModule, MatProgressBarModule,
    TranslateModule,
    CountryCardComponent, KapsarcSnapshotComponent, WorkbenchHeroComponent,
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

  readonly searchTerm = signal('');
  readonly rows = signal<Country[]>([]);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);

  // Inline detail panel state — replaces the dedicated /countries/:id route.
  readonly selectedCountry = signal<Country | null>(null);
  readonly profile = signal<CountryProfile | null>(null);
  readonly snapshot = signal<KapsarcSnapshot | null>(null);
  readonly profileLoading = signal(false);
  readonly flagFailed = signal(false);

  readonly locale = this.localeService.locale;

  readonly empty = computed(
    () => !this.loading() && this.rows().length === 0 && !this.errorKind(),
  );

  // ─── Selected-country derived state for the detail panel ─────
  readonly selectedName = computed(() => {
    const c = this.selectedCountry();
    if (!c) return '';
    return this.locale() === 'ar' ? c.nameAr : c.nameEn;
  });
  readonly description = computed(() => {
    const p = this.profile();
    if (!p) return '';
    return this.locale() === 'ar' ? p.descriptionAr : p.descriptionEn;
  });
  readonly keyInitiatives = computed(() => {
    const p = this.profile();
    if (!p) return '';
    return this.locale() === 'ar' ? p.keyInitiativesAr : p.keyInitiativesEn;
  });
  readonly contactInfo = computed(() => {
    const p = this.profile();
    if (!p) return null;
    return this.locale() === 'ar' ? p.contactInfoAr : p.contactInfoEn;
  });
  readonly selectedFlagSrc = computed(() => {
    const c = this.selectedCountry();
    return c ? flagUrlFor(c) : '';
  });
  readonly selectedFlagEmoji = computed(() => {
    const c = this.selectedCountry();
    return c ? flagEmojiFor(c.isoAlpha2) : '🏳️';
  });

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

  async ngOnInit(): Promise<void> {
    const qp = this.route.snapshot.queryParamMap;
    this.searchTerm.set(qp.get('q') ?? '');
    await this.load();

    // Deep-link to a specific country via ?country=<id> — opens the
    // panel after the list resolves.
    const wantId = qp.get('country');
    if (wantId) {
      const match = this.rows().find((c) => c.id === wantId);
      if (match) this.openPanel(match);
    }
  }

  /** Open the inline detail panel for a country card click. */
  openPanel(country: Country): void {
    this.selectedCountry.set(country);
    this.flagFailed.set(false);
    void this.loadDetails(country);
    this.syncCountryParam(country.id);
  }

  /** Close the panel and clear the deep-link query param. */
  closePanel(): void {
    this.selectedCountry.set(null);
    this.profile.set(null);
    this.snapshot.set(null);
    this.profileLoading.set(false);
    this.syncCountryParam(null);
  }

  onSelectedFlagError(): void {
    this.flagFailed.set(true);
  }

  /** Esc closes the panel. */
  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.selectedCountry()) this.closePanel();
  }

  /** Fetch profile + KAPSARC for the chosen country. Real API wins;
   *  mock falls in if /api/countries/{id}/profile returns 404 (which it
   *  does in dev today since the backend hasn't seeded profile data). */
  private async loadDetails(country: Country): Promise<void> {
    this.profileLoading.set(true);
    this.profile.set(null);
    this.snapshot.set(null);

    const [profileRes, snapshotRes] = await Promise.all([
      this.api.getProfile(country.id),
      this.kapsarcApi.getLatestSnapshot(country.id),
    ]);

    // If the user closed the panel while the request was in flight,
    // drop the result.
    if (this.selectedCountry()?.id !== country.id) return;

    this.profile.set(profileRes.ok ? profileRes.value : getMockProfile(country));
    this.snapshot.set(snapshotRes.ok ? snapshotRes.value : getMockKapsarc(country));
    this.profileLoading.set(false);
  }

  private syncCountryParam(id: string | null): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { country: id },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
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
