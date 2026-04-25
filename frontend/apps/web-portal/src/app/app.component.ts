import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { AppShellComponent } from '@frontend/ui-kit';
import { LocaleSwitcherComponent } from './locale-switcher/locale-switcher.component';

@Component({
  selector: 'cce-root',
  standalone: true,
  imports: [RouterOutlet, AppShellComponent, LocaleSwitcherComponent, TranslateModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppComponent {
  private readonly translate = inject(TranslateService);
  readonly title = this.translate.instant('common.appName') || 'CCE';
}
