import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TranslateModule } from '@ngx-translate/core';
import { IdentityApiService, type Result } from './identity-api.service';
import type { PagedResult, UserListItem } from './identity.types';
import { UsersListPage } from './users-list.page';

const SAMPLE: UserListItem = {
  id: 'u1',
  email: 'alice@example.com',
  userName: 'alice',
  roles: ['SuperAdmin'],
  isActive: true,
};

describe('UsersListPage', () => {
  let fixture: ComponentFixture<UsersListPage>;
  let page: UsersListPage;
  let listUsers: jest.Mock;
  let api: { listUsers: jest.Mock };

  function ok(value: PagedResult<UserListItem>): Result<PagedResult<UserListItem>> {
    return { ok: true, value };
  }

  beforeEach(async () => {
    listUsers = jest.fn().mockResolvedValue(ok({ items: [SAMPLE], page: 1, pageSize: 20, total: 1 }));
    api = { listUsers };

    await TestBed.configureTestingModule({
      imports: [UsersListPage, TranslateModule.forRoot()],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: IdentityApiService, useValue: api },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(UsersListPage);
    page = fixture.componentInstance;
  });

  it('loads users on init with default paging', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    expect(listUsers).toHaveBeenCalledWith({ page: 1, pageSize: 20, search: undefined, role: undefined });
    expect(page.rows()).toEqual([SAMPLE]);
    expect(page.total()).toBe(1);
  });

  it('passes search + role filters when provided', async () => {
    page.searchInput.set('alice');
    page.roleFilter.set('SuperAdmin');
    await page.load();
    expect(listUsers).toHaveBeenLastCalledWith({
      page: 1,
      pageSize: 20,
      search: 'alice',
      role: 'SuperAdmin',
    });
  });

  it('onPage updates page + pageSize signals and reloads', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    listUsers.mockClear();
    page.onPage({ page: 3, pageSize: 50 });
    await fixture.whenStable();
    expect(page.page()).toBe(3);
    expect(page.pageSize()).toBe(50);
    expect(listUsers).toHaveBeenCalledWith({
      page: 3,
      pageSize: 50,
      search: undefined,
      role: undefined,
    });
  });

  it('onSearch resets page to 1 and reloads', async () => {
    page.page.set(5);
    page.searchInput.set('bob');
    listUsers.mockClear();
    page.onSearch();
    await Promise.resolve();
    expect(page.page()).toBe(1);
    expect(listUsers).toHaveBeenCalledWith({ page: 1, pageSize: 20, search: 'bob', role: undefined });
  });

  it('onRoleFilter updates roleFilter signal and reloads from page 1', async () => {
    page.page.set(4);
    listUsers.mockClear();
    page.onRoleFilter('CommunityExpert');
    await Promise.resolve();
    expect(page.roleFilter()).toBe('CommunityExpert');
    expect(page.page()).toBe(1);
    expect(listUsers).toHaveBeenCalledWith({
      page: 1,
      pageSize: 20,
      search: undefined,
      role: 'CommunityExpert',
    });
  });

  it('exposes errorKind when the api returns an error', async () => {
    listUsers.mockResolvedValueOnce({ ok: false, error: { kind: 'server' } });
    await page.load();
    expect(page.errorKind()).toBe('server');
    expect(page.rows()).toEqual([]);
  });

  it('renders error banner when errorKind is set', async () => {
    listUsers.mockResolvedValueOnce({ ok: false, error: { kind: 'forbidden' } });
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    const banner = fixture.nativeElement.querySelector('.cce-users-list__error');
    expect(banner).not.toBeNull();
  });

  it('renders the paged-table once data loads', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('cce-paged-table')).not.toBeNull();
  });
});
