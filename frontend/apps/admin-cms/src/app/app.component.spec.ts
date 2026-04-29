import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TranslateModule } from '@ngx-translate/core';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { of } from 'rxjs';
import { LocaleService } from '@frontend/i18n';
import { AuthService } from './core/auth/auth.service';
import { AppComponent } from './app.component';

describe('AppComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AppComponent, TranslateModule.forRoot()],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideNoopAnimations(),
        LocaleService,
        {
          provide: AuthService,
          useValue: {
            currentUser: () => null,
            isAuthenticated: () => false,
            hasPermission: () => false,
          },
        },
        {
          provide: OidcSecurityService,
          useValue: {
            isAuthenticated$: of({ isAuthenticated: false }),
            authorize: jest.fn(),
            logoff: jest.fn().mockReturnValue(of({})),
          },
        },
      ],
    }).compileComponents();
  });

  it('renders cce-shell', () => {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('cce-shell')).toBeTruthy();
  });

  it('renders cce-app-shell inside the shell', () => {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('cce-app-shell')).toBeTruthy();
  });

  it('renders cce-auth-toolbar inside the shell', () => {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('cce-auth-toolbar')).toBeTruthy();
  });
});
