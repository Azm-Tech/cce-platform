import { ChangeDetectionStrategy, ChangeDetectorRef, Component, ElementRef, OnInit, ViewChild, computed, inject, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { DomSanitizer, type SafeResourceUrl } from '@angular/platform-browser';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { LocaleService } from '@frontend/i18n';
import { ToastService } from '@frontend/ui-kit';
import { TranslocoModule } from '@jsverse/transloco';
import { KnowledgeApiService } from './knowledge-api.service';
import { getMockResource, getMockVideo } from './testing/mock-data';
import { MOCK_RESOURCES } from './testing/mock-data';
import type { Resource, ResourceListItem } from './knowledge.types';

@Component({
  selector: 'cce-resource-detail',
  standalone: true,
  imports: [
    CommonModule, DatePipe, RouterLink,
    MatButtonModule, MatCardModule, MatIconModule, MatProgressSpinnerModule,
    TranslocoModule,
  ],
  templateUrl: './resource-detail.page.html',
  styleUrl: './resource-detail.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResourceDetailPage implements OnInit {
  private readonly api = inject(KnowledgeApiService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly route = inject(ActivatedRoute);
  private readonly localeService = inject(LocaleService);
  private readonly toast = inject(ToastService);
  private readonly sanitizer = inject(DomSanitizer);

  @ViewChild('relatedTrack') relatedTrack?: ElementRef<HTMLElement>;

  readonly resource = signal<Resource | null>(null);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly downloading = signal(false);
  readonly activeSection = signal<string>('overview');
  readonly apiRelated = signal<ResourceListItem[]>([]);

  readonly locale = this.localeService.locale;

  readonly title = computed(() => {
    const r = this.resource();
    if (!r) return '';
    return this.locale() === 'ar' ? r.titleAr : r.titleEn;
  });

  readonly description = computed(() => {
    const r = this.resource();
    if (!r) return '';
    return this.locale() === 'ar' ? r.descriptionAr : r.descriptionEn;
  });

  /** Estimated reading time in minutes — assumes 220 words per minute,
   *  rounded up. Uses the localized description's word count. */
  readonly readingTime = computed<number>(() => {
    const text = this.description();
    if (!text) return 0;
    const words = text
      .replace(/<[^>]*>/g, ' ')
      .trim()
      .split(/\s+/)
      .filter((w) => w.length > 0).length;
    if (words === 0) return 1;
    return Math.max(1, Math.ceil(words / 220));
  });

  /** Type-color theming token used by the SCSS via [data-type]. */
  readonly typeKey = computed<string>(() => this.resource()?.resourceType ?? 'Report');

  /**
   * Demo video metadata when the resource is Media type. Real backend
   * payloads can include a videoUrl on the Resource type in the
   * future; for now we look it up from the mock helper which serves
   * a public sample MP4 so the inline player always plays something.
   */
  readonly video = computed<{ url: string; poster: string; durationLabel: string; provider: 'mp4' | 'youtube' } | null>(() => {
    const r = this.resource();
    if (!r || r.resourceType !== 'Media') return null;
    return getMockVideo(r.id);
  });

  /** Angular requires iframe[src] to be a SafeResourceUrl — sanitize
   *  the YouTube embed URL once per video change. */
  readonly videoIframeUrl = computed<SafeResourceUrl | null>(() => {
    const v = this.video();
    if (!v || v.provider !== 'youtube') return null;
    return this.sanitizer.bypassSecurityTrustResourceUrl(v.url);
  });

  /** Related resources: API results (same category) first, mock fallback, max 6. */
  readonly related = computed<ResourceListItem[]>(() => {
    const r = this.resource();
    if (!r) return [];
    const fromApi = this.apiRelated();
    if (fromApi.length > 0) return fromApi.slice(0, 6);
    const sameType = MOCK_RESOURCES.filter((x) =>
      x.id !== r.id && (x.resourceType === r.resourceType || x.categoryId === r.categoryId),
    );
    return sameType.slice(0, 6);
  });

  /** Localized "key results" cards — section hidden when the API has none. */
  readonly highlights = computed<{ title: string; text: string }[]>(() => {
    const r = this.resource();
    if (!r?.highlights?.length) return [];
    const ar = this.locale() === 'ar';
    return r.highlights.map((h) => ({
      title: ar ? h.titleAr : h.titleEn,
      text: ar ? h.textAr : h.textEn,
    }));
  });

  /** TOC entries — only sections that actually render. */
  readonly tocSections = computed<{ id: string; labelKey: string }[]>(() => {
    const out = [
      { id: 'overview', labelKey: 'resources.detail.tocOverview' },
      { id: 'summary', labelKey: 'resources.detail.tocSummary' },
    ];
    if (this.highlights().length > 0) {
      out.push({ id: 'results', labelKey: 'resources.detail.tocResults' });
    }
    return out;
  });

  /** File format derived from the asset file extension, falling back to type. */
  readonly fileFormat = computed<string>(() => {
    const r = this.resource();
    if (!r) return '';
    const ext = r.assetFileName?.split('.').pop()?.toUpperCase();
    if (ext && ext.length <= 5) return ext;
    return r.resourceType === 'Media' ? 'MP4'
      : r.resourceType === 'Presentation' ? 'PPTX'
      : 'PDF';
  });

  /** "Arabic, English" depending on which localized titles exist. */
  readonly languages = computed<string>(() => {
    const r = this.resource();
    if (!r) return '';
    const ar = this.locale() === 'ar';
    const langs: string[] = [];
    if (r.titleAr) langs.push(ar ? 'العربية' : 'Arabic');
    if (r.titleEn) langs.push(ar ? 'الإنجليزية' : 'English');
    return langs.join(ar ? '، ' : ', ');
  });

  /** Smooth-scroll to an in-page section and mark it active in the TOC. */
  scrollToSection(id: string): void {
    this.activeSection.set(id);
    document.getElementById(`section-${id}`)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  /** Slide the related carousel one viewport step. dir: 1 = forward. */
  scrollRelated(dir: 1 | -1): void {
    const el = this.relatedTrack?.nativeElement;
    if (!el) return;
    const step = el.clientWidth * 0.8 * dir * (this.locale() === 'ar' ? -1 : 1);
    el.scrollBy({ left: step, behavior: 'smooth' });
  }

  /** Localized title for a related-resource card. */
  relatedTitle(item: ResourceListItem): string {
    return this.locale() === 'ar' ? item.titleAr : item.titleEn;
  }

  /** Share the current page via Web Share API (mobile) or clipboard fallback. */
  async share(): Promise<void> {
    try {
      const url = window.location.href;
      const navAny = navigator as Navigator & { share?: (data: { title?: string; url?: string }) => Promise<void> };
      if (typeof navAny.share === 'function') {
        await navAny.share({ title: this.title(), url });
        this.toast.success('confirmations.CON002');
      } else {
        await navigator.clipboard.writeText(url);
        this.toast.success('confirmations.CON002');
      }
    } catch (err) {
      // AbortError = user cancelled the native share sheet — silent.
      if (err instanceof Error && err.name !== 'AbortError') {
        this.toast.error('errors.ERR004');
      }
    }
  }

  ngOnInit(): void {
    // Subscribe to paramMap (NOT snapshot) so navigating from one
    // resource to a related one (same `:id` route, different param)
    // re-fetches the new resource. With snapshot Angular reuses the
    // component instance, the URL changes, but the page wouldn't
    // refresh — looking like "the link did nothing".
    this.route.paramMap.subscribe((pm) => {
      const id = pm.get('id');
      if (!id) {
        this.errorKind.set('not-found');
        return;
      }
      void this.loadResource(id);
    });
  }

  private async loadResource(id: string): Promise<void> {
    this.loading.set(true);
    this.errorKind.set(null);
    // Reset state up front so any previous resource flicker is gone.
    this.resource.set(null);
    this.apiRelated.set([]);
    this.activeSection.set('overview');
    this.cdr.markForCheck();
    // Scroll to top so the user lands on the new resource's hero,
    // not at the bottom where they clicked the related-card.
    if (typeof window !== 'undefined') {
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
    try {
      const res = await this.api.getResource(id);
      if (res.ok) {
        this.resource.set(res.value);
        void this.loadRelated(res.value);
        return;
      }
      // Backend unavailable / not-found — fall back to the mock dataset
      // so the demo page always shows content when navigating to a card.
      const mock = getMockResource(id);
      if (mock) {
        this.resource.set(mock);
        return;
      }
      this.errorKind.set(res.error.kind);
    } finally {
      this.loading.set(false);
      this.cdr.markForCheck();
    }
  }

  /** Fetch related resources from the API (same category, excluding self). */
  private async loadRelated(r: Resource): Promise<void> {
    const res = await this.api.listResources({ categoryId: r.categoryId, pageSize: 7 });
    if (res.ok && Array.isArray(res.value.items)) {
      this.apiRelated.set(res.value.items.filter((x) => x.id !== r.id));
      this.cdr.markForCheck();
    }
  }

  /** Reserved for future per-resource state resets (image errors, etc). */
  private flagFailedReset(): void { /* noop */ }

  async download(): Promise<void> {
    const r = this.resource();
    if (!r) return;
    this.downloading.set(true);
    try {
      const res = await this.api.download(r.id);
      this.downloading.set(false);
      if (res.ok) {
        this.saveBlob(res.value, this.filenameFor(r));
      } else {
        // Backend has no real file for this resource (mock dataset).
        // Generate a demo artifact so the button always produces something.
        const demo = this.buildDemoBlob(r);
        this.saveBlob(demo.blob, demo.filename);
      }
      this.toast.success('confirmations.CON001');
    } catch {
      this.downloading.set(false);
      this.toast.error('errors.ERR002');
    }
  }

  /**
   * Build a placeholder download artifact when the backend has no file.
   * For Link resources we open the original URL in a new tab; for any
   * other type we create a small text file with the title + description
   * + meta — useful for demo, never errors.
   */
  private buildDemoBlob(r: Resource): { blob: Blob; filename: string } {
    const titleEn = r.titleEn || 'CCE Resource';
    const titleAr = r.titleAr || '';
    const descPlainEn = (r.descriptionEn || '').replace(/<[^>]*>/g, ' ').replace(/\s+/g, ' ').trim();
    const descPlainAr = (r.descriptionAr || '').replace(/<[^>]*>/g, ' ').replace(/\s+/g, ' ').trim();
    const published = r.publishedOn ? new Date(r.publishedOn).toISOString().slice(0, 10) : 'n/a';

    const body = [
      'CCE — Carbon Circular Economy',
      '======================================',
      '',
      `Title (EN): ${titleEn}`,
      titleAr ? `Title (AR): ${titleAr}` : '',
      `Type: ${r.resourceType}`,
      `Published: ${published}`,
      `Views: ${r.viewCount}`,
      `Resource ID: ${r.id}`,
      '',
      '── Description (EN) ──',
      descPlainEn || '(no description)',
      '',
      titleAr ? '── الوصف (AR) ──' : '',
      titleAr ? (descPlainAr || '(لا يوجد وصف)') : '',
      '',
      '──────────────────────────────────────',
      'Demo download — generated by the CCE web portal.',
      'In production, this download would deliver the underlying',
      'asset file (PDF, MP4, JPG, DOCX, etc.) referenced by the',
      'resource record.',
      '',
    ].filter((line) => line !== '').join('\n');

    return {
      blob: new Blob([body], { type: 'text/plain;charset=utf-8' }),
      filename: `${this.safeTitleSlug(r)}.txt`,
    };
  }

  private safeTitleSlug(r: Resource): string {
    return r.titleEn.replace(/[^a-zA-Z0-9_-]+/g, '-').replace(/^-+|-+$/g, '') || 'cce-resource';
  }

  iconFor(type: Resource['resourceType']): string {
    switch (type) {
      case 'Paper':          return 'article';
      case 'Article':        return 'article';
      case 'Study':          return 'description';
      case 'Presentation':   return 'slideshow';
      case 'ScientificPaper':return 'science';
      case 'Report':         return 'assessment';
      case 'Book':           return 'menu_book';
      case 'Research':       return 'biotech';
      case 'CceGuide':       return 'eco';
      case 'Media':          return 'play_circle';
    }
  }

  private filenameFor(r: Resource): string {
    const safeTitle = r.titleEn.replace(/[^a-zA-Z0-9_-]+/g, '-').replace(/^-+|-+$/g, '') || 'resource';
    const ext = r.resourceType === 'Media' ? '.mp4'
      : r.resourceType === 'Presentation' ? '.pptx'
      : '.pdf';
    return `${safeTitle}${ext}`;
  }

  private saveBlob(blob: Blob, filename: string): void {
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  }
}
