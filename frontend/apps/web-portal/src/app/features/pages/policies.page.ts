import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { LocaleService } from '@frontend/i18n';
import { TranslocoModule } from '@jsverse/transloco';
import { PagesApiService } from './pages-api.service';
import type { PoliciesContent, PolicySection } from './page.types';

@Component({
  selector: 'cce-policies-page',
  standalone: true,
  imports: [TranslocoModule],
  templateUrl: './policies.page.html',
  styleUrl: './policies.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PoliciesPage implements OnInit {
  private readonly api = inject(PagesApiService);
  private readonly localeService = inject(LocaleService);

  readonly policies = signal<PoliciesContent | null>(null);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly locale = this.localeService.locale;

  readonly sections = computed(() => {
    const p = this.policies();
    if (!p) return [];
    return [...p.sections].sort((a, b) => a.orderIndex - b.orderIndex);
  });

  title(section: PolicySection): string {
    return this.locale() === 'ar' ? section.titleAr : section.titleEn;
  }

  content(section: PolicySection): string {
    return this.locale() === 'ar' ? section.contentAr : section.contentEn;
  }

  async ngOnInit(): Promise<void> {
    this.loading.set(true);
    this.errorKind.set(null);
    const res = await this.api.getPolicies();
    this.loading.set(false);
    if (res.ok) this.policies.set(res.value);
    else this.errorKind.set(res.error.kind);
  }
}
