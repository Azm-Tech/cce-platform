import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormControl, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { TranslateModule } from '@ngx-translate/core';
import { ToastService } from '@frontend/ui-kit';
import { AccountApiService } from '../../features/account/account-api.service';
import type { ServiceRatingPayload } from '../../features/account/account.types';

export interface ServiceRatingDialogData {
  page: string;
  locale: 'ar' | 'en';
}

export interface ServiceRatingDialogResult {
  submitted: boolean;
}

@Component({
  selector: 'cce-service-rating-dialog',
  standalone: true,
  imports: [
    CommonModule, FormsModule, ReactiveFormsModule,
    MatDialogModule, MatButtonModule, MatFormFieldModule, MatIconModule,
    MatInputModule, TranslateModule,
  ],
  templateUrl: './service-rating-dialog.component.html',
  styleUrl: './service-rating-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ServiceRatingDialogComponent {
  private readonly api = inject(AccountApiService);
  private readonly toast = inject(ToastService);
  private readonly dialogRef = inject<MatDialogRef<ServiceRatingDialogComponent, ServiceRatingDialogResult>>(MatDialogRef);
  readonly data = inject<ServiceRatingDialogData>(MAT_DIALOG_DATA);

  readonly stars = [1, 2, 3, 4, 5] as const;
  readonly rating = signal(0);
  readonly comment = new FormControl('', [Validators.maxLength(2000)]);
  readonly submitting = signal(false);
  readonly errorKind = signal<string | null>(null);

  readonly canSubmit = computed(() => this.rating() >= 1 && this.rating() <= 5 && !this.submitting());

  setRating(value: number): void {
    this.rating.set(value);
  }

  isStarFilled(starValue: number): boolean {
    return starValue <= this.rating();
  }

  starLabel(starValue: number): string {
    return `${starValue} of 5 stars`;
  }

  async submit(): Promise<void> {
    if (!this.canSubmit()) return;
    const commentText = (this.comment.value ?? '').trim() || null;
    const payload: ServiceRatingPayload = {
      rating: this.rating(),
      commentAr: this.data.locale === 'ar' ? commentText : null,
      commentEn: this.data.locale === 'en' ? commentText : null,
      page: this.data.page,
      locale: this.data.locale,
    };
    this.submitting.set(true);
    this.errorKind.set(null);
    const res = await this.api.submitServiceRating(payload);
    this.submitting.set(false);
    if (res.ok) {
      this.toast.success('account.serviceRating.toast.thanks');
      this.dialogRef.close({ submitted: true });
    } else {
      this.errorKind.set(res.error.kind);
    }
  }

  cancel(): void {
    this.dialogRef.close({ submitted: false });
  }
}
