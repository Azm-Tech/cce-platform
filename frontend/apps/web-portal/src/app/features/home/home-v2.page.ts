import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';
import { Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { LocaleService } from '@frontend/i18n';
import { TranslocoModule } from '@jsverse/transloco';
import { EventsApiService } from '../events/events-api.service';
import type { Event as EventModel } from '../events/event.types';
import { NewsApiService } from '../news/news-api.service';
import { NewsCardComponent } from '../news/news-card.component';
import type { NewsArticle } from '../news/news.types';
import { HomeApiService } from './home-api.service';
import type { HomepageSection, HomepageSettings } from './home.types';

type FrameworkTab = 'reduce' | 'reuse' | 'recycle' | 'remove';

@Component({
  selector: 'cce-home-v2',
  standalone: true,
  imports: [RouterLink, TranslocoModule, NewsCardComponent, MatButtonModule, MatIconModule],
  templateUrl: './home-v2.page.html',
  styleUrl: './home-v2.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HomeV2Page implements OnInit {
  private readonly homeApi = inject(HomeApiService);
  private readonly newsApi = inject(NewsApiService);
  private readonly eventsApi = inject(EventsApiService);
  private readonly locale = inject(LocaleService);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);

  readonly hasEvaluated = signal(!!localStorage.getItem('cce_evaluated'));

  readonly loading = signal(true);
  /** Set when the core homepage settings call fails (US001 AC4/AC5 — ERR001). */
  readonly errorKind = signal<string | null>(null);
  readonly sections = signal<HomepageSection[]>([]);
  readonly settings = signal<HomepageSettings | null>(null);
  readonly news = signal<NewsArticle[]>([]);
  readonly events = signal<EventModel[]>([]);
  readonly searchQuery = signal('');
  readonly activeCategory = signal<string | null>(null);
  readonly activeTab = signal<FrameworkTab>('reduce');

  readonly searchCategories = ['carbonCapture', 'renewableEnergy', 'recycling', 'policy', 'technology', 'finance'];
  readonly frameworkTabs: FrameworkTab[] = ['reduce', 'reuse', 'recycle', 'remove'];

  readonly lang = computed(() => this.locale.locale() as 'ar' | 'en');
  readonly isAr = computed(() => this.lang() === 'ar');

  readonly heroSection = computed(() => this.sections().find((s) => s.sectionType === 'Hero'));
  readonly heroContent = computed(() => {
    const s = this.heroSection();
    return s ? (this.isAr() ? s.contentAr : s.contentEn) : null;
  });

  readonly featuredNewsSection = computed(() =>
    this.sections().find((s) => s.sectionType === 'FeaturedNews'),
  );
  readonly featuredNewsIntro = computed(() => {
    const s = this.featuredNewsSection();
    return s ? (this.isAr() ? s.contentAr : s.contentEn) : null;
  });

  readonly featuredResourcesSection = computed(() =>
    this.sections().find((s) => s.sectionType === 'FeaturedResources'),
  );
  readonly featuredResourcesIntro = computed(() => {
    const s = this.featuredResourcesSection();
    return s ? (this.isAr() ? s.contentAr : s.contentEn) : null;
  });

  readonly upcomingEventsSection = computed(() =>
    this.sections().find((s) => s.sectionType === 'UpcomingEvents'),
  );
  readonly upcomingEventsIntro = computed(() => {
    const s = this.upcomingEventsSection();
    return s ? (this.isAr() ? s.contentAr : s.contentEn) : null;
  });

  readonly objective = computed(() => {
    const s = this.settings();
    if (!s) return null;
    return this.isAr() ? s.objectiveAr : s.objectiveEn;
  });

  readonly cceConcepts = computed(() => {
    const s = this.settings();
    if (!s) return [];
    const raw = this.isAr() ? s.cceConceptsAr : s.cceConceptsEn;
    if (!raw) return [];
    return raw.split(',').map((c) => c.trim()).filter(Boolean);
  });

  private isVideoFile(url: string): boolean {
    return /\.(mp4|webm|ogg|mov)(\?.*)?$/i.test(url);
  }

  readonly videoFileUrl = computed(() => {
    const url = this.settings()?.videoUrl ?? '/assets/promo.mp4';
    if (!this.isVideoFile(url)) return null;
    return this.sanitizer.bypassSecurityTrustUrl(url);
  });

  readonly videoEmbedUrl = computed(() => {
    const url = this.settings()?.videoUrl;
    if (!url || this.isVideoFile(url)) return null;
    return this.sanitizer.bypassSecurityTrustResourceUrl(this.toEmbedUrl(url));
  });

  private toEmbedUrl(url: string): string {
    const ytMatch = url.match(/(?:youtu\.be\/|youtube\.com\/(?:watch\?v=|embed\/|v\/))([\w-]{11})/);
    if (ytMatch) return `https://www.youtube.com/embed/${ytMatch[1]}`;
    const vimeoMatch = url.match(/vimeo\.com\/(\d+)/);
    if (vimeoMatch) return `https://player.vimeo.com/video/${vimeoMatch[1]}`;
    return url;
  }

  readonly participatingCountries = computed(() => this.settings()?.participatingCountries ?? []);
  readonly featuredNews = computed(() => this.news()?.[0] ?? null);
  readonly remainingNews = computed(() => this.news()?.slice(1) ?? []);
  readonly firstEvent = computed(() => this.events()?.[0] ?? null);

  private extractItems<T>(
    res: { items?: T[] } | { data?: { items?: T[] } | T[] } | T[] | null | undefined,
  ): T[] {
    if (!res) return [];
    if (Array.isArray(res)) return res;
    if ('items' in res && Array.isArray((res as { items?: T[] }).items)) {
      return (res as { items: T[] }).items;
    }
    const inner = (res as { data?: { items?: T[] } | T[] }).data;
    if (!inner) return [];
    if (Array.isArray(inner)) return inner;
    return inner.items ?? [];
  }

  readonly currentTabSub = computed(() => `home.v2.framework.${this.activeTab()}.sub`);
  readonly currentTabBody = computed(() => `home.v2.framework.${this.activeTab()}.body`);

  submitSearch(): void {
    const q = this.searchQuery().trim();
    if (!q) return;
    this.router.navigate(['/knowledge-center'], { queryParams: { q } });
  }

  setCategory(cat: string): void {
    const next = this.activeCategory() === cat ? null : cat;
    this.activeCategory.set(next);
    if (next) {
      this.router.navigate(['/knowledge-center'], { queryParams: { category: next } });
    }
  }

  setTab(tab: FrameworkTab): void {
    this.activeTab.set(tab);
  }

  openEvaluation(): void {
    import('../account/evaluation.dialog').then(({ EvaluationDialogComponent }) => {
      const ref = this.dialog.open(
        EvaluationDialogComponent,
        { panelClass: 'cce-dialog-no-padding', autoFocus: 'first-tabbable' },
      );
      ref.afterClosed().subscribe((submitted) => {
        if (submitted) this.hasEvaluated.set(true);
      });
    });
  }

  async ngOnInit(): Promise<void> {
    const today = new Date().toISOString().split('T')[0];
    const [settingsRes, newsRes, eventsRes] = await Promise.all([
      this.homeApi.getSettings(),
      this.newsApi.listNews({ isFeatured: true, pageSize: 4 }),
      this.eventsApi.listEvents({ pageSize: 3, from: today }),
    ]);
    if (settingsRes.ok) {
      this.settings.set(settingsRes.value);
      this.sections.set(
        settingsRes.value.sections
          .filter((s) => s.isActive)
          .sort((a, b) => a.orderIndex - b.orderIndex),
      );
    } else {
      // Core homepage content failed to load — surface ERR001 (US001 AC4/AC5).
      this.errorKind.set(settingsRes.error.kind);
    }
    if (newsRes.ok) this.news.set(this.extractItems(newsRes.value));
    if (eventsRes.ok) this.events.set(this.extractItems(eventsRes.value));
    this.loading.set(false);
  }
}
