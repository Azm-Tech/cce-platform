
import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { TranslocoModule } from '@jsverse/transloco';
import { NODE_LEVELS, type InteractiveMapNode, type NodeLevel } from '../knowledge-maps.types';

type SortOrder = 'default' | 'alpha';

interface NodeGroup {
  level: NodeLevel;
  nodes: InteractiveMapNode[];
}

/**
 * Accessible alternative to GraphCanvas. Renders nodes grouped by
 * level (0 = Root, 1 = Category, 2 = Topic) with children counts
 * and sort toggle.
 */
@Component({
  selector: 'cce-list-view',
  standalone: true,
  imports: [MatIconModule, TranslocoModule],
  templateUrl: './list-view.component.html',
  styleUrl: './list-view.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ListViewComponent {
  readonly nodes = input.required<InteractiveMapNode[]>();
  readonly selectedId = input<string | null>(null);
  readonly dimmedIds = input<ReadonlySet<string>>(new Set());
  readonly locale = input<'ar' | 'en'>('en');

  readonly nodeSelected = output<string>();

  readonly sortOrder = signal<SortOrder>('default');

  readonly collapsed = signal<ReadonlySet<number>>(new Set());

  readonly grouped = computed<NodeGroup[]>(() => {
    const byLevel: Record<number, InteractiveMapNode[]> = { 0: [], 1: [], 2: [] };
    for (const n of this.nodes()) {
      // Clamp into 0..2 — the synthetic map root (level -1) lands in the Root group.
      const lvl = Math.min(Math.max(n.level, 0), 2);
      byLevel[lvl].push(n);
    }

    const sort = this.sortOrder();
    const localeCode = this.locale();
    const sorted = (rows: InteractiveMapNode[]): InteractiveMapNode[] => {
      if (sort === 'default') return rows;
      return [...rows].sort((a, b) => {
        const an = (localeCode === 'ar' ? a.nameAr : a.nameEn) || '';
        const bn = (localeCode === 'ar' ? b.nameAr : b.nameEn) || '';
        return an.localeCompare(bn, localeCode);
      });
    };

    return NODE_LEVELS.map((l) => ({ level: l, nodes: sorted(byLevel[l] ?? []) }));
  });

  /** Children count per node (nodes where parentId === id). */
  readonly childCounts = computed<ReadonlyMap<string, number>>(() => {
    const map = new Map<string, number>();
    for (const n of this.nodes()) {
      if (n.parentId) {
        map.set(n.parentId, (map.get(n.parentId) ?? 0) + 1);
      }
    }
    return map;
  });

  nameOf(n: InteractiveMapNode): string {
    return this.locale() === 'ar' ? n.nameAr : n.nameEn;
  }

  childCountOf(n: InteractiveMapNode): number {
    return this.childCounts().get(n.id) ?? 0;
  }

  isSelected(n: InteractiveMapNode): boolean {
    return this.selectedId() === n.id;
  }

  isDimmed(n: InteractiveMapNode): boolean {
    return this.dimmedIds().has(n.id);
  }

  isCollapsed(level: number): boolean {
    return this.collapsed().has(level);
  }

  toggleCollapsed(level: number): void {
    this.collapsed.update((prev) => {
      const next = new Set(prev);
      if (next.has(level)) next.delete(level);
      else next.add(level);
      return next;
    });
  }

  setSort(order: SortOrder): void {
    this.sortOrder.set(order);
  }

  onSelect(n: InteractiveMapNode): void {
    this.nodeSelected.emit(n.id);
  }
}
