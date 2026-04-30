import { Route } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { AuthService } from './auth.service';
import { authGuard } from './auth.guard';

describe('authGuard', () => {
  let auth: { isAuthenticated: jest.Mock; signIn: jest.Mock };
  let router: { url: string };

  beforeEach(() => {
    auth = { isAuthenticated: jest.fn(), signIn: jest.fn() };
    router = { url: '/protected' };
    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: auth },
        { provide: 'Router', useValue: router },
      ],
    });
    // Provide Router in the correct token form
    TestBed.overrideProvider(
      // eslint-disable-next-line @typescript-eslint/no-require-imports
      require('@angular/router').Router,
      { useValue: router },
    );
  });

  function run(): boolean {
    return TestBed.runInInjectionContext(() => authGuard({} as Route, []) as boolean);
  }

  it('returns true when user is authenticated', () => {
    auth.isAuthenticated.mockReturnValue(true);
    expect(run()).toBe(true);
    expect(auth.signIn).not.toHaveBeenCalled();
  });

  it('returns false and calls signIn(router.url) when not authenticated', () => {
    auth.isAuthenticated.mockReturnValue(false);
    expect(run()).toBe(false);
    expect(auth.signIn).toHaveBeenCalledWith('/protected');
  });
});
