import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, type PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { TranslocoModule } from '@jsverse/transloco';
import { firstValueFrom } from 'rxjs';
import { ConfirmDialogService, ToastService } from '@frontend/ui-kit';
import { PermissionDirective } from '../../core/auth/permission.directive';
import { InteractiveMapsApiService } from './interactive-maps-api.service';
import { InteractiveMapFormDialogComponent } from './interactive-map-form.dialog';
import type { InteractiveMapDto } from './interactive-maps.types';

@Component({
  selector: 'cce-interactive-maps-list',
  standalone: true,
  imports: [
    MatButtonModule,
    MatIconModule,
    MatPaginatorModule,
    MatProgressBarModule,
    MatTooltipModule,
    TranslocoModule,
    PermissionDirective,
  ],
  templateUrl: './interactive-maps-list.page.html',
  styleUrl: './interactive-maps-list.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InteractiveMapsListPage implements OnInit {
  private readonly api = inject(InteractiveMapsApiService);
  private readonly dialog = inject(MatDialog);
  private readonly confirm = inject(ConfirmDialogService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  readonly page = signal(1);
  readonly pageSize = signal(20);
  readonly rows = signal<InteractiveMapDto[]>([]);
  readonly total = signal(0);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.errorKind.set(null);
    const res = await this.api.listMaps({ page: this.page(), pageSize: this.pageSize() });
    this.loading.set(false);
    if (res.ok) {
      this.rows.set(res.value.items);
      this.total.set(res.value.total);
    } else {
      this.errorKind.set(res.error.kind);
    }
  }

  onPage(e: PageEvent): void {
    this.page.set(e.pageIndex + 1);
    this.pageSize.set(e.pageSize);
    void this.load();
  }

  openDetail(row: InteractiveMapDto): void {
    void this.router.navigate(['/interactive-maps', row.id]);
  }

  async openCreate(): Promise<void> {
    const ref = this.dialog.open(InteractiveMapFormDialogComponent, { data: {}, width: '640px' });
    if (await firstValueFrom(ref.afterClosed())) {
      this.toast.success('interactiveMaps.toast.created');
      void this.load();
    }
  }

  async openEdit(row: InteractiveMapDto): Promise<void> {
    const ref = this.dialog.open(InteractiveMapFormDialogComponent, {
      data: { map: row },
      width: '640px',
    });
    if (await firstValueFrom(ref.afterClosed())) {
      this.toast.success('interactiveMaps.toast.updated');
      void this.load();
    }
  }

  async delete(row: InteractiveMapDto): Promise<void> {
    if (
      !(await this.confirm.confirm({
        titleKey: 'interactiveMaps.delete.title',
        messageKey: 'interactiveMaps.delete.message',
        confirmKey: 'interactiveMaps.delete.confirm',
        cancelKey: 'common.actions.cancel',
      }))
    ) return;
    const res = await this.api.deleteMap(row.id);
    if (res.ok) {
      this.toast.success('interactiveMaps.toast.deleted');
      void this.load();
    } else {
      this.toast.error(`errors.${res.error.kind}`);
    }
  }
}
