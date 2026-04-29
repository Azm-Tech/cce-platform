import { ChangeDetectionStrategy, Component } from '@angular/core';
import { ShellComponent } from './core/layout/shell.component';

@Component({
  selector: 'cce-root',
  standalone: true,
  imports: [ShellComponent],
  templateUrl: './app.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppComponent {}
