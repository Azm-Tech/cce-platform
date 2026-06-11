import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
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
import type { CountryCode } from '../countries/country.types';
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
  readonly countryCodes = signal<CountryCode[]>([]);

  private verificationId = '';

  // Email field (phone type uses phoneCodeControl + localPhoneControl below)
  readonly newValueControl = new FormControl('', [
    Validators.required,
    Validators.email,
  ]);

  // Phone: dial-code autocomplete
  readonly phoneCodeControl = new FormControl<CountryCode | null>(null, [Validators.required]);
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
        cc.name.ar.toLowerCase().includes(q) ||
        cc.name.en.toLowerCase().includes(q) ||
        cc.dialCode.includes(q),
    );
  });
  readonly displayPhoneCode = (cc: CountryCode | null): string => {
    if (!cc) return '';
    const name = this.locale() === 'ar' ? cc.name.ar : cc.name.en;
    return `${name} (${cc.dialCode})`;
  };

  // Phone: local number
  readonly localPhoneControl = new FormControl('', [
    Validators.required,
    Validators.maxLength(15),
    Validators.pattern(/^[\d\s\-()]+$/),
  ]);

  // Display string used in the "OTP sent to …" step 2 message
  readonly phoneDisplay = computed(() => {
    const cc = this.phoneCodeControl.value as CountryCode | null;
    const local = this.localPhoneControl.value ?? '';
    if (!cc) return local;
    return `${cc.dialCode} ${local}`;
  });

  readonly otpControl = new FormControl('', [
    Validators.required,
    Validators.pattern(/^\d{4,8}$/),
  ]);

  ngOnInit(): void {
    if (this.data.type !== 'phone') return;
    void this.countriesApi.listCountryCodes({ isActive: true }).then(res => {
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
    } else {
      this.errorKind.set(res.error.kind);
    }
  }

  async confirmChange(): Promise<void> {
    if (this.otpControl.invalid) {
      this.otpControl.markAsTouched();
      return;
    }
    this.confirming.set(true);
    this.errorKind.set(null);
    const code = this.otpControl.value!.trim();
    const res = this.data.type === 'email'
      ? await this.api.confirmEmailChange(this.verificationId, code)
      : await this.api.confirmPhoneChange(this.verificationId, code);
    this.confirming.set(false);
    if (res.ok) {
      this.ref.close(true);
    } else {
      this.errorKind.set(res.error.kind);
    }
  }

  back(): void {
    this.step.set('request');
    this.otpControl.reset();
    this.errorKind.set(null);
  }

  private buildPhoneValue(): string {
    const cc = this.phoneCodeControl.value as CountryCode;
    const dial = cc.dialCode.replace(/^\+/, '');
    const local = this.localPhoneControl.value!.replace(/\s/g, '');
    return `${dial}${local}`;
  }

  private prefillPhone(phone: string, codes: CountryCode[]): void {
    const stripped = phone.replace(/^\+/, '');
    // Sort by dial-code length descending so longer codes match before shorter ones
    const sorted = [...codes].sort((a, b) => b.dialCode.length - a.dialCode.length);
    const match = sorted.find(cc => stripped.startsWith(cc.dialCode.replace(/^\+/, '')));
    if (!match) return;
    const dialLen = match.dialCode.replace(/^\+/, '').length;
    this.phoneCodeControl.setValue(match, { emitEvent: false });
    this.localPhoneControl.setValue(stripped.slice(dialLen), { emitEvent: false });
  }
}
