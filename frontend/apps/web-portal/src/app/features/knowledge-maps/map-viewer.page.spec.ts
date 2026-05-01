import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TranslateModule } from '@ngx-translate/core';
import { KnowledgeMapsApiService, type Result } from './knowledge-maps-api.service';
import type {
  KnowledgeMap,
  KnowledgeMapEdge,
  KnowledgeMapNode,
} from './knowledge-maps.types';
import { MapViewerPage } from './map-viewer.page';
import { MapViewerStore } from './viewer/map-viewer-store.service';

const MAP: KnowledgeMap = {
  id: 'm1',
  nameAr: 'خريطة', nameEn: 'Map',
  descriptionAr: 'وصف', descriptionEn: 'Description',
  slug: 'main',
  isActive: true,
};
const NODE: KnowledgeMapNode = {
  id: 'n1', mapId: 'm1',
  nameAr: 'تقنية', nameEn: 'Technology',
  nodeType: 'Technology',
  descriptionAr: null, descriptionEn: null,
  iconUrl: null,
  layoutX: 100, layoutY: 200,
  orderIndex: 0,
};
const EDGE: KnowledgeMapEdge = {
  id: 'e1', mapId: 'm1',
  fromNodeId: 'n1', toNodeId: 'n2',
  relationshipType: 'ParentOf',
  orderIndex: 0,
};

function ok<T>(value: T): Result<T> {
  return { ok: true, value };
}

interface RouteSnapshot {
  paramMap: { get: jest.Mock };
  queryParams: Record<string, string | undefined>;
}

interface RouteFixture {
  snapshot: RouteSnapshot;
}

describe('MapViewerPage', () => {
  let fixture: ComponentFixture<MapViewerPage>;
  let page: MapViewerPage;
  let getMap: jest.Mock;
  let getNodes: jest.Mock;
  let getEdges: jest.Mock;

  async function setup(opts: { id?: string | null; query?: Record<string, string> } = {}) {
    getMap = jest.fn().mockResolvedValue(ok(MAP));
    getNodes = jest.fn().mockResolvedValue(ok([NODE]));
    getEdges = jest.fn().mockResolvedValue(ok([EDGE]));

    const routeFixture: RouteFixture = {
      snapshot: {
        paramMap: { get: jest.fn(() => opts.id ?? 'm1') },
        queryParams: opts.query ?? {},
      },
    };

    await TestBed.configureTestingModule({
      imports: [MapViewerPage, TranslateModule.forRoot()],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: KnowledgeMapsApiService, useValue: { getMap, getNodes, getEdges } },
        { provide: ActivatedRoute, useValue: routeFixture },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(MapViewerPage);
    page = fixture.componentInstance;
  }

  it('init with valid id calls store.openTab(id) and renders the active tab header', async () => {
    await setup({ id: 'm1' });
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(getMap).toHaveBeenCalledWith('m1');
    expect(getNodes).toHaveBeenCalledWith('m1');
    expect(getEdges).toHaveBeenCalledWith('m1');
    const html = fixture.nativeElement.textContent ?? '';
    expect(html).toContain('Map');
    expect(html).toContain('Description');
    expect(html).toContain('1 nodes');
    expect(html).toContain('1 edges');
  });

  it('hydrates URL query params (q, type, view, node) into the store before opening', async () => {
    await setup({
      id: 'm1',
      query: { q: 'carbon', type: 'Technology', view: 'list', node: 'n1' },
    });
    fixture.detectChanges();
    await fixture.whenStable();

    expect(page.store.searchTerm()).toBe('carbon');
    expect(Array.from(page.store.filters())).toEqual(['Technology']);
    expect(page.store.viewMode()).toBe('list');
    expect(page.store.selectedNodeId()).toBe('n1');
  });

  it('404 on getMap renders the not-found block', async () => {
    await setup({ id: 'missing' });
    getMap.mockResolvedValueOnce({ ok: false, error: { kind: 'not-found' } });
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(page.store.notFound()).toBe(true);
    const html = fixture.nativeElement.textContent ?? '';
    expect(html).toContain('knowledgeMaps.notFound');
  });

  it('non-404 error renders the error banner with a retry button', async () => {
    await setup({ id: 'm1' });
    getMap.mockResolvedValueOnce({ ok: false, error: { kind: 'server' } });
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(page.store.errorKind()).toBe('server');
    const btn = fixture.nativeElement.querySelector('button[mat-button]') as HTMLButtonElement | null;
    expect(btn).not.toBeNull();
  });

  it('retry() calls store.retry()', async () => {
    await setup({ id: 'm1' });
    fixture.detectChanges();
    await fixture.whenStable();
    const spy = jest.spyOn(page.store, 'retry');
    page.retry();
    expect(spy).toHaveBeenCalled();
  });
});
