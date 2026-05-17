import { ChangeDetectionStrategy, Component, inject } from '@angular/core';

import { MatButtonModule } from '@angular/material/button';
import { TranslocoModule } from '@jsverse/transloco';
import { DevAuthService } from '../auth/dev-auth.service';

/**
 * Auth toolbar — shows sign-in / sign-out + the current dev role label.
 *
 * Uses the cookie-based DevAuthService (replaces OidcSecurityService)
 * which reads the `cce-dev-role` cookie set by the BFF's
 * `/dev/sign-in?role=...` shim. No real OIDC provider needed in dev.
 */
@Component({
  selector: 'cce-auth-toolbar',
  standalone: true,
  imports: [MatButtonModule, TranslocoModule],
  templateUrl: './auth-toolbar.component.html',
  styleUrl: './auth-toolbar.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuthToolbarComponent {
  private readonly auth = inject(DevAuthService);

  readonly isAuthenticated = this.auth.isAuthenticated;
  readonly displayLabel = this.auth.displayLabel;

  signIn(): void {
    this.auth.signIn('cce-admin', window.location.pathname);
  }

  signOut(): void {
    this.auth.signOut();
  }
}
