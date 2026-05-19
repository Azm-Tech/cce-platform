import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormControl, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { TranslocoModule } from '@jsverse/transloco';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { AuthApiService } from '../../core/auth/auth-api.service';

type LoginState =
  | { kind: 'idle' }
  | { kind: 'submitting' }
  | { kind: 'error'; messageKey: string };

@Component({
  selector: 'cce-login',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    TranslocoModule,
  ],
  template: `
    <section class="cce-login">
      <div class="cce-login__card">
        <div class="cce-login__brand">
          <span class="cce-login__brand-glyph" aria-hidden="true"></span>
          <span class="cce-login__brand-name">CCE</span>
        </div>

        <h1 class="cce-login__title">{{ 'account.login.title' | transloco }}</h1>
        <p class="cce-login__subtitle">{{ 'account.login.subtitle' | transloco }}</p>

        @if (isAuthenticated()) {
          <div class="cce-login__already">
            <p>{{ 'account.login.alreadySignedIn' | transloco }}</p>
            <a mat-flat-button color="primary" routerLink="/me/profile">
              {{ 'account.login.openProfile' | transloco }}
            </a>
          </div>
        } @else {
          <form [formGroup]="form" class="cce-login__form" (ngSubmit)="submit()">
            <mat-form-field>
              <mat-label>{{ 'account.login.emailLabel' | transloco }}</mat-label>
              <mat-icon matPrefix>mail</mat-icon>
              <input
                matInput
                type="email"
                formControlName="emailAddress"
                autocomplete="email"
                placeholder="you@example.com"
              />
            </mat-form-field>

            <mat-form-field>
              <mat-label>{{ 'account.login.passwordLabel' | transloco }}</mat-label>
              <mat-icon matPrefix>lock</mat-icon>
              <input
                matInput
                [type]="showPassword() ? 'text' : 'password'"
                formControlName="password"
                autocomplete="current-password"
              />
              <button
                type="button"
                mat-icon-button
                matSuffix
                (click)="toggleShowPassword()"
                [attr.aria-label]="(showPassword() ? 'account.login.hidePassword' : 'account.login.showPassword') | transloco"
              >
                <mat-icon>{{ showPassword() ? 'visibility_off' : 'visibility' }}</mat-icon>
              </button>
            </mat-form-field>

            <div class="cce-login__row">
              <a class="cce-login__forgot" routerLink="/forgot-password">
                {{ 'account.login.forgotPassword' | transloco }}
              </a>
            </div>

            @if (state().kind === 'error') {
              <p class="cce-login__error" role="alert">
                {{ errorMessageKey() | transloco }}
              </p>
            }

            <button
              type="submit"
              mat-flat-button
              color="primary"
              class="cce-login__submit"
              [disabled]="state().kind === 'submitting' || form.invalid"
            >
              {{ submitButtonKey() | transloco }}
            </button>
          </form>

          <p class="cce-login__register-line">
            {{ 'account.login.noAccount' | transloco }}
            <a routerLink="/register" class="cce-login__register-link">
              {{ 'account.login.register' | transloco }}
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
  private readonly authApi = inject(AuthApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly isAuthenticated = this.auth.isAuthenticated;
  readonly state = signal<LoginState>({ kind: 'idle' });
  readonly showPassword = signal(false);

  readonly form = new FormGroup({
    emailAddress: new FormControl('', [Validators.required, Validators.email]),
    password: new FormControl('', [Validators.required]),
  });

  toggleShowPassword(): void {
    this.showPassword.update((v) => !v);
  }

  async submit(): Promise<void> {
    if (this.form.invalid || this.state().kind === 'submitting') return;
    this.state.set({ kind: 'submitting' });
    try {
      const tokens = await firstValueFrom(
        this.authApi.login({
          emailAddress: this.form.value.emailAddress!,
          password: this.form.value.password!,
        }),
      );
      this.auth.setSession(tokens);
      const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/';
      void this.router.navigateByUrl(returnUrl);
    } catch (err) {
      const status = (err as HttpErrorResponse).status;
      if (status === 400 || status === 401) {
        this.state.set({ kind: 'error', messageKey: 'account.login.errorInvalid' });
      } else if (status === 404) {
        this.state.set({ kind: 'error', messageKey: 'account.login.errorNotFound' });
      } else {
        this.state.set({ kind: 'error', messageKey: 'account.login.errorGeneric' });
      }
    }
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
