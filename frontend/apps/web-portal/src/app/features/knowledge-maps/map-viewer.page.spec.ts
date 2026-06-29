import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
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

interface RouteFixture {
  snapshot: { queryParams: Record<string, string | undefined> };
}

describe('MapViewerPage', () => {
  let fixture: ComponentFixture<MapViewerPage>;
  let page: MapViewerPage;
  let getCurrentMap: jest.Mock;
  let getNodeDetails: jest.Mock;

  async function setup(opts: { query?: Record<string, string> } = {}) {
    getCurrentMap = jest.fn().mockResolvedValue(ok(MAP));
    getNodeDetails = jest.fn().mockResolvedValue(
      ok({ node: NODE, topic: null, resources: [], news: [], events: [], posts: [] }),
    );

    const routeFixture: RouteFixture = {
      snapshot: { queryParams: opts.query ?? {} },
    };

    await TestBed.configureTestingModule({
      imports: [
        MapViewerPage,
        TranslocoTestingModule.forRoot({ langs: { en: {}, ar: {} }, translocoConfig: { availableLangs: ['en', 'ar'], defaultLang: 'en' } }),
      ],
      providers: [
        provideNoopAnimations(),
        { provide: Router, useValue: { navigate: jest.fn() } },
        { provide: KnowledgeMapsApiService, useValue: { getCurrentMap, getNodeDetails } },
        { provide: ActivatedRoute, useValue: routeFixture },
        { provide: LocaleService, useValue: { locale: signal<'ar' | 'en'>('en').asReadonly() } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(MapViewerPage);
    page = fixture.componentInstance;
  }

  it('init calls store.loadMap() and renders the map header', async () => {
    await setup();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(getCurrentMap).toHaveBeenCalled();
    const html = fixture.nativeElement.textContent ?? '';
    expect(html).toContain('Map');
  });

  it('deep-links ?node=<id> by selecting that node after the map loads', async () => {
    await setup({ query: { node: 'n1' } });
    await page.ngOnInit();

    expect(page.store.selectedNodeId()).toBe('n1');
  });

  it('404 on getCurrentMap renders the not-found block', async () => {
    await setup();
    getCurrentMap.mockResolvedValueOnce({ ok: false, error: { kind: 'not-found' } });
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(page.store.notFound()).toBe(true);
    const html = fixture.nativeElement.textContent ?? '';
    expect(html).toContain('knowledgeMaps.notFound');
  });

  it('non-404 error renders the error banner with a retry button', async () => {
    await setup();
    getCurrentMap.mockResolvedValueOnce({ ok: false, error: { kind: 'server' } });
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(page.store.errorKind()).toBe('server');
    const btn = fixture.nativeElement.querySelector('.cce-map-viewer__error button') as HTMLButtonElement | null;
    expect(btn).not.toBeNull();
  });

  it('retry() calls store.retry()', async () => {
    await setup();
    fixture.detectChanges();
    await fixture.whenStable();
    const spy = jest.spyOn(page.store, 'retry');
    page.retry();
    expect(spy).toHaveBeenCalled();
  });
});
