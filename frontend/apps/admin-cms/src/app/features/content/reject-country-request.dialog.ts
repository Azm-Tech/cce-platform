import { ChangeDetectionStrategy, Component, Inject, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslocoModule } from '@jsverse/transloco';
import { ContentApiService } from './content-api.service';
import type { AdminCountryContentRequest } from './content.types';

export interface RejectCountryRequestDialogData {
  requestId: string;
}

interface RejectForm {
  adminNotesAr: FormControl<string>;
  adminNotesEn: FormControl<string>;
}

@Component({
  selector: 'cce-reject-country-request-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    TranslocoModule,
  ],
  template: `
    <h2 mat-dialog-title>{{ 'countryRequest.reject.title' | transloco }}</h2>
    <mat-dialog-content>
      <p>{{ 'countryRequest.reject.confirm' | transloco }}</p>
      <form [formGroup]="form" class="cce-reject-form">
        <mat-form-field appearance="outline">
          <mat-label>{{ 'countryRequest.notesAr' | transloco }}</mat-label>
          <textarea matInput formControlName="adminNotesAr" rows="3"></textarea>
          @if (form.controls.adminNotesAr.touched && form.controls.adminNotesAr.invalid) {
            <mat-error>{{ 'common.validation.required' | transloco }}</mat-error>
          }
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>{{ 'countryRequest.notesEn' | transloco }}</mat-label>
          <textarea matInput formControlName="adminNotesEn" rows="3"></textarea>
          @if (form.controls.adminNotesEn.touched && form.controls.adminNotesEn.invalid) {
            <mat-error>{{ 'common.validation.required' | transloco }}</mat-error>
          }
        </mat-form-field>
        @if (errorKind(); as kind) {
          <div class="cce-reject-form__error">{{ ('errors.' + kind) | transloco }}</div>
        }
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" (click)="cancel()">
        {{ 'common.cancel' | transloco }}
      </button>
      <button mat-flat-button color="warn" type="button" (click)="save()" [disabled]="saving()">
        @if (saving()) { <mat-spinner diameter="18" /> }
        {{ 'countryRequest.reject.action' | transloco }}
      </button>
    </mat-dialog-actions>
  `,
  styles: [`.cce-reject-form { display: flex; flex-direction: column; gap: 0.75rem; padding-top: 0.5rem; }
    .cce-reject-form__error { background: var(--danger--50); color: var(--danger--600); padding: 0.6rem 0.85rem; border-radius: 6px; font-size: 0.85rem; }`],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RejectCountryRequestDialogComponent {
  private readonly api = inject(ContentApiService);

  readonly form = new FormGroup<RejectForm>({
    adminNotesAr: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    adminNotesEn: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });
  readonly saving = signal(false);
  readonly errorKind = signal<string | null>(null);

  constructor(
    private readonly ref: MatDialogRef<RejectCountryRequestDialogComponent, AdminCountryContentRequest | null>,
    @Inject(MAT_DIALOG_DATA) readonly data: RejectCountryRequestDialogData,
  ) {}

  async save(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    this.errorKind.set(null);
    const v = this.form.getRawValue();
    const res = await this.api.rejectCountryResourceRequest(this.data.requestId, {
      adminNotesAr: v.adminNotesAr,
      adminNotesEn: v.adminNotesEn,
    });
    this.saving.set(false);
    if (res.ok) this.ref.close(res.value);
    else this.errorKind.set(res.error.kind);
  }

  cancel(): void {
    this.ref.close(null);
  }
}
