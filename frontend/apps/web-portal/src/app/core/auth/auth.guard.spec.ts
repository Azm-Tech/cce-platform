import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { AuthService } from './auth.service';
import { authGuard, _resetAuthGuardForTest } from './auth.guard';

describe('authGuard', () => {
  let auth: { isAuthenticated: jest.Mock; signIn: jest.Mock; refresh: jest.Mock };
  let state: RouterStateSnapshot;

  beforeEach(() => {
    _resetAuthGuardForTest();
    auth = {
      isAuthenticated: jest.fn(),
      signIn: jest.fn(),
      refresh: jest.fn().mockResolvedValue(undefined),
    };
    state = { url: '/me/profile' } as RouterStateSnapshot;
    TestBed.configureTestingModule({
      providers: [{ provide: AuthService, useValue: auth }],
    });
  });

  function run(): Promise<boolean> {
    return TestBed.runInInjectionContext(
      () => authGuard({} as ActivatedRouteSnapshot, state) as Promise<boolean>,
    );
  }

  it('authenticated user returns true and does not refresh or signIn', async () => {
    auth.isAuthenticated.mockReturnValue(true);
    await expect(run()).resolves.toBe(true);
    expect(auth.refresh).not.toHaveBeenCalled();
    expect(auth.signIn).not.toHaveBeenCalled();
  });

  it('cold-start path: unauthenticated then refresh resolves to authenticated -> proceed', async () => {
    auth.isAuthenticated
      .mockReturnValueOnce(false)  // first call (pre-refresh)
      .mockReturnValueOnce(true);  // second call (post-refresh)
    await expect(run()).resolves.toBe(true);
    expect(auth.refresh).toHaveBeenCalledTimes(1);
    expect(auth.signIn).not.toHaveBeenCalled();
  });

  it('truly unauthenticated: refresh runs, still anonymous -> signIn(state.url) + false', async () => {
    auth.isAuthenticated.mockReturnValue(false);
    await expect(run()).resolves.toBe(false);
    expect(auth.refresh).toHaveBeenCalledTimes(1);
    expect(auth.signIn).toHaveBeenCalledWith('/me/profile');
  });

  it('idempotent: a second invocation does NOT re-call refresh', async () => {
    auth.isAuthenticated.mockReturnValue(false);
    await run();
    auth.refresh.mockClear();
    auth.signIn.mockClear();
    await run();
    expect(auth.refresh).not.toHaveBeenCalled();
    expect(auth.signIn).toHaveBeenCalledWith('/me/profile');
  });
});
