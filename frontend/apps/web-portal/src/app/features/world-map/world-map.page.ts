import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { ALL_CITIES, WorldMapComponent, type AnyCity, type FeaturedCity } from './world-map.component';

type CarbonTier = 'low' | 'medium' | 'high';

const REGIONS = [
  { id: 'all',     labelKey: 'explore.region.all' },
  { id: 'mena',    labelKey: 'explore.region.mena' },
  { id: 'africa',  labelKey: 'explore.region.africa' },
  { id: 'europe',  labelKey: 'explore.region.europe' },
  { id: 'asia',    labelKey: 'explore.region.asia' },
  { id: 'americas',labelKey: 'explore.region.americas' },
  { id: 'oceania', labelKey: 'explore.region.oceania' },
] as const;

type RegionId = (typeof REGIONS)[number]['id'];

/** Country code → region. Lazy heuristic: keys cover ~all our cities. */
const COUNTRY_REGION: Record<string, Exclude<RegionId, 'all'>> = {
  // MENA
  SA:'mena', AE:'mena', QA:'mena', KW:'mena', BH:'mena', OM:'mena',
  EG:'mena', JO:'mena', LB:'mena', SY:'mena', IQ:'mena', IR:'mena',
  IL:'mena', PS:'mena', YE:'mena', TR:'mena', MA:'mena', DZ:'mena',
  TN:'mena', LY:'mena', SD:'mena',
  // Africa (sub-Saharan)
  NG:'africa', KE:'africa', ZA:'africa', ET:'africa', GH:'africa',
  CI:'africa', SN:'africa', UG:'africa', RW:'africa', TZ:'africa',
  CD:'africa', AO:'africa', ZW:'africa', MZ:'africa', MG:'africa',
  ER:'africa', SO:'africa', DJ:'africa',
  // Europe
  GB:'europe', FR:'europe', DE:'europe', IT:'europe', ES:'europe',
  NL:'europe', BE:'europe', PT:'europe', IE:'europe', AT:'europe',
  CH:'europe', SE:'europe', NO:'europe', DK:'europe', FI:'europe',
  IS:'europe', PL:'europe', CZ:'europe', HU:'europe', RO:'europe',
  BG:'europe', GR:'europe', RS:'europe', HR:'europe', SI:'europe',
  EE:'europe', LV:'europe', LT:'europe', UA:'europe', BY:'europe', RU:'europe',
  // Asia
  CN:'asia', JP:'asia', KR:'asia', KP:'asia', IN:'asia', PK:'asia',
  BD:'asia', AF:'asia', NP:'asia', LK:'asia', MM:'asia', TH:'asia',
  VN:'asia', LA:'asia', KH:'asia', MY:'asia', SG:'asia', ID:'asia',
  PH:'asia', TW:'asia', HK:'asia', MO:'asia', MN:'asia', KZ:'asia',
  UZ:'asia', GE:'asia', AM:'asia', AZ:'asia',
  // Americas
  US:'americas', CA:'americas', MX:'americas', GT:'americas', SV:'americas',
  HN:'americas', NI:'americas', CR:'americas', PA:'americas', CU:'americas',
  JM:'americas', DO:'americas', PR:'americas', CO:'americas', VE:'americas',
  EC:'americas', PE:'americas', BO:'americas', BR:'americas', AR:'americas',
  CL:'americas', PY:'americas', UY:'americas',
  // Oceania
  AU:'oceania', NZ:'oceania', FJ:'oceania', PG:'oceania',
};

/**
 * Interactive world-map page. Header carries a filter bar (search /
 * tier / region) + a stats bar — mirrors the old "Interactive City"
 * tab's scenario-builder header pattern. Featured cities (60) carry
 * rich metadata; standard cities (~150) carry minimal data and offer a
 * CTA to /interactive-city for sustainability-scenario building.
 */
@Component({
  selector: 'cce-world-map-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, WorldMapComponent, TranslateModule],
  template: `
    <div class="cce-explore">
      <header class="cce-explore__header">
        <h1 class="cce-explore__title">{{ 'explore.title' | translate }}</h1>
        <p class="cce-explore__subtitle">{{ 'explore.subtitle' | translate }}</p>

        <!-- Filter bar -->
        <div class="cce-explore__filters" role="search">
          <div class="cce-explore__filter cce-explore__filter--search">
            <label class="cce-explore__filter-label" for="cce-search">
              {{ 'explore.filters.search' | translate }}
            </label>
            <input
              id="cce-search"
              type="search"
              class="cce-explore__search-input"
              [ngModel]="searchTerm()"
              (ngModelChange)="searchTerm.set($event)"
              [placeholder]="'explore.filters.searchPlaceholder' | translate"
            />
          </div>

          <div class="cce-explore__filter">
            <span class="cce-explore__filter-label">{{ 'explore.filters.tier' | translate }}</span>
            <div class="cce-explore__pills">
              <button
                type="button"
                class="cce-explore__pill cce-explore__pill--low"
                [class.cce-explore__pill--active]="selectedTiers().has('low')"
                (click)="toggleTier('low')"
              >
                {{ 'explore.legend.low' | translate }}
              </button>
              <button
                type="button"
                class="cce-explore__pill cce-explore__pill--medium"
                [class.cce-explore__pill--active]="selectedTiers().has('medium')"
                (click)="toggleTier('medium')"
              >
                {{ 'explore.legend.medium' | translate }}
              </button>
              <button
                type="button"
                class="cce-explore__pill cce-explore__pill--high"
                [class.cce-explore__pill--active]="selectedTiers().has('high')"
                (click)="toggleTier('high')"
              >
                {{ 'explore.legend.high' | translate }}
              </button>
            </div>
          </div>

          <div class="cce-explore__filter">
            <label class="cce-explore__filter-label" for="cce-region">
              {{ 'explore.filters.region' | translate }}
            </label>
            <select
              id="cce-region"
              class="cce-explore__select"
              [ngModel]="selectedRegion()"
              (ngModelChange)="selectedRegion.set($event)"
            >
              @for (r of regions; track r.id) {
                <option [value]="r.id">{{ r.labelKey | translate }}</option>
              }
            </select>
          </div>

          <button
            type="button"
            class="cce-explore__reset"
            (click)="resetFilters()"
            [disabled]="!hasFilters()"
          >
            {{ 'explore.filters.reset' | translate }}
          </button>
        </div>

        <!-- Stats bar (analog of the old interactive-city totals bar) -->
        <div class="cce-explore__stats-bar" aria-live="polite">
          <div class="cce-explore__stat-cell">
            <span class="cce-explore__stat-cell-label">{{ 'explore.stats.shown' | translate }}</span>
            <span class="cce-explore__stat-cell-value">
              {{ stats().shown }}<span class="cce-explore__stat-cell-unit">/ {{ totalCities }}</span>
            </span>
          </div>
          <div class="cce-explore__stat-cell">
            <span class="cce-explore__stat-cell-label">{{ 'explore.stats.totalPop' | translate }}</span>
            <span class="cce-explore__stat-cell-value">
              {{ formatPopulation(stats().totalPop) }}
              <span class="cce-explore__stat-cell-unit">{{ 'explore.stats.popUnit' | translate }}</span>
            </span>
          </div>
          <div class="cce-explore__stat-cell">
            <span class="cce-explore__stat-cell-label">{{ 'explore.stats.tierBreakdown' | translate }}</span>
            <span class="cce-explore__stat-cell-value">
              <span class="cce-explore__tier-mini cce-explore__tier-mini--low">{{ stats().byTier.low }}</span>
              <span class="cce-explore__tier-mini cce-explore__tier-mini--medium">{{ stats().byTier.medium }}</span>
              <span class="cce-explore__tier-mini cce-explore__tier-mini--high">{{ stats().byTier.high }}</span>
            </span>
          </div>
          <div class="cce-explore__stat-cell">
            <span class="cce-explore__stat-cell-label">{{ 'explore.stats.regions' | translate }}</span>
            <span class="cce-explore__stat-cell-value">{{ stats().regionCount }}</span>
          </div>
        </div>

        <p class="cce-explore__hint">{{ 'explore.zoomHint' | translate }}</p>
      </header>

      <div class="cce-explore__stage">
        <div class="cce-explore__map-wrap">
          <cce-world-map
            [selectedCityId]="selectedCityId()"
            [visibleCityIds]="visibleCityIds()"
            (cityClicked)="onCityClicked($event)"
          />
        </div>

        @if (selectedCity(); as city) {
          <aside class="cce-explore__panel" role="complementary" [attr.aria-label]="city.name">
            <button
              type="button"
              class="cce-explore__panel-close"
              (click)="closePanel()"
              [attr.aria-label]="'explore.close' | translate"
            >
              ✕
            </button>
            <div class="cce-explore__panel-header" [class]="'cce-explore__panel-header--' + city.carbonTier">
              <span class="cce-explore__flag">{{ flagEmoji(city.countryCode) }}</span>
              <div>
                <h2 class="cce-explore__city-name">{{ city.name }}</h2>
                <p class="cce-explore__city-country">{{ city.country }}</p>
              </div>
            </div>

            <dl class="cce-explore__stats">
              <div class="cce-explore__stat">
                <dt>{{ 'explore.stats.population' | translate }}</dt>
                <dd>{{ formatPopulation(city.population) }}</dd>
              </div>
              <div class="cce-explore__stat">
                <dt>{{ 'explore.stats.coords' | translate }}</dt>
                <dd>{{ city.lat.toFixed(2) }}°, {{ city.lon.toFixed(2) }}°</dd>
              </div>
              <div class="cce-explore__stat">
                <dt>{{ 'explore.stats.tier' | translate }}</dt>
                <dd class="cce-explore__tier-pill cce-explore__tier-pill--{{ city.carbonTier }}">
                  {{ ('explore.legend.' + city.carbonTier) | translate }}
                </dd>
              </div>
            </dl>

            @if (isFeatured(city)) {
              <h3 class="cce-explore__section-title">{{ 'explore.summary' | translate }}</h3>
              <p class="cce-explore__summary">{{ city.summary }}</p>

              <h3 class="cce-explore__section-title">{{ 'explore.initiatives' | translate }}</h3>
              <ul class="cce-explore__initiatives">
                @for (init of city.initiatives; track init) {
                  <li>{{ init }}</li>
                }
              </ul>
            } @else {
              <p class="cce-explore__minimal-note">{{ 'explore.minimalNote' | translate }}</p>
            }

            <a
              routerLink="/interactive-city"
              class="cce-explore__cta"
              [attr.aria-label]="'explore.scenarioCta' | translate"
            >
              <span>{{ 'explore.scenarioCta' | translate }}</span>
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4">
                <path d="M5 12h14M13 5l7 7-7 7" stroke-linecap="round" stroke-linejoin="round" />
              </svg>
            </a>
          </aside>
        }
      </div>
    </div>
  `,
  styleUrl: './world-map.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorldMapPage {
  readonly regions = REGIONS;
  readonly totalCities = ALL_CITIES.length;

  readonly selectedCityId = signal<string | null>(null);
  readonly searchTerm = signal('');
  readonly selectedTiers = signal<Set<CarbonTier>>(new Set(['low', 'medium', 'high']));
  readonly selectedRegion = signal<RegionId>('all');

  readonly filteredCities = computed<readonly AnyCity[]>(() => {
    const term = this.searchTerm().trim().toLowerCase();
    const tiers = this.selectedTiers();
    const region = this.selectedRegion();
    return ALL_CITIES.filter((c) => {
      if (!tiers.has(c.carbonTier)) return false;
      if (region !== 'all' && COUNTRY_REGION[c.countryCode] !== region) return false;
      if (term && !c.name.toLowerCase().includes(term) && !c.country.toLowerCase().includes(term)) return false;
      return true;
    });
  });

  readonly visibleCityIds = computed<readonly string[] | null>(() => {
    if (!this.hasFilters()) return null;
    return this.filteredCities().map((c) => c.id);
  });

  readonly stats = computed(() => {
    const cities = this.filteredCities();
    const totalPop = cities.reduce((sum, c) => sum + c.population, 0);
    const byTier = { low: 0, medium: 0, high: 0 };
    const regionSet = new Set<string>();
    for (const c of cities) {
      byTier[c.carbonTier]++;
      const r = COUNTRY_REGION[c.countryCode];
      if (r) regionSet.add(r);
    }
    return { shown: cities.length, totalPop, byTier, regionCount: regionSet.size };
  });

  readonly selectedCity = computed<AnyCity | null>(() => {
    const id = this.selectedCityId();
    if (!id) return null;
    return ALL_CITIES.find((c) => c.id === id) ?? null;
  });

  hasFilters(): boolean {
    return (
      this.searchTerm().trim() !== '' ||
      this.selectedRegion() !== 'all' ||
      this.selectedTiers().size !== 3
    );
  }

  toggleTier(tier: CarbonTier): void {
    const next = new Set(this.selectedTiers());
    if (next.has(tier)) next.delete(tier);
    else next.add(tier);
    if (next.size === 0) next.add(tier);
    this.selectedTiers.set(next);
  }

  resetFilters(): void {
    this.searchTerm.set('');
    this.selectedTiers.set(new Set(['low', 'medium', 'high']));
    this.selectedRegion.set('all');
  }

  onCityClicked(city: AnyCity): void {
    this.selectedCityId.set(city.id);
  }

  closePanel(): void {
    this.selectedCityId.set(null);
  }

  isFeatured(city: AnyCity): city is FeaturedCity {
    return city.kind === 'featured';
  }

  formatPopulation(n: number): string {
    if (n >= 1_000_000_000) return `${(n / 1_000_000_000).toFixed(1)}B`;
    if (n >= 1_000_000) return `${(n / 1_000_000).toFixed(1)}M`;
    if (n >= 1_000) return `${(n / 1_000).toFixed(0)}K`;
    return n.toString();
  }

  flagEmoji(cc: string): string {
    if (!cc || cc.length !== 2) return '';
    const A = 0x1f1e6;
    const offset = (ch: string) => A + (ch.toUpperCase().charCodeAt(0) - 65);
    return String.fromCodePoint(offset(cc[0]), offset(cc[1]));
  }
}
