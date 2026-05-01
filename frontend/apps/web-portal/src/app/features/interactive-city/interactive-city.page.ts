import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslateModule } from '@ngx-translate/core';
import { LocaleService } from '@frontend/i18n';
import { InteractiveCityApiService } from './interactive-city-api.service';
import type { CityTechnology } from './interactive-city.types';

@Component({
  selector: 'cce-interactive-city-page',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule, MatChipsModule, MatIconModule, MatProgressBarModule,
    TranslateModule,
  ],
  templateUrl: './interactive-city.page.html',
  styleUrl: './interactive-city.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InteractiveCityPage implements OnInit {
  private readonly api = inject(InteractiveCityApiService);
  private readonly localeService = inject(LocaleService);

  readonly rows = signal<CityTechnology[]>([]);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);

  readonly locale = this.localeService.locale;

  readonly empty = computed(
    () => !this.loading() && this.rows().length === 0 && !this.errorKind(),
  );

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.errorKind.set(null);
    const res = await this.api.listTechnologies();
    this.loading.set(false);
    if (res.ok) this.rows.set(res.value);
    else this.errorKind.set(res.error.kind);
  }

  retry(): void {
    void this.load();
  }

  nameOf(t: CityTechnology): string {
    return this.locale() === 'ar' ? t.nameAr : t.nameEn;
  }

  descriptionOf(t: CityTechnology): string {
    return this.locale() === 'ar' ? t.descriptionAr : t.descriptionEn;
  }

  categoryOf(t: CityTechnology): string {
    return this.locale() === 'ar' ? t.categoryAr : t.categoryEn;
  }
}
