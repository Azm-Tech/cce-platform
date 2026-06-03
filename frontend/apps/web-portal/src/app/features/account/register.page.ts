import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { TranslocoModule } from '@jsverse/transloco';
import { toApiFieldErrors, PASSWORD_STRENGTH_VALIDATORS } from '@frontend/ui-kit';
import { LocaleService } from '@frontend/i18n';
import { AuthApiService } from '../../core/auth/auth-api.service';
import { AuthService } from '../../core/auth/auth.service';
import { CountriesApiService } from '../countries/countries-api.service';
import type { CountryCode } from '../countries/country.types';

function passwordsMatch(group: AbstractControl) {
  const p = group.get('password')?.value as string;
  const c = group.get('confirmPassword')?.value as string;
  return p === c ? null : { passwordMismatch: true };
}

type SubmitState =
  | { kind: 'idle' }
  | { kind: 'submitting' }
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
    MatSelectModule,
    TranslocoModule,
  ],
  templateUrl: './register.page.html',
  styleUrl: './register.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterPage implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly authApi = inject(AuthApiService);
  private readonly countriesApi = inject(CountriesApiService);
  private readonly router = inject(Router);
  readonly locale = inject(LocaleService).locale;

  readonly isAuthenticated = this.auth.isAuthenticated;
  readonly state = signal<SubmitState>({ kind: 'idle' });
  readonly showPassword = signal(false);
  readonly countryCodes = signal<CountryCode[]>([]);

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
      countryCodeId: new FormControl('', [Validators.required]),
      phoneCountryCodeId: new FormControl('', [Validators.required]),
      phoneNumber: new FormControl('', [
        Validators.required,
        Validators.maxLength(15),
        Validators.pattern(/^[\d\s\-()]+$/),
      ]),
      password: new FormControl('', [Validators.required, ...PASSWORD_STRENGTH_VALIDATORS]),
      confirmPassword: new FormControl('', [Validators.required]),
    },
    { validators: passwordsMatch },
  );

  async ngOnInit(): Promise<void> {
    const res = await this.countriesApi.listCountryCodes({ isActive: true });
    if (res.ok) this.countryCodes.set(res.value);
  }

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
    const phoneCode = this.countryCodes().find((cc) => cc.id === v.phoneCountryCodeId);
    const dial = phoneCode?.dialCode?.replace(/^\+/, '') ?? '';
    const localPhone = (v.phoneNumber ?? '').replace(/\s/g, '');
    const fullPhone = phoneCode ? `${dial}${localPhone}` : localPhone;
    this.authApi
      .register({
        firstName: v.firstName!,
        lastName: v.lastName!,
        emailAddress: v.emailAddress!,
        jobTitle: v.jobTitle!,
        organizationName: v.organizationName!,
        countryCodeId: v.countryCodeId!,
        phoneNumber: fullPhone,
        password: v.password!,
        confirmPassword: v.confirmPassword!,
      })
      .subscribe({
        next: () =>
          this.router.navigate(['/verify-phone'], {
            state: { phoneNumber: fullPhone },
          }),
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
