
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslocoModule } from '@jsverse/transloco';
import { LocaleService } from '@frontend/i18n';
import { WorkbenchHeroComponent } from '@frontend/ui-kit';
import { KnowledgeMapsApiService } from './knowledge-maps-api.service';
import type { KnowledgeMap } from './knowledge-maps.types';

/**
 * Public list page for knowledge maps. Polished UX:
 *  - Branded hero with subtitle
 *  - Client-side search filter (debounce-free, instant)
 *  - Cards with a per-card accent gradient cycled by index for
 *    visual variety, an iconified thumbnail band, and an explicit
 *    "Open map" CTA pill
 *  - Skeleton placeholders during initial load
 *  - Branded empty + filtered-empty + error states
 */
@Component({
  selector: 'cce-knowledge-maps-list-page',
  standalone: true,
  imports: [
    FormsModule,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressBarModule,
    TranslocoModule,
    WorkbenchHeroComponent
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

  /** User-typed query — filters the rendered list locally. */
  readonly query = signal('');

  readonly locale = this.localeService.locale;

  /** Filtered result of `rows()` after applying `query`. Lower-cased
   *  contains-match against name + description in the active locale. */
  readonly filtered = computed<KnowledgeMap[]>(() => {
    const q = this.query().trim().toLowerCase();
    const all = this.rows();
    if (!q) return all;
    return all.filter((m) => {
      const haystack = [
        this.nameOf(m),
        this.descriptionOf(m),
        m.slug,
      ]
        .filter(Boolean)
        .join(' ')
        .toLowerCase();
      return haystack.includes(q);
    });
  });

  readonly empty = computed(
    () => !this.loading() && this.rows().length === 0 && !this.errorKind(),
  );

  /** True when the data set is non-empty but the search filter zeroed it. */
  readonly filteredEmpty = computed(
    () =>
      !this.loading() &&
      this.rows().length > 0 &&
      this.filtered().length === 0,
  );

  /** Skeleton placeholder array used by @for during initial load. */
  readonly skeletons = Array.from({ length: 6 });

  /** Accent palettes cycled by card index. Each is a {gradient, ring}
   *  pair used by the SCSS via inline `style.--km-card-grad` /
   *  `--km-card-ring` custom properties. Colors stay within the brand
   *  palette (greens, gold, deep teal). */
  private readonly palettes: ReadonlyArray<{ grad: string; ring: string; icon: string }> = [
    { grad: 'linear-gradient(135deg, var(--color-brand) 0%, var(--color-brand-mid) 50%, var(--color-brand-accent) 100%)', ring: 'var(--color-brand)', icon: 'account_tree' },
    { grad: 'linear-gradient(135deg, var(--color-brand-mid) 0%, var(--color-brand-accent) 60%, var(--color-accent) 100%)', ring: 'var(--color-brand-mid)', icon: 'hub' },
    { grad: 'linear-gradient(135deg, var(--color-brand-dark) 0%, var(--color-brand) 60%, var(--color-brand-mid) 100%)', ring: 'var(--color-brand-dark)', icon: 'schema' },
    { grad: 'linear-gradient(135deg, var(--color-brand-accent) 0%, var(--color-accent) 80%, var(--color-accent) 100%)', ring: 'var(--color-brand-accent)', icon: 'polyline' },
    { grad: 'linear-gradient(135deg, var(--color-brand) 0%, var(--color-accent) 100%)', ring: 'var(--neutrals--500)', icon: 'lan' },
    { grad: 'linear-gradient(135deg, var(--color-brand-darker) 0%, var(--color-brand-mid) 50%, var(--color-brand-accent) 100%)', ring: 'var(--color-brand-darker)', icon: 'device_hub' },
  ];

  /** Returns the palette for a given card index (cycles). */
  paletteFor(index: number): { grad: string; ring: string; icon: string } {
    return this.palettes[index % this.palettes.length];
  }

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

  /** Bound to the search input via [(ngModel)]. */
  onQueryChange(v: string): void {
    this.query.set(v);
  }

  clearQuery(): void {
    this.query.set('');
  }

  nameOf(m: KnowledgeMap): string {
    return this.locale() === 'ar' ? m.nameAr : m.nameEn;
  }

  descriptionOf(m: KnowledgeMap): string {
    return this.locale() === 'ar' ? m.descriptionAr : m.descriptionEn;
  }
}
