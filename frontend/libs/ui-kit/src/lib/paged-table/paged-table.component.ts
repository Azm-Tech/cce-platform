import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  Input,
  Output,
} from '@angular/core';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslateModule } from '@ngx-translate/core';

/**
 * Column descriptor for a paged-table row of type T.
 * - `key` becomes the matColumnDef name and the displayedColumn order.
 * - `labelKey` is an i18n key resolved via the `translate` pipe.
 * - `cell(row)` returns the rendered cell text.
 */
export interface PagedTableColumn<T> {
  readonly key: string;
  readonly labelKey: string;
  readonly cell: (row: T) => string | number;
}

export interface PagedTablePageChange {
  readonly page: number; // 1-based
  readonly pageSize: number;
}

/**
 * Generic paged Material table. Centralises the list-page pattern used by
 * every feature phase (Users, Resources, News, etc).
 *
 * Caller owns the data: it passes `rows`, `total`, `page`, `pageSize`, `loading`
 * and reacts to `(pageChange)` to refetch.
 */
@Component({
  selector: 'cce-paged-table',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatPaginatorModule,
    MatProgressBarModule,
    TranslateModule,
  ],
  templateUrl: './paged-table.component.html',
  styleUrl: './paged-table.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PagedTableComponent<T> {
  @Input({ required: true }) columns: readonly PagedTableColumn<T>[] = [];
  @Input({ required: true }) rows: readonly T[] = [];
  @Input({ required: true }) total = 0;
  /** 1-based page number. */
  @Input() page = 1;
  @Input() pageSize = 20;
  @Input() pageSizeOptions: readonly number[] = [10, 20, 50, 100];
  @Input() loading = false;

  @Output() readonly pageChange = new EventEmitter<PagedTablePageChange>();

  get displayedColumns(): string[] {
    return this.columns.map((c) => c.key);
  }

  /** mat-paginator is 0-based; we expose 1-based. */
  get pageIndex(): number {
    return Math.max(0, this.page - 1);
  }

  onPage(event: PageEvent): void {
    this.pageChange.emit({ page: event.pageIndex + 1, pageSize: event.pageSize });
  }
}
