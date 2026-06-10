import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslocoModule } from '@jsverse/transloco';
import { LocaleService } from '@frontend/i18n';
import { ToastService } from '@frontend/ui-kit';
import { CountriesApiService } from '../countries/countries-api.service';
import type { Country } from '../countries/country.types';
import { AccountApiService } from './account-api.service';
import type { InterestQuestion, InterestTopicOption } from './account.types';

@Component({
  selector: 'cce-preferences-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatAutocompleteModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressBarModule,
    MatProgressSpinnerModule,
    TranslocoModule,
  ],
  templateUrl: './preferences.dialog.html',
  styleUrl: './preferences.dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PreferencesDialogComponent implements OnInit {
  private readonly api = inject(AccountApiService);
  private readonly countriesApi = inject(CountriesApiService);
  private readonly locale = inject(LocaleService);
  private readonly toast = inject(ToastService);
  private readonly ref = inject(MatDialogRef<PreferencesDialogComponent, boolean>);

  readonly questions = signal<InterestQuestion[]>([]);
  readonly countries = signal<Country[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly errorKind = signal<string | null>(null);

  readonly selectedCarbonIds = signal<Set<string>>(new Set());
  readonly selectedKnowledgeId = signal<string | null>(null);
  readonly selectedJobSectorId = signal<string | null>(null);
  readonly targetCountryId = new FormControl<string | null>(null, Validators.required);

  readonly countrySearch = new FormControl('');
  private readonly countrySearchValue = toSignal(this.countrySearch.valueChanges, { initialValue: '' });
  readonly filteredCountries = computed(() => {
    const q = (this.countrySearchValue() ?? '').trim().toLowerCase();
    const all = this.countries();
    if (!q) return all;
    return all.filter(c =>
      c.nameAr.includes(q) || c.nameEn.toLowerCase().includes(q)
    );
  });

  readonly carbonQuestion = computed(() => this.questions().find(q => q.category === 'carbon_area') ?? null);
  readonly knowledgeQuestion = computed(() => this.questions().find(q => q.category === 'knowledge_assessment') ?? null);
  readonly jobSectorQuestion = computed(() => this.questions().find(q => q.category === 'job_sector') ?? null);

  readonly carbonError = signal(false);
  readonly knowledgeError = signal(false);
  readonly jobSectorError = signal(false);

  readonly isAr = computed(() => this.locale.locale() === 'ar');
  readonly optionName = (opt: InterestTopicOption) => this.isAr() ? opt.nameAr : opt.nameEn;

  async ngOnInit(): Promise<void> {
    const [questionsRes, interestsRes, countriesRes] = await Promise.all([
      this.api.getInterestQuestions(),
      this.api.getMyInterests(),
      this.countriesApi.listCountries(),
    ]);
    this.loading.set(false);

    if (questionsRes.ok) this.questions.set(questionsRes.value);
    if (countriesRes.ok) this.countries.set(countriesRes.value);

    if (interestsRes.ok) {
      const saved = interestsRes.value;
      if (saved.carbonAreaTopics?.length) {
        this.selectedCarbonIds.set(new Set(saved.carbonAreaTopics.map(t => t.id)));
      }
      if (saved.knowledgeAssessmentTopic) {
        this.selectedKnowledgeId.set(saved.knowledgeAssessmentTopic.id);
      }
      if (saved.jobSectorTopic) {
        this.selectedJobSectorId.set(saved.jobSectorTopic.id);
      }
      if (saved.targetCountryId) {
        this.targetCountryId.setValue(saved.targetCountryId);
        const match = this.countries().find(c => c.id === saved.targetCountryId);
        if (match) {
          this.countrySearch.setValue(this.isAr() ? match.nameAr : match.nameEn, { emitEvent: false });
        }
      }
    }
  }

  onTargetCountrySelected(id: string | null, displayText: string): void {
    this.targetCountryId.setValue(id);
    this.countrySearch.setValue(id === null ? '' : displayText, { emitEvent: false });
    this.targetCountryId.markAsTouched();
  }

  toggleCarbon(id: string): void {
    const next = new Set(this.selectedCarbonIds());
    if (next.has(id)) next.delete(id);
    else next.add(id);
    this.selectedCarbonIds.set(next);
    if (next.size > 0) this.carbonError.set(false);
  }

  selectKnowledge(id: string): void {
    this.selectedKnowledgeId.set(id);
    this.knowledgeError.set(false);
  }

  selectJobSector(id: string): void {
    this.selectedJobSectorId.set(id);
    this.jobSectorError.set(false);
  }

  optionIcon(opt: InterestTopicOption): string {
    const icons: Record<string, string> = {
      High: 'workspace_premium', Medium: 'auto_stories', Low: 'school',
      Government: 'account_balance', Academic: 'school', Private: 'business_center',
    };
    return icons[opt.nameEn] ?? 'label';
  }

  skip(): void {
    localStorage.setItem('cce_prefs_shown', '1');
    this.ref.close(false);
  }

  async save(): Promise<void> {
    const carbonIds = [...this.selectedCarbonIds()];
    const knowledgeId = this.selectedKnowledgeId();
    const jobSectorId = this.selectedJobSectorId();
    const countryId = this.targetCountryId.value;

    this.carbonError.set(carbonIds.length === 0);
    this.knowledgeError.set(!knowledgeId);
    this.jobSectorError.set(!jobSectorId);
    this.targetCountryId.markAsTouched();

    if (!carbonIds.length || !knowledgeId || !jobSectorId || !countryId) return;

    this.saving.set(true);
    this.errorKind.set(null);
    const res = await this.api.updateMyInterests({
      carbonAreaIds: carbonIds,
      knowledgeAssessmentId: knowledgeId,
      jobSectorId,
      targetCountryId: countryId,
    });
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
