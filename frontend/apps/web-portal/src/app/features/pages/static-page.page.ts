import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { LocaleService } from '@frontend/i18n';
import { TranslateModule } from '@ngx-translate/core';
import { PagesApiService } from './pages-api.service';
import type { PublicPage } from './page.types';

@Component({
  selector: 'cce-static-page',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  templateUrl: './static-page.page.html',
  styleUrl: './static-page.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StaticPagePage implements OnInit {
  private readonly api = inject(PagesApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly locale = inject(LocaleService);

  readonly page = signal<PublicPage | null>(null);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly title = computed(() => {
    const p = this.page();
    if (!p) return '';
    return this.locale.locale() === 'ar' ? p.titleAr : p.titleEn;
  });
  readonly content = computed(() => {
    const p = this.page();
    if (!p) return '';
    return this.locale.locale() === 'ar' ? p.contentAr : p.contentEn;
  });

  async ngOnInit(): Promise<void> {
    const slug = this.route.snapshot.paramMap.get('slug');
    if (!slug) {
      this.errorKind.set('not-found');
      return;
    }
    this.loading.set(true);
    this.errorKind.set(null);
    const res = await this.api.getBySlug(slug);
    this.loading.set(false);
    if (res.ok) this.page.set(res.value);
    else this.errorKind.set(res.error.kind);
  }
}
