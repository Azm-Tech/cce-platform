import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { LocaleService, type SupportedLocale } from '../locale.service';

@Component({
  selector: 'cce-locale-switcher',
  standalone: true,
  imports: [MatButtonModule, TranslocoModule],
  template: `
    <button
      type="button"
      mat-button
      shellHeaderEnd
      (click)="toggle()"
      [attr.aria-label]="('common.locale.switchTo' | transloco) + ' ' + ('common.locale.' + nextLabel() | transloco)"
    >
      {{ "common.locale." + nextLabel() | transloco }}
    </button>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LocaleSwitcherComponent {
  private readonly localeService = inject(LocaleService);
  private readonly translate = inject(TranslocoService);

  readonly current = this.localeService.locale;
  readonly nextLabel = computed<SupportedLocale>(() => (this.current() === 'ar' ? 'en' : 'ar'));

  toggle(): void {
    const next: SupportedLocale = this.nextLabel();
    this.localeService.setLocale(next);
    this.translate.setActiveLang(next);
  }
}
