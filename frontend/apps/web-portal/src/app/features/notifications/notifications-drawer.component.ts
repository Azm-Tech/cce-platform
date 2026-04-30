import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, EventEmitter, Output, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, type PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslateModule } from '@ngx-translate/core';
import { LocaleService } from '@frontend/i18n';
import { ToastService } from '@frontend/ui-kit';
import { NotificationsApiService } from './notifications-api.service';
import { NotificationRowComponent } from './notification-row.component';
import type { UserNotification } from './notification.types';

/**
 * Drawer body for the notifications panel. Hosted by HeaderComponent
 * inside a `<mat-sidenav>` (or by NotificationsPage as a fixed-width
 * page). Emits `(unreadCountChange)` when the local mark-read /
 * mark-all-read actions mutate the count, so the host can keep its
 * badge in sync without re-fetching.
 */
@Component({
  selector: 'cce-notifications-drawer',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule, MatIconModule, MatPaginatorModule, MatProgressBarModule,
    TranslateModule, NotificationRowComponent,
  ],
  templateUrl: './notifications-drawer.component.html',
  styleUrl: './notifications-drawer.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotificationsDrawerComponent {
  private readonly api = inject(NotificationsApiService);
  private readonly localeService = inject(LocaleService);
  private readonly toast = inject(ToastService);

  /** Emits whenever local actions change the unread count. Host updates its badge. */
  @Output() readonly unreadCountChange = new EventEmitter<number>();

  readonly rows = signal<UserNotification[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly pageSize = signal(10);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);

  readonly empty = computed(() => !this.loading() && this.rows().length === 0 && !this.errorKind());
  readonly locale = this.localeService.locale;

  readonly hasUnread = computed(() => this.rows().some((r) => r.status !== 'Read'));

  /** Public — called by host when the drawer opens. */
  async refresh(): Promise<void> {
    this.loading.set(true);
    this.errorKind.set(null);
    const res = await this.api.list({ page: this.page(), pageSize: this.pageSize() });
    this.loading.set(false);
    if (res.ok) {
      this.rows.set(res.value.items);
      this.total.set(Number(res.value.total));
      this.emitUnreadDelta();
    } else {
      this.errorKind.set(res.error.kind);
    }
  }

  async onMarkRead(id: string): Promise<void> {
    const res = await this.api.markRead(id);
    if (res.ok) {
      this.rows.update((rows) =>
        rows.map((r) =>
          r.id === id
            ? { ...r, status: 'Read' as const, readOn: new Date().toISOString() }
            : r,
        ),
      );
      this.emitUnreadDelta();
    }
  }

  async onMarkAllRead(): Promise<void> {
    const res = await this.api.markAllRead();
    if (res.ok) {
      this.rows.update((rows) =>
        rows.map((r) =>
          r.status === 'Read' ? r : { ...r, status: 'Read' as const, readOn: new Date().toISOString() },
        ),
      );
      this.toast.success('notifications.markedToast', { n: res.value });
      this.unreadCountChange.emit(0);
    }
  }

  onPage(e: PageEvent): void {
    this.page.set(e.pageIndex + 1);
    this.pageSize.set(e.pageSize);
    void this.refresh();
  }

  retry(): void {
    void this.refresh();
  }

  /** Emits the local unread tally so the host badge stays in sync. */
  private emitUnreadDelta(): void {
    const unread = this.rows().filter((r) => r.status !== 'Read').length;
    this.unreadCountChange.emit(unread);
  }
}
