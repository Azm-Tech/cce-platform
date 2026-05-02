import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  effect,
  inject,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslateModule } from '@ngx-translate/core';
import { LocaleService } from '@frontend/i18n';
import type { KnowledgeMapNode, NodeType } from './knowledge-maps.types';
import { GraphCanvasComponent } from './viewer/graph-canvas.component';
import { MapViewerStore } from './viewer/map-viewer-store.service';
import { NodeDetailPanelComponent } from './viewer/node-detail-panel.component';
import { SearchAndFiltersComponent } from './viewer/search-and-filters.component';
import { TabsBarComponent } from './viewer/tabs-bar.component';
import { buildUrlPatch, parseUrlState } from './viewer/url-state';

/**
 * Top-level page for the knowledge-map viewer at /knowledge-maps/:id.
 *
 * Provides MapViewerStore at the component level so each route
 * activation gets a fresh state container. Hydrates URL query params
 * (?open + ?q + ?type + ?view + ?node) into the store before
 * opening the active tab. Renders TabsBar + SearchAndFilters above
 * the graph; GraphCanvas + NodeDetailPanel side-by-side.
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
    SearchAndFiltersComponent,
    TabsBarComponent,
  ],
  providers: [MapViewerStore],
  templateUrl: './map-viewer.page.html',
  styleUrl: './map-viewer.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MapViewerPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly localeService = inject(LocaleService);
  private readonly destroyRef = inject(DestroyRef);
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

  /** Initial-frame guard: skip the URL-sync effect's first run so we don't immediately overwrite the URL the user navigated to. */
  private hydrated = false;

  constructor() {
    // ─── URL sync: search + filter + open-tabs signals → URL ───
    effect(() => {
      const q = this.store.searchTerm();
      const filters = Array.from(this.store.filters());
      // open = open tabs other than the active one (the active tab is in the route :id).
      const activeId = this.store.activeId();
      const otherIds = this.store
        .openTabs()
        .map((t) => t.id)
        .filter((tid) => tid !== activeId);
      // Always read all reactive deps so the effect re-fires;
      // skip the post-hydration baseline run.
      if (!this.hydrated) return;
      const patch = buildUrlPatch({ q, filters, open: otherIds });
      void this.router.navigate([], {
        relativeTo: this.route,
        queryParams: patch,
        queryParamsHandling: 'merge',
        replaceUrl: true,
      });
    });

    // ─── Sync activeId to route :id when user navigates between tabs ───
    // Angular reuses the component instance on same-route param changes,
    // so we subscribe to paramMap and switch the active tab in the store.
    this.route.paramMap
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((params) => {
        const newId = params.get('id');
        if (!newId) return;
        if (this.store.tabsById().has(newId) && this.store.activeId() !== newId) {
          this.store.setActive(newId);
        }
      });
  }

  async ngOnInit(): Promise<void> {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) return;

    // Hydrate non-route URL state into the store before opening the tab.
    const url = parseUrlState(this.route.snapshot.queryParams);
    this.store.setSearch(url.q);
    this.store.setFilters(url.filters);
    this.store.setViewMode(url.view);
    if (url.node) this.store.selectNode(url.node);

    // Mark hydration complete so the URL-sync effect starts firing on
    // subsequent user-driven changes.
    this.hydrated = true;

    // Open the active tab first so it's the foreground.
    await this.store.openTab(id);

    // Open any additional tabs from ?open= so they're available in the strip.
    const otherTabsToOpen = url.open.filter((openId) => openId !== id);
    if (otherTabsToOpen.length > 0) {
      for (const otherId of otherTabsToOpen) {
        await this.store.openTab(otherId);
      }
      // openTab(otherId) flips activeId — restore the route :id as active.
      // setActive() clears selectedNodeId, so re-apply the hydrated selection.
      this.store.setActive(id);
      if (url.node) this.store.selectNode(url.node);
    }
  }

  /** GraphCanvas (nodeClick) handler. */
  onNodeClick(id: string): void {
    this.store.selectNode(id);
  }

  /** GraphCanvas (selectionChange) handler — feeds the export multi-select. */
  onSelectionChange(ids: ReadonlySet<string>): void {
    this.store.setSelection(ids);
  }

  /** NodeDetailPanel (closed) handler — clears selection. */
  onPanelClosed(): void {
    this.store.selectNode(null);
  }

  /** NodeDetailPanel (nodeSelected) handler — re-selects via store, closing the explore loop. */
  onPanelNodeSelected(id: string): void {
    this.store.selectNode(id);
  }

  /** SearchAndFilters (searchTermChange) handler. */
  onSearchTermChange(term: string): void {
    this.store.setSearch(term);
  }

  /** SearchAndFilters (filtersChange) handler. */
  onFiltersChange(filters: ReadonlySet<NodeType>): void {
    this.store.setFilters(Array.from(filters));
  }

  /** TabsBar (tabSelected) handler — navigates to the new active tab. */
  onTabSelected(id: string): void {
    void this.router.navigate(['/knowledge-maps', id], {
      queryParamsHandling: 'preserve',
    });
  }

  /** TabsBar (tabClosed) handler — closes a tab; routes accordingly. */
  onTabClosed(id: string): void {
    const wasActive = this.store.activeId() === id;
    this.store.closeTab(id);
    const remaining = this.store.openTabs();
    if (remaining.length === 0) {
      void this.router.navigate(['/knowledge-maps']);
      return;
    }
    if (wasActive) {
      const fallback = this.store.activeId();
      if (fallback) {
        void this.router.navigate(['/knowledge-maps', fallback], {
          queryParamsHandling: 'preserve',
        });
      }
    }
  }

  retry(): void {
    void this.store.retry();
  }
}
