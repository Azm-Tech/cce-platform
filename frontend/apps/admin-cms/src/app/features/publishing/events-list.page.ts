import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, type PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTableModule } from '@angular/material/table';
import { TranslocoModule } from '@jsverse/transloco';
import { firstValueFrom } from 'rxjs';
import { PermissionDirective } from '../../core/auth/permission.directive';
import { LocaleService } from '@frontend/i18n';
import { ConfirmDialogService, ToastService } from '@frontend/ui-kit';
import { EventFormDialogComponent } from './event-form.dialog';
import { formatLocaleDate } from '../../core/util/format-locale-date';
import { PublishingApiService } from './publishing-api.service';
import { RescheduleEventDialogComponent } from './reschedule-event.dialog';
import type { Event as CceEvent } from './publishing.types';

@Component({
  selector: 'cce-events-list',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule,
    MatPaginatorModule, MatProgressBarModule, MatTableModule,
    TranslocoModule, PermissionDirective,
  ],
  templateUrl: './events-list.page.html',
  styleUrl: './events-list.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EventsListPage implements OnInit {
  private readonly api = inject(PublishingApiService);
  private readonly dialog = inject(MatDialog);
  private readonly confirm = inject(ConfirmDialogService);
  private readonly toast = inject(ToastService);
  private readonly localeService = inject(LocaleService);

  readonly locale = this.localeService.locale;
  readonly displayedColumns = ['title', 'startsOn', 'endsOn', 'location', 'actions'];
  readonly searchInput = signal('');
  readonly page = signal(1);
  readonly pageSize = signal(20);
  readonly rows = signal<CceEvent[]>([]);
  readonly total = signal(0);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);

  ngOnInit(): void { void this.load(); }

  /** Title in the active language, falling back to the other if missing. */
  title(r: CceEvent): string {
    return this.locale() === 'ar' ? r.titleAr || r.titleEn : r.titleEn || r.titleAr;
  }

  /** Location in the active language; online URL or em-dash as fallback. */
  location(r: CceEvent): string {
    const loc = this.locale() === 'ar' ? r.locationAr : r.locationEn;
    return loc || r.locationEn || r.locationAr || r.onlineMeetingUrl || '—';
  }

  formatDate(iso: string | null): string {
    return formatLocaleDate(iso, this.locale(), {
      day: 'numeric', month: 'long', year: 'numeric', hour: '2-digit', minute: '2-digit',
    });
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.errorKind.set(null);
    const res = await this.api.listEvents({
      page: this.page(),
      pageSize: this.pageSize(),
      search: this.searchInput() || undefined,
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
  onSearch(): void { this.page.set(1); void this.load(); }

  async openCreate(): Promise<void> {
    const ref = this.dialog.open(EventFormDialogComponent, { data: { mode: 'create' }, width: '720px' });
    if (await firstValueFrom(ref.afterClosed())) {
      this.toast.success('events.create.toast');
      void this.load();
    }
  }
  async openEdit(row: CceEvent): Promise<void> {
    const ref = this.dialog.open(EventFormDialogComponent, { data: { event: row, mode: 'edit' }, width: '720px' });
    if (await firstValueFrom(ref.afterClosed())) {
      this.toast.success('events.edit.toast');
      void this.load();
    }
  }
  openView(row: CceEvent): void {
    this.dialog.open(EventFormDialogComponent, { data: { event: row, mode: 'view' }, width: '720px' });
  }
  async reschedule(row: CceEvent): Promise<void> {
    const ref = this.dialog.open(RescheduleEventDialogComponent, { data: { event: row }, width: '480px' });
    if (await firstValueFrom(ref.afterClosed())) {
      this.toast.success('events.reschedule.toast');
      void this.load();
    }
  }
  async delete(row: CceEvent): Promise<void> {
    if (!(await this.confirm.confirm({
      titleKey: 'events.delete.title', messageKey: 'events.delete.message',
      confirmKey: 'events.delete.confirm', cancelKey: 'common.actions.cancel',
    }))) return;
    const res = await this.api.deleteEvent(row.id);
    if (res.ok) { this.toast.success('events.delete.toast'); void this.load(); }
    else this.toast.error('errors.ERR028');
  }
}
