import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  Input,
  Output,
  computed,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { TranslateModule } from '@ngx-translate/core';
import type { ResourceCategory } from './knowledge.types';

@Component({
  selector: 'cce-categories-tree',
  standalone: true,
  imports: [CommonModule, MatButtonModule, TranslateModule],
  templateUrl: './categories-tree.component.html',
  styleUrl: './categories-tree.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CategoriesTreeComponent {
  private readonly _categories = signal<readonly ResourceCategory[]>([]);
  private readonly _selectedId = signal<string | null>(null);
  private readonly _locale = signal<'ar' | 'en'>('en');

  @Input({ required: true }) set categories(v: readonly ResourceCategory[]) {
    this._categories.set(v);
  }
  @Input() set selectedId(v: string | null) {
    this._selectedId.set(v);
  }
  @Input() set locale(v: 'ar' | 'en') {
    this._locale.set(v);
  }

  @Output() readonly selectionChange = new EventEmitter<string | null>();

  readonly roots = computed(() =>
    this._categories()
      .filter((c) => c.parentId === null)
      .slice()
      .sort((a, b) => a.orderIndex - b.orderIndex),
  );

  readonly selected = this._selectedId.asReadonly();

  childrenOf(parentId: string): ResourceCategory[] {
    return this._categories()
      .filter((c) => c.parentId === parentId)
      .slice()
      .sort((a, b) => a.orderIndex - b.orderIndex);
  }

  labelOf(c: ResourceCategory): string {
    return this._locale() === 'ar' ? c.nameAr : c.nameEn;
  }

  select(id: string | null): void {
    this.selectionChange.emit(id);
  }
}
