import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { AuthService } from './auth.service';
import { PermissionDirective } from './permission.directive';

@Component({
  standalone: true,
  imports: [PermissionDirective],
  template: `<button type="button" *ccePermission="'User.Read'">edit</button>`,
})
class HostComponent {}

describe('PermissionDirective', () => {
  let fixture: ComponentFixture<HostComponent>;
  let auth: AuthService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HostComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    auth = TestBed.inject(AuthService);
    fixture = TestBed.createComponent(HostComponent);
  });

  it('hides element when user lacks permission', () => {
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('button')).toBeNull();
  });

  it('shows element when user has permission', () => {
    auth._setUserForTest({ id: '1', email: 'x', userName: 'x', permissions: ['User.Read'] });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('button')).not.toBeNull();
  });

  it('reactively hides when permission is revoked', () => {
    auth._setUserForTest({ id: '1', email: 'x', userName: 'x', permissions: ['User.Read'] });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('button')).not.toBeNull();

    auth._setUserForTest(null);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('button')).toBeNull();
  });
});
