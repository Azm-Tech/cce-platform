import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { debounceTime } from 'rxjs/operators';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { TranslateModule } from '@ngx-translate/core';
import { LocaleService } from '@frontend/i18n';
import type { CityTechnology } from '../interactive-city.types';
import { ScenarioBuilderStore } from './scenario-builder-store.service';

interface CatalogGroup {
  category: string;
  rows: CityTechnology[];
}

/**
 * Left-rail technology catalog. Search input filters by name/category
 * (locale-aware), 200ms debounced. Cards grouped by `categoryEn` /
 * `categoryAr` per locale. Clicking a card calls `store.toggle(id)`;
 * selected cards get `aria-pressed="true"` + a visual ring.
 */
@Component({
  selector: 'cce-technology-catalog',
  standalone: true,
  imports: [
    DecimalPipe,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    TranslateModule,
  ],
  templateUrl: './technology-catalog.component.html',
  styleUrl: './technology-catalog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TechnologyCatalogComponent implements OnInit {
  private readonly store = inject(ScenarioBuilderStore);
  private readonly localeService = inject(LocaleService);
  private readonly destroyRef = inject(DestroyRef);

  readonly searchControl = new FormControl<string>('', { nonNullable: true });
  /** Debounced search query — drives the filter. */
  private readonly query = signal<string>('');

  readonly locale = this.localeService.locale;

  /** Catalog rows + selectedIds + query, all signals. */
  readonly groups = computed<CatalogGroup[]>(() => {
    const q = this.query().trim().toLowerCase();
    const rows = this.store.technologies().filter((t) => {
      if (q === '') return true;
      const haystack = `${t.nameEn} ${t.nameAr} ${t.categoryEn} ${t.categoryAr}`.toLowerCase();
      return haystack.includes(q);
    });
    const byCategory = new Map<string, CityTechnology[]>();
    for (const t of rows) {
      const cat = this.locale() === 'ar' ? t.categoryAr : t.categoryEn;
      const list = byCategory.get(cat) ?? [];
      list.push(t);
      byCategory.set(cat, list);
    }
    return Array.from(byCategory, ([category, rows]) => ({ category, rows }));
  });

  readonly selectedIds = this.store.selectedIds;

  readonly hasResults = computed(() => this.groups().reduce((acc, g) => acc + g.rows.length, 0) > 0);
  readonly hasCatalog = computed(() => this.store.technologies().length > 0);

  ngOnInit(): void {
    this.searchControl.valueChanges
      .pipe(debounceTime(200), takeUntilDestroyed(this.destroyRef))
      .subscribe((value) => this.query.set(value));
  }

  nameOf(t: CityTechnology): string {
    return this.locale() === 'ar' ? t.nameAr : t.nameEn;
  }

  toggle(id: string): void {
    this.store.toggle(id);
  }

  isSelected(id: string): boolean {
    return this.selectedIds().has(id);
  }
}
