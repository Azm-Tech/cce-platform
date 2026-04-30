import { CommonModule, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, type PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { LocaleService } from '@frontend/i18n';
import { TranslateModule } from '@ngx-translate/core';
import { FilterRailComponent } from '../../core/layout/filter-rail.component';
import { EventsApiService } from './events-api.service';
import type { Event } from './event.types';

@Component({
  selector: 'cce-events-list',
  standalone: true,
  imports: [
    CommonModule, DatePipe, FormsModule, RouterLink,
    MatButtonModule, MatCardModule, MatFormFieldModule, MatIconModule,
    MatInputModule, MatPaginatorModule, MatProgressBarModule,
    TranslateModule, FilterRailComponent,
  ],
  templateUrl: './events-list.page.html',
  styleUrl: './events-list.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EventsListPage implements OnInit {
  private readonly api = inject(EventsApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly localeService = inject(LocaleService);

  readonly from = signal('');
  readonly to = signal('');
  readonly page = signal(1);
  readonly pageSize = signal(12);
  readonly rows = signal<Event[]>([]);
  readonly total = signal(0);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly empty = computed(() => !this.loading() && this.rows().length === 0 && !this.errorKind());

  readonly locale = this.localeService.locale;

  ngOnInit(): void {
    const qp = this.route.snapshot.queryParamMap;
    const p = Number(qp.get('page') ?? 1);
    this.page.set(Number.isFinite(p) && p >= 1 ? p : 1);
    this.from.set(qp.get('from') ?? '');
    this.to.set(qp.get('to') ?? '');
    void this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.errorKind.set(null);
    const res = await this.api.listEvents({
      page: this.page(),
      pageSize: this.pageSize(),
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
    this.syncUrl();
  }

  onFromChange(value: string): void {
    this.from.set(value);
    this.page.set(1);
    void this.load();
    this.syncUrl();
  }

  onToChange(value: string): void {
    this.to.set(value);
    this.page.set(1);
    void this.load();
    this.syncUrl();
  }

  titleOf(e: Event): string {
    return this.locale() === 'ar' ? e.titleAr : e.titleEn;
  }

  locationOf(e: Event): string | null {
    if (e.onlineMeetingUrl) return e.onlineMeetingUrl;
    return this.locale() === 'ar' ? e.locationAr : e.locationEn;
  }

  private syncUrl(): void {
    void this.router.navigate(['./'], {
      relativeTo: this.route,
      queryParams: {
        page: this.page() === 1 ? null : this.page(),
        from: this.from() || null,
        to: this.to() || null,
      },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }
}
