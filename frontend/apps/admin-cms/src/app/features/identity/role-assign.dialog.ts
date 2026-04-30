import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, Inject, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import {
  MAT_DIALOG_DATA,
  MatDialogModule,
  MatDialogRef,
} from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { TranslateModule } from '@ngx-translate/core';
import { IdentityApiService } from './identity-api.service';
import { KNOWN_ROLES, type UserDetail } from './identity.types';

export interface RoleAssignDialogData {
  userId: string;
  currentRoles: string[];
}

/**
 * Modal that lets a SuperAdmin reassign roles for a single user. Closes
 * with the updated {@link UserDetail} on success, or `null` on cancel.
 */
@Component({
  selector: 'cce-role-assign-dialog',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    TranslateModule,
  ],
  templateUrl: './role-assign.dialog.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RoleAssignDialogComponent {
  private readonly api = inject(IdentityApiService);
  readonly knownRoles = KNOWN_ROLES;
  readonly selected = signal<string[]>([]);
  readonly saving = signal(false);
  readonly errorKind = signal<string | null>(null);

  constructor(
    private readonly ref: MatDialogRef<RoleAssignDialogComponent, UserDetail | null>,
    @Inject(MAT_DIALOG_DATA) readonly data: RoleAssignDialogData,
  ) {
    this.selected.set([...data.currentRoles]);
  }

  onSelectionChange(roles: string[]): void {
    this.selected.set(roles);
  }

  async save(): Promise<void> {
    this.saving.set(true);
    this.errorKind.set(null);
    const res = await this.api.assignRoles(this.data.userId, this.selected());
    this.saving.set(false);
    if (res.ok) {
      this.ref.close(res.value);
    } else {
      this.errorKind.set(res.error.kind);
    }
  }

  cancel(): void {
    this.ref.close(null);
  }
}
