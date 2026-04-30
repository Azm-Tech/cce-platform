import { CommonModule, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatPaginatorModule, type PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { TranslateModule } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
import { ToastService } from '@frontend/ui-kit';
import { ApproveExpertDialogComponent, type ApproveExpertDialogData } from './approve-expert.dialog';
import { ExpertApiService } from './expert-api.service';
import { EXPERT_STATUSES, type ExpertRegistrationStatus, type ExpertRequest } from './expert.types';
import { RejectExpertDialogComponent, type RejectExpertDialogData } from './reject-expert.dialog';

/**
 * Admin → Expert requests list. Filterable by status; per-row Approve/Reject
 * actions open dedicated dialogs (Tasks 2.2 + 2.3).
 */
@Component({
  selector: 'cce-expert-requests-list',
  standalone: true,
  imports: [
    CommonModule,
    DatePipe,
    FormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatPaginatorModule,
    MatProgressBarModule,
    MatSelectModule,
    MatTableModule,
    TranslateModule,
  ],
  templateUrl: './expert-requests-list.page.html',
  styleUrl: './expert-requests-list.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExpertRequestsListPage implements OnInit {
  private readonly api = inject(ExpertApiService);
  private readonly dialog = inject(MatDialog);
  private readonly toast = inject(ToastService);

  readonly displayedColumns = ['user', 'submitted', 'tags', 'status', 'actions'];
  readonly statuses = EXPERT_STATUSES;

  readonly statusFilter = signal<ExpertRegistrationStatus | ''>('Pending');
  readonly page = signal(1);
  readonly pageSize = signal(20);
  readonly rows = signal<ExpertRequest[]>([]);
  readonly total = signal(0);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.errorKind.set(null);
    const res = await this.api.listRequests({
      page: this.page(),
      pageSize: this.pageSize(),
      status: this.statusFilter() || undefined,
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

  onStatusFilter(value: ExpertRegistrationStatus | ''): void {
    this.statusFilter.set(value);
    this.page.set(1);
    void this.load();
  }

  async approve(row: ExpertRequest): Promise<void> {
    const data: ApproveExpertDialogData = { requestId: row.id, requesterName: row.requestedByUserName };
    const ref = this.dialog.open(ApproveExpertDialogComponent, { data, width: '480px' });
    const updated = await firstValueFrom(ref.afterClosed());
    if (updated) {
      this.toast.success('experts.approve.toast');
      void this.load();
    }
  }

  async reject(row: ExpertRequest): Promise<void> {
    const data: RejectExpertDialogData = { requestId: row.id, requesterName: row.requestedByUserName };
    const ref = this.dialog.open(RejectExpertDialogComponent, { data, width: '520px' });
    const updated = await firstValueFrom(ref.afterClosed());
    if (updated) {
      this.toast.success('experts.reject.toast');
      void this.load();
    }
  }
}
