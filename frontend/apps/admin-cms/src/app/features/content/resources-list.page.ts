import { CommonModule, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, type PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { TranslateModule } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
import { PermissionDirective } from '../../core/auth/permission.directive';
import { ConfirmDialogService, ToastService } from '@frontend/ui-kit';
import { ContentApiService } from './content-api.service';
import {
  ResourceFormDialogComponent,
  type ResourceFormDialogData,
} from './resource-form.dialog';
import type { Resource } from './content.types';

/**
 * Admin → Resources list. Filters: search, isPublished. Per-row actions:
 * Edit, Publish (only when not yet published).
 */
@Component({
  selector: 'cce-resources-list',
  standalone: true,
  imports: [
    CommonModule,
    DatePipe,
    FormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressBarModule,
    MatSelectModule,
    MatTableModule,
    TranslateModule,
    PermissionDirective,
  ],
  templateUrl: './resources-list.page.html',
  styleUrl: './resources-list.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResourcesListPage implements OnInit {
  private readonly api = inject(ContentApiService);
  private readonly dialog = inject(MatDialog);
  private readonly confirm = inject(ConfirmDialogService);
  private readonly toast = inject(ToastService);

  readonly displayedColumns = ['titleEn', 'resourceType', 'isPublished', 'publishedOn', 'views', 'actions'];
  readonly publishedFilters = [
    { value: '', labelKey: 'resources.filter.all' },
    { value: 'true', labelKey: 'resources.filter.published' },
    { value: 'false', labelKey: 'resources.filter.draft' },
  ];

  readonly searchInput = signal('');
  readonly publishedFilter = signal<string>('');
  readonly page = signal(1);
  readonly pageSize = signal(20);
  readonly rows = signal<Resource[]>([]);
  readonly total = signal(0);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.errorKind.set(null);
    const isPublished = this.publishedFilter();
    const res = await this.api.listResources({
      page: this.page(),
      pageSize: this.pageSize(),
      search: this.searchInput() || undefined,
      isPublished: isPublished === '' ? undefined : isPublished === 'true',
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

  onSearch(): void {
    this.page.set(1);
    void this.load();
  }

  onPublishedFilter(value: string): void {
    this.publishedFilter.set(value);
    this.page.set(1);
    void this.load();
  }

  async openCreate(): Promise<void> {
    const data: ResourceFormDialogData = {};
    const ref = this.dialog.open(ResourceFormDialogComponent, { data, width: '720px' });
    const created = await firstValueFrom(ref.afterClosed());
    if (created) {
      this.toast.success('resources.create.toast');
      void this.load();
    }
  }

  async openEdit(row: Resource): Promise<void> {
    const data: ResourceFormDialogData = { resource: row };
    const ref = this.dialog.open(ResourceFormDialogComponent, { data, width: '720px' });
    const updated = await firstValueFrom(ref.afterClosed());
    if (updated) {
      this.toast.success('resources.edit.toast');
      void this.load();
    }
  }

  async publish(row: Resource): Promise<void> {
    const confirmed = await this.confirm.confirm({
      titleKey: 'resources.publish.title',
      messageKey: 'resources.publish.message',
      confirmKey: 'resources.publish.confirm',
      cancelKey: 'common.actions.cancel',
    });
    if (!confirmed) return;
    const res = await this.api.publishResource(row.id);
    if (res.ok) {
      this.toast.success('resources.publish.toast');
      void this.load();
    } else {
      this.toast.error(`errors.${res.error.kind}`);
    }
  }
}
