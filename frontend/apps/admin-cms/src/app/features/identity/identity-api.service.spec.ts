import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { IdentityApiService } from './identity-api.service';
import type { PagedResult, StateRepAssignment, UserDetail, UserListItem } from './identity.types';

describe('IdentityApiService', () => {
  let sut: IdentityApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    sut = TestBed.inject(IdentityApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  describe('listUsers', () => {
    it('issues GET with default params (no query string)', async () => {
      const promise = sut.listUsers();
      const req = http.expectOne((r) => r.url === '/api/admin/users');
      expect(req.request.method).toBe('GET');
      expect(req.request.params.keys()).toEqual([]);
      const body: PagedResult<UserListItem> = { items: [], page: 1, pageSize: 20, total: 0 };
      req.flush(body);
      const res = await promise;
      expect(res).toEqual({ ok: true, value: body });
    });

    it('builds query string with page/pageSize/search/role', async () => {
      const promise = sut.listUsers({ page: 2, pageSize: 50, search: 'alice', role: 'SuperAdmin' });
      const req = http.expectOne((r) => r.url === '/api/admin/users');
      expect(req.request.params.get('page')).toBe('2');
      expect(req.request.params.get('pageSize')).toBe('50');
      expect(req.request.params.get('search')).toBe('alice');
      expect(req.request.params.get('role')).toBe('SuperAdmin');
      req.flush({ items: [], page: 2, pageSize: 50, total: 0 });
      await promise;
    });

    it('returns FeatureError on 500', async () => {
      const promise = sut.listUsers();
      http.expectOne('/api/admin/users').flush('boom', { status: 500, statusText: 'Server Error' });
      const res = await promise;
      expect(res.ok).toBe(false);
      if (!res.ok) expect(res.error.kind).toBe('server');
    });
  });

  describe('getUser', () => {
    it('issues GET /api/admin/users/{id}', async () => {
      const promise = sut.getUser('abc-123');
      const req = http.expectOne('/api/admin/users/abc-123');
      expect(req.request.method).toBe('GET');
      const body: UserDetail = {
        id: 'abc-123',
        email: 'a@b.com',
        userName: 'a',
        localePreference: 'en',
        knowledgeLevel: 'Beginner',
        interests: [],
        countryId: null,
        avatarUrl: null,
        roles: [],
        isActive: true,
      };
      req.flush(body);
      const res = await promise;
      expect(res).toEqual({ ok: true, value: body });
    });

    it('maps 404 to not-found FeatureError', async () => {
      const promise = sut.getUser('missing');
      http.expectOne('/api/admin/users/missing').flush('', { status: 404, statusText: 'Not Found' });
      const res = await promise;
      expect(res.ok).toBe(false);
      if (!res.ok) expect(res.error.kind).toBe('not-found');
    });
  });

  describe('assignRoles', () => {
    it('issues PUT with roles body', async () => {
      const promise = sut.assignRoles('user-1', ['SuperAdmin', 'ContentManager']);
      const req = http.expectOne('/api/admin/users/user-1/roles');
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual({ roles: ['SuperAdmin', 'ContentManager'] });
      const body: UserDetail = {
        id: 'user-1',
        email: 'a@b.com',
        userName: 'a',
        localePreference: 'en',
        knowledgeLevel: 'Beginner',
        interests: [],
        countryId: null,
        avatarUrl: null,
        roles: ['SuperAdmin', 'ContentManager'],
        isActive: true,
      };
      req.flush(body);
      const res = await promise;
      if (res.ok) expect(res.value.roles).toEqual(['SuperAdmin', 'ContentManager']);
      else fail('expected ok');
    });
  });

  describe('listStateRepAssignments', () => {
    it('builds active flag query string', async () => {
      const promise = sut.listStateRepAssignments({ active: false });
      const req = http.expectOne((r) => r.url === '/api/admin/state-rep-assignments');
      expect(req.request.params.get('active')).toBe('false');
      req.flush({ items: [], page: 1, pageSize: 20, total: 0 });
      await promise;
    });
  });

  describe('createStateRepAssignment', () => {
    it('issues POST and returns the created row', async () => {
      const promise = sut.createStateRepAssignment({
        userId: '11111111-1111-1111-1111-111111111111',
        countryId: '22222222-2222-2222-2222-222222222222',
      });
      const req = http.expectOne('/api/admin/state-rep-assignments');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({
        userId: '11111111-1111-1111-1111-111111111111',
        countryId: '22222222-2222-2222-2222-222222222222',
      });
      const body: StateRepAssignment = {
        id: 'a-id',
        userId: '11111111-1111-1111-1111-111111111111',
        userName: 'alice',
        countryId: '22222222-2222-2222-2222-222222222222',
        assignedOn: '2026-04-29T00:00:00Z',
        assignedById: 'admin-id',
        revokedOn: null,
        revokedById: null,
        isActive: true,
      };
      req.flush(body);
      const res = await promise;
      expect(res).toEqual({ ok: true, value: body });
    });

    it('maps 409 with /duplicate type to duplicate FeatureError', async () => {
      const promise = sut.createStateRepAssignment({ userId: 'u', countryId: 'c' });
      http.expectOne('/api/admin/state-rep-assignments').flush(
        { type: 'urn:cce:errors/duplicate', title: 'Duplicate' },
        { status: 409, statusText: 'Conflict' },
      );
      const res = await promise;
      expect(res.ok).toBe(false);
      if (!res.ok) expect(res.error.kind).toBe('duplicate');
    });
  });

  describe('revokeStateRepAssignment', () => {
    it('issues DELETE and returns void on 204', async () => {
      const promise = sut.revokeStateRepAssignment('rep-1');
      const req = http.expectOne('/api/admin/state-rep-assignments/rep-1');
      expect(req.request.method).toBe('DELETE');
      req.flush(null, { status: 204, statusText: 'No Content' });
      const res = await promise;
      expect(res.ok).toBe(true);
    });
  });
});
