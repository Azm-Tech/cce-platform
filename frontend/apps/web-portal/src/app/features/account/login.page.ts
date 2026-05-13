import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth.service';

interface LoginFormModel {
  email: string;
  password: string;
}

type LoginState =
  | { kind: 'idle' }
  | { kind: 'submitting' }
  | { kind: 'error'; messageKey: string };

/**
 * Public sign-in page.
 *
 * The page renders a real-looking email/password form so users have a
 * familiar mental model when arriving from the header's "Sign in"
 * button. On submit, we hand off to the BFF dev-mode shim by calling
 * `AuthService.signIn(returnUrl)` — that hits `/auth/login`, follows
 * the redirect chain through `/dev/sign-in?role=...`, sets the
 * `cce-dev-role` cookie, and lands the user back at `returnUrl`.
 *
 * Quick role buttons sit beneath the form so demos can switch personas
 * (cce-user, cce-pro-user, cce-admin, cce-cms-editor, cce-cms-author)
 * without re-typing credentials. A "Don't have an account? Register"
 * link routes to `/register`.
 */
@Component({
  selector: 'cce-login',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    FormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    TranslateModule,
  ],
  template: `
    <section class="cce-login">
      <div class="cce-login__card">
        <div class="cce-login__brand">
          <span class="cce-login__brand-glyph" aria-hidden="true"></span>
          <span class="cce-login__brand-name">CCE</span>
        </div>

        <h1 class="cce-login__title">{{ 'account.login.title' | translate }}</h1>
        <p class="cce-login__subtitle">{{ 'account.login.subtitle' | translate }}</p>

        @if (isAuthenticated()) {
          <div class="cce-login__already">
            <p>{{ 'account.login.alreadySignedIn' | translate }}</p>
            <a mat-flat-button color="primary" routerLink="/me/profile">
              {{ 'account.login.openProfile' | translate }}
            </a>
          </div>
        } @else {
          <form #form="ngForm" class="cce-login__form" (ngSubmit)="submit(form)">
            <mat-form-field appearance="outline">
              <mat-label>{{ 'account.login.emailLabel' | translate }}</mat-label>
              <mat-icon matPrefix>mail</mat-icon>
              <input
                matInput
                type="email"
                name="email"
                autocomplete="email"
                [(ngModel)]="model.email"
                required
                placeholder="you@example.com"
              />
            </mat-form-field>

            <mat-form-field appearance="outline">
              <mat-label>{{ 'account.login.passwordLabel' | translate }}</mat-label>
              <mat-icon matPrefix>lock</mat-icon>
              <input
                matInput
                [type]="showPassword() ? 'text' : 'password'"
                name="password"
                autocomplete="current-password"
                [(ngModel)]="model.password"
                required
                minlength="1"
              />
              <button
                type="button"
                mat-icon-button
                matSuffix
                (click)="toggleShowPassword()"
                [attr.aria-label]="(showPassword() ? 'account.login.hidePassword' : 'account.login.showPassword') | translate"
              >
                <mat-icon>{{ showPassword() ? 'visibility_off' : 'visibility' }}</mat-icon>
              </button>
            </mat-form-field>

            <div class="cce-login__row">
              <a class="cce-login__forgot" href="#" (click)="$event.preventDefault()">
                {{ 'account.login.forgotPassword' | translate }}
              </a>
            </div>

            @if (state().kind === 'error') {
              <p class="cce-login__error" role="alert">
                {{ errorMessageKey() | translate }}
              </p>
            }

            <button
              type="submit"
              mat-flat-button
              color="primary"
              class="cce-login__submit"
              [disabled]="state().kind === 'submitting' || form.invalid"
            >
              {{ submitButtonKey() | translate }}
            </button>
          </form>

          <div class="cce-login__divider">
            <span>{{ 'account.login.orDemoAs' | translate }}</span>
          </div>

          <div class="cce-login__roles">
            @for (r of demoRoles; track r.role) {
              <button
                type="button"
                class="cce-login__role"
                [attr.data-role]="r.role"
                (click)="signInAs(r.role)"
              >
                <span class="cce-login__role-dot"></span>
                <span class="cce-login__role-label">{{ r.label }}</span>
                <span class="cce-login__role-desc">{{ r.desc }}</span>
              </button>
            }
          </div>

          <p class="cce-login__register-line">
            {{ 'account.login.noAccount' | translate }}
            <a routerLink="/register" class="cce-login__register-link">
              {{ 'account.login.register' | translate }}
            </a>
          </p>
        }
      </div>
    </section>
  `,
  styleUrl: './login.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginPage {
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly isAuthenticated = this.auth.isAuthenticated;
  readonly state = signal<LoginState>({ kind: 'idle' });
  readonly showPassword = signal(false);

  model: LoginFormModel = { email: '', password: '' };

  /**
   * The five dev-mode roles supported by the BFF's `/dev/sign-in`
   * handler — verified against the backend's valid-role list:
   *   cce-admin · cce-editor · cce-reviewer · cce-expert · cce-user
   * Each maps to a deterministic seeded user so the demo can switch
   * personas instantly.
   */
  readonly demoRoles: ReadonlyArray<{ role: string; label: string; desc: string }> = [
    { role: 'cce-user',     label: 'End User',     desc: 'Browse + community' },
    { role: 'cce-expert',   label: 'Verified Expert', desc: 'Trusted contributor' },
    { role: 'cce-reviewer', label: 'Reviewer',     desc: 'Validate submissions' },
    { role: 'cce-editor',   label: 'CMS Editor',   desc: 'Manage content' },
    { role: 'cce-admin',    label: 'Platform Admin', desc: 'Full access' },
  ];

  toggleShowPassword(): void {
    this.showPassword.update((v) => !v);
  }

  submit(form: NgForm): void {
    if (form.invalid || this.state().kind === 'submitting') return;
    this.state.set({ kind: 'submitting' });
    // Email/password is cosmetic in dev mode — the BFF doesn't validate
    // them. Treat any non-empty submission as "sign me in as the
    // default end-user role". Real production would call a credential
    // exchange endpoint here.
    this.signInAs('cce-user');
  }

  signInAs(role: string): void {
    // Admin / Editor / Reviewer personas live in the admin-cms app
    // (port 4201). The web-portal at /:4200 is the public end-user
    // surface, so signing in as a back-office role on the public app
    // would land on a profile page with no admin tools. Route those
    // personas to the admin-cms; the admin app's BFF (5002) sets its
    // OWN cookie and lands the user on /profile inside admin-cms.
    const adminPersonas = new Set(['cce-admin', 'cce-editor', 'cce-reviewer']);
    if (adminPersonas.has(role)) {
      const adminBase = `${window.location.protocol}//${window.location.hostname}:4201`;
      window.location.assign(
        `${adminBase}/dev/sign-in?role=${encodeURIComponent(role)}&returnUrl=${encodeURIComponent('/profile')}`,
      );
      return;
    }
    // End-user / verified-expert personas stay on the public web-portal.
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/';
    window.location.assign(
      `/dev/sign-in?role=${encodeURIComponent(role)}&returnUrl=${encodeURIComponent(returnUrl)}`,
    );
  }

  errorMessageKey(): string {
    const s = this.state();
    return s.kind === 'error' ? s.messageKey : '';
  }

  submitButtonKey(): string {
    return this.state().kind === 'submitting'
      ? 'account.login.submittingButton'
      : 'account.login.submitButton';
  }
}
