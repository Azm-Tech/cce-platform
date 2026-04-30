import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { TranslateModule } from '@ngx-translate/core';
import type { Country } from './country.types';

@Component({
  selector: 'cce-country-card',
  standalone: true,
  imports: [CommonModule, RouterLink, MatCardModule, TranslateModule],
  template: `
    <a class="cce-country-card" [routerLink]="['/countries', country().id]">
      <mat-card>
        <div class="cce-country-card__body">
          @if (country().flagUrl) {
            <img
              class="cce-country-card__flag"
              [src]="country().flagUrl"
              [alt]="name()"
              width="48"
              height="32"
              loading="lazy"
            />
          }
          <div class="cce-country-card__text">
            <div class="cce-country-card__name">{{ name() }}</div>
            <div class="cce-country-card__iso">{{ country().isoAlpha3 }}</div>
          </div>
        </div>
      </mat-card>
    </a>
  `,
  styleUrl: './country-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CountryCardComponent {
  readonly country = input.required<Country>();
  readonly locale = input<'ar' | 'en'>('en');

  readonly name = computed(() => {
    const c = this.country();
    return this.locale() === 'ar' ? c.nameAr : c.nameEn;
  });
}
