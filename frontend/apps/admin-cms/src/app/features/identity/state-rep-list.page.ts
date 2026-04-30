import { CommonModule, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, type PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTableModule } from '@angular/material/table';
import { TranslateModule } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
import { PermissionDirective } from '../../core/auth/permission.directive';
import { ConfirmDialogService, ToastService } from '@frontend/ui-kit';
import { IdentityApiService } from './identity-api.service';
import { StateRepCreateDialogComponent } from './state-rep-create.dialog';
import type { StateRepAssignment } from './identity.types';

/**
 * Admin → State-rep assignments list. Paged Material table with
 * "active" toggle, "New assignment" CTA (opens StateRepCreateDialog),
 * and per-row "Revoke" action (confirm-dialog → DELETE).
 *
 * v0.1.0 displays raw GUIDs for countries; Phase 06 swaps in country
 * names once the Country admin endpoint surfaces a name lookup.
 */
@Component({
  selector: 'cce-state-rep-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    DatePipe,
    MatButtonModule,
    MatCheckboxModule,
    MatIconModule,
    MatPaginatorModule,
    MatProgressBarModule,
    MatTableModule,
    TranslateModule,
    PermissionDirective,
  ],
  templateUrl: './state-rep-list.page.html',
  styleUrl: './state-rep-list.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StateRepListPage implements OnInit {
  private readonly api = inject(IdentityApiService);
  private readonly dialog = inject(MatDialog);
  private readonly confirm = inject(ConfirmDialogService);
  private readonly toast = inject(ToastService);

  readonly displayedColumns = ['userName', 'countryId', 'assignedOn', 'isActive', 'actions'];

  readonly activeOnly = signal(true);
  readonly page = signal(1);
  readonly pageSize = signal(20);
  readonly rows = signal<StateRepAssignment[]>([]);
  readonly total = signal(0);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.errorKind.set(null);
    const res = await this.api.listStateRepAssignments({
      page: this.page(),
      pageSize: this.pageSize(),
      active: this.activeOnly(),
    });
    this.loading.set(false);
    if (res.ok) {
      this.rows.set(res.value.items);
      this.total.set(Number(res.value.total));
    } else {
      this.errorKind.set(res.error.kind);
    }
  }

  onPage(e: PageEvent): void {
    this.page.set(e.pageIndex + 1);
    this.pageSize.set(e.pageSize);
    void this.load();
  }

  onActiveToggle(value: boolean): void {
    this.activeOnly.set(value);
    this.page.set(1);
    void this.load();
  }

  async openCreate(): Promise<void> {
    const ref = this.dialog.open(StateRepCreateDialogComponent, { width: '480px' });
    const created = await firstValueFrom(ref.afterClosed());
    if (created) {
      this.toast.success('stateRep.create.toast');
      this.page.set(1);
      void this.load();
    }
  }

  async revoke(row: StateRepAssignment): Promise<void> {
    const confirmed = await this.confirm.confirm({
      titleKey: 'stateRep.revoke.title',
      messageKey: 'stateRep.revoke.message',
      confirmKey: 'stateRep.revoke.confirm',
      cancelKey: 'common.actions.cancel',
    });
    if (!confirmed) return;
    const res = await this.api.revokeStateRepAssignment(row.id);
    if (res.ok) {
      this.toast.success('stateRep.revoke.toast');
      void this.load();
    } else {
      this.toast.error(`errors.${res.error.kind}`);
    }
  }
}
