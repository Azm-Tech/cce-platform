import { CommonModule, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormControl, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { LocaleService } from '@frontend/i18n';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { ToastService } from '@frontend/ui-kit';
import { NewsApiService } from './news-api.service';
import { SharePostDialogComponent, type SharePostDialogData } from '../community/share-post-dialog.component';
import type { NewsArticle } from './news.types';

interface TocItem {
  id: string;
  label: string;
  kind: 'heading' | 'static';
}

@Component({
  selector: 'cce-news-detail',
  standalone: true,
  imports: [
    CommonModule, DatePipe, FormsModule, ReactiveFormsModule, RouterLink,
    MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule, TranslocoModule,
  ],
  templateUrl: './news-detail.page.html',
  styleUrl: './news-detail.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NewsDetailPage implements OnInit {
  private readonly api = inject(NewsApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly localeService = inject(LocaleService);
  private readonly toast = inject(ToastService);
  private readonly transloco = inject(TranslocoService);
  private readonly dialog = inject(MatDialog);

  readonly article = signal<NewsArticle | null>(null);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly related = signal<NewsArticle[]>([]);
  readonly activeTocId = signal<string>('overview');

  readonly newsletterEmail = new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] });

  readonly locale = this.localeService.locale;
  readonly isAr = computed(() => this.locale() === 'ar');

  readonly title = computed(() => {
    const a = this.article();
    if (!a) return '';
    return this.isAr() ? a.titleAr : a.titleEn;
  });

  readonly contentHtml = computed(() => {
    const a = this.article();
    if (!a) return '';
    const raw = this.isAr() ? a.contentAr : a.contentEn;
    // Replace &nbsp; entities with regular spaces so text wraps normally.
    return (raw ?? '').replace(/&nbsp;/g, ' ').replace(/ /g, ' ');
  });

  readonly topicLabel = computed(() => {
    const a = this.article();
    if (!a) return null;
    return this.isAr() ? a.topicNameAr : a.topicNameEn;
  });

  /** Approximate read time, ~200 wpm, min 1 min. */
  readonly readingTime = computed(() => {
    this.locale(); // reactive dependency — re-run on language switch
    const html = this.contentHtml();
    const words = html.replace(/<[^>]*>/g, '').trim().split(/\s+/).filter(Boolean).length;
    const minutes = Math.max(1, Math.round(words / 200));
    return `${minutes} ${this.transloco.translate('news.detail.minRead')}`;
  });

  /** Chips: the article's topic followed by its tags (both captured in the
   *  admin form). De-duplicated, falsy values dropped. */
  readonly tags = computed<string[]>(() => {
    const a = this.article();
    if (!a) return [];
    const topic = this.topicLabel();
    const list = [...(topic ? [topic] : []), ...(a.tags ?? [])];
    return Array.from(new Set(list.filter((t): t is string => !!t)));
  });

  /** Table of contents. Includes both the static section anchors we render
   *  on the page and any `<h2>`s detected in the article body. */
  readonly toc = computed<TocItem[]>(() => {
    this.locale(); // reactive dependency — re-run on language switch
    const items: TocItem[] = [{ id: 'overview', label: this.transloco.translate('news.detail.overview'), kind: 'static' }];
    const html = this.contentHtml();
    if (typeof window !== 'undefined' && html) {
      const tmp = document.createElement('div');
      tmp.innerHTML = html;
      const headings = Array.from(tmp.querySelectorAll('h2, h3'));
      headings.forEach((h, i) => {
        const text = (h.textContent ?? '').trim();
        if (!text) return;
        items.push({ id: `body-${i}`, label: text, kind: 'heading' });
      });
    }
    if (this.article()) {
      items.push({ id: 'publisher', label: this.transloco.translate('news.detail.publisher'), kind: 'static' });
    }
    if (this.related().length > 0) {
      items.push({ id: 'related', label: this.transloco.translate('news.detail.relatedTitle'), kind: 'static' });
    }
    return items;
  });

  readonly absoluteUrl = computed(() => {
    if (typeof window === 'undefined') return '';
    const a = this.article();
    if (!a) return window.location.href;
    return new URL(this.router.createUrlTree(['/news', a.id]).toString(), window.location.origin).toString();
  });

  /** Open the shared share dialog (same as community posts). */
  openShareDialog(): void {
    this.dialog.open<SharePostDialogComponent, SharePostDialogData>(
      SharePostDialogComponent,
      {
        data: { url: this.absoluteUrl(), title: this.title() },
        width: '480px',
        maxWidth: '95vw',
        autoFocus: false,
        panelClass: 'cce-share-dialog',
      },
    );
  }

  async ngOnInit(): Promise<void> {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.errorKind.set('not-found');
      return;
    }
    this.loading.set(true);
    this.errorKind.set(null);
    const res = await this.api.getById(id);
    this.loading.set(false);
    if (res.ok) {
      this.article.set(res.value);
      void this.loadRelated(res.value.id);
    } else {
      this.errorKind.set(res.error.kind);
    }
  }

  private async loadRelated(currentId: string): Promise<void> {
    const res = await this.api.listNews({ pageSize: 6 });
    if (res.ok) {
      this.related.set(res.value.items.filter((n) => n.id !== currentId).slice(0, 4));
    }
  }

  scrollToToc(id: string): void {
    this.activeTocId.set(id);
    if (typeof document === 'undefined') return;
    const el = document.getElementById(id);
    if (el) el.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  excerpt(article: NewsArticle): string {
    const raw = this.isAr() ? article.contentAr : article.contentEn;
    const stripped = (raw ?? '').replace(/<[^>]*>/g, '').trim();
    return stripped.length > 120 ? stripped.slice(0, 120) + '…' : stripped;
  }

  articleTitle(article: NewsArticle): string {
    return this.isAr() ? article.titleAr : article.titleEn;
  }

  submitNewsletter(): void {
    if (this.newsletterEmail.invalid) {
      this.newsletterEmail.markAsTouched();
      return;
    }
    // No /api/newsletter/subscribe endpoint yet — visual stub only.
    this.toast.success('confirmations.CON003');
    this.newsletterEmail.reset('');
  }
}
