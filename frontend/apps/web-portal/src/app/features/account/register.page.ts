import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth.service';

/**
 * Public landing page that hands off to Keycloak's registration flow.
 *
 * `POST /api/users/register` is a 302-redirect endpoint — the SPA
 * cannot follow a 302 cross-origin via fetch (the browser blocks the
 * redirect for `mode: 'cors'`). Instead, this page renders a primary
 * button that calls `window.location.assign('/api/users/register')`,
 * letting the browser follow the redirect natively.
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
        <button
          type="button"
          mat-flat-button
          color="primary"
          (click)="continueToSignUp()"
        >
          {{ 'account.register.continueButton' | translate }}
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

  continueToSignUp(): void {
    window.location.assign('/api/users/register');
  }
}
