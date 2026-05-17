import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { RouterOutlet } from '@angular/router';
import { MatSidenavModule } from '@angular/material/sidenav';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { AppShellComponent } from '@frontend/ui-kit';
import { LocaleSwitcherComponent } from '@frontend/i18n';
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
  readonly title = this.translate.translate('common.appName') || 'CCE Admin';
}
