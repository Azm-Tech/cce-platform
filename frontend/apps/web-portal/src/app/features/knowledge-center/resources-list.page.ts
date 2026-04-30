import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, type PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { LocaleService } from '@frontend/i18n';
import { TranslateModule } from '@ngx-translate/core';
import { FilterRailComponent } from '../../core/layout/filter-rail.component';
import { CategoriesTreeComponent } from './categories-tree.component';
import { KnowledgeApiService } from './knowledge-api.service';
import { ResourceCardComponent } from './resource-card.component';
import {
  RESOURCE_TYPES,
  type ResourceCategory,
  type ResourceListItem,
  type ResourceType,
} from './knowledge.types';

@Component({
  selector: 'cce-resources-list',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatFormFieldModule, MatInputModule, MatPaginatorModule,
    MatProgressBarModule, MatSelectModule,
    TranslateModule,
    FilterRailComponent, CategoriesTreeComponent, ResourceCardComponent,
  ],
  templateUrl: './resources-list.page.html',
  styleUrl: './resources-list.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResourcesListPage implements OnInit {
  private readonly api = inject(KnowledgeApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly localeService = inject(LocaleService);

  readonly resourceTypes = RESOURCE_TYPES;
  readonly categories = signal<ResourceCategory[]>([]);
  readonly categoryId = signal<string | null>(null);
  readonly countryId = signal<string>('');
  readonly resourceType = signal<ResourceType | ''>('');
  readonly page = signal(1);
  readonly pageSize = signal(20);
  readonly rows = signal<ResourceListItem[]>([]);
  readonly total = signal(0);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly empty = computed(() => !this.loading() && this.rows().length === 0 && !this.errorKind());

  readonly locale = this.localeService.locale;

  ngOnInit(): void {
    const qp = this.route.snapshot.queryParamMap;
    const p = Number(qp.get('page') ?? 1);
    const ps = Number(qp.get('pageSize') ?? 20);
    this.page.set(Number.isFinite(p) && p >= 1 ? p : 1);
    this.pageSize.set(Number.isFinite(ps) && ps >= 1 ? ps : 20);
    this.categoryId.set(qp.get('categoryId') || null);
    this.countryId.set(qp.get('countryId') ?? '');
    const rt = qp.get('resourceType') as ResourceType | null;
    this.resourceType.set(rt && (RESOURCE_TYPES as readonly string[]).includes(rt) ? rt : '');

    void this.loadCategories();
    void this.load();
  }

  async loadCategories(): Promise<void> {
    const res = await this.api.listCategories();
    if (res.ok) this.categories.set(res.value);
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.errorKind.set(null);
    const res = await this.api.listResources({
      page: this.page(),
      pageSize: this.pageSize(),
      categoryId: this.categoryId() ?? undefined,
      countryId: this.countryId() || undefined,
      resourceType: this.resourceType() || undefined,
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
    this.syncUrl();
  }

  onCategoryChange(id: string | null): void {
    this.categoryId.set(id);
    this.page.set(1);
    void this.load();
    this.syncUrl();
  }

  onCountryChange(value: string): void {
    this.countryId.set(value);
    this.page.set(1);
    void this.load();
    this.syncUrl();
  }

  onResourceTypeChange(value: ResourceType | ''): void {
    this.resourceType.set(value);
    this.page.set(1);
    void this.load();
    this.syncUrl();
  }

  private syncUrl(): void {
    void this.router.navigate(['./'], {
      relativeTo: this.route,
      queryParams: {
        page: this.page() === 1 ? null : this.page(),
        pageSize: this.pageSize() === 20 ? null : this.pageSize(),
        categoryId: this.categoryId(),
        countryId: this.countryId() || null,
        resourceType: this.resourceType() || null,
      },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }
}
