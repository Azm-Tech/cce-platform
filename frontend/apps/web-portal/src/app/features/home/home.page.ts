
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LocaleService } from '@frontend/i18n';
import { TranslocoModule } from '@jsverse/transloco';
import { HomeApiService } from './home-api.service';
import type { HomepageSection, HomepageSettings } from './home.types';

@Component({
  selector: 'cce-home',
  standalone: true,
  imports: [RouterLink, TranslocoModule],
  templateUrl: './home.page.html',
  styleUrl: './home.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HomePage implements OnInit {
  private readonly api = inject(HomeApiService);
  private readonly locale = inject(LocaleService);

  readonly sections = signal<HomepageSection[]>([]);
  readonly settings = signal<HomepageSettings | null>(null);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);

  readonly objective = computed(() => {
    const s = this.settings();
    if (!s) return null;
    return this.locale.locale() === 'ar' ? s.objectiveAr : s.objectiveEn;
  });

  readonly cceConcepts = computed(() => {
    const s = this.settings();
    if (!s) return [];
    const raw = this.locale.locale() === 'ar' ? s.cceConceptsAr : s.cceConceptsEn;
    if (!raw) return [];
    return raw.split(',').map((c) => c.trim()).filter(Boolean);
  });

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
    const [sectionsRes, settingsRes] = await Promise.all([
      this.api.listSections(),
      this.api.getSettings(),
    ]);
    this.loading.set(false);
    if (sectionsRes.ok) this.sections.set(sectionsRes.value);
    else this.errorKind.set(sectionsRes.error.kind);
    if (settingsRes.ok) this.settings.set(settingsRes.value);
  }
}
