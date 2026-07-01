import { CommonModule, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import {
  FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators,
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { Router } from '@angular/router';
import { TranslocoModule } from '@jsverse/transloco';
import { LocaleService } from '@frontend/i18n';
import { ToastService, TranslateFieldComponent } from '@frontend/ui-kit';
import { MediaApiService } from '../../core/media/media-api.service';
import { AccountApiService } from './account-api.service';
import type { ExpertRequestStatus, InterestTopicOption, SubmitExpertRequestPayload } from './account.types';

interface ExpertFormShape {
  requestedBioAr: FormControl<string>;
  requestedBioEn: FormControl<string>;
}

@Component({
  selector: 'cce-expert-request-page',
  standalone: true,
  imports: [
    CommonModule, DatePipe, ReactiveFormsModule,
    MatButtonModule, MatFormFieldModule, MatInputModule, MatProgressBarModule,
    TranslateFieldComponent, TranslocoModule,
  ],
  templateUrl: './expert-request.page.html',
  styleUrl: './expert-request.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExpertRequestPage implements OnInit {
  private readonly api = inject(AccountApiService);
  private readonly mediaApi = inject(MediaApiService);
  private readonly localeService = inject(LocaleService);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);

  readonly status = signal<ExpertRequestStatus | null>(null);
  readonly allTopics = signal<InterestTopicOption[]>([]);
  readonly selectedTags = signal<Set<string>>(new Set());
  readonly loading = signal(false);
  readonly submitting = signal(false);
  readonly uploadingCv = signal(false);
  readonly cvFileName = signal<string | null>(null);
  readonly cvAssetFileId = signal<string | null>(null);
  readonly errorKind = signal<string | null>(null);
  readonly submitErrorKind = signal<string | null>(null);

  readonly isAr = computed(() => this.locale() === 'ar');

  /** True when status is null (no request yet) OR user clicked resubmit after rejection. */
  readonly showForm = signal(false);

  readonly locale = this.localeService.locale;

  readonly rejectionReason = computed(() => {
    const s = this.status();
    if (s?.status !== 'Rejected') return '';
    return this.locale() === 'ar' ? s.rejectionReasonAr ?? '' : s.rejectionReasonEn ?? '';
  });

  readonly form: FormGroup<ExpertFormShape> = this.fb.nonNullable.group({
    requestedBioAr: this.fb.nonNullable.control('', [Validators.required, Validators.minLength(50), Validators.maxLength(2000)]),
    requestedBioEn: this.fb.nonNullable.control('', [Validators.required, Validators.minLength(50), Validators.maxLength(2000)]),
  });

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.errorKind.set(null);
    const [statusRes, topicsRes] = await Promise.all([
      this.api.getExpertStatus(),
      this.api.getInterestQuestions(),
    ]);
    this.loading.set(false);
    if (statusRes.ok) {
      this.status.set(statusRes.value);
      this.showForm.set(statusRes.value === null);
    } else {
      this.errorKind.set(statusRes.error.kind);
    }
    if (topicsRes.ok) {
      const seen = new Set<string>();
      const flat: InterestTopicOption[] = [];
      for (const q of topicsRes.value) {
        for (const opt of q.options) {
          if (opt.isActive && !seen.has(opt.id)) { seen.add(opt.id); flat.push(opt); }
        }
      }
      this.allTopics.set(flat);
    }
  }

  resubmit(): void {
    this.form.reset();
    this.submitErrorKind.set(null);
    this.cvFileName.set(null);
    this.cvAssetFileId.set(null);
    this.selectedTags.set(new Set());
    this.showForm.set(true);
  }

  toggleTag(id: string): void {
    const next = new Set(this.selectedTags());
    if (next.has(id)) { next.delete(id); } else { next.add(id); }
    this.selectedTags.set(next);
  }

  async onCvFileChange(event: Event): Promise<void> {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.uploadingCv.set(true);
    const res = await this.mediaApi.uploadAsset(file);
    this.uploadingCv.set(false);
    if (res.ok) {
      this.cvAssetFileId.set(res.value.id);
      this.cvFileName.set(file.name);
    } else {
      this.toast.error('account.expert.cvUploadError');
    }
  }

  async submit(): Promise<void> {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    const payload: SubmitExpertRequestPayload = {
      requestedBioAr: v.requestedBioAr,
      requestedBioEn: v.requestedBioEn,
      requestedTags: [...this.selectedTags()].map(id => this.allTopics().find(t => t.id === id)?.nameEn ?? '').filter(Boolean),
      cvAssetFileId: this.cvAssetFileId(),
    };
    this.submitting.set(true);
    this.submitErrorKind.set(null);
    const res = await this.api.submitExpertRequest(payload);
    this.submitting.set(false);
    if (res.ok) {
      this.status.set(res.value);
      this.showForm.set(false);
      this.toast.success('account.expert.toast.submitted');
      void this.router.navigate(['/me/profile']);
    } else {
      this.submitErrorKind.set(res.error.kind);
    }
  }

}
