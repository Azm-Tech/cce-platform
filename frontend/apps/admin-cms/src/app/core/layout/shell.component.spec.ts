import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TranslateModule } from '@ngx-translate/core';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { of } from 'rxjs';
import { LocaleService } from '@frontend/i18n';
import { AuthService } from '../auth/auth.service';
import { ShellComponent } from './shell.component';

const authStub = {
  currentUser: () => null,
  isAuthenticated: () => false,
  hasPermission: () => false,
};

const oidcStub = {
  isAuthenticated$: of({ isAuthenticated: false }),
  authorize: jest.fn(),
  logoff: jest.fn().mockReturnValue(of({})),
};

describe('ShellComponent', () => {
  let fixture: ComponentFixture<ShellComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ShellComponent, TranslateModule.forRoot()],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        LocaleService,
        { provide: AuthService, useValue: authStub },
        { provide: OidcSecurityService, useValue: oidcStub },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ShellComponent);
    fixture.detectChanges();
  });

  it('renders mat-sidenav-container', () => {
    expect(fixture.nativeElement.querySelector('mat-sidenav-container')).not.toBeNull();
  });

  it('renders cce-app-shell with appTitle', () => {
    const appShell = fixture.nativeElement.querySelector('cce-app-shell');
    expect(appShell).not.toBeNull();
  });

  it('renders cce-side-nav inside the sidenav drawer', () => {
    const sidenav = fixture.nativeElement.querySelector('mat-sidenav');
    expect(sidenav).not.toBeNull();
    expect(sidenav.querySelector('cce-side-nav')).not.toBeNull();
  });

  it('renders router-outlet', () => {
    expect(fixture.nativeElement.querySelector('router-outlet')).not.toBeNull();
  });
});
