import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { TranslateModule } from '@ngx-translate/core';
import { DevAuthService } from '../core/auth/dev-auth.service';

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
  imports: [CommonModule, MatButtonModule, TranslateModule],
  templateUrl: './auth-toolbar.component.html',
  styleUrl: './auth-toolbar.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuthToolbarComponent {
  private readonly auth = inject(DevAuthService);

  readonly isAuthenticated = this.auth.isAuthenticated;
  readonly displayLabel = this.auth.displayLabel;

  signIn(): void {
    // Default to platform-admin role; the dev sign-in cookie shim
    // bounces back to the current page after setting the cookie.
    this.auth.signIn('cce-admin', window.location.pathname);
  }

  signOut(): void {
    this.auth.signOut();
  }
}
