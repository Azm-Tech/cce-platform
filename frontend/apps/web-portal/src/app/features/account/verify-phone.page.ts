import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  ElementRef,
  OnDestroy,
  OnInit,
  computed,
  inject,
  signal,
  viewChildren,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { Subscription, interval } from 'rxjs';
import { map } from 'rxjs/operators';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslocoModule } from '@jsverse/transloco';
import { LocaleService } from '@frontend/i18n';
import { CountriesApiService } from '../countries/countries-api.service';
import type { Country } from '../countries/country.types';
import { AuthApiService } from '../../core/auth/auth-api.service';

type PageState = 'sending' | 'idle' | 'verifying' | 'error';

@Component({
  selector: 'cce-verify-phone',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatAutocompleteModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    TranslocoModule,
  ],
  templateUrl: './verify-phone.page.html',
  styleUrl: './verify-phone.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VerifyPhonePage implements OnInit, OnDestroy {
  private readonly router = inject(Router);
  private readonly authApi = inject(AuthApiService);
  private readonly countriesApi = inject(CountriesApiService);
  private readonly cdr = inject(ChangeDetectorRef);
  readonly locale = inject(LocaleService).locale;

  readonly step = signal<'enter-phone' | 'verify'>('verify');
  readonly state = signal<PageState>('sending');
  readonly errorKey = signal<string>('');
  readonly countdown = signal<number>(0);
  readonly phoneNumber = signal<string>('');
  readonly countryCodes = signal<Country[]>([]);

  // Dial-code autocomplete
  readonly phoneCodeControl = new FormControl<Country | null>(null, [Validators.required]);
  private readonly phoneCodeInput = toSignal(
    this.phoneCodeControl.valueChanges.pipe(map(v => (typeof v === 'string' ? v : ''))),
    { initialValue: '' },
  );
  readonly filteredPhoneCodes = computed(() => {
    const q = this.phoneCodeInput().toLowerCase();
    const all = this.countryCodes();
    if (!q) return all;
    return all.filter(
      cc =>
        cc.nameAr.toLowerCase().includes(q) ||
        cc.nameEn.toLowerCase().includes(q) ||
        cc.dialCode.includes(q),
    );
  });
  readonly displayPhoneCode = (cc: Country | null): string => {
    if (!cc) return '';
    const name = this.locale() === 'ar' ? cc.nameAr : cc.nameEn;
    return `${name} (${cc.dialCode})`;
  };

  // Local phone number
  readonly localPhoneControl = new FormControl('', [
    Validators.required,
    Validators.maxLength(15),
    Validators.pattern(/^[\d\s\-()]+$/),
  ]);

  readonly digits = Array.from({ length: 6 }, () => signal(''));
  readonly otpInputs = viewChildren<ElementRef<HTMLInputElement>>('otpInput');

  verificationId = '';
  private countdownSub?: Subscription;

  constructor() {
    const nav = this.router.getCurrentNavigation();
    const phone = (nav?.extras.state as { phoneNumber?: string })?.phoneNumber ?? '';
    if (phone) {
      this.phoneNumber.set(phone);
    } else {
      this.step.set('enter-phone');
      this.state.set('idle');
    }
  }

  ngOnInit(): void {
    if (this.step() === 'verify') {
      this.sendOtp();
    } else {
      void this.countriesApi.listCountries({ pageSize: 1000, isCceCountry: false }).then(res => {
        if (res.ok) this.countryCodes.set(res.value);
      });
    }
  }

  ngOnDestroy(): void {
    this.countdownSub?.unsubscribe();
  }

  get otpCode(): string {
    return this.digits.map(d => d()).join('');
  }

  get isComplete(): boolean {
    return this.digits.every(d => /^\d$/.test(d()));
  }

  onDigitInput(event: Event, index: number): void {
    const input = event.target as HTMLInputElement;
    const digit = input.value.replace(/\D/g, '').slice(-1);
    this.digits[index].set(digit);
    input.value = digit;
    if (digit && index < 5) this.focusBox(index + 1);
  }

  onKeyDown(event: KeyboardEvent, index: number): void {
    if (event.key === 'Backspace') {
      if (this.digits[index]()) {
        this.digits[index].set('');
      } else if (index > 0) {
        this.digits[index - 1].set('');
        this.focusBox(index - 1);
      }
    } else if (event.key === 'ArrowLeft' && index > 0) {
      this.focusBox(index - 1);
    } else if (event.key === 'ArrowRight' && index < 5) {
      this.focusBox(index + 1);
    }
  }

  onPaste(event: ClipboardEvent): void {
    event.preventDefault();
    const text = event.clipboardData?.getData('text') ?? '';
    const pasted = text.replace(/\D/g, '').slice(0, 6).split('');
    pasted.forEach((d, i) => this.digits[i]?.set(d));
    this.focusBox(Math.min(pasted.length, 5));
  }

  submitPhone(): void {
    this.phoneCodeControl.markAllAsTouched();
    this.localPhoneControl.markAllAsTouched();
    if (this.phoneCodeControl.invalid || this.localPhoneControl.invalid || this.state() === 'sending') return;
    const phoneCode = this.phoneCodeControl.value as Country;
    const dial = phoneCode.dialCode.replace(/^\+/, '');
    const local = this.localPhoneControl.value!.replace(/\s/g, '');
    this.phoneNumber.set(`${dial}${local}`);
    this.step.set('verify');
    this.sendOtp();
  }

  resend(): void {
    if (this.countdown() > 0 || this.state() === 'sending') return;
    this.digits.forEach(d => d.set(''));
    this.sendOtp();
  }

  submit(): void {
    if (!this.isComplete || this.state() === 'verifying') return;
    this.errorKey.set('');
    this.state.set('verifying');
    this.authApi.verifyPhoneOtp(this.verificationId, this.otpCode).subscribe({
      next: () => this.router.navigate(['/login']),
      error: () => {
        this.errorKey.set('account.verifyPhone.errorInvalid');
        this.state.set('idle');
      },
    });
  }

  private sendOtp(): void {
    this.state.set('sending');
    this.errorKey.set('');
    this.authApi.requestPhoneOtp(this.phoneNumber()).subscribe({
      next: (res) => {
        this.verificationId = res.verificationId;
        this.state.set('idle');
        this.startCountdown();
        setTimeout(() => this.focusBox(0));
      },
      error: (err: { apiCode?: string }) => {
        if (err?.apiCode === 'ERR124') {
          // Rate-limited — keep the OTP input visible, restart the countdown
          this.errorKey.set('account.verifyPhone.errorRateLimit');
          this.state.set(this.verificationId ? 'idle' : 'error');
          this.startCountdown();
        } else {
          this.errorKey.set('account.verifyPhone.errorGeneric');
          this.state.set('error');
        }
      },
    });
  }

  private focusBox(index: number): void {
    this.otpInputs()[index]?.nativeElement.focus();
  }

  private startCountdown(): void {
    this.countdownSub?.unsubscribe();
    this.countdown.set(60);
    this.countdownSub = interval(1000).subscribe(() => {
      const next = this.countdown() - 1;
      if (next <= 0) {
        this.countdown.set(0);
        this.countdownSub?.unsubscribe();
      } else {
        this.countdown.set(next);
      }
      this.cdr.markForCheck();
    });
  }
}
