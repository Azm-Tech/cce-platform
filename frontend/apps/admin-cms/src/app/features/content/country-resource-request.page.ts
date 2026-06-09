import { DatePipe, NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, type PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { MatTabsModule } from '@angular/material/tabs';
import { TranslocoModule } from '@jsverse/transloco';
import { ContentApiService } from './content-api.service';
import {
  AdminContentRequestStatus,
  AdminContentType,
  adminContentRequestStatusKey,
  RESOURCE_TYPE_FROM_VALUE,
  type AdminContentRequestStatusValue,
  type AdminCountryContentRequest,
} from './content.types';

@Component({
  selector: 'cce-country-resource-request',
  standalone: true,
  imports: [
    DatePipe,
    NgTemplateOutlet,
    FormsModule,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatPaginatorModule,
    MatProgressBarModule,
    MatSelectModule,
    MatTableModule,
    MatTabsModule,
    TranslocoModule,
  ],
  templateUrl: './country-resource-request.page.html',
  styleUrl: './country-resource-request.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CountryResourceRequestPage implements OnInit {
  private readonly api = inject(ContentApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  private readonly TYPES: AdminContentType[] = [
    AdminContentType.Resource,
    AdminContentType.News,
    AdminContentType.Event,
  ];

  private readonly TYPE_TAB_INDEX: Record<AdminContentType, number> = {
    [AdminContentType.Resource]: 0,
    [AdminContentType.News]: 1,
    [AdminContentType.Event]: 2,
  };

  readonly AdminContentType = AdminContentType;
  readonly AdminContentRequestStatus = AdminContentRequestStatus;
  readonly statusKey = adminContentRequestStatusKey;
  readonly resourceTypeLabel = (n: number | null) =>
    n != null ? RESOURCE_TYPE_FROM_VALUE[n] ?? String(n) : '—';

  readonly STATUSES: Array<{ value: AdminContentRequestStatusValue | ''; label: string }> = [
    { value: '', label: 'countryRequest.filter.allStatuses' },
    { value: AdminContentRequestStatus.Pending, label: 'countryRequest.status.pending' },
    { value: AdminContentRequestStatus.Approved, label: 'countryRequest.status.approved' },
    { value: AdminContentRequestStatus.Rejected, label: 'countryRequest.status.rejected' },
  ];

  readonly displayedColumns = ['title', 'submittedOn', 'status', 'actions'];

  readonly activeType = signal<AdminContentType>(AdminContentType.Resource);
  readonly tabIndex = computed(() => this.TYPE_TAB_INDEX[this.activeType()]);
  readonly statusFilter = signal<AdminContentRequestStatusValue | ''>('');
  readonly page = signal(1);
  readonly pageSize = signal(20);
  readonly rows = signal<AdminCountryContentRequest[]>([]);
  readonly total = signal(0);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);

  ngOnInit(): void {
    const typeParam = this.route.snapshot.queryParamMap.get('type') as AdminContentType | null;
    if (typeParam && Object.values(AdminContentType).includes(typeParam)) {
      this.activeType.set(typeParam);
    }
    void this.load();
  }

  onTabChange(index: number): void {
    const type = this.TYPES[index] ?? AdminContentType.Resource;
    this.activeType.set(type);
    this.page.set(1);
    this.statusFilter.set('');
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { type },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
    void this.load();
  }

  onStatusFilter(value: AdminContentRequestStatusValue | ''): void {
    this.statusFilter.set(value);
    this.page.set(1);
    void this.load();
  }

  onPage(e: PageEvent): void {
    this.page.set(e.pageIndex + 1);
    this.pageSize.set(e.pageSize);
    void this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.errorKind.set(null);
    const res = await this.api.listCountryRequests({
      type: this.activeType(),
      status: this.statusFilter() !== '' ? (this.statusFilter() as AdminContentRequestStatusValue) : undefined,
      page: this.page(),
      pageSize: this.pageSize(),
    });
    this.loading.set(false);
    if (res.ok) {
      this.rows.set(res.value.items ?? []);
      this.total.set(Number(res.value.total));
    } else {
      this.errorKind.set(res.error.kind);
    }
  }
}
