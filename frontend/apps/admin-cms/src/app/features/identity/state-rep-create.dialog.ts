
import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { toSignal } from '@angular/core/rxjs-interop';
import { startWith } from 'rxjs';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslocoModule } from '@jsverse/transloco';
import { LocaleService } from '@frontend/i18n';
import { CountryApiService } from '../countries/country-api.service';
import type { Country } from '../countries/country.types';
import { IdentityApiService } from './identity-api.service';
import { Role } from './identity.types';
import type { StateRepAssignment, UserListItem } from './identity.types';

interface StateRepCreateForm {
  userId: FormControl<string>;
  countryId: FormControl<string>;
}

@Component({
  selector: 'cce-state-rep-create-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatAutocompleteModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    TranslocoModule,
  ],
  templateUrl: './state-rep-create.dialog.html',
  styleUrl: './state-rep-create.dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StateRepCreateDialogComponent implements OnInit {
  private readonly api = inject(IdentityApiService);
  private readonly countryApi = inject(CountryApiService);
  private readonly localeService = inject(LocaleService);
  private readonly ref =
    inject<MatDialogRef<StateRepCreateDialogComponent, StateRepAssignment | null>>(MatDialogRef);

  readonly isAr = computed(() => this.localeService.locale() === 'ar');

  readonly form = new FormGroup<StateRepCreateForm>({
    userId: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    countryId: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });
  readonly saving = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly loading = signal(true);

  readonly users = signal<UserListItem[]>([]);
  readonly countries = signal<Country[]>([]);

  readonly userFilterCtrl = new FormControl('');
  readonly countryFilterCtrl = new FormControl('');

  private readonly userFilter = toSignal(
    this.userFilterCtrl.valueChanges.pipe(startWith('')),
    { initialValue: '' },
  );
  private readonly countryFilter = toSignal(
    this.countryFilterCtrl.valueChanges.pipe(startWith('')),
    { initialValue: '' },
  );

  readonly filteredUsers = computed(() => {
    const term = (this.userFilter() ?? '').toLowerCase();
    if (!term) return this.users();
    return this.users().filter(
      (u) =>
        (u.userName ?? '').toLowerCase().includes(term) ||
        (u.email ?? '').toLowerCase().includes(term),
    );
  });

  readonly filteredCountries = computed(() => {
    const term = (this.countryFilter() ?? '').toLowerCase();
    if (!term) return this.countries();
    return this.countries().filter(
      (c) => c.nameAr.includes(term) || c.nameEn.toLowerCase().includes(term),
    );
  });

  /** Autocomplete selection → set the form id + reflect the label in the input. */
  onUserSelected(id: string, display: string): void {
    this.form.controls.userId.setValue(id);
    this.userFilterCtrl.setValue(display, { emitEvent: false });
  }
  onCountrySelected(id: string, display: string): void {
    this.form.controls.countryId.setValue(id);
    this.countryFilterCtrl.setValue(display, { emitEvent: false });
  }

  async ngOnInit(): Promise<void> {
    const [usersRes, countriesRes] = await Promise.all([
      this.api.listUsers({ pageSize: 100, role: Role.StateRepresentative }),
      this.countryApi.listCountries({ pageSize: 1000, isCceCountry: false }),
    ]);
    if (usersRes.ok) this.users.set(usersRes.value.items);
    if (countriesRes.ok) this.countries.set(countriesRes.value.items);
    this.loading.set(false);
  }

  async save(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    this.errorKind.set(null);
    const res = await this.api.createStateRepAssignment(this.form.getRawValue());
    this.saving.set(false);
    if (res.ok) {
      this.ref.close(res.value);
    } else {
      this.errorKind.set(res.error.kind);
    }
  }

  cancel(): void {
    this.ref.close(null);
  }
}
