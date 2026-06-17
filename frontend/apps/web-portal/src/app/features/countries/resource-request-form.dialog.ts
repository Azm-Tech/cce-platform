
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { TranslocoModule } from '@jsverse/transloco';
import { LocaleService } from '@frontend/i18n';
import { RichTextEditorComponent, ToastService } from '@frontend/ui-kit';
import { MediaApiService } from '../../core/media/media-api.service';
import { RESOURCE_TYPE_VALUE, RESOURCE_TYPES, type ResourceCategory } from '../knowledge-center/knowledge.types';
import { CountriesApiService } from './countries-api.service';
import { ContentType, type CountryContentRequest } from './country.types';

const ALLOWED_TYPES = ['application/pdf', 'application/msword',
  'application/vnd.openxmlformats-officedocument.wordprocessingml.document'];

interface ResourceRequestForm {
  titleAr: FormControl<string>;
  titleEn: FormControl<string>;
  descriptionAr: FormControl<string>;
  descriptionEn: FormControl<string>;
  resourceType: FormControl<string>;
  categoryId: FormControl<string>;
}

@Component({
  selector: 'cce-resource-request-form-dialog',
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
    RichTextEditorComponent,
    TranslocoModule,
  ],
  templateUrl: './resource-request-form.dialog.html',
  styleUrl: './resource-request-form.dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResourceRequestFormDialogComponent implements OnInit {
  private readonly api = inject(CountriesApiService);
  private readonly media = inject(MediaApiService);

  /** Inline-image uploader for the rich-text editor — uploads to the
   *  public media store and returns the URL (no base64 in content). */
  readonly uploadImage = async (file: File): Promise<string | null> => {
    const res = await this.media.uploadFile(file);
    return res.ok ? res.value.url : null;
  };
  private readonly toast = inject(ToastService);
  private readonly localeService = inject(LocaleService);
  private readonly ref =
    inject<MatDialogRef<ResourceRequestFormDialogComponent, CountryContentRequest | null>>(MatDialogRef);
  private readonly countryId = inject<string>(MAT_DIALOG_DATA);

  readonly resourceTypes = RESOURCE_TYPES;
  readonly categories = signal<ResourceCategory[]>([]);
  readonly locale = this.localeService.locale;

  readonly resourceTypeSearch = new FormControl('');
  private readonly resourceTypeSearchValue = toSignal(this.resourceTypeSearch.valueChanges, { initialValue: '' });
  readonly filteredResourceTypes = computed(() => {
    const q = (this.resourceTypeSearchValue() ?? '').trim().toLowerCase();
    if (!q) return this.resourceTypes;
    return this.resourceTypes.filter(t => t.toLowerCase().includes(q));
  });

  readonly form = new FormGroup<ResourceRequestForm>({
    titleAr: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(255)] }),
    titleEn: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(255)] }),
    descriptionAr: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(500)] }),
    descriptionEn: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(500)] }),
    resourceType: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    categoryId: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });

  readonly selectedFile = signal<File | null>(null);
  readonly fileError = signal<string | null>(null);
  readonly saving = signal(false);
  readonly errorKey = signal<string | null>(null);

  ngOnInit(): void {
    void this.loadCategories();
  }

  private async loadCategories(): Promise<void> {
    const res = await this.api.listResourceCategories();
    if (res.ok) this.categories.set(res.value);
  }

  onFileChange(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0] ?? null;
    this.fileError.set(null);
    if (!file) { this.selectedFile.set(null); return; }
    if (!ALLOWED_TYPES.includes(file.type)) {
      this.fileError.set('resourceRequest.form.fileTypeError');
      this.selectedFile.set(null);
      return;
    }
    this.selectedFile.set(file);
  }

  async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.errorKey.set('errors.ERR013');
      return;
    }
    if (!this.selectedFile()) {
      this.fileError.set('resourceRequest.form.fileRequired');
      return;
    }

    this.saving.set(true);
    this.errorKey.set(null);

    const uploadRes = await this.media.uploadAsset(this.selectedFile()!);
    if (!uploadRes.ok) {
      this.saving.set(false);
      this.errorKey.set('errors.ERR029');
      return;
    }

    const v = this.form.getRawValue();
    const submitRes = await this.api.submitRequest({
      countryId: this.countryId,
      content: {
        type: ContentType.Resource,
        titleAr: v.titleAr,
        titleEn: v.titleEn,
        descriptionAr: v.descriptionAr,
        descriptionEn: v.descriptionEn,
        resourceType: RESOURCE_TYPE_VALUE[v.resourceType as keyof typeof RESOURCE_TYPE_VALUE] ?? 0,
        categoryId: v.categoryId || null,
        assetFileId: uploadRes.value.id,
      },
    });

    this.saving.set(false);
    if (submitRes.ok) {
      this.toast.success('confirmations.CON024');
      this.ref.close(submitRes.value);
    } else {
      this.errorKey.set('errors.ERR029');
    }
  }

  onResourceTypeSelected(value: string, displayText: string): void {
    this.form.controls.resourceType.setValue(value);
    this.resourceTypeSearch.setValue(displayText, { emitEvent: false });
    this.form.controls.resourceType.markAsTouched();
  }

  cancel(): void {
    this.ref.close(null);
  }
}
