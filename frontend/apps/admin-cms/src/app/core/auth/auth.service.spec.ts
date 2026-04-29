import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthService, CurrentUser } from './auth.service';

const sampleUser: CurrentUser = {
  id: 'abc-123',
  email: 'admin@cce.local',
  userName: 'admin',
  permissions: ['User.Read', 'Role.Assign'],
};

describe('AuthService', () => {
  let sut: AuthService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    sut = TestBed.inject(AuthService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('starts unauthenticated', () => {
    expect(sut.currentUser()).toBeNull();
    expect(sut.isAuthenticated()).toBe(false);
  });

  it('populates currentUser on /api/me 200', async () => {
    const promise = sut.refresh();
    httpTesting.expectOne('/api/me').flush(sampleUser);
    await promise;
    expect(sut.currentUser()).toEqual(sampleUser);
    expect(sut.isAuthenticated()).toBe(true);
  });

  it('sets currentUser to null on /api/me 401', async () => {
    sut._setUserForTest(sampleUser); // pretend a stale session
    const promise = sut.refresh();
    httpTesting.expectOne('/api/me').flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });
    await promise;
    expect(sut.currentUser()).toBeNull();
  });

  it('hasPermission returns true when user has the permission', () => {
    sut._setUserForTest(sampleUser);
    expect(sut.hasPermission('User.Read')).toBe(true);
    expect(sut.hasPermission('User.RoleAssign')).toBe(false);
  });

  it('hasPermission returns false when user is null', () => {
    expect(sut.hasPermission('User.Read')).toBe(false);
  });
});
