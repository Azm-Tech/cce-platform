import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { of } from 'rxjs';
import { AuthToolbarComponent } from './auth-toolbar.component';

describe('AuthToolbarComponent', () => {
  let fixture: ComponentFixture<AuthToolbarComponent>;
  let component: AuthToolbarComponent;
  let oidc: jest.Mocked<Pick<OidcSecurityService, 'authorize' | 'logoff'>>;

  beforeEach(async () => {
    oidc = {
      authorize: jest.fn(),
      logoff: jest.fn().mockReturnValue(of({})),
    } as unknown as jest.Mocked<Pick<OidcSecurityService, 'authorize' | 'logoff'>>;
    await TestBed.configureTestingModule({
      imports: [AuthToolbarComponent, TranslateModule.forRoot()],
      providers: [
        {
          provide: OidcSecurityService,
          useValue: { ...oidc, isAuthenticated$: of({ isAuthenticated: false }) },
        },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(AuthToolbarComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('signIn() invokes oidc.authorize()', () => {
    component.signIn();
    expect(TestBed.inject(OidcSecurityService).authorize).toHaveBeenCalledTimes(1);
  });

  it('signOut() invokes oidc.logoff()', () => {
    component.signOut();
    expect(TestBed.inject(OidcSecurityService).logoff).toHaveBeenCalledTimes(1);
  });
});
