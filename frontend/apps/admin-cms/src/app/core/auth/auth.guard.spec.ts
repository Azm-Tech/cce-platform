import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot } from '@angular/router';
import { CceAdminRole } from '@frontend/contracts';
import { AuthService } from './auth.service';
import { authGuard } from './auth.guard';

describe('authGuard (admin-cms)', () => {
  let auth: { isAuthenticated: jest.Mock; hasAnyRole: jest.Mock };
  let router: { createUrlTree: jest.Mock };
  let state: RouterStateSnapshot;

  beforeEach(() => {
    auth = { isAuthenticated: jest.fn(), hasAnyRole: jest.fn() };
    router = { createUrlTree: jest.fn((cmds, extras) => ({ cmds, extras })) };
    state = { url: '/users' } as RouterStateSnapshot;
    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: auth },
        { provide: Router, useValue: router },
      ],
    });
  });

  function run() {
    return TestBed.runInInjectionContext(
      () => authGuard({} as ActivatedRouteSnapshot, state),
    );
  }

  it('returns true for an authenticated admin', () => {
    auth.isAuthenticated.mockReturnValue(true);
    auth.hasAnyRole.mockReturnValue(true);
    expect(run()).toBe(true);
  });

  it('redirects to /login with returnUrl when not authenticated', () => {
    auth.isAuthenticated.mockReturnValue(false);
    run();
    expect(router.createUrlTree).toHaveBeenCalledWith(
      ['/login'],
      expect.objectContaining({ queryParams: { returnUrl: '/users' } }),
    );
  });

  it('omits returnUrl when the current URL is /', () => {
    auth.isAuthenticated.mockReturnValue(false);
    state = { url: '/' } as RouterStateSnapshot;
    run();
    expect(router.createUrlTree).toHaveBeenCalledWith(
      ['/login'],
      expect.objectContaining({ queryParams: undefined }),
    );
  });

  it('redirects authenticated non-admin to /login without returnUrl', () => {
    auth.isAuthenticated.mockReturnValue(true);
    auth.hasAnyRole.mockReturnValue(false);
    run();
    expect(router.createUrlTree).toHaveBeenCalledWith(['/login']);
    expect(router.createUrlTree).toHaveBeenCalledTimes(1);
    const [[path]] = router.createUrlTree.mock.calls;
    expect(path).toEqual(['/login']);
  });

  it('passes all CceAdminRole values to hasAnyRole', () => {
    auth.isAuthenticated.mockReturnValue(true);
    auth.hasAnyRole.mockReturnValue(true);
    run();
    const allRoles = Object.values(CceAdminRole);
    expect(auth.hasAnyRole).toHaveBeenCalledWith(...allRoles);
  });
});
