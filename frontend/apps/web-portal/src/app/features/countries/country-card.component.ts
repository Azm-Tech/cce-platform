import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { TranslocoModule } from '@jsverse/transloco';
import type { Country, CountryCardStats } from './country.types';
import { flagUrlFor, flagEmojiFor } from './flag-helpers';

@Component({
  selector: 'cce-country-card',
  standalone: true,
  imports: [RouterLink, MatButtonModule, TranslocoModule],
  template: `
    <div class="cce-country-card">
      <div class="cce-country-card__flag-wrap" aria-hidden="true">
        @if (!imgFailed()) {
          <img
            class="cce-country-card__flag"
            [src]="flagSrc()"
            [alt]="name()"
            width="40"
            height="28"
            loading="lazy"
            referrerpolicy="no-referrer"
            (error)="onImgError()"
          />
        } @else {
          <span class="cce-country-card__flag-emoji">{{ flagEmoji() }}</span>
        }
      </div>

      <div class="cce-country-card__name">{{ name() }}</div>

      @if (stats(); as s) {
        <div class="cce-country-card__stat">
          <span class="cce-country-card__stat-label">{{ 'countries.card.emissionReduction' | transloco }}</span>
          <span class="cce-country-card__stat-value">
            <span class="cce-country-card__trend" [class.cce-country-card__trend--up]="s.emissionTrend === 'up'" [class.cce-country-card__trend--down]="s.emissionTrend === 'down'">
              {{ s.emissionTrend === 'up' ? '↗' : s.emissionTrend === 'down' ? '↘' : '→' }}
            </span>
            {{ s.emissionReductionPct }}%
          </span>
        </div>
        <hr class="cce-country-card__divider" />
        <div class="cce-country-card__stat">
          <span class="cce-country-card__stat-label">{{ 'countries.detail.classification' | transloco }}</span>
          <span class="cce-country-card__stat-value cce-country-card__stat-value--rank">
            {{ s.globalRank }} {{ 'countries.card.of' | transloco }} {{ s.totalCountries }}
          </span>
        </div>
      }

      <a
        class="cce-country-card__btn"
        mat-stroked-button
        [routerLink]="['/countries', country().id]"
        (click)="$event.stopPropagation()"
      >
        {{ 'countries.card.viewReport' | transloco }}
      </a>
    </div>
  `,
  styleUrl: './country-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CountryCardComponent {
  readonly country = input.required<Country>();
  readonly locale = input<'ar' | 'en'>('en');
  readonly stats = input<CountryCardStats | null>(null);

  readonly imgFailed = signal(false);

  readonly name = computed(() => {
    const c = this.country();
    return this.locale() === 'ar' ? c.nameAr : c.nameEn;
  });

  readonly flagSrc = computed(() => flagUrlFor(this.country()));
  readonly flagEmoji = computed(() => flagEmojiFor(this.country().isoAlpha2));

  onImgError(): void {
    this.imgFailed.set(true);
  }
}
