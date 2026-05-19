import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormControl, FormGroup, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { TranslocoModule } from '@jsverse/transloco';
import { AuthApiService } from '../../core/auth/auth-api.service';

type PageState =
  | { kind: 'idle' }
  | { kind: 'submitting' }
  | { kind: 'success' }
  | { kind: 'error'; messageKey: string };

@Component({
  selector: 'cce-forgot-password',
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
    <section class="cce-auth">
      <div class="cce-auth__card">
        <div class="cce-auth__brand">
          <span class="cce-auth__brand-glyph" aria-hidden="true"></span>
          <span class="cce-auth__brand-name">CCE</span>
        </div>

        <h1 class="cce-auth__title">{{ 'account.forgotPassword.title' | transloco }}</h1>

        @if (state().kind === 'success') {
          <p class="cce-auth__subtitle">{{ 'account.forgotPassword.successBody' | transloco }}</p>
          <a routerLink="/login" mat-flat-button color="primary" class="cce-auth__submit">
            {{ 'account.forgotPassword.backToLogin' | transloco }}
          </a>
        } @else {
          <p class="cce-auth__subtitle">{{ 'account.forgotPassword.subtitle' | transloco }}</p>

          <form [formGroup]="form" class="cce-auth__form" (ngSubmit)="submit()">
            <mat-form-field>
              <mat-label>{{ 'account.forgotPassword.emailLabel' | transloco }}</mat-label>
              <mat-icon matPrefix>mail</mat-icon>
              <input
                matInput
                type="email"
                formControlName="emailAddress"
                autocomplete="email"
                placeholder="you@example.com"
              />
            </mat-form-field>

            @if (state().kind === 'error') {
              <p class="cce-auth__error" role="alert">
                {{ errorMessageKey() | transloco }}
              </p>
            }

            <button
              type="submit"
              mat-flat-button
              color="primary"
              class="cce-auth__submit"
              [disabled]="state().kind === 'submitting' || form.invalid"
            >
              {{ submitButtonKey() | transloco }}
            </button>
          </form>

          <p class="cce-auth__footer-line">
            <a routerLink="/login" class="cce-auth__footer-link">
              {{ 'account.forgotPassword.backToLogin' | transloco }}
            </a>
          </p>
        }
      </div>
    </section>
  `,
  styleUrl: './forgot-password.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ForgotPasswordPage {
  private readonly authApi = inject(AuthApiService);

  readonly state = signal<PageState>({ kind: 'idle' });

  readonly form = new FormGroup({
    emailAddress: new FormControl('', [Validators.required, Validators.email]),
  });

  submit(): void {
    if (this.form.invalid || this.state().kind === 'submitting') return;
    this.state.set({ kind: 'submitting' });
    this.authApi.forgotPassword(this.form.value.emailAddress!).subscribe({
      next: () => this.state.set({ kind: 'success' }),
      error: (err: HttpErrorResponse) => {
        if (err.status === 404) {
          this.state.set({ kind: 'error', messageKey: 'account.forgotPassword.errorNotFound' });
        } else {
          this.state.set({ kind: 'error', messageKey: 'account.forgotPassword.errorGeneric' });
        }
      },
    });
  }

  errorMessageKey(): string {
    const s = this.state();
    return s.kind === 'error' ? s.messageKey : '';
  }

  submitButtonKey(): string {
    return this.state().kind === 'submitting'
      ? 'account.forgotPassword.submittingButton'
      : 'account.forgotPassword.submitButton';
  }
}
