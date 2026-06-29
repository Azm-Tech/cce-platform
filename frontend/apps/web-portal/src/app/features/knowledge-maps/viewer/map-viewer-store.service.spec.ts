import { TestBed } from '@angular/core/testing';
import { KnowledgeMapsApiService, type Result } from '../knowledge-maps-api.service';
import type { InteractiveMap, InteractiveMapNode } from '../knowledge-maps.types';
import { MapViewerStore } from './map-viewer-store.service';

const N1: InteractiveMapNode = {
  id: 'n1',
  nameAr: 'تقنية', nameEn: 'Tech 1',
  iconKey: 'tech',
  level: 0,
  parentId: null,
  topicId: 't1',
  tags: [],
};
const N2: InteractiveMapNode = { ...N1, id: 'n2', nameEn: 'Tech 2', level: 1, parentId: 'n1' };
const NODES: InteractiveMapNode[] = [N1, N2];

const MAP: InteractiveMap = {
  id: 'm1',
  nameAr: 'خريطة', nameEn: 'Map',
  descriptionAr: 'وصف', descriptionEn: 'Description',
  nodes: NODES,
};

function ok<T>(value: T): Result<T> {
  return { ok: true, value };
}

describe('MapViewerStore', () => {
  let sut: MapViewerStore;
  let getCurrentMap: jest.Mock;

  beforeEach(() => {
    getCurrentMap = jest.fn().mockResolvedValue(ok(MAP));

    TestBed.configureTestingModule({
      providers: [
        MapViewerStore,
        { provide: KnowledgeMapsApiService, useValue: { getCurrentMap } },
      ],
    });
    sut = TestBed.inject(MapViewerStore);
  });

  it('loadMap calls api.getCurrentMap and lands the map with a synthetic root + embedded nodes', async () => {
    await sut.loadMap();
    expect(getCurrentMap).toHaveBeenCalled();
    expect(sut.map()).toEqual(MAP);

    // A synthetic root (the map itself, level -1) is prepended, and any
    // parentless node is re-parented onto it.
    const nodes = sut.nodes();
    expect(nodes).toHaveLength(3);
    expect(nodes[0].id).toBe('m1__root');
    expect(nodes[0].level).toBe(-1);
    expect(nodes[0].nameEn).toBe('Map');
    expect(nodes.find((n) => n.id === 'n1')?.parentId).toBe('m1__root');
    expect(nodes.find((n) => n.id === 'n2')?.parentId).toBe('n1');
  });

  it('loadMap failure on getCurrentMap (404) sets errorKind and notFound computed', async () => {
    getCurrentMap.mockResolvedValueOnce({ ok: false, error: { kind: 'not-found' } });
    await sut.loadMap();
    expect(sut.errorKind()).toBe('not-found');
    expect(sut.notFound()).toBe(true);
    expect(sut.map()).toBeNull();
    expect(sut.nodes()).toHaveLength(0);
  });

  it('selectNode + selectedNode computed resolves the node in the loaded map', async () => {
    await sut.loadMap();
    sut.selectNode('n1');
    // N1 is re-parented onto the synthetic root during loadMap.
    expect(sut.selectedNode()?.id).toBe('n1');
    expect(sut.selectedNode()?.parentId).toBe('m1__root');
    sut.selectNode(null);
    expect(sut.selectedNode()).toBeNull();
  });

  it('setSearch / setFilters / setViewMode mutate the corresponding signals', () => {
    sut.setSearch('carbon');
    expect(sut.searchTerm()).toBe('carbon');
    sut.setFilters([0, 1]);
    expect(Array.from(sut.filters())).toEqual([0, 1]);
    sut.setViewMode('list');
    expect(sut.viewMode()).toBe('list');
  });

  it('map is null before loadMap is called', () => {
    expect(sut.map()).toBeNull();
    expect(sut.nodes()).toHaveLength(0);
  });

  it('retry() re-runs loadMap', async () => {
    await sut.loadMap();
    getCurrentMap.mockClear();
    await sut.retry();
    expect(getCurrentMap).toHaveBeenCalled();
    expect(sut.map()).toEqual(MAP);
  });

  it('dimmedIds is empty when no filter is active', async () => {
    await sut.loadMap();
    expect(sut.searchTerm()).toBe('');
    expect(sut.filters().size).toBe(0);
    expect(sut.dimmedIds().size).toBe(0);
    expect(sut.matchedIds().size).toBe(3); // synthetic root + both nodes match
  });

  it('search term narrows matchedIds and dims the rest', async () => {
    await sut.loadMap();
    sut.setSearch('Tech 1');
    expect(sut.matchedIds().has('n1')).toBe(true);
    expect(sut.matchedIds().has('n2')).toBe(false);
    expect(sut.dimmedIds().has('n2')).toBe(true);
    expect(sut.dimmedIds().has('n1')).toBe(false);
  });

  it('level filter dims nodes outside the filter set', async () => {
    await sut.loadMap();
    // root is level -1, N1 level 0, N2 level 1 — filter to level 2 dims all three
    sut.setFilters([2]);
    expect(sut.matchedIds().size).toBe(0);
    expect(sut.dimmedIds().size).toBe(3);
  });
});
