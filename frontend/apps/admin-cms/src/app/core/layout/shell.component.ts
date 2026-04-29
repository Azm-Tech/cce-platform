import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { MatSidenavModule } from '@angular/material/sidenav';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { AppShellComponent } from '@frontend/ui-kit';
import { LocaleSwitcherComponent } from '../../locale-switcher/locale-switcher.component';
import { AuthToolbarComponent } from '../../auth-toolbar/auth-toolbar.component';
import { SideNavComponent } from './side-nav.component';

@Component({
  selector: 'cce-shell',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    MatSidenavModule,
    AppShellComponent,
    SideNavComponent,
    LocaleSwitcherComponent,
    AuthToolbarComponent,
    TranslateModule,
  ],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShellComponent {
  private readonly translate = inject(TranslateService);
  readonly title = this.translate.instant('common.appName') || 'CCE Admin';
}
