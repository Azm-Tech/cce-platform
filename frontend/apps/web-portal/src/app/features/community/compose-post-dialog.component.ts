import { ChangeDetectionStrategy, Component, inject, viewChild } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { TranslocoModule } from '@jsverse/transloco';
import {
  ComposePostFormComponent,
  type ComposePostSubmittedEvent,
} from './compose-post-form.component';
import type { PublicTopic } from './community.types';

export interface ComposePostDialogData {
  topics: PublicTopic[];
  preselectedTopicId?: string | null;
}

export interface ComposePostDialogResult {
  submitted: boolean;
  postId?: string;
}

@Component({
  selector: 'cce-compose-post-dialog',
  standalone: true,
  imports: [MatDialogModule, MatIconModule, TranslocoModule, ComposePostFormComponent],
  templateUrl: './compose-post-dialog.component.html',
  styleUrl: './compose-post-dialog.component.scss',
  // Default required — dialog overlay boundary breaks Transloco with OnPush
  changeDetection: ChangeDetectionStrategy.Default,
})
export class ComposePostDialogComponent {
  readonly data = inject<ComposePostDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef =
    inject<MatDialogRef<ComposePostDialogComponent, ComposePostDialogResult>>(MatDialogRef);

  readonly formRef = viewChild.required(ComposePostFormComponent);

  onFormSubmitted(event: ComposePostSubmittedEvent): void {
    this.dialogRef.close({ submitted: true, postId: event.postId });
  }

  cancel(): void {
    this.dialogRef.close({ submitted: false });
  }

  /** Open the dialog with consistent width/config from any caller. */
  static open(
    dialog: MatDialog,
    data: ComposePostDialogData,
  ): MatDialogRef<ComposePostDialogComponent, ComposePostDialogResult> {
    return dialog.open<ComposePostDialogComponent, ComposePostDialogData, ComposePostDialogResult>(
      ComposePostDialogComponent,
      {
        data,
        width: '600px',
        maxWidth: '96vw',
        autoFocus: 'first-tabbable',
      },
    );
  }
}
