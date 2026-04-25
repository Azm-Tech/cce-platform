import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslateModule } from '@ngx-translate/core';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { of } from 'rxjs';
import { ProfilePage } from './profile.page';

describe('ProfilePage', () => {
  let fixture: ComponentFixture<ProfilePage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProfilePage, TranslateModule.forRoot()],
      providers: [
        {
          provide: OidcSecurityService,
          useValue: {
            userData$: of({
              userData: {
                preferred_username: 'admin@cce.local',
                email: 'admin@cce.local',
                upn: 'admin@cce.local',
                groups: ['SuperAdmin'],
              },
            }),
          },
        },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(ProfilePage);
    fixture.detectChanges();
  });

  it('renders preferred_username from userData', () => {
    expect(fixture.nativeElement.textContent).toContain('admin@cce.local');
  });

  it('renders SuperAdmin group', () => {
    expect(fixture.nativeElement.textContent).toContain('SuperAdmin');
  });
});
