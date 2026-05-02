import { CommonModule } from '@angular/common';
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
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { TranslateModule } from '@ngx-translate/core';
import { NODE_TYPES, type NodeType } from '../knowledge-maps.types';

const SEARCH_DEBOUNCE_MS = 200;

/**
 * Search input + NodeType filter chips. Lives above the graph.
 *
 * Manual setTimeout debounce on the input handler (no RxJS — we don't
 * want a whole observable graph for one signal). Cleanup the timer
 * on input changes + on destroy.
 */
@Component({
  selector: 'cce-search-and-filters',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatChipsModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    TranslateModule,
  ],
  templateUrl: './search-and-filters.component.html',
  styleUrl: './search-and-filters.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SearchAndFiltersComponent implements OnDestroy {
  readonly searchTerm = input<string>('');
  readonly filters = input<ReadonlySet<NodeType>>(new Set());
  readonly nodeTypes = input<readonly NodeType[]>(NODE_TYPES);

  readonly searchTermChange = output<string>();
  readonly filtersChange = output<ReadonlySet<NodeType>>();

  /** Local mirror of the input — drives the [(ngModel)] binding. */
  readonly inputValue = signal('');

  private debounceTimer: ReturnType<typeof setTimeout> | null = null;

  constructor() {
    // Sync external input changes into the local input value.
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

  isActive(type: NodeType): boolean {
    return this.filters().has(type);
  }

  toggleFilter(type: NodeType): void {
    const next = new Set(this.filters());
    if (next.has(type)) next.delete(type);
    else next.add(type);
    this.filtersChange.emit(next);
  }
}
