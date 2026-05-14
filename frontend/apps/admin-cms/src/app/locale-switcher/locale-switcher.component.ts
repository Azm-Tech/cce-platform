import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { LocaleService, type SupportedLocale } from '@frontend/i18n';

@Component({
  selector: 'cce-locale-switcher',
  standalone: true,
  imports: [MatButtonModule, TranslocoModule],
  templateUrl: './locale-switcher.component.html',
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
