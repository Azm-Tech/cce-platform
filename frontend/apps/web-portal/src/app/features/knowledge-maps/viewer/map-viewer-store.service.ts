import { Injectable, computed, inject, signal } from '@angular/core';
import { KnowledgeMapsApiService } from '../knowledge-maps-api.service';
import { nodeMatches } from '../lib/search';
import type { InteractiveMap, InteractiveMapNode, NodeDetails } from '../knowledge-maps.types';

export interface ViewerTab {
  id: string;
  metadata: InteractiveMap;
  nodes: InteractiveMapNode[];
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
 */
@Injectable()
export class MapViewerStore {
  private readonly api = inject(KnowledgeMapsApiService);

  private readonly _tabsById = signal<Map<string, ViewerTab>>(new Map());
  private readonly _activeId = signal<string | null>(null);
  private readonly _selectedNodeId = signal<string | null>(null);
  private readonly _searchTerm = signal('');
  private readonly _filters = signal<Set<number>>(new Set());
  private readonly _viewMode = signal<ViewMode>('graph');
  private readonly _selection = signal<Set<string>>(new Set());
  private readonly _loading = signal(false);
  private readonly _errorKind = signal<string | null>(null);
  private readonly _nodeDetails = signal<NodeDetails | null>(null);
  private readonly _detailsLoading = signal(false);
  private readonly detailsCache = new Map<string, NodeDetails>();

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
  readonly nodeDetails = this._nodeDetails.asReadonly();
  readonly detailsLoading = this._detailsLoading.asReadonly();

  // ─── Computed ───
  readonly openTabs = computed(() => Array.from(this._tabsById().values()));
  readonly activeTab = computed<ViewerTab | null>(() => {
    const id = this._activeId();
    return id ? (this._tabsById().get(id) ?? null) : null;
  });
  readonly selectedNode = computed<InteractiveMapNode | null>(() => {
    const tab = this.activeTab();
    const sid = this._selectedNodeId();
    if (!tab || !sid) return null;
    return tab.nodes.find((n) => n.id === sid) ?? null;
  });
  readonly notFound = computed(() => this._errorKind() === 'not-found');

  /** Set of node ids matching the current search term + level filters in the active tab. */
  readonly matchedIds = computed<ReadonlySet<string>>(() => {
    const tab = this.activeTab();
    if (!tab) return new Set();
    const term = this._searchTerm();
    const filters = this._filters();
    const matched = new Set<string>();
    for (const n of tab.nodes) {
      if (nodeMatches(n, term, filters)) matched.add(n.id);
    }
    return matched;
  });

  readonly dimmedIds = computed<ReadonlySet<string>>(() => {
    const tab = this.activeTab();
    if (!tab) return new Set();
    if (!this._searchTerm().trim() && this._filters().size === 0) {
      return new Set();
    }
    const matched = this.matchedIds();
    const dimmed = new Set<string>();
    for (const n of tab.nodes) {
      if (!matched.has(n.id)) dimmed.add(n.id);
    }
    return dimmed;
  });

  // ─── Actions ───

  /** Loads map (with embedded nodes) and sets the tab active. */
  async openTab(id: string): Promise<void> {
    if (this._tabsById().has(id)) {
      this._activeId.set(id);
      return;
    }
    this._loading.set(true);
    this._errorKind.set(null);
    const mapRes = await this.api.getMap(id);
    this._loading.set(false);
    if (!mapRes.ok) {
      this._errorKind.set(mapRes.error.kind);
      return;
    }
    const mapData = mapRes.value;

    // Synthesize the map itself as the central root node (level = -1).
    // The API returns nameAr/nameEn on the map object — this IS the Figma
    // center node. Nodes from mapData.nodes that have no parentId connect to it.
    const rootId = `${id}__root`;
    const syntheticRoot: InteractiveMapNode = {
      id: rootId,
      nameAr: mapData.nameAr.trim(),
      nameEn: mapData.nameEn.trim(),
      iconKey: 'co2',
      level: -1,
      parentId: null,
      category: null,
      topicId: '',
      tags: [],
    };
    const nodes: InteractiveMapNode[] = [
      syntheticRoot,
      ...mapData.nodes.map((n) => (!n.parentId ? { ...n, parentId: rootId } : n)),
    ];

    const tab: ViewerTab = {
      id,
      metadata: mapData,
      nodes,
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
    if (!id) {
      this._nodeDetails.set(null);
      this._detailsLoading.set(false);
      return;
    }
    // The synthetic map root (level -1) has no backing topic — no details fetch.
    if (id.endsWith('__root')) {
      this._nodeDetails.set(null);
      this._detailsLoading.set(false);
      return;
    }
    // Clear so the drawer shows a loading state for the newly-selected node.
    this._nodeDetails.set(null);
    void this.loadNodeDetails(id);
  }

  /** Lazily fetch (and cache) the topic content for a node's detail drawer. */
  async loadNodeDetails(nodeId: string): Promise<void> {
    const cached = this.detailsCache.get(nodeId);
    if (cached) {
      this._nodeDetails.set(cached);
      this._detailsLoading.set(false);
      return;
    }
    this._detailsLoading.set(true);
    const res = await this.api.getNodeDetails(nodeId);
    // Ignore a stale response if the user has since selected a different node.
    if (this._selectedNodeId() !== nodeId) return;
    this._detailsLoading.set(false);
    if (res.ok) {
      this.detailsCache.set(nodeId, res.value);
      this._nodeDetails.set(res.value);
    }
  }

  setSearch(term: string): void {
    this._searchTerm.set(term);
  }

  setFilters(levels: number[]): void {
    this._filters.set(new Set(levels));
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
    return undefined;
  }
}
