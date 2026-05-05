import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { ALL_CITIES, WorldMapComponent, type AnyCity, type FeaturedCity } from './world-map.component';

/**
 * Interactive world-map page. Renders <cce-world-map> with a side
 * detail panel that slides in when a city is clicked.
 *
 * Cities come in two flavors:
 *   - "featured" (60 entries): rich metadata (initiatives + summary)
 *   - "standard" (~150 entries): basic data; panel offers a CTA to
 *     build a sustainability scenario in /interactive-city.
 */
@Component({
  selector: 'cce-world-map-page',
  standalone: true,
  imports: [CommonModule, RouterLink, WorldMapComponent, TranslateModule],
  template: `
    <div class="cce-explore">
      <header class="cce-explore__header">
        <h1 class="cce-explore__title">{{ 'explore.title' | translate }}</h1>
        <p class="cce-explore__subtitle">{{ 'explore.subtitle' | translate }}</p>
        <div class="cce-explore__legend">
          <span class="cce-explore__legend-item cce-explore__legend-item--low">
            <span class="cce-explore__dot"></span>{{ 'explore.legend.low' | translate }}
          </span>
          <span class="cce-explore__legend-item cce-explore__legend-item--medium">
            <span class="cce-explore__dot"></span>{{ 'explore.legend.medium' | translate }}
          </span>
          <span class="cce-explore__legend-item cce-explore__legend-item--high">
            <span class="cce-explore__dot"></span>{{ 'explore.legend.high' | translate }}
          </span>
          <span class="cce-explore__count">{{ cityCount }} {{ 'explore.cities' | translate }}</span>
        </div>
        <p class="cce-explore__hint">{{ 'explore.zoomHint' | translate }}</p>
      </header>

      <div class="cce-explore__stage">
        <div class="cce-explore__map-wrap">
          <cce-world-map
            [selectedCityId]="selectedCityId()"
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
  readonly selectedCityId = signal<string | null>(null);
  readonly selectedCity = computed<AnyCity | null>(() => {
    const id = this.selectedCityId();
    if (!id) return null;
    return ALL_CITIES.find((c) => c.id === id) ?? null;
  });
  readonly cityCount = ALL_CITIES.length;

  onCityClicked(city: AnyCity): void {
    this.selectedCityId.set(city.id);
  }

  closePanel(): void {
    this.selectedCityId.set(null);
  }

  /** Type guard: featured cities (from cities.data.ts) carry summary + initiatives. */
  isFeatured(city: AnyCity): city is FeaturedCity {
    return city.kind === 'featured';
  }

  formatPopulation(n: number): string {
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
