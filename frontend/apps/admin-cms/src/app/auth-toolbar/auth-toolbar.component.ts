import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { TranslateModule } from '@ngx-translate/core';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';

@Component({
  selector: 'cce-auth-toolbar',
  standalone: true,
  imports: [CommonModule, MatButtonModule, TranslateModule],
  templateUrl: './auth-toolbar.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuthToolbarComponent {
  private readonly oidc = inject(OidcSecurityService);

  // isAuthenticated$ is provided by angular-auth-oidc-client; map to a plain bool signal.
  readonly isAuthenticated = toSignal(
    this.oidc.isAuthenticated$.pipe(map((v) => v.isAuthenticated)),
    { initialValue: false },
  );

  signIn(): void {
    this.oidc.authorize();
  }

  signOut(): void {
    this.oidc.logoff().subscribe();
  }
}
