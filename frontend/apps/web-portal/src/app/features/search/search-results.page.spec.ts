import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, provideRouter, convertToParamMap, ParamMap } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { LocaleService } from '@frontend/i18n';
import { TranslateModule } from '@ngx-translate/core';
import { SearchApiService, type Result } from './search-api.service';
import type { PagedResult, SearchHit } from './search.types';
import { SearchResultsPage } from './search-results.page';

const HIT: SearchHit = {
  id: 'h1',
  type: 'News',
  titleAr: 'عنوان', titleEn: 'Title',
  excerptAr: 'مقتطف', excerptEn: 'Excerpt',
  score: 0.95,
};

describe('SearchResultsPage', () => {
  let fixture: ComponentFixture<SearchResultsPage>;
  let page: SearchResultsPage;
  let search: jest.Mock;
  let routerNavigate: jest.Mock;
  let queryParamMap$: BehaviorSubject<ParamMap>;

  function ok(value: PagedResult<SearchHit>): Result<PagedResult<SearchHit>> {
    return { ok: true, value };
  }

  async function setup(initial: Record<string, string | null> = {}) {
    queryParamMap$ = new BehaviorSubject(convertToParamMap(initial));
    const localeSig = signal<'ar' | 'en'>('en');

    await TestBed.configureTestingModule({
      imports: [SearchResultsPage, TranslateModule.forRoot()],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: SearchApiService, useValue: { search } },
        { provide: LocaleService, useValue: { locale: localeSig.asReadonly() } },
        {
          provide: ActivatedRoute,
          useValue: { queryParamMap: queryParamMap$.asObservable() },
        },
      ],
    }).compileComponents();

    const router = TestBed.inject(Router);
    routerNavigate = jest.spyOn(router, 'navigate').mockResolvedValue(true) as unknown as jest.Mock;
    fixture = TestBed.createComponent(SearchResultsPage);
    page = fixture.componentInstance;
  }

  beforeEach(() => {
    search = jest.fn().mockResolvedValue(
      ok({ items: [HIT], page: 1, pageSize: 20, total: 1 }),
    );
  });

  it('?q=carbon triggers search({ q: "carbon" }) on init', async () => {
    await setup({ q: 'carbon' });
    fixture.detectChanges();
    await fixture.whenStable();
    expect(search).toHaveBeenCalledWith({ q: 'carbon', type: undefined, page: 1, pageSize: 20 });
    expect(page.rows()).toHaveLength(1);
  });

  it('?q=carbon&type=News passes type to API call', async () => {
    await setup({ q: 'carbon', type: 'News' });
    fixture.detectChanges();
    await fixture.whenStable();
    expect(search).toHaveBeenCalledWith({ q: 'carbon', type: 'News', page: 1, pageSize: 20 });
  });

  it('empty q skips API call and renders type-a-query hint state', async () => {
    await setup({});
    fixture.detectChanges();
    await fixture.whenStable();
    expect(search).not.toHaveBeenCalled();
    expect(page.noQuery()).toBe(true);
  });

  it('toggleType syncs ?type= to URL and resets ?page=', async () => {
    await setup({ q: 'x' });
    fixture.detectChanges();
    await fixture.whenStable();
    routerNavigate.mockClear();
    page.toggleType('Events');
    expect(routerNavigate).toHaveBeenCalled();
    const args = routerNavigate.mock.calls[0];
    expect(args[1].queryParams).toEqual({ type: 'Events', page: null });
  });

  it('clicking the *active* type clears it (toggleType to null)', async () => {
    await setup({ q: 'x', type: 'News' });
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.type()).toBe('News');
    routerNavigate.mockClear();
    page.toggleType('News');
    const args = routerNavigate.mock.calls[0];
    expect(args[1].queryParams).toEqual({ type: null, page: null });
  });

  it('paginator change syncs page + pageSize to URL', async () => {
    await setup({ q: 'x' });
    fixture.detectChanges();
    await fixture.whenStable();
    routerNavigate.mockClear();
    page.onPage({ pageIndex: 2, pageSize: 50, length: 1, previousPageIndex: 0 });
    const args = routerNavigate.mock.calls[0];
    expect(args[1].queryParams).toEqual({ page: 3, pageSize: 50 });
  });
});
