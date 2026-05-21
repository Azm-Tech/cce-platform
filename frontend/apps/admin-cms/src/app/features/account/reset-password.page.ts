import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { TranslocoModule } from '@jsverse/transloco';
import { LocaleSwitcherComponent } from '@frontend/i18n';
import { AuthApiService } from '../../core/auth/auth-api.service';

function passwordsMatch(group: AbstractControl) {
  const p = group.get('newPassword')?.value as string;
  const c = group.get('confirmPassword')?.value as string;
  return p === c ? null : { passwordMismatch: true };
}

type PageState =
  | { kind: 'idle' }
  | { kind: 'submitting' }
  | { kind: 'success' }
  | { kind: 'error'; messageKey: string };

@Component({
  selector: 'cce-admin-reset-password',
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
  templateUrl: './reset-password.page.html',
  styleUrl: './forgot-password.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResetPasswordPage {
  private readonly authApi = inject(AuthApiService);
  private readonly route = inject(ActivatedRoute);

  private readonly emailAddress = this.route.snapshot.queryParamMap.get('email') ?? '';
  private readonly token = this.route.snapshot.queryParamMap.get('token') ?? '';

  readonly state = signal<PageState>({ kind: 'idle' });
  readonly showPassword = signal(false);

  readonly form = new FormGroup(
    {
      newPassword: new FormControl('', [
        Validators.required,
        Validators.minLength(12),
        Validators.maxLength(20),
        Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$/),
      ]),
      confirmPassword: new FormControl('', [Validators.required]),
    },
    { validators: passwordsMatch },
  );

  get passwordMismatch(): boolean {
    return (
      this.form.hasError('passwordMismatch') &&
      (this.form.get('confirmPassword')?.touched ?? false)
    );
  }

  get passwordStrengthError(): boolean {
    const ctrl = this.form.get('newPassword');
    return (ctrl?.touched ?? false) && (ctrl?.hasError('pattern') ?? false);
  }

  toggleShowPassword(): void {
    this.showPassword.update((v) => !v);
  }

  submit(): void {
    if (this.form.invalid || this.state().kind === 'submitting') return;
    this.state.set({ kind: 'submitting' });
    this.authApi
      .resetPassword({
        emailAddress: this.emailAddress,
        token: this.token,
        newPassword: this.form.value.newPassword!,
        confirmPassword: this.form.value.confirmPassword!,
      })
      .subscribe({
        next: () => this.state.set({ kind: 'success' }),
        error: (err: HttpErrorResponse) => {
          if (err.status === 400 || err.status === 422) {
            this.state.set({ kind: 'error', messageKey: 'account.resetPassword.errorInvalidToken' });
          } else {
            this.state.set({ kind: 'error', messageKey: 'account.resetPassword.errorGeneric' });
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
      ? 'account.resetPassword.submittingButton'
      : 'account.resetPassword.submitButton';
  }
}
