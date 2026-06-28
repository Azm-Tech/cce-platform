import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslocoModule } from '@jsverse/transloco';
import { RichTextEditorComponent, ToastService } from '@frontend/ui-kit';
import { MediaApiService } from '../../core/media/media-api.service';
import { CountriesApiService } from './countries-api.service';
import type { StateProfile } from './country.types';

const ALLOWED_NDC_TYPES = ['application/pdf'];

interface EditProfileForm {
  descriptionAr: FormControl<string>;
  descriptionEn: FormControl<string>;
  keyInitiativesAr: FormControl<string>;
  keyInitiativesEn: FormControl<string>;
  contactInfoAr: FormControl<string>;
  contactInfoEn: FormControl<string>;
  population: FormControl<string>;
  areaSqKm: FormControl<string>;
  gdpPerCapita: FormControl<string>;
}

@Component({
  selector: 'cce-state-profile-edit-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    TranslocoModule,
    RichTextEditorComponent,
  ],
  templateUrl: './state-profile-edit.dialog.html',
  styleUrl: './state-profile-edit.dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StateProfileEditDialogComponent {
  private readonly api = inject(CountriesApiService);
  private readonly media = inject(MediaApiService);

  /** Inline-image uploader for the rich-text editor — uploads to the
   *  public media store and returns the URL (no base64 in content). */
  readonly uploadImage = async (file: File): Promise<string | null> => {
    const res = await this.media.uploadFile(file);
    return res.ok ? res.value.url : null;
  };
  private readonly toast = inject(ToastService);
  private readonly ref =
    inject<MatDialogRef<StateProfileEditDialogComponent, StateProfile | null>>(MatDialogRef);
  protected readonly current = inject<StateProfile>(MAT_DIALOG_DATA);

  /** Existing NDC asset id, from either the object (`ndcDocument`) or bare
   *  id (`ndcAssetId`) read shape. */
  protected readonly currentNdcAssetId =
    this.current.ndcDocument?.assetId ?? this.current.ndcAssetId ?? null;

  readonly form = new FormGroup<EditProfileForm>({
    descriptionAr: new FormControl(this.current.descriptionAr ?? '', { nonNullable: true }),
    descriptionEn: new FormControl(this.current.descriptionEn ?? '', { nonNullable: true }),
    keyInitiativesAr: new FormControl(this.current.keyInitiativesAr ?? '', { nonNullable: true }),
    keyInitiativesEn: new FormControl(this.current.keyInitiativesEn ?? '', { nonNullable: true }),
    contactInfoAr: new FormControl(this.current.contactInfoAr ?? '', { nonNullable: true }),
    contactInfoEn: new FormControl(this.current.contactInfoEn ?? '', { nonNullable: true }),
    population: new FormControl(
      this.current.population != null ? String(this.current.population) : '',
      { nonNullable: true, validators: [Validators.required, Validators.min(1), Validators.pattern(/^\d+$/)] },
    ),
    areaSqKm: new FormControl(
      this.current.areaSqKm != null ? String(this.current.areaSqKm) : '',
      { nonNullable: true, validators: [Validators.required, Validators.min(0.01)] },
    ),
    gdpPerCapita: new FormControl(
      this.current.gdpPerCapita != null ? String(this.current.gdpPerCapita) : '',
      { nonNullable: true, validators: [Validators.required, Validators.min(0.01)] },
    ),
  });

  readonly selectedNdc = signal<File | null>(null);
  readonly ndcError = signal<string | null>(null);
  readonly saving = signal(false);
  readonly errorKey = signal<string | null>(null);

  onNdcChange(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0] ?? null;
    this.ndcError.set(null);
    if (!file) { this.selectedNdc.set(null); return; }
    if (!ALLOWED_NDC_TYPES.includes(file.type)) {
      this.ndcError.set('stateProfile.edit.ndcTypeError');
      this.selectedNdc.set(null);
      return;
    }
    this.selectedNdc.set(file);
  }

  /** Open the currently-stored NDC document (by its asset id) in a new tab. */
  async viewCurrentNdc(): Promise<void> {
    if (!this.currentNdcAssetId) return;
    const res = await this.media.getAsset(this.currentNdcAssetId);
    if (res.ok) window.open(res.value.url, '_blank', 'noopener');
  }

  async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.errorKey.set('errors.ERR013');
      return;
    }

    this.saving.set(true);
    this.errorKey.set(null);

    let ndcAssetId: string | null = this.currentNdcAssetId;
    if (this.selectedNdc()) {
      const uploadRes = await this.media.uploadAsset(this.selectedNdc()!);
      if (!uploadRes.ok) {
        this.saving.set(false);
        this.errorKey.set('errors.ERR029');
        return;
      }
      ndcAssetId = uploadRes.value.id;
    }

    const v = this.form.getRawValue();
    const res = await this.api.updateStateProfile(this.current.countryId, {
      descriptionAr: v.descriptionAr || null,
      descriptionEn: v.descriptionEn || null,
      keyInitiativesAr: v.keyInitiativesAr || null,
      keyInitiativesEn: v.keyInitiativesEn || null,
      contactInfoAr: v.contactInfoAr || null,
      contactInfoEn: v.contactInfoEn || null,
      population: v.population ? parseInt(v.population, 10) : null,
      areaSqKm: v.areaSqKm ? parseFloat(v.areaSqKm) : null,
      gdpPerCapita: v.gdpPerCapita ? parseFloat(v.gdpPerCapita) : null,
      ndcAssetId,
    });

    this.saving.set(false);
    if (res.ok) {
      this.toast.success('confirmations.CON026');
      this.ref.close(res.value);
    } else {
      this.errorKey.set('errors.ERR033');
    }
  }

  cancel(): void { this.ref.close(null); }
}
