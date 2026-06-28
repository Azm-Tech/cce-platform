import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { LocaleService } from '@frontend/i18n';
import { KnowledgeMapsApiService, type Result } from './knowledge-maps-api.service';
import type { InteractiveMap, InteractiveMapNode } from './knowledge-maps.types';
import { MapViewerPage } from './map-viewer.page';

const NODE: InteractiveMapNode = {
  id: 'n1',
  nameAr: 'تقنية', nameEn: 'Technology',
  iconKey: 'tech',
  level: 1,
  parentId: null,
  topicId: 't1',
  tags: [],
};

const MAP: InteractiveMap = {
  id: 'm1',
  nameAr: 'خريطة', nameEn: 'Map',
  descriptionAr: 'وصف', descriptionEn: 'Description',
  nodes: [NODE],
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
  paramMap: import('rxjs').Observable<unknown>;
}

describe('MapViewerPage', () => {
  let fixture: ComponentFixture<MapViewerPage>;
  let page: MapViewerPage;
  let getMap: jest.Mock;

  async function setup(opts: { id?: string | null; query?: Record<string, string> } = {}) {
    getMap = jest.fn().mockResolvedValue(ok(MAP));

    const { of } = await import('rxjs');
    const routeFixture: RouteFixture = {
      snapshot: {
        paramMap: { get: jest.fn(() => opts.id ?? 'm1') },
        queryParams: opts.query ?? {},
      },
      paramMap: of({ get: (_key: string) => opts.id ?? 'm1' }),
    };

    await TestBed.configureTestingModule({
      imports: [
        MapViewerPage,
        TranslocoTestingModule.forRoot({ langs: { en: {}, ar: {} }, translocoConfig: { availableLangs: ['en', 'ar'], defaultLang: 'en' } }),
      ],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: KnowledgeMapsApiService, useValue: { getMap } },
        { provide: ActivatedRoute, useValue: routeFixture },
        { provide: LocaleService, useValue: { locale: signal<'ar' | 'en'>('en').asReadonly() } },
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
    const html = fixture.nativeElement.textContent ?? '';
    expect(html).toContain('Map');
  });

  it('hydrates URL query params (q, type, view, node) into the store before opening', async () => {
    await setup({
      id: 'm1',
      query: { q: 'carbon', type: '1', view: 'list', node: 'n1' },
    });
    fixture.detectChanges();
    await fixture.whenStable();

    expect(page.store.searchTerm()).toBe('carbon');
    expect(Array.from(page.store.filters())).toEqual([1]);
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
    const btn = fixture.nativeElement.querySelector('.cce-map-viewer__error button') as HTMLButtonElement | null;
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
