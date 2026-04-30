import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, type PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTableModule } from '@angular/material/table';
import { TranslateModule } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';
import { PermissionDirective } from '../../core/auth/permission.directive';
import { ConfirmDialogService, ToastService } from '@frontend/ui-kit';
import { ResourceCategoryFormDialogComponent } from './resource-category-form.dialog';
import { TaxonomyApiService } from './taxonomy-api.service';
import type { ResourceCategory } from './taxonomy.types';

@Component({
  selector: 'cce-resource-categories',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatButtonModule, MatFormFieldModule, MatIconModule,
    MatPaginatorModule, MatProgressBarModule, MatTableModule,
    TranslateModule, PermissionDirective,
  ],
  templateUrl: './resource-categories.page.html',
  styleUrl: './taxonomies-list.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResourceCategoriesPage implements OnInit {
  private readonly api = inject(TaxonomyApiService);
  private readonly dialog = inject(MatDialog);
  private readonly confirm = inject(ConfirmDialogService);
  private readonly toast = inject(ToastService);

  readonly displayedColumns = ['nameEn', 'slug', 'orderIndex', 'isActive', 'actions'];
  readonly page = signal(1);
  readonly pageSize = signal(20);
  readonly rows = signal<ResourceCategory[]>([]);
  readonly total = signal(0);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);

  ngOnInit(): void { void this.load(); }

  async load(): Promise<void> {
    this.loading.set(true);
    this.errorKind.set(null);
    const res = await this.api.listCategories({ page: this.page(), pageSize: this.pageSize() });
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

  async openCreate(): Promise<void> {
    const ref = this.dialog.open(ResourceCategoryFormDialogComponent, { data: {}, width: '600px' });
    if (await firstValueFrom(ref.afterClosed())) {
      this.toast.success('taxonomies.category.create.toast');
      void this.load();
    }
  }
  async openEdit(row: ResourceCategory): Promise<void> {
    const ref = this.dialog.open(ResourceCategoryFormDialogComponent, { data: { category: row }, width: '600px' });
    if (await firstValueFrom(ref.afterClosed())) {
      this.toast.success('taxonomies.category.edit.toast');
      void this.load();
    }
  }
  async delete(row: ResourceCategory): Promise<void> {
    if (!(await this.confirm.confirm({
      titleKey: 'taxonomies.category.delete.title',
      messageKey: 'taxonomies.category.delete.message',
      confirmKey: 'taxonomies.category.delete.confirm',
      cancelKey: 'common.actions.cancel',
    }))) return;
    const res = await this.api.deleteCategory(row.id);
    if (res.ok) { this.toast.success('taxonomies.category.delete.toast'); void this.load(); }
    else this.toast.error(`errors.${res.error.kind}`);
  }
}
