
import { ChangeDetectionStrategy, Component, EventEmitter, Output, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, type PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslocoModule } from '@jsverse/transloco';
import { LocaleService } from '@frontend/i18n';
import { ToastService } from '@frontend/ui-kit';
import { NotificationsApiService } from './notifications-api.service';
import { NotificationRowComponent } from './notification-row.component';
import type { UserNotification } from './notification.types';

/**
 * Drawer body for the notifications panel. Hosted by HeaderComponent
 * inside a `<mat-sidenav>` (or by NotificationsPage as a fixed-width
 * page). Emits `(readStateChanged)` after a mark-read / mark-all-read so the
 * host can re-fetch the authoritative GLOBAL unread count — the drawer only
 * knows the current page, so it must not report an absolute count itself.
 */
@Component({
  selector: 'cce-notifications-drawer',
  standalone: true,
  imports: [
    MatButtonModule,
    MatIconModule,
    MatPaginatorModule,
    MatProgressBarModule,
    TranslocoModule,
    NotificationRowComponent
],
  templateUrl: './notifications-drawer.component.html',
  styleUrl: './notifications-drawer.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotificationsDrawerComponent {
  private readonly api = inject(NotificationsApiService);
  private readonly localeService = inject(LocaleService);
  private readonly toast = inject(ToastService);

  /** Emits after a mark-read/mark-all-read so the host re-fetches the global
   *  unread count (the drawer can't know the count beyond the current page). */
  @Output() readonly readStateChanged = new EventEmitter<void>();

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
      this.readStateChanged.emit();
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
      this.readStateChanged.emit();
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
}
