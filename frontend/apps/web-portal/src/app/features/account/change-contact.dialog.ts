import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslocoModule } from '@jsverse/transloco';
import { AccountApiService } from './account-api.service';

export interface ChangeContactDialogData {
  type: 'email' | 'phone';
}

@Component({
  selector: 'cce-change-contact-dialog',
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
  ],
  templateUrl: './change-contact.dialog.html',
  styleUrl: './change-contact.dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChangeContactDialogComponent {
  private readonly api = inject(AccountApiService);
  private readonly ref = inject(MatDialogRef<ChangeContactDialogComponent, boolean>);
  readonly data = inject<ChangeContactDialogData>(MAT_DIALOG_DATA);

  readonly step = signal<'request' | 'verify'>('request');
  readonly sending = signal(false);
  readonly confirming = signal(false);
  readonly errorKind = signal<string | null>(null);

  private verificationId = '';

  readonly newValueControl = new FormControl('', [
    Validators.required,
    ...(this.data.type === 'email'
      ? [Validators.email]
      : [Validators.pattern(/^\+[1-9]\d{6,14}$/)]),
  ]);

  readonly otpControl = new FormControl('', [
    Validators.required,
    Validators.pattern(/^\d{4,8}$/),
  ]);

  async requestChange(): Promise<void> {
    if (this.newValueControl.invalid) {
      this.newValueControl.markAsTouched();
      return;
    }
    this.sending.set(true);
    this.errorKind.set(null);
    const value = this.newValueControl.value!.trim();
    const res = this.data.type === 'email'
      ? await this.api.requestEmailChange(value)
      : await this.api.requestPhoneChange(value);
    this.sending.set(false);
    if (res.ok) {
      this.verificationId = res.value.verificationId;
      this.step.set('verify');
    } else {
      this.errorKind.set(res.error.kind);
    }
  }

  async confirmChange(): Promise<void> {
    if (this.otpControl.invalid) {
      this.otpControl.markAsTouched();
      return;
    }
    this.confirming.set(true);
    this.errorKind.set(null);
    const code = this.otpControl.value!.trim();
    const res = this.data.type === 'email'
      ? await this.api.confirmEmailChange(this.verificationId, code)
      : await this.api.confirmPhoneChange(this.verificationId, code);
    this.confirming.set(false);
    if (res.ok) {
      this.ref.close(true);
    } else {
      this.errorKind.set(res.error.kind);
    }
  }

  back(): void {
    this.step.set('request');
    this.otpControl.reset();
    this.errorKind.set(null);
  }
}
