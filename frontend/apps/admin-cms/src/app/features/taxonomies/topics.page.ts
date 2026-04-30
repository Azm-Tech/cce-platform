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
import { TranslateModule } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
import { PermissionDirective } from '../../core/auth/permission.directive';
import { ConfirmDialogService } from '../../core/ui/confirm-dialog.service';
import { ToastService } from '../../core/ui/toast.service';
import { TaxonomyApiService } from './taxonomy-api.service';
import { TopicFormDialogComponent } from './topic-form.dialog';
import type { Topic } from './taxonomy.types';

@Component({
  selector: 'cce-topics',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule,
    MatPaginatorModule, MatProgressBarModule, MatTableModule,
    TranslateModule, PermissionDirective,
  ],
  templateUrl: './topics.page.html',
  styleUrl: './taxonomies-list.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TopicsPage implements OnInit {
  private readonly api = inject(TaxonomyApiService);
  private readonly dialog = inject(MatDialog);
  private readonly confirm = inject(ConfirmDialogService);
  private readonly toast = inject(ToastService);

  readonly displayedColumns = ['nameEn', 'slug', 'orderIndex', 'isActive', 'actions'];
  readonly searchInput = signal('');
  readonly page = signal(1);
  readonly pageSize = signal(20);
  readonly rows = signal<Topic[]>([]);
  readonly total = signal(0);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);

  ngOnInit(): void { void this.load(); }

  async load(): Promise<void> {
    this.loading.set(true);
    this.errorKind.set(null);
    const res = await this.api.listTopics({
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
    const ref = this.dialog.open(TopicFormDialogComponent, { data: {}, width: '720px' });
    if (await firstValueFrom(ref.afterClosed())) {
      this.toast.success('taxonomies.topic.create.toast');
      void this.load();
    }
  }
  async openEdit(row: Topic): Promise<void> {
    const ref = this.dialog.open(TopicFormDialogComponent, { data: { topic: row }, width: '720px' });
    if (await firstValueFrom(ref.afterClosed())) {
      this.toast.success('taxonomies.topic.edit.toast');
      void this.load();
    }
  }
  async delete(row: Topic): Promise<void> {
    if (!(await this.confirm.confirm({
      titleKey: 'taxonomies.topic.delete.title',
      messageKey: 'taxonomies.topic.delete.message',
      confirmKey: 'taxonomies.topic.delete.confirm',
      cancelKey: 'common.actions.cancel',
    }))) return;
    const res = await this.api.deleteTopic(row.id);
    if (res.ok) { this.toast.success('taxonomies.topic.delete.toast'); void this.load(); }
    else this.toast.error(`errors.${res.error.kind}`);
  }
}
