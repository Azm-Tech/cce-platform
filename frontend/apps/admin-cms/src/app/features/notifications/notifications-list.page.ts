import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, type PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { TranslateModule } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
import { PermissionDirective } from '../../core/auth/permission.directive';
import { ToastService } from '@frontend/ui-kit';
import { NotificationApiService } from './notification-api.service';
import { NotificationFormDialogComponent } from './notification-form.dialog';
import {
  NOTIFICATION_CHANNELS,
  type NotificationChannel,
  type NotificationTemplate,
} from './notification.types';

@Component({
  selector: 'cce-notifications-list',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatButtonModule, MatFormFieldModule, MatIconModule,
    MatPaginatorModule, MatProgressBarModule, MatSelectModule, MatTableModule,
    TranslateModule, PermissionDirective,
  ],
  templateUrl: './notifications-list.page.html',
  styleUrl: './notifications.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotificationsListPage implements OnInit {
  private readonly api = inject(NotificationApiService);
  private readonly dialog = inject(MatDialog);
  private readonly toast = inject(ToastService);

  readonly displayedColumns = ['code', 'channel', 'subjectEn', 'isActive', 'actions'];
  readonly channels = NOTIFICATION_CHANNELS;
  readonly channelFilter = signal<NotificationChannel | ''>('');
  readonly page = signal(1);
  readonly pageSize = signal(20);
  readonly rows = signal<NotificationTemplate[]>([]);
  readonly total = signal(0);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);

  ngOnInit(): void { void this.load(); }

  async load(): Promise<void> {
    this.loading.set(true);
    this.errorKind.set(null);
    const res = await this.api.list({
      page: this.page(),
      pageSize: this.pageSize(),
      channel: this.channelFilter() || undefined,
    });
    this.loading.set(false);
    if (res.ok) {
      this.rows.set(res.value.items);
      this.total.set(Number(res.value.total));
    } else this.errorKind.set(res.error.kind);
  }

  onPage(e: PageEvent): void {
    this.page.set(e.pageIndex + 1);
    this.pageSize.set(e.pageSize);
    void this.load();
  }
  onChannelFilter(value: NotificationChannel | ''): void {
    this.channelFilter.set(value);
    this.page.set(1);
    void this.load();
  }

  async openCreate(): Promise<void> {
    const ref = this.dialog.open(NotificationFormDialogComponent, { data: {}, width: '720px' });
    if (await firstValueFrom(ref.afterClosed())) {
      this.toast.success('notifications.create.toast');
      void this.load();
    }
  }
  async openEdit(row: NotificationTemplate): Promise<void> {
    const ref = this.dialog.open(NotificationFormDialogComponent, { data: { template: row }, width: '720px' });
    if (await firstValueFrom(ref.afterClosed())) {
      this.toast.success('notifications.edit.toast');
      void this.load();
    }
  }
}
