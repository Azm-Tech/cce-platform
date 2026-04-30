import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslateModule } from '@ngx-translate/core';
import { IdentityApiService } from './identity-api.service';
import type { StateRepAssignment } from './identity.types';

const GUID_RE = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

interface StateRepCreateForm {
  userId: FormControl<string>;
  countryId: FormControl<string>;
}

/**
 * Modal that lets a SuperAdmin create a state-rep assignment by entering
 * a user GUID + country GUID. v0.1.0 takes free-text GUIDs (admin power-
 * user flow); a future phase will replace these with searchable pickers
 * once the country/expert-profile catalogs land in the admin UI.
 */
@Component({
  selector: 'cce-state-rep-create-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    TranslateModule,
  ],
  templateUrl: './state-rep-create.dialog.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StateRepCreateDialogComponent {
  private readonly api = inject(IdentityApiService);
  private readonly ref =
    inject<MatDialogRef<StateRepCreateDialogComponent, StateRepAssignment | null>>(MatDialogRef);

  readonly form = new FormGroup<StateRepCreateForm>({
    userId: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(GUID_RE)],
    }),
    countryId: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(GUID_RE)],
    }),
  });
  readonly saving = signal(false);
  readonly errorKind = signal<string | null>(null);

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
