import { ChangeDetectionStrategy, Component, Inject, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { TranslocoModule } from '@jsverse/transloco';
import { InteractiveMapsApiService } from './interactive-maps-api.service';
import type { InteractiveMapDto } from './interactive-maps.types';

export interface InteractiveMapFormData {
  map?: InteractiveMapDto;
}

interface MapForm {
  nameAr: FormControl<string>;
  nameEn: FormControl<string>;
  descriptionAr: FormControl<string>;
  descriptionEn: FormControl<string>;
  isActive: FormControl<boolean>;
}

@Component({
  selector: 'cce-interactive-map-form-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCheckboxModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    TranslocoModule,
  ],
  templateUrl: './interactive-map-form.dialog.html',
  styleUrl: './interactive-map-form.dialog.scss',
  changeDetection: ChangeDetectionStrategy.Default,
})
export class InteractiveMapFormDialogComponent {
  private readonly api = inject(InteractiveMapsApiService);
  readonly saving = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly isEdit: boolean;
  readonly form: FormGroup<MapForm>;

  constructor(
    private readonly ref: MatDialogRef<InteractiveMapFormDialogComponent, true | null>,
    @Inject(MAT_DIALOG_DATA) readonly data: InteractiveMapFormData,
  ) {
    this.isEdit = data.map !== undefined;
    const m = data.map;
    this.form = new FormGroup<MapForm>({
      nameAr: new FormControl(m?.nameAr ?? '', { nonNullable: true, validators: [Validators.required] }),
      nameEn: new FormControl(m?.nameEn ?? '', { nonNullable: true, validators: [Validators.required] }),
      descriptionAr: new FormControl(m?.descriptionAr ?? '', { nonNullable: true }),
      descriptionEn: new FormControl(m?.descriptionEn ?? '', { nonNullable: true }),
      isActive: new FormControl(m?.isActive ?? true, { nonNullable: true }),
    });
  }

  async save(): Promise<void> {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.saving.set(true);
    this.errorKind.set(null);
    const v = this.form.getRawValue();
    const res = this.isEdit && this.data.map
      ? await this.api.updateMap(this.data.map.id, {
          nameAr: v.nameAr || null,
          nameEn: v.nameEn || null,
          descriptionAr: v.descriptionAr || null,
          descriptionEn: v.descriptionEn || null,
          isActive: v.isActive,
        })
      : await this.api.createMap({
          nameAr: v.nameAr || null,
          nameEn: v.nameEn || null,
          descriptionAr: v.descriptionAr || null,
          descriptionEn: v.descriptionEn || null,
        });
    this.saving.set(false);
    if (res.ok) this.ref.close(true);
    else this.errorKind.set(res.error.kind);
  }

  cancel(): void { this.ref.close(null); }
}
