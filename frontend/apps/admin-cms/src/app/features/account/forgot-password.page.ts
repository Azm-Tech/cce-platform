import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormControl, FormGroup, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HttpContext, HttpErrorResponse } from '@angular/common/http';
import { SUPPRESS_ERROR_TOAST } from '@frontend/ui-kit';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { TranslocoModule } from '@jsverse/transloco';
import { LocaleSwitcherComponent } from '@frontend/i18n';
import { AuthApiService } from '../../core/auth/auth-api.service';

type PageState =
  | { kind: 'idle' }
  | { kind: 'submitting' }
  | { kind: 'success' }
  | { kind: 'error'; messageKey: string };

@Component({
  selector: 'cce-admin-forgot-password',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    TranslocoModule,
    LocaleSwitcherComponent,
  ],
  templateUrl: './forgot-password.page.html',
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
    const context = new HttpContext().set(SUPPRESS_ERROR_TOAST, [404]);
    this.authApi.forgotPassword(this.form.value.emailAddress!, context).subscribe({
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
