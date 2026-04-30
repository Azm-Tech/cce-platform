import { CommonModule } from '@angular/common';
import { Component, Inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { TranslateModule } from '@ngx-translate/core';

export interface ConfirmDialogData {
  titleKey: string;
  messageKey: string;
  confirmKey?: string;
  cancelKey?: string;
}

@Component({
  selector: 'cce-confirm-dialog',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatDialogModule, TranslateModule],
  template: `
    <h2 mat-dialog-title>{{ data.titleKey | translate }}</h2>
    <mat-dialog-content>{{ data.messageKey | translate }}</mat-dialog-content>
    <mat-dialog-actions align="end">
      <button type="button" mat-button (click)="onCancel()">{{ (data.cancelKey ?? 'common.actions.cancel') | translate }}</button>
      <button type="button" mat-flat-button color="primary" (click)="onConfirm()">{{ (data.confirmKey ?? 'common.actions.save') | translate }}</button>
    </mat-dialog-actions>
  `,
})
export class ConfirmDialogComponent {
  constructor(
    private readonly ref: MatDialogRef<ConfirmDialogComponent, boolean>,
    @Inject(MAT_DIALOG_DATA) readonly data: ConfirmDialogData,
  ) {}

  onConfirm(): void { this.ref.close(true); }
  onCancel(): void { this.ref.close(false); }
}
