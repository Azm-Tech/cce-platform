import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  OnInit,
  computed,
  inject,
  signal,
  viewChildren,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslocoModule } from '@jsverse/transloco';
import { map } from 'rxjs/operators';
import { LocaleService } from '@frontend/i18n';
import { CountriesApiService } from '../countries/countries-api.service';
import type { Country } from '../countries/country.types';
import { AccountApiService } from './account-api.service';

export interface ChangeContactDialogData {
  type: 'email' | 'phone';
  currentPhone?: string | null;
}

@Component({
  selector: 'cce-change-contact-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatAutocompleteModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    TranslocoModule,
  ],
  templateUrl: './change-contact.dialog.html',
  styleUrl: './change-contact.dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChangeContactDialogComponent implements OnInit {
  private readonly api = inject(AccountApiService);
  private readonly countriesApi = inject(CountriesApiService);
  private readonly ref = inject(MatDialogRef<ChangeContactDialogComponent, boolean>);
  readonly locale = inject(LocaleService).locale;
  readonly data = inject<ChangeContactDialogData>(MAT_DIALOG_DATA);

  readonly step = signal<'request' | 'verify'>('request');
  readonly sending = signal(false);
  readonly confirming = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly countryCodes = signal<Country[]>([]);

  private verificationId = '';

  // ── Email ────────────────────────────────────────────────────
  readonly newValueControl = new FormControl('', [Validators.required, Validators.email]);

  // ── Phone: dial-code autocomplete ────────────────────────────
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

  // ── Phone: local number ──────────────────────────────────────
  readonly localPhoneControl = new FormControl('', [
    Validators.required,
    Validators.maxLength(15),
    Validators.pattern(/^[\d\s\-()]+$/),
  ]);

  readonly phoneDisplay = computed(() => {
    const cc = this.phoneCodeControl.value as Country | null;
    const local = this.localPhoneControl.value ?? '';
    if (!cc) return local;
    return `${cc.dialCode} ${local}`;
  });

  // ── OTP boxes ────────────────────────────────────────────────
  readonly digits = Array.from({ length: 6 }, () => signal(''));
  readonly otpInputs = viewChildren<ElementRef<HTMLInputElement>>('otpInput');

  get otpCode(): string {
    return this.digits.map(d => d()).join('');
  }

  get isOtpComplete(): boolean {
    return this.digits.every(d => /^\d$/.test(d()));
  }

  ngOnInit(): void {
    if (this.data.type !== 'phone') return;
    void this.countriesApi.listCountries({ pageSize: 1000, isCceCountry: false }).then(res => {
      if (!res.ok) return;
      this.countryCodes.set(res.value);
      if (this.data.currentPhone) {
        this.prefillPhone(this.data.currentPhone, res.value);
      }
    });
  }

  async requestChange(): Promise<void> {
    if (this.data.type === 'phone') {
      this.phoneCodeControl.markAllAsTouched();
      this.localPhoneControl.markAllAsTouched();
      if (this.phoneCodeControl.invalid || this.localPhoneControl.invalid) return;
    } else {
      this.newValueControl.markAsTouched();
      if (this.newValueControl.invalid) return;
    }
    this.sending.set(true);
    this.errorKind.set(null);
    const res = this.data.type === 'email'
      ? await this.api.requestEmailChange(this.newValueControl.value!.trim())
      : await this.api.requestPhoneChange(this.buildPhoneValue());
    this.sending.set(false);
    if (res.ok) {
      this.verificationId = res.value.verificationId;
      this.step.set('verify');
      setTimeout(() => this.focusBox(0));
    } else {
      this.errorKind.set(res.error.kind);
    }
  }

  async confirmChange(): Promise<void> {
    if (!this.isOtpComplete) return;
    this.confirming.set(true);
    this.errorKind.set(null);
    const res = this.data.type === 'email'
      ? await this.api.confirmEmailChange(this.verificationId, this.otpCode)
      : await this.api.confirmPhoneChange(this.verificationId, this.otpCode);
    this.confirming.set(false);
    if (res.ok) {
      this.ref.close(true);
    } else {
      this.errorKind.set(res.error.kind);
    }
  }

  back(): void {
    this.step.set('request');
    this.digits.forEach(d => d.set(''));
    this.errorKind.set(null);
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

  private focusBox(index: number): void {
    this.otpInputs()[index]?.nativeElement.focus();
  }

  private buildPhoneValue(): string {
    const cc = this.phoneCodeControl.value as Country;
    const dial = cc.dialCode.replace(/^\+/, '');
    const local = this.localPhoneControl.value!.replace(/\s/g, '');
    return `${dial}${local}`;
  }

  private prefillPhone(phone: string, codes: Country[]): void {
    const stripped = phone.replace(/^\+/, '');
    const sorted = [...codes].sort((a, b) => b.dialCode.length - a.dialCode.length);
    const match = sorted.find(cc => stripped.startsWith(cc.dialCode.replace(/^\+/, '')));
    if (!match) return;
    const dialLen = match.dialCode.replace(/^\+/, '').length;
    this.phoneCodeControl.setValue(match, { emitEvent: false });
    this.localPhoneControl.setValue(stripped.slice(dialLen), { emitEvent: false });
  }
}
