import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import {
  FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators,
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatRadioModule } from '@angular/material/radio';
import { MatSelectModule } from '@angular/material/select';
import { TranslateModule } from '@ngx-translate/core';
import { LocaleService } from '@frontend/i18n';
import { ToastService } from '@frontend/ui-kit';
import { CountriesApiService } from '../countries/countries-api.service';
import type { Country } from '../countries/country.types';
import { AccountApiService } from './account-api.service';
import { KNOWLEDGE_LEVELS, type KnowledgeLevel, type UpdateMyProfilePayload, type UserProfile } from './account.types';

interface ProfileFormShape {
  localePreference: FormControl<string>;
  knowledgeLevel: FormControl<KnowledgeLevel>;
  interests: FormControl<string>;        // comma-separated; serialized to string[] on submit
  countryId: FormControl<string | null>;
  avatarUrl: FormControl<string | null>;
}

@Component({
  selector: 'cce-profile-page',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatButtonModule, MatChipsModule, MatFormFieldModule, MatIconModule,
    MatInputModule, MatProgressBarModule, MatRadioModule, MatSelectModule,
    TranslateModule,
  ],
  templateUrl: './profile.page.html',
  styleUrl: './profile.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProfilePage implements OnInit {
  private readonly api = inject(AccountApiService);
  private readonly countriesApi = inject(CountriesApiService);
  private readonly localeService = inject(LocaleService);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(FormBuilder);

  readonly profile = signal<UserProfile | null>(null);
  readonly countries = signal<Country[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly saveErrorKind = signal<string | null>(null);
  readonly mode = signal<'view' | 'edit'>('view');

  readonly knowledgeLevels = KNOWLEDGE_LEVELS;
  readonly locale = this.localeService.locale;

  readonly notProvisioned = computed(() => this.errorKind() === 'not-found');

  readonly countryName = computed(() => {
    const p = this.profile();
    if (!p?.countryId) return '—';
    const match = this.countries().find((c) => c.id === p.countryId);
    if (!match) return '—';
    return this.locale() === 'ar' ? match.nameAr : match.nameEn;
  });

  readonly form: FormGroup<ProfileFormShape> = this.fb.nonNullable.group({
    localePreference: this.fb.nonNullable.control('en', Validators.required),
    knowledgeLevel: this.fb.nonNullable.control<KnowledgeLevel>('Beginner', Validators.required),
    interests: this.fb.nonNullable.control(''),
    countryId: this.fb.control<string | null>(null),
    avatarUrl: this.fb.control<string | null>(null),
  });

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.errorKind.set(null);
    const [profileRes, countriesRes] = await Promise.all([
      this.api.getProfile(),
      this.countriesApi.listCountries({}),
    ]);
    this.loading.set(false);
    if (profileRes.ok) this.profile.set(profileRes.value);
    else this.errorKind.set(profileRes.error.kind);
    if (countriesRes.ok) this.countries.set(countriesRes.value);
  }

  enterEditMode(): void {
    const p = this.profile();
    if (!p) return;
    this.form.reset({
      localePreference: p.localePreference,
      knowledgeLevel: p.knowledgeLevel,
      interests: p.interests.join(', '),
      countryId: p.countryId,
      avatarUrl: p.avatarUrl,
    });
    this.saveErrorKind.set(null);
    this.mode.set('edit');
  }

  cancelEdit(): void {
    this.mode.set('view');
    this.saveErrorKind.set(null);
  }

  async save(): Promise<void> {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    const payload: UpdateMyProfilePayload = {
      localePreference: v.localePreference,
      knowledgeLevel: v.knowledgeLevel,
      interests: this.parseInterests(v.interests),
      avatarUrl: v.avatarUrl,
      countryId: v.countryId,
    };
    this.saving.set(true);
    this.saveErrorKind.set(null);
    const res = await this.api.updateProfile(payload);
    this.saving.set(false);
    if (res.ok) {
      this.profile.set(res.value);
      this.mode.set('view');
      this.toast.success('account.profile.toast.saved');
    } else {
      this.saveErrorKind.set(res.error.kind);
    }
  }

  retry(): void {
    void this.load();
  }

  /** Splits the comma-separated interests input into a deduped, trimmed string[]. */
  private parseInterests(raw: string): string[] {
    return Array.from(
      new Set(
        raw
          .split(',')
          .map((s) => s.trim())
          .filter((s) => s.length > 0),
      ),
    );
  }
}
