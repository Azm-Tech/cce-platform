import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';
import { guestGuard } from './guest.guard';

describe('guestGuard (admin-cms)', () => {
  let auth: { isAuthenticated: jest.Mock };
  let router: { createUrlTree: jest.Mock };

  beforeEach(() => {
    auth = { isAuthenticated: jest.fn() };
    router = { createUrlTree: jest.fn((cmds) => cmds) };
    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: auth },
        { provide: Router, useValue: router },
      ],
    });
  });

  function run() {
    return TestBed.runInInjectionContext(() => guestGuard({} as never, {} as never));
  }

  it('allows unauthenticated users through', () => {
    auth.isAuthenticated.mockReturnValue(false);
    expect(run()).toBe(true);
    expect(router.createUrlTree).not.toHaveBeenCalled();
  });

  it('redirects authenticated admins to /', () => {
    auth.isAuthenticated.mockReturnValue(true);
    run();
    expect(router.createUrlTree).toHaveBeenCalledWith(['/']);
  });
});
