
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  ViewChild,
  computed,
  effect,
  inject,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslocoModule } from '@jsverse/transloco';
import { LocaleService } from '@frontend/i18n';
import { downloadBlob, buildFilename } from './lib/download';
import { exportJson } from './lib/export-json';
import { exportPdf } from './lib/export-pdf';
import { exportPng } from './lib/export-png';
import { exportSvg } from './lib/export-svg';
import { ExportMenuComponent, type ExportFormat } from './viewer/export-menu.component';
import { GraphCanvasComponent } from './viewer/graph-canvas.component';
import { ListViewComponent } from './viewer/list-view.component';
import { MapViewerStore, type ViewMode } from './viewer/map-viewer-store.service';
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
    RouterLink,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    TranslocoModule,
    GraphCanvasComponent,
    NodeDetailPanelComponent,
    SearchAndFiltersComponent,
    TabsBarComponent,
    ExportMenuComponent,
    ListViewComponent,
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

  @ViewChild(GraphCanvasComponent) canvas?: GraphCanvasComponent;

  readonly locale = this.localeService.locale;
  readonly mirrored = computed(() => this.locale() === 'ar');

  /** All nodes of the active tab — passed to the detail panel for parent/child resolution. */
  readonly allNodes = computed(() => this.store.activeTab()?.nodes ?? []);

  private hydrated = false;

  constructor() {
    // ─── URL sync: search + filter + open-tabs signals → URL ───
    effect(() => {
      const q = this.store.searchTerm();
      const filters = Array.from(this.store.filters());
      const activeId = this.store.activeId();
      const otherIds = this.store
        .openTabs()
        .map((t) => t.id)
        .filter((tid) => tid !== activeId);
      const view = this.store.viewMode();
      if (!this.hydrated) return;
      const patch = buildUrlPatch({ q, filters, open: otherIds, view });
      void this.router.navigate([], {
        relativeTo: this.route,
        queryParams: patch,
        queryParamsHandling: 'merge',
        replaceUrl: true,
      });
    });

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

    const url = parseUrlState(this.route.snapshot.queryParams);
    this.store.setSearch(url.q);
    this.store.setFilters(url.filters);
    this.store.setViewMode(url.view);
    if (url.node) this.store.selectNode(url.node);

    this.hydrated = true;

    await this.store.openTab(id);

    const otherTabsToOpen = url.open.filter((openId) => openId !== id);
    if (otherTabsToOpen.length > 0) {
      for (const otherId of otherTabsToOpen) {
        await this.store.openTab(otherId);
      }
      this.store.setActive(id);
      if (url.node) this.store.selectNode(url.node);
    }
  }

  onNodeClick(id: string): void {
    this.store.selectNode(id);
  }

  onSelectionChange(ids: ReadonlySet<string>): void {
    this.store.setSelection(ids);
  }

  onPanelClosed(): void {
    this.store.selectNode(null);
  }

  /** Drawer link navigation — route to the related content's detail page. */
  onPanelLink(e: { kind: 'resource' | 'news' | 'event' | 'post'; id: string }): void {
    const path: Record<typeof e.kind, string[]> = {
      resource: ['/knowledge-center', e.id],
      news: ['/news', e.id],
      event: ['/events', e.id],
      post: ['/community/posts', e.id],
    };
    void this.router.navigate(path[e.kind]);
  }

  onSearchTermChange(term: string): void {
    this.store.setSearch(term);
  }

  onFiltersChange(filters: ReadonlySet<number>): void {
    this.store.setFilters(Array.from(filters));
  }

  onTabSelected(id: string): void {
    void this.router.navigate(['/knowledge-maps', id], {
      queryParamsHandling: 'preserve',
    });
  }

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

  onSetViewMode(mode: ViewMode): void {
    this.store.setViewMode(mode);
  }

  onClearFilters(): void {
    this.store.setSearch('');
    this.store.setFilters([]);
  }

  retry(): void {
    void this.store.retry();
  }

  async onExportFormat(format: ExportFormat): Promise<void> {
    const tab = this.store.activeTab();
    if (!tab) return;
    const cy = this.canvas?.getCytoscape() ?? null;
    if (!cy && format !== 'json') return;

    const selection = this.store.selection();
    const useFull = selection.size === 0;
    const filename = buildFilename(tab.metadata.nameEn, format);

    let blob: Blob;
    switch (format) {
      case 'png':
        blob = await exportPng(cy as never, { full: useFull });
        break;
      case 'svg':
        blob = await exportSvg(cy as never, { full: useFull });
        break;
      case 'pdf':
        blob = await exportPdf(cy as never, { full: useFull });
        break;
      case 'json':
        blob = exportJson({
          map: {
            id: tab.id,
            nameAr: tab.metadata.nameAr,
            nameEn: tab.metadata.nameEn,
          },
          nodes: useFull ? tab.nodes : tab.nodes.filter((n) => selection.has(n.id)),
          exportedAt: new Date().toISOString(),
        });
        break;
    }
    downloadBlob(blob, filename);
  }
}
