import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, Inject, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import {
  MAT_DIALOG_DATA,
  MatDialogModule,
  MatDialogRef,
} from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { TranslateModule } from '@ngx-translate/core';
import { AssetUploadComponent } from './asset-upload.component';
import { ContentApiService } from './content-api.service';
import {
  RESOURCE_TYPES,
  type AssetFile,
  type Resource,
  type ResourceType,
} from './content.types';

const GUID_RE = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export interface ResourceFormDialogData {
  /** When set, the dialog is in edit-mode for this resource. Otherwise it's create-mode. */
  resource?: Resource;
}

interface ResourceForm {
  titleAr: FormControl<string>;
  titleEn: FormControl<string>;
  descriptionAr: FormControl<string>;
  descriptionEn: FormControl<string>;
  resourceType: FormControl<ResourceType>;
  categoryId: FormControl<string>;
  countryId: FormControl<string>;
}

/**
 * Single dialog for both Create and Edit. The shape of the form is the same
 * either way; on edit, asset replacement is intentionally NOT supported in
 * v0.1.0 (would require a separate domain operation; backend doesn't expose
 * one). Edit only lets the admin update text + resourceType + category.
 */
@Component({
  selector: 'cce-resource-form-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    AssetUploadComponent,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    TranslateModule,
  ],
  templateUrl: './resource-form.dialog.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResourceFormDialogComponent {
  private readonly api = inject(ContentApiService);

  readonly resourceTypes = RESOURCE_TYPES;
  readonly form: FormGroup<ResourceForm>;
  readonly assetFile = signal<AssetFile | null>(null);
  readonly saving = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly isEdit: boolean;

  constructor(
    private readonly ref: MatDialogRef<ResourceFormDialogComponent, Resource | null>,
    @Inject(MAT_DIALOG_DATA) readonly data: ResourceFormDialogData,
  ) {
    this.isEdit = data.resource !== undefined;
    this.form = new FormGroup<ResourceForm>({
      titleAr: new FormControl(data.resource?.titleAr ?? '', { nonNullable: true, validators: [Validators.required] }),
      titleEn: new FormControl(data.resource?.titleEn ?? '', { nonNullable: true, validators: [Validators.required] }),
      descriptionAr: new FormControl(data.resource?.descriptionAr ?? '', { nonNullable: true, validators: [Validators.required] }),
      descriptionEn: new FormControl(data.resource?.descriptionEn ?? '', { nonNullable: true, validators: [Validators.required] }),
      resourceType: new FormControl<ResourceType>(data.resource?.resourceType ?? 'Pdf', { nonNullable: true, validators: [Validators.required] }),
      categoryId: new FormControl(data.resource?.categoryId ?? '', { nonNullable: true, validators: [Validators.required, Validators.pattern(GUID_RE)] }),
      countryId: new FormControl(data.resource?.countryId ?? '', { nonNullable: true }),
    });
  }

  onAssetUploaded(asset: AssetFile): void {
    this.assetFile.set(asset);
  }

  async save(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    if (!this.isEdit && !this.assetFile()) {
      this.errorKind.set('validation');
      return;
    }
    this.saving.set(true);
    this.errorKind.set(null);
    const v = this.form.getRawValue();

    if (this.isEdit && this.data.resource) {
      const res = await this.api.updateResource(this.data.resource.id, {
        titleAr: v.titleAr,
        titleEn: v.titleEn,
        descriptionAr: v.descriptionAr,
        descriptionEn: v.descriptionEn,
        resourceType: v.resourceType,
        categoryId: v.categoryId,
        rowVersion: this.data.resource.rowVersion,
      });
      this.saving.set(false);
      if (res.ok) this.ref.close(res.value);
      else this.errorKind.set(res.error.kind);
    } else {
      const asset = this.assetFile();
      if (!asset) {
        this.saving.set(false);
        this.errorKind.set('validation');
        return;
      }
      const res = await this.api.createResource({
        titleAr: v.titleAr,
        titleEn: v.titleEn,
        descriptionAr: v.descriptionAr,
        descriptionEn: v.descriptionEn,
        resourceType: v.resourceType,
        categoryId: v.categoryId,
        countryId: v.countryId || null,
        assetFileId: asset.id,
      });
      this.saving.set(false);
      if (res.ok) this.ref.close(res.value);
      else this.errorKind.set(res.error.kind);
    }
  }

  cancel(): void {
    this.ref.close(null);
  }
}
