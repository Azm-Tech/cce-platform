import { Injectable, computed, inject, signal } from '@angular/core';
import { KnowledgeMapsApiService } from '../knowledge-maps-api.service';
import type {
  KnowledgeMap,
  KnowledgeMapEdge,
  KnowledgeMapNode,
  NodeType,
} from '../knowledge-maps.types';

export interface ViewerTab {
  id: string;
  metadata: KnowledgeMap;
  nodes: KnowledgeMapNode[];
  edges: KnowledgeMapEdge[];
  loadedAt: number;
}

export type ViewMode = 'graph' | 'list';

/**
 * Signal-driven state container for the map viewer.
 *
 * Provided at the route component level (`providers: [MapViewerStore]`),
 * so each route activation gets a fresh instance. Holds open tabs,
 * the active tab, the selected node, search + filter state, view
 * mode, and the multi-select for export.
 *
 * Actions (openTab, closeTab, setActive, selectNode, setSearch,
 * setFilters, setViewMode, setSelection, retry) mutate signals;
 * computed signals (openTabs, activeTab, selectedNode, notFound)
 * derive read-only views.
 */
@Injectable()
export class MapViewerStore {
  private readonly api = inject(KnowledgeMapsApiService);

  private readonly _tabsById = signal<Map<string, ViewerTab>>(new Map());
  private readonly _activeId = signal<string | null>(null);
  private readonly _selectedNodeId = signal<string | null>(null);
  private readonly _searchTerm = signal('');
  private readonly _filters = signal<Set<NodeType>>(new Set());
  private readonly _viewMode = signal<ViewMode>('graph');
  private readonly _selection = signal<Set<string>>(new Set());
  private readonly _loading = signal(false);
  private readonly _errorKind = signal<string | null>(null);

  // ─── Read-only signal accessors ───
  readonly tabsById = this._tabsById.asReadonly();
  readonly activeId = this._activeId.asReadonly();
  readonly selectedNodeId = this._selectedNodeId.asReadonly();
  readonly searchTerm = this._searchTerm.asReadonly();
  readonly filters = this._filters.asReadonly();
  readonly viewMode = this._viewMode.asReadonly();
  readonly selection = this._selection.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly errorKind = this._errorKind.asReadonly();

  // ─── Computed ───
  readonly openTabs = computed(() => Array.from(this._tabsById().values()));
  readonly activeTab = computed<ViewerTab | null>(() => {
    const id = this._activeId();
    return id ? (this._tabsById().get(id) ?? null) : null;
  });
  readonly selectedNode = computed<KnowledgeMapNode | null>(() => {
    const tab = this.activeTab();
    const sid = this._selectedNodeId();
    if (!tab || !sid) return null;
    return tab.nodes.find((n) => n.id === sid) ?? null;
  });
  readonly notFound = computed(() => this._errorKind() === 'not-found');

  // ─── Actions ───

  /** Loads map + nodes + edges in parallel. Adds the tab to the store and sets it active. */
  async openTab(id: string): Promise<void> {
    // Already open? just switch.
    if (this._tabsById().has(id)) {
      this._activeId.set(id);
      return;
    }
    this._loading.set(true);
    this._errorKind.set(null);
    const [mapRes, nodesRes, edgesRes] = await Promise.all([
      this.api.getMap(id),
      this.api.getNodes(id),
      this.api.getEdges(id),
    ]);
    this._loading.set(false);
    if (!mapRes.ok) {
      this._errorKind.set(mapRes.error.kind);
      return;
    }
    if (!nodesRes.ok) {
      this._errorKind.set(nodesRes.error.kind);
      return;
    }
    if (!edgesRes.ok) {
      this._errorKind.set(edgesRes.error.kind);
      return;
    }
    const tab: ViewerTab = {
      id,
      metadata: mapRes.value,
      nodes: nodesRes.value,
      edges: edgesRes.value,
      loadedAt: Date.now(),
    };
    this._tabsById.update((m) => {
      const next = new Map(m);
      next.set(id, tab);
      return next;
    });
    this._activeId.set(id);
  }

  closeTab(id: string): void {
    this._tabsById.update((m) => {
      const next = new Map(m);
      next.delete(id);
      return next;
    });
    if (this._activeId() === id) {
      const remaining = Array.from(this._tabsById().keys());
      this._activeId.set(remaining[remaining.length - 1] ?? null);
    }
  }

  setActive(id: string): void {
    if (!this._tabsById().has(id)) return;
    this._activeId.set(id);
    this._selectedNodeId.set(null);
  }

  selectNode(id: string | null): void {
    this._selectedNodeId.set(id);
  }

  setSearch(term: string): void {
    this._searchTerm.set(term);
  }

  setFilters(types: NodeType[]): void {
    this._filters.set(new Set(types));
  }

  setViewMode(mode: ViewMode): void {
    this._viewMode.set(mode);
  }

  setSelection(ids: ReadonlySet<string>): void {
    this._selection.set(new Set(ids));
  }

  retry(): Promise<void> | void {
    const id = this._activeId();
    if (id) return this.openTab(id);
    // No active tab — nothing to retry.
    return undefined;
  }
}
