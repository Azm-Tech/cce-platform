
import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, type PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTabsModule } from '@angular/material/tabs';
import { TranslocoModule } from '@jsverse/transloco';
import { ActivatedRoute, Router } from '@angular/router';
import { LocaleService } from '@frontend/i18n';
import { CountriesApiService } from './countries-api.service';
import { EventRequestFormDialogComponent } from './event-request-form.dialog';
import { NewsRequestFormDialogComponent } from './news-request-form.dialog';
import { RequestDetailDialogComponent } from './request-detail.dialog';
import { ResourceRequestFormDialogComponent } from './resource-request-form.dialog';
import { ContentType, contentRequestStatusKey, type CountryContentRequest } from './country.types';

type RequestTab = 'resources' | 'news' | 'events';

@Component({
  selector: 'cce-my-requests',
  standalone: true,
  imports: [
    DatePipe,
    MatButtonModule,
    MatIconModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatTabsModule,
    TranslocoModule,
  ],
  templateUrl: './my-requests.page.html',
  styleUrl: './my-requests.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyRequestsPage implements OnInit {
  private readonly api = inject(CountriesApiService);
  private readonly dialog = inject(MatDialog);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly locale = inject(LocaleService).locale;

  readonly activeTab = signal<RequestTab>('resources');
  readonly tabIndex = computed<number>(() => {
    const t = this.activeTab();
    return t === 'resources' ? 0 : t === 'news' ? 1 : 2;
  });

  readonly loadingResources = signal(true);
  readonly loadingNews = signal(true);
  readonly loadingEvents = signal(true);

  readonly resourcesError = signal<string | null>(null);
  readonly newsEventsError = signal<string | null>(null);

  readonly resourceRequests = signal<CountryContentRequest[]>([]);
  readonly newsRequests = signal<CountryContentRequest[]>([]);
  readonly eventRequests = signal<CountryContentRequest[]>([]);

  readonly resourcePage = signal(1);
  readonly resourcePageSize = signal(10);
  readonly resourceTotal = signal(0);

  readonly newsPage = signal(1);
  readonly newsPageSize = signal(10);
  readonly newsTotal = signal(0);

  readonly eventPage = signal(1);
  readonly eventPageSize = signal(10);
  readonly eventTotal = signal(0);

  readonly statusKey = contentRequestStatusKey;
  private countryId = '';

  async ngOnInit(): Promise<void> {
    const tabParam = this.route.snapshot.queryParamMap.get('tab') as RequestTab | null;
    if (tabParam === 'resources' || tabParam === 'news' || tabParam === 'events') {
      this.activeTab.set(tabParam);
    }
    const profileRes = await this.api.getStateProfile();
    if (profileRes.ok) this.countryId = profileRes.value.countryId;
    await Promise.all([this.loadResources(), this.loadNewsEvents()]);
  }

  onTabChange(index: number): void {
    const tabs: RequestTab[] = ['resources', 'news', 'events'];
    const tab: RequestTab = tabs[index] ?? 'resources';
    this.activeTab.set(tab);
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { tab },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }

  onResourcePage(e: PageEvent): void {
    this.resourcePage.set(e.pageIndex + 1);
    this.resourcePageSize.set(e.pageSize);
    this.loadingResources.set(true);
    this.resourcesError.set(null);
    void this.loadResources();
  }

  onNewsPage(e: PageEvent): void {
    this.newsPage.set(e.pageIndex + 1);
    this.newsPageSize.set(e.pageSize);
    this.loadingNews.set(true);
    void this.loadNews();
  }

  onEventPage(e: PageEvent): void {
    this.eventPage.set(e.pageIndex + 1);
    this.eventPageSize.set(e.pageSize);
    this.loadingEvents.set(true);
    void this.loadEvent();
  }

  async retryResources(): Promise<void> {
    this.resourcePage.set(1);
    this.loadingResources.set(true);
    this.resourcesError.set(null);
    await this.loadResources();
  }

  async retryNewsEvents(): Promise<void> {
    this.newsPage.set(1);
    this.eventPage.set(1);
    this.loadingNews.set(true);
    this.loadingEvents.set(true);
    this.newsEventsError.set(null);
    await this.loadNewsEvents();
  }

  openDetail(request: CountryContentRequest): void {
    this.dialog.open(RequestDetailDialogComponent, {
      data: request,
      width: '560px',
      maxWidth: '95vw',
    });
  }

  openAddResource(): void {
    const ref = this.dialog.open<ResourceRequestFormDialogComponent, string, CountryContentRequest | null>(
      ResourceRequestFormDialogComponent,
      { data: this.countryId, width: '640px', maxWidth: '95vw', disableClose: true },
    );
    ref.afterClosed().subscribe((result) => {
      if (result) void this.retryResources();
    });
  }

  openAddNews(): void {
    const ref = this.dialog.open<NewsRequestFormDialogComponent, string, CountryContentRequest | null>(
      NewsRequestFormDialogComponent,
      { data: this.countryId, width: '600px', maxWidth: '95vw', disableClose: true },
    );
    ref.afterClosed().subscribe((result) => {
      if (result) void this.retryNewsEvents();
    });
  }

  openAddEvent(): void {
    const ref = this.dialog.open<EventRequestFormDialogComponent, string, CountryContentRequest | null>(
      EventRequestFormDialogComponent,
      { data: this.countryId, width: '640px', maxWidth: '95vw', disableClose: true },
    );
    ref.afterClosed().subscribe((result) => {
      if (result) void this.retryNewsEvents();
    });
  }

  private async loadResources(): Promise<void> {
    const res = await this.api.listMyRequests({
      type: ContentType.Resource,
      page: this.resourcePage(),
      pageSize: this.resourcePageSize(),
    });
    this.loadingResources.set(false);
    if (res.ok) {
      this.resourceRequests.set(res.value.items);
      this.resourceTotal.set(res.value.total);
    } else {
      this.resourcesError.set(res.error.kind);
    }
  }

  private async loadNews(): Promise<void> {
    const res = await this.api.listMyRequests({
      type: ContentType.News,
      page: this.newsPage(),
      pageSize: this.newsPageSize(),
    });
    this.loadingNews.set(false);
    if (res.ok) {
      this.newsRequests.set(res.value.items);
      this.newsTotal.set(res.value.total);
    } else {
      this.newsEventsError.set(res.error.kind);
    }
  }

  private async loadEvent(): Promise<void> {
    const res = await this.api.listMyRequests({
      type: ContentType.Event,
      page: this.eventPage(),
      pageSize: this.eventPageSize(),
    });
    this.loadingEvents.set(false);
    if (res.ok) {
      this.eventRequests.set(res.value.items);
      this.eventTotal.set(res.value.total);
    }
  }

  private async loadNewsEvents(): Promise<void> {
    await Promise.all([this.loadNews(), this.loadEvent()]);
  }
}
