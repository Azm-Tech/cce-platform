import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslateModule } from '@ngx-translate/core';
import { LocaleService } from '@frontend/i18n';
import { KnowledgeMapsApiService } from './knowledge-maps-api.service';
import type { KnowledgeMap } from './knowledge-maps.types';

@Component({
  selector: 'cce-knowledge-maps-list-page',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule, MatCardModule, MatIconModule, MatProgressBarModule,
    TranslateModule,
  ],
  templateUrl: './knowledge-maps-list.page.html',
  styleUrl: './knowledge-maps-list.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class KnowledgeMapsListPage implements OnInit {
  private readonly api = inject(KnowledgeMapsApiService);
  private readonly localeService = inject(LocaleService);

  readonly rows = signal<KnowledgeMap[]>([]);
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
    const res = await this.api.listMaps();
    this.loading.set(false);
    if (res.ok) this.rows.set(res.value);
    else this.errorKind.set(res.error.kind);
  }

  retry(): void {
    void this.load();
  }

  nameOf(m: KnowledgeMap): string {
    return this.locale() === 'ar' ? m.nameAr : m.nameEn;
  }

  descriptionOf(m: KnowledgeMap): string {
    return this.locale() === 'ar' ? m.descriptionAr : m.descriptionEn;
  }
}
