import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PortalShellComponent } from './core/layout/portal-shell.component';

@Component({
  selector: 'cce-root',
  standalone: true,
  imports: [PortalShellComponent],
  templateUrl: './app.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppComponent {}
