
import { ChangeDetectionStrategy, Component, Inject, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import {
  MAT_DIALOG_DATA,
  MatDialogModule,
  MatDialogRef,
} from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { TranslocoModule } from '@jsverse/transloco';
import { LocaleService } from '@frontend/i18n';
import { RichTextEditorComponent } from '@frontend/ui-kit';
import { AssetUploadComponent } from './asset-upload.component';
import { ContentApiService } from './content-api.service';
import {
  RESOURCE_TYPES,
  type AssetFile,
  type Resource,
  type ResourceType,
} from './content.types';
import { TaxonomyApiService } from '../taxonomies/taxonomy-api.service';
import type { ResourceCategory } from '../taxonomies/taxonomy.types';
import { CountryApiService } from '../countries/country-api.service';
import type { Country } from '../countries/country.types';

export interface ResourceFormDialogData {
  /** When set, the dialog is in edit-mode for this resource. Otherwise it's create-mode. */
  resource?: Resource;
}

interface ResourceForm {
  titleAr: FormControl<string>;
  titleEn: FormControl<string>;
  descriptionAr: FormControl<string>;
  descriptionEn: FormControl<string>;
  /** Null until the admin explicitly picks a type (US047 — no silent default). */
  resourceType: FormControl<ResourceType | null>;
  categoryId: FormControl<string>;
  countryIds: FormControl<string[]>;
}

/** Server-enforced limits (verified against the live API 2026-06-07 —
 *  VAL001 with per-field errors). Client mirrors them exactly. */
const TITLE_MAX = 255;
const DESCRIPTION_MAX = 500;

@Component({
  selector: 'cce-resource-form-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    AssetUploadComponent,
    RichTextEditorComponent,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    TranslocoModule,
  ],
  templateUrl: './resource-form.dialog.html',
  styles: [`
    .cce-resource-form {
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
      padding-top: 0.25rem;
    }

    .cce-resource-form__row {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 1rem;
      align-items: start;
    }

    .cce-resource-form__row mat-form-field,
    .cce-resource-form__row cce-rich-text-editor {
      width: 100%;
    }

    .cce-resource-form__full {
      width: 100%;
    }

    /* Compact the rich-text editors so two stacked ones don't blow past
       the dialog's max-height and trigger an outer scroll. */
    .cce-resource-form cce-rich-text-editor ::ng-deep .ql-editor {
      min-height: 80px;
    }

    /* Let the content region itself scroll (not the whole panel), and
       drop MDC's default bottom padding that left dead space under the
       form before the action bar. */
    :host ::ng-deep .mat-mdc-dialog-content {
      max-height: 72vh;
      padding-bottom: 0.5rem;
    }

    .cce-resource-form__asset {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
      padding: 1rem;
      border: 1px solid rgba(0, 0, 0, 0.12);
      border-radius: 4px;
      background: rgba(0, 0, 0, 0.02);
    }

    .cce-resource-form__asset-label {
      font-size: 0.85rem;
      color: rgba(0, 0, 0, 0.6);
      font-weight: 500;
    }

    .cce-resource-form__asset-existing {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 8px 12px;
      border: 1px solid rgba(0, 0, 0, 0.12);
      border-radius: 4px;
      background: var(--white);
      font-size: 0.9rem;
      color: rgba(0, 0, 0, 0.87);

      mat-icon { font-size: 18px; width: 18px; height: 18px; color: rgba(0,0,0,0.54); }

      span { flex: 1; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    }

    .cce-resource-form__asset-change {
      margin-inline-start: auto;
      flex-shrink: 0;
    }

    .cce-resource-form__error {
      background: var(--danger--50);
      color: var(--danger--600);
      padding: 0.6rem 0.85rem;
      border-radius: 6px;
      font-size: 0.85rem;
    }

    @media (max-width: 600px) {
      .cce-resource-form__row { grid-template-columns: 1fr; }
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResourceFormDialogComponent {
  private readonly api = inject(ContentApiService);

  /** Inline-image uploader for the rich-text editor (asset media store). */
  readonly uploadImage = async (file: File): Promise<string | null> => {
    const res = await this.api.uploadMedia(file);
    return res.ok ? res.value.url : null;
  };
  private readonly taxonomy = inject(TaxonomyApiService);
  private readonly countryApi = inject(CountryApiService);
  private readonly localeService = inject(LocaleService);

  readonly resourceTypes = RESOURCE_TYPES;
  readonly titleMax = TITLE_MAX;
  readonly descriptionMax = DESCRIPTION_MAX;
  readonly form: FormGroup<ResourceForm>;
  readonly assetFile = signal<AssetFile | null>(null);
  readonly replacingFile = signal(false);
  readonly saving = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly isEdit: boolean;
  readonly categories = signal<ResourceCategory[]>([]);
  readonly countries = signal<Country[]>([]);
  readonly locale = this.localeService.locale;

  constructor(
    private readonly ref: MatDialogRef<ResourceFormDialogComponent, Resource | null>,
    @Inject(MAT_DIALOG_DATA) readonly data: ResourceFormDialogData,
  ) {
    this.isEdit = data.resource !== undefined;
    this.form = new FormGroup<ResourceForm>({
      titleAr: new FormControl(data.resource?.titleAr ?? '', { nonNullable: true, validators: [Validators.required, Validators.maxLength(TITLE_MAX)] }),
      titleEn: new FormControl(data.resource?.titleEn ?? '', { nonNullable: true, validators: [Validators.required, Validators.maxLength(TITLE_MAX)] }),
      descriptionAr: new FormControl(data.resource?.descriptionAr ?? '', { nonNullable: true, validators: [Validators.required, Validators.maxLength(DESCRIPTION_MAX)] }),
      descriptionEn: new FormControl(data.resource?.descriptionEn ?? '', { nonNullable: true, validators: [Validators.required, Validators.maxLength(DESCRIPTION_MAX)] }),
      resourceType: new FormControl<ResourceType | null>(data.resource?.resourceType ?? null, { validators: [Validators.required] }),
      categoryId: new FormControl(data.resource?.categoryId ?? '', { nonNullable: true, validators: [Validators.required] }),
      countryIds: new FormControl<string[]>(data.resource?.countryIds ?? [], { nonNullable: true, validators: [Validators.required] }),
    });
    void this.loadDropdowns();
  }

  private async loadDropdowns(): Promise<void> {
    const [catRes, countryRes] = await Promise.all([
      this.taxonomy.listCategories({ isActive: true, pageSize: 200 }),
      this.countryApi.listCountries({ pageSize: 500, isCceCountry: false }),
    ]);
    if (catRes.ok) this.categories.set(catRes.value.items);
    if (countryRes.ok) this.countries.set(countryRes.value.items);
  }

  onAssetUploaded(asset: AssetFile): void {
    this.assetFile.set(asset);
    this.replacingFile.set(false);
  }

  startReplaceFile(): void {
    this.replacingFile.set(true);
    this.assetFile.set(null);
  }

  cancelReplaceFile(): void {
    this.replacingFile.set(false);
    this.assetFile.set(null);
  }

  async save(): Promise<void> {
    // ERR013 (BRD): missing/invalid required fields — shown as a banner,
    // so the submit button stays clickable and AC6 is actually reachable.
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.errorKind.set('ERR013');
      return;
    }
    if (!this.isEdit && !this.assetFile()) {
      this.errorKind.set('ERR013');
      return;
    }
    this.saving.set(true);
    this.errorKind.set(null);
    const v = this.form.getRawValue();
    if (!v.resourceType) return; // unreachable when form is valid — narrows the type

    if (this.isEdit && this.data.resource) {
      const newAsset = this.assetFile();
      const res = await this.api.updateResource(this.data.resource.id, {
        titleAr: v.titleAr,
        titleEn: v.titleEn,
        descriptionAr: v.descriptionAr,
        descriptionEn: v.descriptionEn,
        resourceType: v.resourceType,
        categoryId: v.categoryId,
        countryIds: v.countryIds,
        ...(newAsset ? { assetFileId: newAsset.id } : {}),
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
        countryIds: v.countryIds,
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
