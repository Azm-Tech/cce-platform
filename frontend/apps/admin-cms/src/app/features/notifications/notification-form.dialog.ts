import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, Inject, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { TranslateModule } from '@ngx-translate/core';
import { NotificationApiService } from './notification-api.service';
import {
  NOTIFICATION_CHANNELS,
  type NotificationChannel,
  type NotificationTemplate,
} from './notification.types';

export interface NotificationFormData {
  template?: NotificationTemplate;
}

interface NotificationForm {
  code: FormControl<string>;
  subjectAr: FormControl<string>;
  subjectEn: FormControl<string>;
  bodyAr: FormControl<string>;
  bodyEn: FormControl<string>;
  channel: FormControl<NotificationChannel>;
  variableSchemaJson: FormControl<string>;
  isActive: FormControl<boolean>;
}

@Component({
  selector: 'cce-notification-form-dialog',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatButtonModule, MatCheckboxModule,
    MatDialogModule, MatFormFieldModule, MatInputModule, MatSelectModule, TranslateModule,
  ],
  templateUrl: './notification-form.dialog.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotificationFormDialogComponent {
  private readonly api = inject(NotificationApiService);
  readonly channels = NOTIFICATION_CHANNELS;
  readonly form: FormGroup<NotificationForm>;
  readonly saving = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly isEdit: boolean;

  constructor(
    private readonly ref: MatDialogRef<NotificationFormDialogComponent, NotificationTemplate | null>,
    @Inject(MAT_DIALOG_DATA) readonly data: NotificationFormData,
  ) {
    this.isEdit = data.template !== undefined;
    const t = data.template;
    this.form = new FormGroup<NotificationForm>({
      code: new FormControl(t?.code ?? '', { nonNullable: true, validators: [Validators.required] }),
      subjectAr: new FormControl(t?.subjectAr ?? '', { nonNullable: true, validators: [Validators.required] }),
      subjectEn: new FormControl(t?.subjectEn ?? '', { nonNullable: true, validators: [Validators.required] }),
      bodyAr: new FormControl(t?.bodyAr ?? '', { nonNullable: true, validators: [Validators.required] }),
      bodyEn: new FormControl(t?.bodyEn ?? '', { nonNullable: true, validators: [Validators.required] }),
      channel: new FormControl<NotificationChannel>(t?.channel ?? 'Email', { nonNullable: true, validators: [Validators.required] }),
      variableSchemaJson: new FormControl(t?.variableSchemaJson ?? '{}', { nonNullable: true, validators: [Validators.required] }),
      isActive: new FormControl(t?.isActive ?? true, { nonNullable: true }),
    });
    if (this.isEdit) {
      this.form.controls.code.disable();
      this.form.controls.channel.disable();
      this.form.controls.variableSchemaJson.disable();
    }
  }

  async save(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving.set(true);
    this.errorKind.set(null);
    const v = this.form.getRawValue();

    const res = this.isEdit && this.data.template
      ? await this.api.update(this.data.template.id, {
          subjectAr: v.subjectAr,
          subjectEn: v.subjectEn,
          bodyAr: v.bodyAr,
          bodyEn: v.bodyEn,
          isActive: v.isActive,
        })
      : await this.api.create({
          code: v.code,
          subjectAr: v.subjectAr,
          subjectEn: v.subjectEn,
          bodyAr: v.bodyAr,
          bodyEn: v.bodyEn,
          channel: v.channel,
          variableSchemaJson: v.variableSchemaJson,
        });
    this.saving.set(false);
    if (res.ok) this.ref.close(res.value);
    else this.errorKind.set(res.error.kind);
  }

  cancel(): void { this.ref.close(null); }
}
