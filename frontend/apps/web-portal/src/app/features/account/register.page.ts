import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { TranslocoModule } from '@jsverse/transloco';
import { toApiFieldErrors, PASSWORD_STRENGTH_VALIDATORS } from '@frontend/ui-kit';
import { AuthApiService } from '../../core/auth/auth-api.service';
import { AuthService } from '../../core/auth/auth.service';

function passwordsMatch(group: AbstractControl) {
  const p = group.get('password')?.value as string;
  const c = group.get('confirmPassword')?.value as string;
  return p === c ? null : { passwordMismatch: true };
}

type SubmitState =
  | { kind: 'idle' }
  | { kind: 'submitting' }
  | { kind: 'success' }
  | { kind: 'error'; messageKey: string };


@Component({
  selector: 'cce-register',
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
  templateUrl: './register.page.html',
  styleUrl: './register.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterPage {
  private readonly auth = inject(AuthService);
  private readonly authApi = inject(AuthApiService);

  readonly isAuthenticated = this.auth.isAuthenticated;
  readonly state = signal<SubmitState>({ kind: 'idle' });
  readonly showPassword = signal(false);

  readonly form = new FormGroup(
    {
      firstName: new FormControl('', [
        Validators.required,
        Validators.maxLength(50),
        Validators.pattern(/^[a-zA-Z؀-ۿ\s'\-]+$/),
      ]),
      lastName: new FormControl('', [
        Validators.required,
        Validators.maxLength(50),
        Validators.pattern(/^[a-zA-Z؀-ۿ\s'\-]+$/),
      ]),
      emailAddress: new FormControl('', [
        Validators.required,
        Validators.email,
        Validators.maxLength(100),
      ]),
      jobTitle: new FormControl('', [Validators.required, Validators.maxLength(50)]),
      organizationName: new FormControl('', [Validators.required, Validators.maxLength(100)]),
      phoneNumber: new FormControl('', [
        Validators.required,
        Validators.maxLength(15),
        Validators.pattern(/^\+?[\d\s\-()]+$/),
      ]),
      password: new FormControl('', [Validators.required, ...PASSWORD_STRENGTH_VALIDATORS]),
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
    const ctrl = this.form.get('password');
    return (ctrl?.touched ?? false) && (ctrl?.hasError('pattern') ?? false);
  }

  toggleShowPassword(): void {
    this.showPassword.update((v) => !v);
  }

  submit(): void {
    if (this.form.invalid || this.state().kind === 'submitting') return;
    this.state.set({ kind: 'submitting' });
    const v = this.form.value;
    this.authApi
      .register({
        firstName: v.firstName!,
        lastName: v.lastName!,
        emailAddress: v.emailAddress!,
        jobTitle: v.jobTitle!,
        organizationName: v.organizationName!,
        phoneNumber: v.phoneNumber!,
        password: v.password!,
        confirmPassword: v.confirmPassword!,
      })
      .subscribe({
        next: () => this.state.set({ kind: 'success' }),
        error: (err: HttpErrorResponse) => {
          if (err.status === 409) {
            this.state.set({ kind: 'error', messageKey: 'account.register.errorConflict' });
          } else if (err.status === 400) {
            const fieldErrors = toApiFieldErrors(err);
            if (Object.keys(fieldErrors).length > 0) {
              for (const [field, message] of Object.entries(fieldErrors)) {
                this.form.get(field)?.setErrors({ serverError: message });
              }
              this.state.set({ kind: 'idle' });
            } else {
              this.state.set({ kind: 'error', messageKey: 'account.register.errorValidation' });
            }
          } else {
            this.state.set({ kind: 'error', messageKey: 'account.register.errorGeneric' });
          }
        },
      });
  }

  clearServerError(field: string): void {
    const ctrl = this.form.get(field);
    if (!ctrl?.hasError('serverError')) return;
    const { serverError: _, ...remaining } = ctrl.errors!;
    ctrl.setErrors(Object.keys(remaining).length ? remaining : null);
  }

errorMessageKey(): string {
    const s = this.state();
    return s.kind === 'error' ? s.messageKey : '';
  }

  submitButtonKey(): string {
    return this.state().kind === 'submitting'
      ? 'account.register.submittingButton'
      : 'account.register.submitButton';
  }
}
