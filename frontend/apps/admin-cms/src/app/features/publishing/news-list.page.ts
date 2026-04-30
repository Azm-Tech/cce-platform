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
import { NewsFormDialogComponent, type NewsFormDialogData } from './news-form.dialog';
import { PublishingApiService } from './publishing-api.service';
import type { News } from './publishing.types';

@Component({
  selector: 'cce-news-list',
  standalone: true,
  imports: [
    CommonModule, DatePipe, FormsModule,
    MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule,
    MatPaginatorModule, MatProgressBarModule, MatSelectModule, MatTableModule,
    TranslateModule, PermissionDirective,
  ],
  templateUrl: './news-list.page.html',
  styleUrl: './news-list.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NewsListPage implements OnInit {
  private readonly api = inject(PublishingApiService);
  private readonly dialog = inject(MatDialog);
  private readonly confirm = inject(ConfirmDialogService);
  private readonly toast = inject(ToastService);

  readonly displayedColumns = ['titleEn', 'slug', 'isPublished', 'publishedOn', 'actions'];
  readonly searchInput = signal('');
  readonly publishedFilter = signal<string>('');
  readonly page = signal(1);
  readonly pageSize = signal(20);
  readonly rows = signal<News[]>([]);
  readonly total = signal(0);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);

  ngOnInit(): void { void this.load(); }

  async load(): Promise<void> {
    this.loading.set(true);
    this.errorKind.set(null);
    const filter = this.publishedFilter();
    const res = await this.api.listNews({
      page: this.page(),
      pageSize: this.pageSize(),
      search: this.searchInput() || undefined,
      isPublished: filter === '' ? undefined : filter === 'true',
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
  onPublishedFilter(v: string): void { this.publishedFilter.set(v); this.page.set(1); void this.load(); }

  async openCreate(): Promise<void> {
    const data: NewsFormDialogData = {};
    const ref = this.dialog.open(NewsFormDialogComponent, { data, width: '720px' });
    if (await firstValueFrom(ref.afterClosed())) {
      this.toast.success('news.create.toast');
      void this.load();
    }
  }
  async openEdit(row: News): Promise<void> {
    const ref = this.dialog.open(NewsFormDialogComponent, { data: { news: row }, width: '720px' });
    if (await firstValueFrom(ref.afterClosed())) {
      this.toast.success('news.edit.toast');
      void this.load();
    }
  }
  async publish(row: News): Promise<void> {
    if (!(await this.confirm.confirm({
      titleKey: 'news.publish.title', messageKey: 'news.publish.message',
      confirmKey: 'news.publish.confirm', cancelKey: 'common.actions.cancel',
    }))) return;
    const res = await this.api.publishNews(row.id);
    if (res.ok) { this.toast.success('news.publish.toast'); void this.load(); }
    else this.toast.error(`errors.${res.error.kind}`);
  }
  async delete(row: News): Promise<void> {
    if (!(await this.confirm.confirm({
      titleKey: 'news.delete.title', messageKey: 'news.delete.message',
      confirmKey: 'news.delete.confirm', cancelKey: 'common.actions.cancel',
    }))) return;
    const res = await this.api.deleteNews(row.id);
    if (res.ok) { this.toast.success('news.delete.toast'); void this.load(); }
    else this.toast.error(`errors.${res.error.kind}`);
  }
}
