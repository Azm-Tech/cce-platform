import { CommonModule, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, type PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTableModule } from '@angular/material/table';
import { TranslateModule } from '@ngx-translate/core';
import { AuditApiService } from './audit-api.service';
import type { AuditEvent } from './audit.types';

/**
 * Admin → Audit log query page. Server-side pagination + filters
 * (actor, actionPrefix, resourceType, correlationId, from/to date range).
 * Each row is expandable to show the JSON diff payload when present.
 */
@Component({
  selector: 'cce-audit',
  standalone: true,
  imports: [
    CommonModule, DatePipe, FormsModule,
    MatButtonModule, MatExpansionModule, MatFormFieldModule, MatInputModule,
    MatPaginatorModule, MatProgressBarModule, MatTableModule, TranslateModule,
  ],
  templateUrl: './audit.page.html',
  styleUrl: './audit.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuditPage implements OnInit {
  private readonly api = inject(AuditApiService);

  readonly displayedColumns = ['occurredOn', 'actor', 'action', 'resource', 'correlationId'];
  readonly actor = signal('');
  readonly actionPrefix = signal('');
  readonly resourceType = signal('');
  readonly correlationId = signal('');
  readonly from = signal('');
  readonly to = signal('');
  readonly page = signal(1);
  readonly pageSize = signal(50);
  readonly rows = signal<AuditEvent[]>([]);
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
      actor: this.actor() || undefined,
      actionPrefix: this.actionPrefix() || undefined,
      resourceType: this.resourceType() || undefined,
      correlationId: this.correlationId() || undefined,
      from: this.from() || undefined,
      to: this.to() || undefined,
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
  applyFilters(): void {
    this.page.set(1);
    void this.load();
  }
  clearFilters(): void {
    this.actor.set('');
    this.actionPrefix.set('');
    this.resourceType.set('');
    this.correlationId.set('');
    this.from.set('');
    this.to.set('');
    this.applyFilters();
  }
}
