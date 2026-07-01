import { CommonModule, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatPaginatorModule, type PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { TranslocoModule } from '@jsverse/transloco';
import { StatusBadgeComponent } from '@frontend/ui-kit';
import { ExpertApiService } from './expert-api.service';
import {
  EXPERT_STATUSES,
  EXPERT_STATUS_BADGES,
  type ExpertRegistrationStatus,
  type ExpertRequest,
} from './expert.types';
import { TaxonomyApiService } from '../taxonomies/taxonomy-api.service';
import type { Topic } from '../taxonomies/taxonomy.types';

@Component({
  selector: 'cce-expert-requests-list',
  standalone: true,
  imports: [
    CommonModule,
    DatePipe,
    FormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatPaginatorModule,
    MatProgressBarModule,
    MatSelectModule,
    MatTableModule,
    TranslocoModule,
    StatusBadgeComponent,
  ],
  templateUrl: './expert-requests-list.page.html',
  styleUrl: './expert-requests-list.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExpertRequestsListPage implements OnInit {
  private readonly api = inject(ExpertApiService);
  private readonly taxonomy = inject(TaxonomyApiService);

  readonly displayedColumns = ['user', 'submitted', 'tags', 'cv', 'status', 'actions'];
  readonly statuses = EXPERT_STATUSES;
  readonly statusBadges = EXPERT_STATUS_BADGES;

  readonly statusFilter = signal<ExpertRegistrationStatus | ''>('');
  readonly page = signal(1);
  readonly pageSize = signal(20);
  readonly rows = signal<ExpertRequest[]>([]);
  readonly total = signal(0);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);
  private readonly topicsMap = signal<Map<string, Topic>>(new Map());

  ngOnInit(): void {
    void this.loadTopics();
    void this.load();
  }

  private async loadTopics(): Promise<void> {
    const res = await this.taxonomy.listTopics({ pageSize: 200 });
    if (res.ok) {
      this.topicsMap.set(new Map(res.value.items.map(t => [t.id, t])));
    }
  }

  tagLabel(id: string): string {
    const t = this.topicsMap().get(id);
    return t ? (t.nameAr || t.nameEn || id) : id;
  }

  resolveTagNames(ids: string[]): string {
    return ids.map(id => this.tagLabel(id)).join('، ');
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.errorKind.set(null);
    const res = await this.api.listRequests({
      page: this.page(),
      pageSize: this.pageSize(),
      status: this.statusFilter() || undefined,
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

  onStatusFilter(value: ExpertRegistrationStatus | ''): void {
    this.statusFilter.set(value);
    this.page.set(1);
    void this.load();
  }

}
