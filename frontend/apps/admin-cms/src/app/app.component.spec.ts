import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { of } from 'rxjs';
import { AppComponent } from './app.component';

describe('AppComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AppComponent, TranslateModule.forRoot()],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
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

  it('renders the cce-app-shell', () => {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('cce-app-shell')).toBeTruthy();
  });

  it('renders the cce-auth-toolbar', () => {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('cce-auth-toolbar')).toBeTruthy();
  });
});
