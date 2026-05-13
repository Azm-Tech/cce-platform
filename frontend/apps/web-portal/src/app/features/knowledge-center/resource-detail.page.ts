import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { DomSanitizer, type SafeResourceUrl } from '@angular/platform-browser';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { LocaleService } from '@frontend/i18n';
import { ToastService } from '@frontend/ui-kit';
import { TranslateModule } from '@ngx-translate/core';
import { KnowledgeApiService } from './knowledge-api.service';
import { getMockResource, getMockVideo } from './mock-data';
import { MOCK_RESOURCES } from './mock-data';
import type { Resource, ResourceListItem } from './knowledge.types';

@Component({
  selector: 'cce-resource-detail',
  standalone: true,
  imports: [
    CommonModule, DatePipe, RouterLink,
    MatButtonModule, MatCardModule, MatIconModule, MatProgressSpinnerModule,
    TranslateModule,
  ],
  templateUrl: './resource-detail.page.html',
  styleUrl: './resource-detail.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResourceDetailPage implements OnInit {
  private readonly api = inject(KnowledgeApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly localeService = inject(LocaleService);
  private readonly toast = inject(ToastService);
  private readonly sanitizer = inject(DomSanitizer);

  readonly resource = signal<Resource | null>(null);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly downloading = signal(false);

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
  readonly typeKey = computed<string>(() => this.resource()?.resourceType ?? 'Document');

  /**
   * Demo video metadata when the resource is a Video. Real backend
   * payloads can include a videoUrl on the Resource type in the
   * future; for now we look it up from the mock helper which serves
   * a public sample MP4 so the inline player always plays something.
   */
  readonly video = computed<{ url: string; poster: string; durationLabel: string; provider: 'mp4' | 'youtube' } | null>(() => {
    const r = this.resource();
    if (!r || r.resourceType !== 'Video') return null;
    return getMockVideo(r.id);
  });

  /** Angular requires iframe[src] to be a SafeResourceUrl — sanitize
   *  the YouTube embed URL once per video change. */
  readonly videoIframeUrl = computed<SafeResourceUrl | null>(() => {
    const v = this.video();
    if (!v || v.provider !== 'youtube') return null;
    return this.sanitizer.bypassSecurityTrustResourceUrl(v.url);
  });

  /** Related resources: same type or category, excluding self, max 3. */
  readonly related = computed<ResourceListItem[]>(() => {
    const r = this.resource();
    if (!r) return [];
    const sameType = MOCK_RESOURCES.filter((x) =>
      x.id !== r.id && (x.resourceType === r.resourceType || x.categoryId === r.categoryId),
    );
    return sameType.slice(0, 3);
  });

  /** Localized title for a related-resource card. */
  relatedTitle(item: ResourceListItem): string {
    return this.locale() === 'ar' ? item.titleAr : item.titleEn;
  }

  /** Copy the current page URL to clipboard. */
  async share(): Promise<void> {
    try {
      const url = window.location.href;
      // Prefer the Web Share API (mobile + Safari); fall back to clipboard.
      const navAny = navigator as Navigator & { share?: (data: { title?: string; url?: string }) => Promise<void> };
      if (typeof navAny.share === 'function') {
        await navAny.share({ title: this.title(), url });
      } else {
        await navigator.clipboard.writeText(url);
        this.toast.success('resources.share.copied');
      }
    } catch {
      // user cancelled the share dialog OR clipboard blocked — silent.
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
    this.flagFailedReset();
    // Scroll to top so the user lands on the new resource's hero,
    // not at the bottom where they clicked the related-card.
    if (typeof window !== 'undefined') {
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }
    const res = await this.api.getResource(id);
    this.loading.set(false);
    if (res.ok) {
      this.resource.set(res.value);
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
  }

  /** Reserved for future per-resource state resets (image errors, etc). */
  private flagFailedReset(): void { /* noop */ }

  async download(): Promise<void> {
    const r = this.resource();
    if (!r) return;
    this.downloading.set(true);
    const res = await this.api.download(r.id);
    this.downloading.set(false);
    if (res.ok) {
      this.saveBlob(res.value, this.filenameFor(r));
      this.toast.success('resources.download.toast');
      return;
    }
    // Backend has no real file for this resource (mock dataset).
    // Generate a useful demo artifact from the title + description so
    // the download button always produces something the user can save.
    const demo = this.buildDemoBlob(r);
    this.saveBlob(demo.blob, demo.filename);
    this.toast.success('resources.download.toast');
  }

  /**
   * Build a placeholder download artifact when the backend has no file.
   * For Link resources we open the original URL in a new tab; for any
   * other type we create a small text file with the title + description
   * + meta — useful for demo, never errors.
   */
  private buildDemoBlob(r: Resource): { blob: Blob; filename: string } {
    if (r.resourceType === 'Link') {
      // Links don't really get "downloaded" — open in a new tab AND
      // produce a tiny .url shortcut file as the saved artifact.
      const url = `${window.location.origin}/knowledge-center/${r.id}`;
      window.open(url, '_blank', 'noopener,noreferrer');
      const txt = `[InternetShortcut]\nURL=${url}\n`;
      return {
        blob: new Blob([txt], { type: 'application/internet-shortcut' }),
        filename: `${this.safeTitleSlug(r)}.url`,
      };
    }

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
      case 'Pdf': return 'picture_as_pdf';
      case 'Video': return 'play_circle';
      case 'Image': return 'image';
      case 'Link': return 'link';
      case 'Document': return 'description';
    }
  }

  private filenameFor(r: Resource): string {
    const safeTitle = r.titleEn.replace(/[^a-zA-Z0-9_-]+/g, '-').replace(/^-+|-+$/g, '') || 'resource';
    const ext = r.resourceType === 'Pdf' ? '.pdf'
      : r.resourceType === 'Video' ? '.mp4'
      : r.resourceType === 'Image' ? '.jpg'
      : r.resourceType === 'Document' ? '.docx'
      : '';
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
