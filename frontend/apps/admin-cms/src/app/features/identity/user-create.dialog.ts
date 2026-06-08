import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom, map } from 'rxjs';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { TranslocoModule } from '@jsverse/transloco';
import { LocaleService } from '@frontend/i18n';
import { CountryApiService } from '../countries/country-api.service';
import type { Country } from '../countries/country.types';
import { IdentityApiService } from './identity-api.service';
import { ASSIGNABLE_ROLES, type UserListItem } from './identity.types';

interface CountryCode {
  id: string;
  dialCode: string;
  name: { ar: string; en: string };
  isActive: boolean;
}

interface CreateForm {
  firstName: FormControl<string>;
  lastName: FormControl<string>;
  email: FormControl<string>;
  country: FormControl<Country | null>;
  phoneCode: FormControl<CountryCode | null>;
  phoneNumber: FormControl<string>;
  role: FormControl<string>;
}

@Component({
  selector: 'cce-user-create-dialog',
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
    MatSelectModule,
    TranslocoModule,
  ],
  templateUrl: './user-create.dialog.html',
  styleUrl: './user-create.dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserCreateDialogComponent implements OnInit {
  private readonly api = inject(IdentityApiService);
  private readonly countryApi = inject(CountryApiService);
  private readonly http = inject(HttpClient);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);
  readonly locale = inject(LocaleService).locale;

  readonly countries = signal<Country[]>([]);
  readonly countryCodes = signal<CountryCode[]>([]);
  readonly saving = signal(false);
  readonly errorKey = signal<string | null>(null);
  readonly assignableRoles = ASSIGNABLE_ROLES;

  readonly form = new FormGroup<CreateForm>({
    firstName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(50)],
    }),
    lastName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(50)],
    }),
    email: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email, Validators.maxLength(100)],
    }),
    country: new FormControl<Country | null>(null, [Validators.required]),
    phoneCode: new FormControl<CountryCode | null>(null, [Validators.required]),
    phoneNumber: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(15), Validators.pattern(/^[\d\s\-()]+$/)],
    }),
    role: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });

  // Track typed text in each autocomplete to drive filtering
  private readonly countryInput = toSignal(
    this.form.get('country')!.valueChanges.pipe(map((v) => (typeof v === 'string' ? v : ''))),
    { initialValue: '' },
  );
  private readonly phoneCodeInput = toSignal(
    this.form.get('phoneCode')!.valueChanges.pipe(map((v) => (typeof v === 'string' ? v : ''))),
    { initialValue: '' },
  );

  readonly filteredCountries = computed(() => {
    const q = this.countryInput().toLowerCase();
    const all = this.countries();
    if (!q) return all;
    return all.filter(
      (c) => c.nameEn.toLowerCase().includes(q) || c.nameAr.toLowerCase().includes(q),
    );
  });

  readonly filteredPhoneCodes = computed(() => {
    const q = this.phoneCodeInput().toLowerCase();
    const all = this.countryCodes();
    if (!q) return all;
    return all.filter(
      (cc) =>
        cc.name.en.toLowerCase().includes(q) ||
        cc.name.ar.toLowerCase().includes(q) ||
        cc.dialCode.includes(q),
    );
  });

  /** Display functions passed to [displayWith] */
  readonly displayCountry = (c: Country | null): string => {
    if (!c) return '';
    return this.locale() === 'ar' ? c.nameAr : c.nameEn;
  };

  readonly displayPhoneCode = (cc: CountryCode | null): string => {
    if (!cc) return '';
    const name = this.locale() === 'ar' ? cc.name.ar : cc.name.en;
    return `${name} (${cc.dialCode})`;
  };

  constructor(private readonly ref: MatDialogRef<UserCreateDialogComponent, UserListItem | null>) {}

  async ngOnInit(): Promise<void> {
    const [countriesRes, rawCodes] = await Promise.all([
      this.countryApi.listCountries({ pageSize: 200, isActive: true }),
      firstValueFrom(
        this.http.get<{ data: CountryCode[] }>('/api/country-codes', {
          params: { isActive: 'true' },
        }),
      )
        .then((r) => r.data)
        .catch(() => [] as CountryCode[]),
    ]);

    if (countriesRes.ok) this.countries.set(countriesRes.value.items);
    this.countryCodes.set(rawCodes);
    this.cdr.markForCheck();

    // Auto-set phone code when a country is selected
    this.form.get('country')!.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((val) => {
        if (!val || typeof val === 'string') return;
        const match = this.countryCodes().find(
          (cc) =>
            cc.name.en.toLowerCase() === val.nameEn.toLowerCase() ||
            cc.name.ar === val.nameAr,
        );
        if (match) {
          this.form.get('phoneCode')!.setValue(match, { emitEvent: false });
          this.cdr.markForCheck();
        }
      });
  }

  async submit(): Promise<void> {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }
    const { firstName, lastName, email, country, phoneCode, phoneNumber, role } =
      this.form.getRawValue();
    this.saving.set(true);
    this.errorKey.set(null);
    const res = await this.api.createUser({
      firstName,
      lastName,
      email,
      phoneNumber,
      countryId: country!.id,
      phoneCountryCodeId: phoneCode!.id,
      role,
    });
    this.saving.set(false);
    if (res.ok) {
      this.ref.close(res.value);
    } else if (res.error.kind === 'duplicate') {
      this.errorKey.set('users.create.errorConflict');
    } else if (res.error.kind === 'validation') {
      for (const [field, messages] of Object.entries(res.error.fieldErrors)) {
        const ctrl = this.form.get(field);
        if (ctrl) {
          ctrl.setErrors({ apiError: messages[0] });
          ctrl.markAsTouched();
        }
      }
      this.cdr.markForCheck();
    } else {
      this.errorKey.set('users.create.errorGeneric');
    }
  }

  fieldError(name: string): string | null {
    return this.form.get(name)?.getError('apiError') ?? null;
  }

  cancel(): void {
    this.ref.close(null);
  }
}
