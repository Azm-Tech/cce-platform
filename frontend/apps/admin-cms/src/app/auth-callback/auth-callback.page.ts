import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';

/**
 * Public landing route for the OIDC redirect URI configured in
 * `buildCceOidcConfig` (`${origin}/auth/callback`). The
 * `angular-auth-oidc-client` library processes the URL fragment / query
 * (id_token, code, error, etc.) inside its own bootstrap flow; this page
 * just gives the router a target to match so it does not throw NG04002,
 * and renders a "signing in" message during the brief moment between
 * redirect and the library's auto-navigation back to the original route.
 *
 * No guard — anyone landing on /auth/callback during the redirect dance
 * must be allowed in, even when not yet authenticated.
 */
@Component({
  selector: 'cce-auth-callback',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  template: `
    <div class="cce-auth-callback">
      <p>{{ 'common.loading' | translate }}</p>
    </div>
  `,
  styles: [
    `:host { display: block; padding: 2rem; text-align: center; }
     .cce-auth-callback { color: rgba(0, 0, 0, 0.6); }`,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuthCallbackPage {}
