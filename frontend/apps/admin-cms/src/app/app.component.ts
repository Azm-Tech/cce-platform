import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { AppShellComponent } from '@frontend/ui-kit';
import { LocaleSwitcherComponent } from './locale-switcher/locale-switcher.component';
import { AuthToolbarComponent } from './auth-toolbar/auth-toolbar.component';

@Component({
  selector: 'cce-root',
  standalone: true,
  imports: [RouterOutlet, AppShellComponent, LocaleSwitcherComponent, AuthToolbarComponent, TranslateModule],
  templateUrl: './app.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppComponent {
  private readonly translate = inject(TranslateService);
  readonly title = this.translate.instant('common.appName') || 'CCE Admin';
}
