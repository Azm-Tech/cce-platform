import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { TranslocoModule } from '@jsverse/transloco';
import { ToastService, TranslateFieldComponent } from '@frontend/ui-kit';
import { CountryApiService } from '../countries/country-api.service';
import type { Country } from '../countries/country.types';
import { PublishingApiService } from './publishing-api.service';

interface SettingsForm {
  videoUrl: FormControl<string>;
  objectiveAr: FormControl<string>;
  objectiveEn: FormControl<string>;
  cceConceptsAr: FormControl<string>;
  cceConceptsEn: FormControl<string>;
  participatingCountryIds: FormControl<string[]>;
}

@Component({
  selector: 'cce-homepage-settings',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressBarModule,
    MatSelectModule,
    TranslocoModule,
    TranslateFieldComponent,
  ],
  templateUrl: './homepage-settings.page.html',
  styleUrl: './homepage-settings.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HomepageSettingsPage implements OnInit {
  private readonly api = inject(PublishingApiService);
  private readonly countryApi = inject(CountryApiService);
  private readonly toast = inject(ToastService);

  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly saveError = signal<string | null>(null);
  readonly countries = signal<Country[]>([]);

  readonly form = new FormGroup<SettingsForm>({
    videoUrl: new FormControl('', { nonNullable: true }),
    objectiveAr: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(2000)] }),
    objectiveEn: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(2000)] }),
    cceConceptsAr: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(2000)] }),
    cceConceptsEn: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(2000)] }),
    participatingCountryIds: new FormControl<string[]>([], { nonNullable: true }),
  });

  readonly compareCountryId = (a: string | null, b: string | null): boolean => a === b;

  ngOnInit(): void { void this.load(); }

  async load(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(null);
    const [settingsRes, countriesRes] = await Promise.all([
      this.api.getHomepageSettings(),
      this.countryApi.listCountries({ pageSize: 1000, isCceCountry: false }),
    ]);
    this.loading.set(false);
    if (countriesRes.ok) {
      this.countries.set(countriesRes.value.items.sort((a, b) => a.nameEn.localeCompare(b.nameEn)));
    }
    if (settingsRes.ok) {
      const s = settingsRes.value;
      this.form.patchValue({
        videoUrl: s.videoUrl ?? '',
        objectiveAr: s.objectiveAr ?? '',
        objectiveEn: s.objectiveEn ?? '',
        cceConceptsAr: s.cceConceptsAr ?? '',
        cceConceptsEn: s.cceConceptsEn ?? '',
      });
      // Defer multi-select patch until after the @for has projected mat-options
      // into the DOM, otherwise mat-select can't mark them as selected.
      setTimeout(() => {
        this.form.controls.participatingCountryIds.setValue(s.participatingCountryIds ?? []);
      }, 0);
    } else {
      this.loadError.set(settingsRes.error.kind);
    }
  }

  async save(): Promise<void> {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.saving.set(true);
    this.saveError.set(null);
    const v = this.form.getRawValue();
    const res = await this.api.updateHomepageSettings({
      videoUrl: v.videoUrl || null,
      objectiveAr: v.objectiveAr || null,
      objectiveEn: v.objectiveEn || null,
      cceConceptsAr: v.cceConceptsAr || null,
      cceConceptsEn: v.cceConceptsEn || null,
      participatingCountryIds: v.participatingCountryIds,
    });
    this.saving.set(false);
    if (res.ok) {
      this.toast.success('homepageSettings.save.toast');
    } else {
      this.saveError.set(res.error.kind);
      this.toast.error(`errors.${res.error.kind}`);
    }
  }
}
