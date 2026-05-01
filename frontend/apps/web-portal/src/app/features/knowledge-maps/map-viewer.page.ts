import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslateModule } from '@ngx-translate/core';
import { LocaleService } from '@frontend/i18n';
import type { KnowledgeMapNode } from './knowledge-maps.types';
import { GraphCanvasComponent } from './viewer/graph-canvas.component';
import { MapViewerStore } from './viewer/map-viewer-store.service';
import { NodeDetailPanelComponent } from './viewer/node-detail-panel.component';
import { parseUrlState } from './viewer/url-state';

/**
 * Top-level page for the knowledge-map viewer at /knowledge-maps/:id.
 *
 * Provides MapViewerStore at the component level so each route
 * activation gets a fresh state container. Hydrates URL query params
 * into the store before opening the active tab. Renders progress /
 * not-found / error / active-tab-header + the GraphCanvas + the
 * NodeDetailPanel side-by-side.
 */
@Component({
  selector: 'cce-map-viewer-page',
  standalone: true,
  imports: [
    CommonModule, RouterLink,
    MatButtonModule, MatIconModule, MatProgressBarModule,
    TranslateModule,
    GraphCanvasComponent,
    NodeDetailPanelComponent,
  ],
  providers: [MapViewerStore],
  templateUrl: './map-viewer.page.html',
  styleUrl: './map-viewer.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MapViewerPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly localeService = inject(LocaleService);
  readonly store = inject(MapViewerStore);

  /** Active locale signal — drives node label selection in GraphCanvas. */
  readonly locale = this.localeService.locale;
  /** Mirror x-coordinates when locale === 'ar'. */
  readonly mirrored = computed(() => this.locale() === 'ar');

  /** Outbound edges for the currently selected node within the active tab. */
  readonly outboundEdges = computed(() => {
    const tab = this.store.activeTab();
    const node = this.store.selectedNode();
    if (!tab || !node) return [];
    return tab.edges.filter((e) => e.fromNodeId === node.id);
  });

  /** Pre-resolved target nodes for the outbound edges (used by the panel). */
  readonly outboundTargets = computed<KnowledgeMapNode[]>(() => {
    const tab = this.store.activeTab();
    const edges = this.outboundEdges();
    if (!tab || edges.length === 0) return [];
    const targetIds = new Set(edges.map((e) => e.toNodeId));
    return tab.nodes.filter((n) => targetIds.has(n.id));
  });

  async ngOnInit(): Promise<void> {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) return;

    // Hydrate non-route URL state into the store before opening the tab.
    const url = parseUrlState(this.route.snapshot.queryParams);
    this.store.setSearch(url.q);
    this.store.setFilters(url.filters);
    this.store.setViewMode(url.view);
    if (url.node) this.store.selectNode(url.node);

    await this.store.openTab(id);
  }

  /** GraphCanvas (nodeClick) handler. */
  onNodeClick(id: string): void {
    this.store.selectNode(id);
  }

  /** NodeDetailPanel (closed) handler — clears selection. */
  onPanelClosed(): void {
    this.store.selectNode(null);
  }

  /** NodeDetailPanel (nodeSelected) handler — re-selects via store, closing the explore loop. */
  onPanelNodeSelected(id: string): void {
    this.store.selectNode(id);
  }

  retry(): void {
    void this.store.retry();
  }
}
