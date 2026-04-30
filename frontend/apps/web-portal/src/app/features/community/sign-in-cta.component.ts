import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth.service';

/**
 * Anonymous-friendly write-affordance placeholder. Drop in place of
 * compose / reply / rate controls when `auth.isAuthenticated() === false`.
 *
 * Calls `auth.signIn(currentUrl)` so the user lands back on the same
 * page after the BFF round-trip through Keycloak.
 */
@Component({
  selector: 'cce-sign-in-cta',
  standalone: true,
  imports: [CommonModule, MatButtonModule, TranslateModule],
  template: `
    <div class="cce-sign-in-cta" role="status">
      <p class="cce-sign-in-cta__message">{{ messageKey() | translate }}</p>
      <button
        type="button"
        mat-flat-button
        color="primary"
        (click)="signIn()"
      >
        {{ 'community.signInButton' | translate }}
      </button>
    </div>
  `,
  styleUrl: './sign-in-cta.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SignInCtaComponent {
  private readonly auth = inject(AuthService);

  /** i18n key. Default is the generic "Sign in to post". Override per context. */
  readonly messageKey = input<string>('community.signInToPost');

  signIn(): void {
    this.auth.signIn(window.location.pathname + window.location.search);
  }
}
