import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TranslateModule } from '@ngx-translate/core';
import { ExpertApiService, type Result } from './expert-api.service';
import type { ExpertProfile, PagedResult } from './expert.types';
import { ExpertProfilesListPage } from './expert-profiles-list.page';

const SAMPLE: ExpertProfile = {
  id: 'p1',
  userId: 'u1',
  userName: 'alice',
  bioAr: '',
  bioEn: '',
  expertiseTags: ['ccs'],
  academicTitleAr: 'دكتور',
  academicTitleEn: 'Dr.',
  approvedOn: '2026-04-29',
  approvedById: 'admin',
};

describe('ExpertProfilesListPage', () => {
  let fixture: ComponentFixture<ExpertProfilesListPage>;
  let page: ExpertProfilesListPage;
  let listProfiles: jest.Mock;

  function ok(value: PagedResult<ExpertProfile>): Result<PagedResult<ExpertProfile>> {
    return { ok: true, value };
  }

  beforeEach(async () => {
    listProfiles = jest.fn().mockResolvedValue(ok({ items: [SAMPLE], page: 1, pageSize: 20, total: 1 }));
    await TestBed.configureTestingModule({
      imports: [ExpertProfilesListPage, TranslateModule.forRoot()],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: ExpertApiService, useValue: { listProfiles } },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(ExpertProfilesListPage);
    page = fixture.componentInstance;
  });

  it('loads on init', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    expect(listProfiles).toHaveBeenCalledWith({ page: 1, pageSize: 20, search: undefined });
    expect(page.rows()).toEqual([SAMPLE]);
  });

  it('onSearch resets page + passes search query', async () => {
    page.page.set(4);
    page.searchInput.set('alice');
    listProfiles.mockClear();
    page.onSearch();
    await Promise.resolve();
    expect(page.page()).toBe(1);
    expect(listProfiles).toHaveBeenCalledWith({ page: 1, pageSize: 20, search: 'alice' });
  });

  it('onPage updates page + size and reloads', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    listProfiles.mockClear();
    page.onPage({ pageIndex: 2, pageSize: 50, length: 1, previousPageIndex: 0 });
    await Promise.resolve();
    expect(page.page()).toBe(3);
    expect(page.pageSize()).toBe(50);
  });
});
