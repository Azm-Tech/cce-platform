import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { LocaleService } from '@frontend/i18n';
import { TranslocoModule } from '@jsverse/transloco';
import { PagesApiService } from './pages-api.service';
import type { AboutContent } from './page.types';

@Component({
  selector: 'cce-about-page',
  standalone: true,
  imports: [TranslocoModule],
  templateUrl: './about.page.html',
  styleUrl: './about.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AboutPage implements OnInit {
  private readonly api = inject(PagesApiService);
  private readonly localeService = inject(LocaleService);
  private readonly sanitizer = inject(DomSanitizer);

  readonly about = signal<AboutContent | null>(null);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly locale = this.localeService.locale;

  readonly description = computed(() => {
    const a = this.about();
    if (!a) return '';
    return this.locale() === 'ar' ? a.descriptionAr : a.descriptionEn;
  });

  readonly safeVideoUrl = computed<SafeResourceUrl | null>(() => {
    const url = this.about()?.howToUseVideoUrl;
    if (!url) return null;
    return this.sanitizer.bypassSecurityTrustResourceUrl(this.toEmbedUrl(url));
  });

  async ngOnInit(): Promise<void> {
    this.loading.set(true);
    this.errorKind.set(null);
    const res = await this.api.getAbout();
    this.loading.set(false);
    if (res.ok) this.about.set(res.value);
    else this.errorKind.set(res.error.kind);
  }

  private toEmbedUrl(url: string): string {
    const ytMatch = url.match(/(?:youtu\.be\/|youtube\.com\/(?:watch\?v=|embed\/|v\/))([\w-]{11})/);
    if (ytMatch) return `https://www.youtube.com/embed/${ytMatch[1]}`;
    const vimeoMatch = url.match(/vimeo\.com\/(\d+)/);
    if (vimeoMatch) return `https://player.vimeo.com/video/${vimeoMatch[1]}`;
    return url;
  }
}
