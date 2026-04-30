import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { LocaleService } from '@frontend/i18n';
import { TranslateModule } from '@ngx-translate/core';
import { HomeApiService } from './home-api.service';
import type { HomepageSection } from './home.types';

@Component({
  selector: 'cce-home',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  templateUrl: './home.page.html',
  styleUrl: './home.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HomePage implements OnInit {
  private readonly api = inject(HomeApiService);
  private readonly locale = inject(LocaleService);

  readonly sections = signal<HomepageSection[]>([]);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly localizedSections = computed(() => {
    const isArabic = this.locale.locale() === 'ar';
    return this.sections()
      .filter((s) => s.isActive)
      .sort((a, b) => a.orderIndex - b.orderIndex)
      .map((s) => ({
        ...s,
        content: isArabic ? s.contentAr : s.contentEn,
      }));
  });

  async ngOnInit(): Promise<void> {
    this.loading.set(true);
    this.errorKind.set(null);
    const res = await this.api.listSections();
    this.loading.set(false);
    if (res.ok) this.sections.set(res.value);
    else this.errorKind.set(res.error.kind);
  }
}
