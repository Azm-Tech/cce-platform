import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';

import { RouterOutlet } from '@angular/router';
import { MatSidenavModule } from '@angular/material/sidenav';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { AppShellComponent } from '@frontend/ui-kit';
import { LocaleSwitcherComponent, LocaleService } from '@frontend/i18n';
import { AuthToolbarComponent } from './auth-toolbar.component';
import { SideNavComponent } from './side-nav.component';

@Component({
  selector: 'cce-shell',
  standalone: true,
  imports: [
    RouterOutlet,
    MatSidenavModule,
    AppShellComponent,
    SideNavComponent,
    LocaleSwitcherComponent,
    AuthToolbarComponent,
    TranslocoModule
],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShellComponent {
  private readonly translate = inject(TranslocoService);
  private readonly localeService = inject(LocaleService);
  readonly title = toSignal(this.translate.selectTranslate('common.appName'), { initialValue: 'CCE' });
  readonly dir = computed(() => this.localeService.locale() === 'ar' ? 'rtl' : 'ltr');
}
