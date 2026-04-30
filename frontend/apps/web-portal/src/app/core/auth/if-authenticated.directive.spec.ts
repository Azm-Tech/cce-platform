import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { AuthService } from './auth.service';
import { IfAuthenticatedDirective } from './if-authenticated.directive';

@Component({
  standalone: true,
  imports: [IfAuthenticatedDirective],
  template: `<button type="button" *ifAuthenticated>edit</button>`,
})
class HostComponent {}

describe('IfAuthenticatedDirective', () => {
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

  it('hides element when not authenticated', () => {
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('button')).toBeNull();
  });

  it('shows element when authenticated', () => {
    auth._setUserForTest({ id: '1', email: 'a', userName: 'a', displayNameAr: null, displayNameEn: null, avatarUrl: null, countryId: null, isExpert: false });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('button')).not.toBeNull();
  });

  it('reactively hides when user signs out', () => {
    auth._setUserForTest({ id: '1', email: 'a', userName: 'a', displayNameAr: null, displayNameEn: null, avatarUrl: null, countryId: null, isExpert: false });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('button')).not.toBeNull();
    auth._setUserForTest(null);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('button')).toBeNull();
  });
});
