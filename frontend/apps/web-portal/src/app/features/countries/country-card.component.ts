import { ChangeDetectionStrategy, Component, EventEmitter, computed, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { TranslateModule } from '@ngx-translate/core';
import type { Country } from './country.types';
import { flagUrlFor, flagEmojiFor } from './flag-helpers';

@Component({
  selector: 'cce-country-card',
  standalone: true,
  imports: [CommonModule, MatCardModule, TranslateModule],
  template: `
    <button
      type="button"
      class="cce-country-card"
      [class.cce-country-card--selected]="isSelected()"
      [attr.aria-pressed]="isSelected()"
      (click)="cardClick.emit(country())"
    >
      <mat-card>
        <div class="cce-country-card__body">
          <span class="cce-country-card__flag-wrap" [attr.aria-hidden]="true">
            @if (!imgFailed()) {
              <img
                class="cce-country-card__flag"
                [src]="flagSrc()"
                [alt]="name()"
                width="48"
                height="32"
                loading="lazy"
                referrerpolicy="no-referrer"
                (error)="onImgError()"
              />
            } @else {
              <!-- Fallback: emoji flag from ISO alpha-2 -->
              <span class="cce-country-card__flag-fallback">{{ flagEmoji() }}</span>
            }
          </span>
          <div class="cce-country-card__text">
            <div class="cce-country-card__name">{{ name() }}</div>
            <div class="cce-country-card__iso">{{ country().isoAlpha3 }}</div>
          </div>
        </div>
      </mat-card>
    </button>
  `,
  styleUrl: './country-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CountryCardComponent {
  readonly country = input.required<Country>();
  readonly locale = input<'ar' | 'en'>('en');
  readonly isSelected = input<boolean>(false);

  /** Emits when the card is clicked — parent shows the detail panel
   *  inline rather than routing the user to a new page. */
  readonly cardClick = output<Country>();

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
