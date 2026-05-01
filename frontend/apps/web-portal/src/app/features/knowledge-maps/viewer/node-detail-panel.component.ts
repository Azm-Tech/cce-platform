import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  HostListener,
  computed,
  input,
  output,
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { TranslateModule } from '@ngx-translate/core';
import type { KnowledgeMapEdge, KnowledgeMapNode } from '../knowledge-maps.types';

/**
 * Side-panel (desktop) / bottom-sheet (mobile) showing the details
 * of the currently selected node. CSS-driven responsiveness — a
 * single component handles both breakpoints via a media query at
 * 720px.
 *
 * The panel renders nothing when `node()` is null. When a node is
 * provided, it shows:
 *   - Localized name (h2)
 *   - Localized description (or "—" when null)
 *   - Node-type badge
 *   - Outbound edges list (relationship-type badge + target node
 *     name; click emits (nodeSelected) so the parent can re-route
 *     selection through the store, closing the explore loop)
 *
 * ESC keyboard shortcut + a close button both emit (closed).
 */
@Component({
  selector: 'cce-node-detail-panel',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule, TranslateModule],
  templateUrl: './node-detail-panel.component.html',
  styleUrl: './node-detail-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NodeDetailPanelComponent {
  readonly node = input<KnowledgeMapNode | null>(null);
  /** Edges where fromNodeId === node().id. Resolved by parent. */
  readonly outboundEdges = input<KnowledgeMapEdge[]>([]);
  /** Pre-resolved target nodes for outboundEdges (parent looks them up). */
  readonly outboundTargets = input<KnowledgeMapNode[]>([]);
  readonly locale = input<'ar' | 'en'>('en');

  readonly closed = output<void>();
  readonly nodeSelected = output<string>();

  readonly name = computed(() => {
    const n = this.node();
    if (!n) return '';
    return this.locale() === 'ar' ? n.nameAr : n.nameEn;
  });

  readonly description = computed(() => {
    const n = this.node();
    if (!n) return '';
    const raw = this.locale() === 'ar' ? n.descriptionAr : n.descriptionEn;
    return raw ?? '—';
  });

  readonly hasOutboundEdges = computed(() => this.outboundEdges().length > 0);

  /** Resolves the localized target name for an edge. */
  targetNameOf(edge: KnowledgeMapEdge): string {
    const target = this.outboundTargets().find((n) => n.id === edge.toNodeId);
    if (!target) return edge.toNodeId;
    return this.locale() === 'ar' ? target.nameAr : target.nameEn;
  }

  onClose(): void {
    this.closed.emit();
  }

  onEdgeClick(edge: KnowledgeMapEdge): void {
    this.nodeSelected.emit(edge.toNodeId);
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.node()) this.closed.emit();
  }
}
