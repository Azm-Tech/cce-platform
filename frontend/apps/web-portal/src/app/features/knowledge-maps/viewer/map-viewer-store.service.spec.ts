import { TestBed } from '@angular/core/testing';
import { KnowledgeMapsApiService, type Result } from '../knowledge-maps-api.service';
import type {
  KnowledgeMap,
  KnowledgeMapEdge,
  KnowledgeMapNode,
} from '../knowledge-maps.types';
import { MapViewerStore } from './map-viewer-store.service';

const MAP: KnowledgeMap = {
  id: 'm1',
  nameAr: 'خريطة', nameEn: 'Map',
  descriptionAr: 'وصف', descriptionEn: 'Description',
  slug: 'main',
  isActive: true,
};

const N1: KnowledgeMapNode = {
  id: 'n1', mapId: 'm1',
  nameAr: 'تقنية', nameEn: 'Tech 1',
  nodeType: 'Technology',
  descriptionAr: null, descriptionEn: null,
  iconUrl: null,
  layoutX: 100, layoutY: 200,
  orderIndex: 0,
};
const N2: KnowledgeMapNode = { ...N1, id: 'n2', nameEn: 'Tech 2', layoutX: 300 };
const NODES: KnowledgeMapNode[] = [N1, N2];

const EDGES: KnowledgeMapEdge[] = [
  { id: 'e1', mapId: 'm1', fromNodeId: 'n1', toNodeId: 'n2', relationshipType: 'ParentOf', orderIndex: 0 },
];

function ok<T>(value: T): Result<T> {
  return { ok: true, value };
}

describe('MapViewerStore', () => {
  let sut: MapViewerStore;
  let getMap: jest.Mock;
  let getNodes: jest.Mock;
  let getEdges: jest.Mock;

  beforeEach(() => {
    getMap = jest.fn().mockResolvedValue(ok(MAP));
    getNodes = jest.fn().mockResolvedValue(ok(NODES));
    getEdges = jest.fn().mockResolvedValue(ok(EDGES));

    TestBed.configureTestingModule({
      providers: [
        MapViewerStore,
        { provide: KnowledgeMapsApiService, useValue: { getMap, getNodes, getEdges } },
      ],
    });
    sut = TestBed.inject(MapViewerStore);
  });

  it('openTab calls api 3x in parallel and lands the tab in the store', async () => {
    await sut.openTab('m1');
    expect(getMap).toHaveBeenCalledWith('m1');
    expect(getNodes).toHaveBeenCalledWith('m1');
    expect(getEdges).toHaveBeenCalledWith('m1');
    expect(sut.openTabs()).toHaveLength(1);
    expect(sut.activeId()).toBe('m1');
    expect(sut.activeTab()?.metadata).toEqual(MAP);
  });

  it('openTab failure on getMap (404) sets errorKind and notFound computed', async () => {
    getMap.mockResolvedValueOnce({ ok: false, error: { kind: 'not-found' } });
    await sut.openTab('missing');
    expect(sut.errorKind()).toBe('not-found');
    expect(sut.notFound()).toBe(true);
    expect(sut.openTabs()).toHaveLength(0);
  });

  it('opening an already-open tab just switches active without re-fetching', async () => {
    await sut.openTab('m1');
    getMap.mockClear();
    getNodes.mockClear();
    getEdges.mockClear();
    await sut.openTab('m1');
    expect(getMap).not.toHaveBeenCalled();
    expect(getNodes).not.toHaveBeenCalled();
    expect(getEdges).not.toHaveBeenCalled();
    expect(sut.activeId()).toBe('m1');
  });

  it('closeTab removes the tab and falls back to the last remaining as active', async () => {
    // open two tabs
    await sut.openTab('m1');
    getMap.mockResolvedValueOnce(ok({ ...MAP, id: 'm2' }));
    getNodes.mockResolvedValueOnce(ok([]));
    getEdges.mockResolvedValueOnce(ok([]));
    await sut.openTab('m2');
    expect(sut.activeId()).toBe('m2');
    sut.closeTab('m2');
    expect(sut.openTabs()).toHaveLength(1);
    expect(sut.activeId()).toBe('m1');
  });

  it('closeTab on the last tab leaves activeId null', async () => {
    await sut.openTab('m1');
    sut.closeTab('m1');
    expect(sut.openTabs()).toHaveLength(0);
    expect(sut.activeId()).toBeNull();
  });

  it('selectNode + selectedNode computed resolves the node in the active tab', async () => {
    await sut.openTab('m1');
    sut.selectNode('n1');
    expect(sut.selectedNode()).toEqual(N1);
    sut.selectNode(null);
    expect(sut.selectedNode()).toBeNull();
  });

  it('setSearch / setFilters / setViewMode mutate the corresponding signals', () => {
    sut.setSearch('carbon');
    expect(sut.searchTerm()).toBe('carbon');
    sut.setFilters(['Technology', 'Sector']);
    expect(Array.from(sut.filters())).toEqual(['Technology', 'Sector']);
    sut.setViewMode('list');
    expect(sut.viewMode()).toBe('list');
  });

  it('openTabs computed mirrors the underlying tabs map size', async () => {
    expect(sut.openTabs()).toHaveLength(0);
    await sut.openTab('m1');
    expect(sut.openTabs()).toHaveLength(1);
  });

  it('activeTab is null when no tab has been opened', () => {
    expect(sut.activeTab()).toBeNull();
  });

  it('retry() re-runs openTab on the current active id', async () => {
    await sut.openTab('m1');
    getMap.mockClear();
    getNodes.mockClear();
    getEdges.mockClear();
    // After a tab is already open, retry() short-circuits via the
    // already-open path — verifies activeId is still m1 and no extra
    // network calls were made.
    await sut.retry();
    expect(sut.activeId()).toBe('m1');
    expect(getMap).not.toHaveBeenCalled();
  });

  it('dimmedIds is empty when no filter is active (no dimming on an unfiltered graph)', async () => {
    await sut.openTab('m1');
    expect(sut.searchTerm()).toBe('');
    expect(sut.filters().size).toBe(0);
    expect(sut.dimmedIds().size).toBe(0);
    expect(sut.matchedIds().size).toBe(2); // both nodes match
  });

  it('search term narrows matchedIds and dims the rest', async () => {
    await sut.openTab('m1');
    sut.setSearch('Tech 1');
    // N1 has nameEn='Tech 1', N2 has nameEn='Tech 2'
    expect(sut.matchedIds().has('n1')).toBe(true);
    expect(sut.matchedIds().has('n2')).toBe(false);
    expect(sut.dimmedIds().has('n2')).toBe(true);
    expect(sut.dimmedIds().has('n1')).toBe(false);
  });

  it('NodeType filter dims nodes outside the filter set', async () => {
    await sut.openTab('m1');
    // Both fixture nodes are Technology — filter to Sector should dim both.
    sut.setFilters(['Sector']);
    expect(sut.matchedIds().size).toBe(0);
    expect(sut.dimmedIds().size).toBe(2);
  });
});
