import { Injectable, computed, inject, signal } from '@angular/core';
import { KnowledgeMapsApiService } from '../knowledge-maps-api.service';
import { nodeMatches } from '../lib/search';
import type { InteractiveMap, InteractiveMapNode, NodeDetails } from '../knowledge-maps.types';

export type ViewMode = 'graph' | 'list';

/**
 * Signal-driven state container for the map viewer.
 *
 * The system holds a single interactive map, so the store loads that one
 * map (via `getCurrentMap()`) and exposes its metadata + nodes directly —
 * no tabs, no active id. Provided at the route component level
 * (`providers: [MapViewerStore]`) so each activation gets a fresh instance.
 * Holds the loaded map, the selected node, search + filter state, and the
 * view mode.
 */
@Injectable()
export class MapViewerStore {
  private readonly api = inject(KnowledgeMapsApiService);

  private readonly _map = signal<InteractiveMap | null>(null);
  private readonly _nodes = signal<InteractiveMapNode[]>([]);
  private readonly _selectedNodeId = signal<string | null>(null);
  private readonly _searchTerm = signal('');
  private readonly _filters = signal<Set<number>>(new Set());
  private readonly _viewMode = signal<ViewMode>('graph');
  private readonly _loading = signal(false);
  private readonly _errorKind = signal<string | null>(null);
  private readonly _nodeDetails = signal<NodeDetails | null>(null);
  private readonly _detailsLoading = signal(false);
  private readonly detailsCache = new Map<string, NodeDetails>();

  // ─── Read-only signal accessors ───
  readonly map = this._map.asReadonly();
  readonly nodes = this._nodes.asReadonly();
  readonly selectedNodeId = this._selectedNodeId.asReadonly();
  readonly searchTerm = this._searchTerm.asReadonly();
  readonly filters = this._filters.asReadonly();
  readonly viewMode = this._viewMode.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly errorKind = this._errorKind.asReadonly();
  readonly nodeDetails = this._nodeDetails.asReadonly();
  readonly detailsLoading = this._detailsLoading.asReadonly();

  // ─── Computed ───
  readonly selectedNode = computed<InteractiveMapNode | null>(() => {
    const sid = this._selectedNodeId();
    if (!sid) return null;
    return this._nodes().find((n) => n.id === sid) ?? null;
  });
  readonly notFound = computed(() => this._errorKind() === 'not-found');

  /** Set of node ids matching the current search term + level filters. */
  readonly matchedIds = computed<ReadonlySet<string>>(() => {
    const term = this._searchTerm();
    const filters = this._filters();
    const matched = new Set<string>();
    for (const n of this._nodes()) {
      if (nodeMatches(n, term, filters)) matched.add(n.id);
    }
    return matched;
  });

  readonly dimmedIds = computed<ReadonlySet<string>>(() => {
    if (!this._searchTerm().trim() && this._filters().size === 0) {
      return new Set();
    }
    const matched = this.matchedIds();
    const dimmed = new Set<string>();
    for (const n of this._nodes()) {
      if (!matched.has(n.id)) dimmed.add(n.id);
    }
    return dimmed;
  });

  // ─── Actions ───

  /** Loads the single map (with embedded nodes) and a synthetic root. */
  async loadMap(): Promise<void> {
    this._loading.set(true);
    this._errorKind.set(null);
    const mapRes = await this.api.getCurrentMap();
    this._loading.set(false);
    if (!mapRes.ok) {
      this._errorKind.set(mapRes.error.kind);
      return;
    }
    const mapData = mapRes.value;

    // Synthesize the map itself as the central root node (level = -1).
    // The API returns nameAr/nameEn on the map object — this IS the Figma
    // center node. Nodes from mapData.nodes that have no parentId connect to it.
    const rootId = `${mapData.id}__root`;
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
    const reparented = mapData.nodes.map((n) =>
      !n.parentId ? { ...n, parentId: rootId } : n,
    );

    // The radial layout places the root's children by array order
    // (1→top-right, 2→bottom-right, 3→bottom-left, 4→top-left). Order the
    // main branches to match the Figma reading layout (RTL): top-right =
    // Reduction, bottom-right = Removal, bottom-left = Recycling, top-left =
    // Reuse. Unknown names keep their original relative order (other maps).
    const BRANCH_ORDER = ['Reduction', 'Removal', 'Recycling', 'Reuse'];
    const branchRank = (n: InteractiveMapNode): number => {
      const i = BRANCH_ORDER.indexOf(n.nameEn ?? '');
      return i === -1 ? BRANCH_ORDER.length : i;
    };
    const branches = reparented
      .filter((n) => n.parentId === rootId)
      .sort((a, b) => branchRank(a) - branchRank(b));
    const leaves = reparented.filter((n) => n.parentId !== rootId);
    const nodes: InteractiveMapNode[] = [syntheticRoot, ...branches, ...leaves];

    this._map.set(mapData);
    this._nodes.set(nodes);
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

  retry(): Promise<void> {
    return this.loadMap();
  }
}
