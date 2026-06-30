import { ChangeDetectionStrategy, Component, Inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { TranslocoModule } from '@jsverse/transloco';

export interface ModerationRejectDialogData {
  /** Short preview of the content being rejected — shown read-only for context. */
  contentPreview: string | null;
}

export type ModerationRejectDialogResult = { reason: string };

interface RejectForm {
  reason: FormControl<string>;
}

/**
 * Reject-confirmation dialog for the moderation queue. Collects an OPTIONAL
 * reason (the API accepts none) and shows the content preview for context.
 * Returns `{ reason }` on confirm, `null` on cancel.
 */
@Component({
  selector: 'cce-moderation-reject-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    TranslocoModule,
  ],
  templateUrl: './moderation-reject.dialog.html',
  styleUrl: './moderation-reject.dialog.scss',
  // Default CD — dialog overlay + Transloco require it.
  changeDetection: ChangeDetectionStrategy.Default,
})
export class ModerationRejectDialogComponent {
  readonly form: FormGroup<RejectForm> = new FormGroup<RejectForm>({
    reason: new FormControl('', { nonNullable: true }),
  });

  constructor(
    private readonly ref: MatDialogRef<ModerationRejectDialogComponent, ModerationRejectDialogResult | null>,
    @Inject(MAT_DIALOG_DATA) readonly data: ModerationRejectDialogData,
  ) {}

  confirm(): void {
    this.ref.close({ reason: this.form.getRawValue().reason.trim() });
  }

  cancel(): void {
    this.ref.close(null);
  }
}
