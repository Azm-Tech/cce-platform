import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth.service';
import { RegisterPage } from './register.page';

describe('RegisterPage', () => {
  let fixture: ComponentFixture<RegisterPage>;
  let isAuthenticatedSig: ReturnType<typeof signal<boolean>>;
  let assignMock: jest.Mock;

  beforeEach(async () => {
    // jsdom: window.location.assign is read-only by default; redefine.
    assignMock = jest.fn();
    Object.defineProperty(window, 'location', {
      configurable: true,
      writable: true,
      value: { ...window.location, assign: assignMock },
    });

    isAuthenticatedSig = signal(false);

    await TestBed.configureTestingModule({
      imports: [RegisterPage, TranslateModule.forRoot()],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        {
          provide: AuthService,
          useValue: { isAuthenticated: isAuthenticatedSig.asReadonly() },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(RegisterPage);
  });

  it('continueToSignUp button calls window.location.assign(/api/users/register)', () => {
    fixture.detectChanges();
    const btn = fixture.nativeElement.querySelector('button[mat-flat-button]') as HTMLButtonElement;
    expect(btn).not.toBeNull();
    btn.click();
    expect(assignMock).toHaveBeenCalledWith('/api/users/register');
  });

  it('when authenticated, hides the continue button and renders an "Open profile" link', () => {
    isAuthenticatedSig.set(true);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('button[mat-flat-button]')).toBeNull();
    const link = fixture.nativeElement.querySelector('a[mat-flat-button]') as HTMLAnchorElement;
    expect(link).not.toBeNull();
    expect(link.getAttribute('href')).toBe('/me/profile');
  });

  it('renders title + body via i18n keys (smoke check that template binds)', () => {
    fixture.detectChanges();
    const html = fixture.nativeElement.textContent ?? '';
    // With TranslateModule.forRoot() and no translations loaded, ngx-translate
    // returns the key text unchanged — verify the keys appear, proving the
    // template is bound to the right translation keys.
    expect(html).toContain('account.register.title');
    expect(html).toContain('account.register.body');
    expect(html).toContain('account.register.continueButton');
  });
});
