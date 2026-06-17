import { CommonModule, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslocoModule } from '@jsverse/transloco';
import { RichTextEditorComponent, ToastService } from '@frontend/ui-kit';
import { LocaleService } from '@frontend/i18n';
import { CountryApiService } from './country-api.service';
import { ContentApiService } from '../content/content-api.service';
import type { Country, CountryProfile } from './country.types';

interface CountryForm {
  nameAr: FormControl<string>;
  nameEn: FormControl<string>;
  regionAr: FormControl<string>;
  regionEn: FormControl<string>;
  isActive: FormControl<boolean>;
}

interface ProfileForm {
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

const ALLOWED_NDC_TYPES = ['application/pdf'];

/**
 * Admin → Country detail. Edits both the country header (name + region +
 * isActive) and the country profile (description / initiatives / contact)
 * on a single page. Profile uses the upsert endpoint, so a missing profile
 * (404) is treated as "not yet created" and the form starts empty.
 */
@Component({
  selector: 'cce-country-detail',
  standalone: true,
  imports: [
    CommonModule, DatePipe, ReactiveFormsModule, RouterLink,
    MatButtonModule, MatCardModule, MatCheckboxModule, MatFormFieldModule,
    MatIconModule, MatInputModule, MatProgressBarModule, TranslocoModule,
    RichTextEditorComponent,
  ],
  templateUrl: './country-detail.page.html',
  styleUrl: './countries.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CountryDetailPage implements OnInit {
  private readonly api = inject(CountryApiService);
  private readonly content = inject(ContentApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  private readonly locale = inject(LocaleService);

  /** Active locale → pick the matching bilingual field for display. */
  readonly isAr = computed(() => this.locale.locale() === 'ar');

  /** Existing NDC asset id, from either read shape (object or bare id). */
  readonly ndcAssetId = computed(
    () => this.profile()?.ndcDocument?.assetId ?? this.profile()?.ndcAssetId ?? null,
  );

  /** Inline-image uploader for the rich-text editors (asset media store). */
  readonly uploadImage = async (file: File): Promise<string | null> => {
    const res = await this.content.uploadMedia(file);
    return res.ok ? res.value.url : null;
  };

  readonly countryId = signal('');
  readonly country = signal<Country | null>(null);
  readonly profile = signal<CountryProfile | null>(null);
  readonly profileMissing = signal(false);
  readonly loading = signal(false);
  readonly savingCountry = signal(false);
  readonly savingProfile = signal(false);
  readonly errorKind = signal<string | null>(null);

  readonly countryForm = new FormGroup<CountryForm>({
    nameAr: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    nameEn: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    regionAr: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    regionEn: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    isActive: new FormControl(true, { nonNullable: true }),
  });

  readonly profileForm = new FormGroup<ProfileForm>({
    descriptionAr: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    descriptionEn: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    keyInitiativesAr: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    keyInitiativesEn: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    contactInfoAr: new FormControl('', { nonNullable: true }),
    contactInfoEn: new FormControl('', { nonNullable: true }),
    population: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.min(1), Validators.pattern(/^\d+$/)],
    }),
    areaSqKm: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.min(0.01)],
    }),
    gdpPerCapita: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.min(0.01)],
    }),
  });

  readonly selectedNdc = signal<File | null>(null);
  readonly ndcError = signal<string | null>(null);

  onNdcChange(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0] ?? null;
    this.ndcError.set(null);
    if (!file) { this.selectedNdc.set(null); return; }
    if (!ALLOWED_NDC_TYPES.includes(file.type)) {
      this.ndcError.set('countries.field.ndcTypeError');
      this.selectedNdc.set(null);
      return;
    }
    this.selectedNdc.set(file);
  }

  /** Open the currently-stored NDC document (by its asset id) in a new tab. */
  async viewNdc(): Promise<void> {
    const id = this.ndcAssetId();
    if (!id) return;
    const res = await this.content.getAsset(id);
    if (res.ok) window.open(res.value.url, '_blank', 'noopener');
    else this.toast.error(`errors.${res.error.kind}`);
  }

  async ngOnInit(): Promise<void> {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) return;
    this.countryId.set(id);
    await this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.errorKind.set(null);
    const [countryRes, profileRes] = await Promise.all([
      this.api.getCountry(this.countryId()),
      this.api.getProfile(this.countryId()),
    ]);
    this.loading.set(false);

    if (countryRes.ok) {
      this.country.set(countryRes.value);
      this.countryForm.patchValue({
        nameAr: countryRes.value.nameAr,
        nameEn: countryRes.value.nameEn,
        regionAr: countryRes.value.regionAr,
        regionEn: countryRes.value.regionEn,
        isActive: countryRes.value.isActive,
      });
    } else this.errorKind.set(countryRes.error.kind);

    if (profileRes.ok) {
      this.profile.set(profileRes.value);
      this.profileForm.patchValue({
        descriptionAr: profileRes.value.descriptionAr,
        descriptionEn: profileRes.value.descriptionEn,
        keyInitiativesAr: profileRes.value.keyInitiativesAr,
        keyInitiativesEn: profileRes.value.keyInitiativesEn,
        contactInfoAr: profileRes.value.contactInfoAr ?? '',
        contactInfoEn: profileRes.value.contactInfoEn ?? '',
        population: profileRes.value.population != null ? String(profileRes.value.population) : '',
        areaSqKm: profileRes.value.areaSqKm != null ? String(profileRes.value.areaSqKm) : '',
        gdpPerCapita: profileRes.value.gdpPerCapita != null ? String(profileRes.value.gdpPerCapita) : '',
      });
    } else if (profileRes.error.kind === 'not-found') {
      this.profile.set(null);
      this.profileMissing.set(true);
    }
  }

  async saveCountry(): Promise<void> {
    if (this.countryForm.invalid) {
      this.countryForm.markAllAsTouched();
      return;
    }
    this.savingCountry.set(true);
    const v = this.countryForm.getRawValue();
    const res = await this.api.updateCountry(this.countryId(), v);
    this.savingCountry.set(false);
    if (res.ok) {
      this.country.set(res.value);
      this.toast.success('countries.edit.toast');
    } else this.toast.error(`errors.${res.error.kind}`);
  }

  async saveProfile(): Promise<void> {
    if (this.profileForm.invalid) {
      this.profileForm.markAllAsTouched();
      return;
    }
    this.savingProfile.set(true);

    // Upload a newly-selected NDC document; otherwise keep the existing one.
    let ndcAssetId: string | null = this.ndcAssetId();
    if (this.selectedNdc()) {
      const uploadRes = await this.content.uploadAsset(this.selectedNdc()!);
      if (!uploadRes.ok) {
        this.savingProfile.set(false);
        this.toast.error(`errors.${uploadRes.error.kind}`);
        return;
      }
      ndcAssetId = uploadRes.value.id;
    }

    const v = this.profileForm.getRawValue();
    const res = await this.api.upsertProfile(this.countryId(), {
      descriptionAr: v.descriptionAr,
      descriptionEn: v.descriptionEn,
      keyInitiativesAr: v.keyInitiativesAr,
      keyInitiativesEn: v.keyInitiativesEn,
      contactInfoAr: v.contactInfoAr || null,
      contactInfoEn: v.contactInfoEn || null,
      population: v.population ? parseInt(v.population, 10) : null,
      areaSqKm: v.areaSqKm ? parseFloat(v.areaSqKm) : null,
      gdpPerCapita: v.gdpPerCapita ? parseFloat(v.gdpPerCapita) : null,
      ndcAssetId,
      rowVersion: this.profile()?.rowVersion ?? '',
    });
    this.savingProfile.set(false);
    if (res.ok) this.selectedNdc.set(null);
    if (res.ok) {
      this.profile.set(res.value);
      this.profileMissing.set(false);
      this.toast.success('countries.profile.toast');
    } else this.toast.error(`errors.${res.error.kind}`);
  }
}
