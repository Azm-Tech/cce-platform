
import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  effect,
  input,
  output,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { TranslocoModule } from '@jsverse/transloco';
import { NODE_LEVELS, type NodeLevel } from '../knowledge-maps.types';

const SEARCH_DEBOUNCE_MS = 200;

/**
 * Search input + level filter chips. Lives above the graph.
 *
 * Manual setTimeout debounce on the input handler (no RxJS). Chips
 * represent node levels (0 = Root, 1 = Category, 2 = Topic).
 */
@Component({
  selector: 'cce-search-and-filters',
  standalone: true,
  imports: [
    FormsModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    TranslocoModule,
  ],
  templateUrl: './search-and-filters.component.html',
  styleUrl: './search-and-filters.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SearchAndFiltersComponent implements OnDestroy {
  readonly searchTerm = input<string>('');
  readonly filters = input<ReadonlySet<number>>(new Set());
  readonly nodeLevels = input<readonly NodeLevel[]>(NODE_LEVELS);

  readonly searchTermChange = output<string>();
  readonly filtersChange = output<ReadonlySet<number>>();

  readonly inputValue = signal('');

  private debounceTimer: ReturnType<typeof setTimeout> | null = null;

  constructor() {
    effect(() => {
      this.inputValue.set(this.searchTerm());
    });
  }

  ngOnDestroy(): void {
    if (this.debounceTimer) clearTimeout(this.debounceTimer);
  }

  onInput(value: string): void {
    this.inputValue.set(value);
    if (this.debounceTimer) clearTimeout(this.debounceTimer);
    this.debounceTimer = setTimeout(() => {
      this.debounceTimer = null;
      this.searchTermChange.emit(value);
    }, SEARCH_DEBOUNCE_MS);
  }

  isActive(level: number): boolean {
    return this.filters().has(level);
  }

  toggleFilter(level: number): void {
    const next = new Set(this.filters());
    if (next.has(level)) next.delete(level);
    else next.add(level);
    this.filtersChange.emit(next);
  }
}
