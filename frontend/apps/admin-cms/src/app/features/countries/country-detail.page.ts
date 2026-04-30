import { CommonModule, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslateModule } from '@ngx-translate/core';
import { ToastService } from '@frontend/ui-kit';
import { CountryApiService } from './country-api.service';
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
}

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
    MatIconModule, MatInputModule, MatProgressBarModule, TranslateModule,
  ],
  templateUrl: './country-detail.page.html',
  styleUrl: './countries.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CountryDetailPage implements OnInit {
  private readonly api = inject(CountryApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);

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
  });

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
    const v = this.profileForm.getRawValue();
    const res = await this.api.upsertProfile(this.countryId(), {
      descriptionAr: v.descriptionAr,
      descriptionEn: v.descriptionEn,
      keyInitiativesAr: v.keyInitiativesAr,
      keyInitiativesEn: v.keyInitiativesEn,
      contactInfoAr: v.contactInfoAr || null,
      contactInfoEn: v.contactInfoEn || null,
      rowVersion: this.profile()?.rowVersion ?? '',
    });
    this.savingProfile.set(false);
    if (res.ok) {
      this.profile.set(res.value);
      this.profileMissing.set(false);
      this.toast.success('countries.profile.toast');
    } else this.toast.error(`errors.${res.error.kind}`);
  }
}
