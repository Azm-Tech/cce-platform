import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import {
  NODE_TYPES,
  type KnowledgeMapEdge,
  type KnowledgeMapNode,
  type NodeType,
} from '../knowledge-maps.types';

interface NodeGroup {
  type: NodeType;
  nodes: KnowledgeMapNode[];
}

/**
 * Accessible alternative to GraphCanvas. Renders the same node data
 * as a structured <ul> tree grouped by NodeType. Keyboard- and
 * screen-reader-friendly (per spec §8 — W3C-recommended pattern for
 * SVG-style diagrams). Visual graph + list view are dual presentations
 * of the same underlying store state, so toggling between them
 * preserves selection + dim state.
 *
 * Click any node row -> emits (nodeSelected) — same handler the
 * GraphCanvas uses, so the explore loop is identical in both views.
 */
@Component({
  selector: 'cce-list-view',
  standalone: true,
  imports: [CommonModule, TranslateModule],
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

  readonly grouped = computed<NodeGroup[]>(() => {
    const byType: Record<NodeType, KnowledgeMapNode[]> = {
      Technology: [],
      Sector: [],
      SubTopic: [],
    };
    for (const n of this.nodes()) byType[n.nodeType].push(n);
    return NODE_TYPES.map((t) => ({ type: t, nodes: byType[t] }));
  });

  readonly outboundCounts = computed<ReadonlyMap<string, number>>(() => {
    const map = new Map<string, number>();
    for (const e of this.edges()) {
      map.set(e.fromNodeId, (map.get(e.fromNodeId) ?? 0) + 1);
    }
    return map;
  });

  nameOf(n: KnowledgeMapNode): string {
    return this.locale() === 'ar' ? n.nameAr : n.nameEn;
  }

  outboundCountOf(n: KnowledgeMapNode): number {
    return this.outboundCounts().get(n.id) ?? 0;
  }

  isSelected(n: KnowledgeMapNode): boolean {
    return this.selectedId() === n.id;
  }

  isDimmed(n: KnowledgeMapNode): boolean {
    return this.dimmedIds().has(n.id);
  }

  onSelect(n: KnowledgeMapNode): void {
    this.nodeSelected.emit(n.id);
  }
}
