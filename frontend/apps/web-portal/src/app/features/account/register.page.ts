import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth.service';

/**
 * Public landing page for the /register route.
 *
 * Sub-11 changed CCE's IdP from Keycloak (with hosted self-service
 * registration) to multi-tenant Entra ID. Anonymous self-service
 * registration is deferred to Sub-11d (needs an IEmailSender abstraction
 * to deliver temp passwords). For now, this page tells users how to get
 * an account:
 *
 *   - Internal users (cce.local): synced via Entra ID Connect — already
 *     have accounts, click sign-in.
 *   - Partner-tenant users: sign in with their existing Entra ID tenant.
 *   - External users without an Entra ID account: contact a CCE admin.
 *
 * `POST /api/users/register` is now an admin-only Graph user-create call
 * (Sub-11 Phase 01); the public flow no longer hits it.
 */
@Component({
  selector: 'cce-register',
  standalone: true,
  imports: [CommonModule, RouterLink, MatButtonModule, TranslateModule],
  template: `
    <section class="cce-register">
      <h1 class="cce-register__title">{{ 'account.register.title' | translate }}</h1>

      @if (isAuthenticated()) {
        <p class="cce-register__body">{{ 'account.register.alreadySignedIn' | translate }}</p>
        <a mat-flat-button color="primary" routerLink="/me/profile">
          {{ 'account.register.openProfile' | translate }}
        </a>
      } @else {
        <p class="cce-register__body">{{ 'account.register.body' | translate }}</p>
        <p class="cce-register__hint">{{ 'account.register.contactHint' | translate }}</p>
        <button
          type="button"
          mat-flat-button
          color="primary"
          (click)="signIn()"
        >
          {{ 'account.register.signInButton' | translate }}
        </button>
      }
    </section>
  `,
  styleUrl: './register.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterPage {
  private readonly auth = inject(AuthService);
  readonly isAuthenticated = this.auth.isAuthenticated;

  signIn(): void {
    this.auth.signIn('/me/profile');
  }
}
