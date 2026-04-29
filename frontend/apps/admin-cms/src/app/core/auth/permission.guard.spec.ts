import { Route } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { runInInjectionContext } from '@angular/core';
import { AuthService } from './auth.service';
import { permissionGuard } from './permission.guard';

describe('permissionGuard', () => {
  let auth: { hasPermission: jest.Mock };

  beforeEach(() => {
    auth = { hasPermission: jest.fn() };
    TestBed.configureTestingModule({
      providers: [{ provide: AuthService, useValue: auth }],
    });
  });

  function run(route: Route): boolean | Promise<boolean> | unknown {
    return TestBed.runInInjectionContext(() => permissionGuard(route, []));
  }

  it('allows when no permission required', () => {
    expect(run({ data: {} })).toBe(true);
  });

  it('allows when user has the permission', () => {
    auth.hasPermission.mockReturnValue(true);
    expect(run({ data: { permission: 'User.Read' } })).toBe(true);
    expect(auth.hasPermission).toHaveBeenCalledWith('User.Read');
  });

  it('denies when user lacks the permission', () => {
    auth.hasPermission.mockReturnValue(false);
    expect(run({ data: { permission: 'User.Read' } })).toBe(false);
  });
});
