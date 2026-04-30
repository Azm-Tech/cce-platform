import { CommonModule, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import {
  FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators,
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslateModule } from '@ngx-translate/core';
import { LocaleService } from '@frontend/i18n';
import { ToastService } from '@frontend/ui-kit';
import { AccountApiService } from './account-api.service';
import type { ExpertRequestStatus, SubmitExpertRequestPayload } from './account.types';

interface ExpertFormShape {
  requestedBioAr: FormControl<string>;
  requestedBioEn: FormControl<string>;
  requestedTags: FormControl<string>;     // comma-separated
}

@Component({
  selector: 'cce-expert-request-page',
  standalone: true,
  imports: [
    CommonModule, DatePipe, ReactiveFormsModule,
    MatButtonModule, MatFormFieldModule, MatInputModule, MatProgressBarModule,
    TranslateModule,
  ],
  templateUrl: './expert-request.page.html',
  styleUrl: './expert-request.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExpertRequestPage implements OnInit {
  private readonly api = inject(AccountApiService);
  private readonly localeService = inject(LocaleService);
  private readonly toast = inject(ToastService);
  private readonly fb = inject(FormBuilder);

  readonly status = signal<ExpertRequestStatus | null>(null);
  readonly loading = signal(false);
  readonly submitting = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly submitErrorKind = signal<string | null>(null);

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
    requestedTags: this.fb.nonNullable.control(''),
  });

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.errorKind.set(null);
    const res = await this.api.getExpertStatus();
    this.loading.set(false);
    if (res.ok) {
      this.status.set(res.value);
      this.showForm.set(res.value === null);
    } else {
      this.errorKind.set(res.error.kind);
    }
  }

  resubmit(): void {
    this.form.reset();
    this.submitErrorKind.set(null);
    this.showForm.set(true);
  }

  async submit(): Promise<void> {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    const payload: SubmitExpertRequestPayload = {
      requestedBioAr: v.requestedBioAr,
      requestedBioEn: v.requestedBioEn,
      requestedTags: this.parseTags(v.requestedTags),
    };
    this.submitting.set(true);
    this.submitErrorKind.set(null);
    const res = await this.api.submitExpertRequest(payload);
    this.submitting.set(false);
    if (res.ok) {
      this.status.set(res.value);
      this.showForm.set(false);
      this.toast.success('account.expert.toast.submitted');
    } else {
      this.submitErrorKind.set(res.error.kind);
    }
  }

  private parseTags(raw: string): string[] {
    return Array.from(
      new Set(
        raw
          .split(',')
          .map((s) => s.trim())
          .filter((s) => s.length > 0),
      ),
    ).slice(0, 10);
  }
}
