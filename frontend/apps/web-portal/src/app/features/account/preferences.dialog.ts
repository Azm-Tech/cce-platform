import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { TranslocoModule } from '@jsverse/transloco';
import { LocaleService } from '@frontend/i18n';
import { ToastService } from '@frontend/ui-kit';
import { CommunityApiService } from '../community/community-api.service';
import type { PublicTopic } from '../community/community.types';
import { CountriesApiService } from '../countries/countries-api.service';
import type { Country } from '../countries/country.types';
import { AccountApiService } from './account-api.service';
import {
  PERSONALIZED_KNOWLEDGE_LEVELS,
  SECTORS_OF_WORK,
  type PersonalizedKnowledgeLevel,
  type PersonalizedSuggestionsPayload,
  type SectorOfWork,
  type UserProfile,
} from './account.types';

@Component({
  selector: 'cce-preferences-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatProgressBarModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    TranslocoModule,
  ],
  templateUrl: './preferences.dialog.html',
  styleUrl: './preferences.dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PreferencesDialogComponent implements OnInit {
  private readonly api = inject(AccountApiService);
  private readonly communityApi = inject(CommunityApiService);
  private readonly countriesApi = inject(CountriesApiService);
  private readonly locale = inject(LocaleService);
  private readonly toast = inject(ToastService);
  private readonly ref = inject(MatDialogRef<PreferencesDialogComponent, boolean>);

  readonly profile = inject<UserProfile>(MAT_DIALOG_DATA);

  readonly topics = signal<PublicTopic[]>([]);
  readonly countries = signal<Country[]>([]);
  readonly topicsLoading = signal(true);
  readonly saving = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly interestsError = signal(false);

  readonly selectedTopicIds = signal<Set<string>>(new Set());
  readonly knowledgeLevels = PERSONALIZED_KNOWLEDGE_LEVELS;
  readonly sectorsOfWork = SECTORS_OF_WORK;

  readonly isAr = computed(() => this.locale.locale() === 'ar');
  readonly topicName = (t: PublicTopic) => this.isAr() ? t.nameAr : t.nameEn;

  readonly form = new FormGroup({
    knowledgeLevel: new FormControl<PersonalizedKnowledgeLevel | null>(null, Validators.required),
    sectorOfWork: new FormControl<SectorOfWork | null>(null, Validators.required),
    countryId: new FormControl<string | null>(null, Validators.required),
  });

  async ngOnInit(): Promise<void> {
    this.form.patchValue({ countryId: this.profile.countryId ?? null });
    if (this.profile.interests?.length) {
      this.selectedTopicIds.set(new Set(this.profile.interests));
    }

    const [topicsRes, countriesRes] = await Promise.all([
      this.communityApi.listTopics(),
      this.countriesApi.listCountries(),
    ]);
    this.topicsLoading.set(false);
    if (topicsRes.ok) {
      this.topics.set(topicsRes.value.filter((t) => t.parentId === null));
    }
    if (countriesRes.ok) this.countries.set(countriesRes.value);
  }

  toggleTopic(id: string): void {
    const next = new Set(this.selectedTopicIds());
    if (next.has(id)) next.delete(id);
    else next.add(id);
    this.selectedTopicIds.set(next);
    if (next.size > 0) this.interestsError.set(false);
  }

  isSelected(id: string): boolean {
    return this.selectedTopicIds().has(id);
  }

  skip(): void {
    localStorage.setItem('cce_prefs_shown', '1');
    this.ref.close(false);
  }

  async save(): Promise<void> {
    this.form.markAllAsTouched();
    const noInterests = this.selectedTopicIds().size === 0;
    this.interestsError.set(noInterests);
    if (this.form.invalid || noInterests) return;

    const v = this.form.getRawValue();
    const payload: PersonalizedSuggestionsPayload = {
      interests: [...this.selectedTopicIds()],
      knowledgeLevel: v.knowledgeLevel!,
      sectorOfWork: v.sectorOfWork!,
      countryId: v.countryId!,
    };
    this.saving.set(true);
    this.errorKind.set(null);
    const res = await this.api.submitPersonalizedSuggestions(payload);
    this.saving.set(false);
    if (res.ok) {
      localStorage.setItem('cce_prefs_shown', '1');
      this.toast.success('preferences.success');
      this.ref.close(true);
    } else {
      this.errorKind.set(res.error.kind);
    }
  }
}
