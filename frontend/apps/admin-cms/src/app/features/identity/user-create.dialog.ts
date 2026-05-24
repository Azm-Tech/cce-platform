import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { TranslocoModule } from '@jsverse/transloco';
import { CountryApiService } from '../countries/country-api.service';
import type { Country } from '../countries/country.types';
import { IdentityApiService } from './identity-api.service';
import { ASSIGNABLE_ROLES, type UserListItem } from './identity.types';

interface CreateForm {
  firstName: FormControl<string>;
  lastName: FormControl<string>;
  email: FormControl<string>;
  phoneNumber: FormControl<string>;
  countryId: FormControl<string>;
  role: FormControl<string>;
}

@Component({
  selector: 'cce-user-create-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
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
  private readonly cdr = inject(ChangeDetectorRef);

  readonly countries = signal<Country[]>([]);
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
    phoneNumber: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(15), Validators.pattern(/^\+?[\d\s\-()]+$/)],
    }),
    countryId: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    role: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });

  constructor(private readonly ref: MatDialogRef<UserCreateDialogComponent, UserListItem | null>) {}

  async ngOnInit(): Promise<void> {
    const res = await this.countryApi.listCountries({ pageSize: 200, isActive: true });
    if (res.ok) this.countries.set(res.value.items);
  }

  async submit(): Promise<void> {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    this.errorKey.set(null);
    const res = await this.api.createUser(this.form.getRawValue());
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
