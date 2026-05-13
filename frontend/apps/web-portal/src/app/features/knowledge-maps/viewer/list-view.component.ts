import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output, signal } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { TranslateModule } from '@ngx-translate/core';
import {
  NODE_TYPES,
  type KnowledgeMapEdge,
  type KnowledgeMapNode,
  type NodeType,
  type RelationshipType,
} from '../knowledge-maps.types';

type SortOrder = 'default' | 'alpha';

interface NodeGroup {
  type: NodeType;
  nodes: KnowledgeMapNode[];
}

/** Per-relationship outbound edge counts for a single node. */
interface EdgeBreakdown {
  ParentOf: number;
  RelatedTo: number;
  RequiredBy: number;
  total: number;
}

/**
 * Accessible alternative to GraphCanvas. Renders the same node data
 * as a structured set of grouped node lists, with brand-aligned UX:
 *
 *   - Sort toggle (default order ↔ alphabetical) at the top of the view.
 *   - Each section header carries a shape pip that matches the
 *     Cytoscape node shape (circle / round-rect / diamond) so the
 *     visual mapping between graph + list views is obvious.
 *   - Sections are collapsible — click the header to fold the rows.
 *   - Each row shows a small composition pill (parent / related /
 *     required outbound counts) so users can spot dense or hub-like
 *     nodes at a glance.
 *
 * Click any node row → emits (nodeSelected) — same handler the
 * GraphCanvas uses, so the explore loop is identical in both views.
 */
@Component({
  selector: 'cce-list-view',
  standalone: true,
  imports: [CommonModule, MatIconModule, TranslateModule],
  templateUrl: './list-view.component.html',
  styleUrl: './list-view.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ListViewComponent {
  readonly nodes = input.required<KnowledgeMapNode[]>();
  readonly edges = input.required<KnowledgeMapEdge[]>();
  readonly selectedId = input<string | null>(null);
  readonly dimmedIds = input<ReadonlySet<string>>(new Set());
  readonly locale = input<'ar' | 'en'>('en');

  readonly nodeSelected = output<string>();

  /** Sort order applied within each section. Default = original order. */
  readonly sortOrder = signal<SortOrder>('default');

  /** Set of collapsed section types. Empty = all sections expanded. */
  readonly collapsed = signal<ReadonlySet<NodeType>>(new Set());

  /** Group nodes by type and apply the sort order. */
  readonly grouped = computed<NodeGroup[]>(() => {
    const byType: Record<NodeType, KnowledgeMapNode[]> = {
      Technology: [],
      Sector: [],
      SubTopic: [],
    };
    for (const n of this.nodes()) byType[n.nodeType].push(n);

    const sort = this.sortOrder();
    const localeCode = this.locale();
    const sorted = (rows: KnowledgeMapNode[]): KnowledgeMapNode[] => {
      if (sort === 'default') return rows;
      return [...rows].sort((a, b) => {
        const an = (localeCode === 'ar' ? a.nameAr : a.nameEn) || '';
        const bn = (localeCode === 'ar' ? b.nameAr : b.nameEn) || '';
        return an.localeCompare(bn, localeCode);
      });
    };
    return NODE_TYPES.map((t) => ({ type: t, nodes: sorted(byType[t]) }));
  });

  /** Outbound edge counts broken down by relationship type per node. */
  readonly edgeBreakdowns = computed<ReadonlyMap<string, EdgeBreakdown>>(() => {
    const map = new Map<string, EdgeBreakdown>();
    for (const e of this.edges()) {
      const slot = map.get(e.fromNodeId) ?? { ParentOf: 0, RelatedTo: 0, RequiredBy: 0, total: 0 };
      slot[e.relationshipType] += 1;
      slot.total += 1;
      map.set(e.fromNodeId, slot);
    }
    return map;
  });

  /** Outbound edge totals (used for the legacy count badge). */
  readonly outboundCounts = computed<ReadonlyMap<string, number>>(() => {
    const map = new Map<string, number>();
    for (const [id, b] of this.edgeBreakdowns()) map.set(id, b.total);
    return map;
  });

  /** Relationship-type ordering for the per-row composition pills. */
  readonly relTypes: readonly RelationshipType[] = ['ParentOf', 'RelatedTo', 'RequiredBy'];

  nameOf(n: KnowledgeMapNode): string {
    return this.locale() === 'ar' ? n.nameAr : n.nameEn;
  }

  outboundCountOf(n: KnowledgeMapNode): number {
    return this.outboundCounts().get(n.id) ?? 0;
  }

  breakdownOf(n: KnowledgeMapNode): EdgeBreakdown {
    return this.edgeBreakdowns().get(n.id) ?? { ParentOf: 0, RelatedTo: 0, RequiredBy: 0, total: 0 };
  }

  isSelected(n: KnowledgeMapNode): boolean {
    return this.selectedId() === n.id;
  }

  isDimmed(n: KnowledgeMapNode): boolean {
    return this.dimmedIds().has(n.id);
  }

  isCollapsed(type: NodeType): boolean {
    return this.collapsed().has(type);
  }

  toggleCollapsed(type: NodeType): void {
    this.collapsed.update((prev) => {
      const next = new Set(prev);
      if (next.has(type)) next.delete(type);
      else next.add(type);
      return next;
    });
  }

  setSort(order: SortOrder): void {
    this.sortOrder.set(order);
  }

  onSelect(n: KnowledgeMapNode): void {
    this.nodeSelected.emit(n.id);
  }
}
