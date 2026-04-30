import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthService, CurrentUser } from './auth.service';

const sampleUser: CurrentUser = {
  id: 'abc-123',
  email: 'user@cce.local',
  userName: 'user',
  displayNameAr: 'مستخدم',
  displayNameEn: 'User',
  avatarUrl: null,
  countryId: 'sa',
  isExpert: false,
};

describe('AuthService', () => {
  let sut: AuthService;
  let httpTesting: HttpTestingController;
  let assignMock: jest.Mock;

  beforeEach(() => {
    // jsdom makes window.location non-configurable; redefine it so we can spy on assign.
    assignMock = jest.fn();
    Object.defineProperty(window, 'location', {
      configurable: true,
      writable: true,
      value: { ...window.location, assign: assignMock },
    });

    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    sut = TestBed.inject(AuthService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('currentUser starts null and isAuthenticated is false', () => {
    expect(sut.currentUser()).toBeNull();
    expect(sut.isAuthenticated()).toBe(false);
  });

  it('refresh() populates currentUser on /api/me 200', async () => {
    const promise = sut.refresh();
    httpTesting.expectOne('/api/me').flush(sampleUser);
    await promise;
    expect(sut.currentUser()).toEqual(sampleUser);
    expect(sut.isAuthenticated()).toBe(true);
  });

  it('refresh() keeps currentUser null on /api/me 401 and does not throw', async () => {
    const promise = sut.refresh();
    httpTesting.expectOne('/api/me').flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });
    await expect(promise).resolves.toBeUndefined();
    expect(sut.currentUser()).toBeNull();
  });

  it('signIn() calls window.location.assign with /auth/login?returnUrl= encoded path', () => {
    sut.signIn('/my/path?foo=bar');
    expect(assignMock).toHaveBeenCalledWith('/auth/login?returnUrl=%2Fmy%2Fpath%3Ffoo%3Dbar');
  });

  it('signOut posts to /auth/logout, clears user, and navigates home', async () => {
    sut._setUserForTest({ id: '1', email: 'a@b', userName: 'a', displayNameAr: null, displayNameEn: null, avatarUrl: null, countryId: null, isExpert: false });
    const promise = sut.signOut();
    const req = httpTesting.expectOne('/auth/logout');
    expect(req.request.method).toBe('POST');
    req.flush({});
    await promise;
    expect(sut.currentUser()).toBeNull();
    expect(assignMock).toHaveBeenCalledWith('/');
  });
});
