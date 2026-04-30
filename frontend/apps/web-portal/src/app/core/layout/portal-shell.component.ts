import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { HeaderComponent } from './header.component';
import { FooterComponent } from './footer.component';

@Component({
  selector: 'cce-portal-shell',
  standalone: true,
  imports: [RouterOutlet, HeaderComponent, FooterComponent, TranslateModule],
  templateUrl: './portal-shell.component.html',
  styleUrl: './portal-shell.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PortalShellComponent {}
