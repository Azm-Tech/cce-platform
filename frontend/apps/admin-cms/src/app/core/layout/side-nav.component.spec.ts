import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { signal } from '@angular/core';
import { of } from 'rxjs';
import { AuthService, CurrentUser } from '../auth/auth.service';
import { SideNavComponent } from './side-nav.component';
import { NAV_ITEMS } from './nav-config';

describe('SideNavComponent', () => {
  let fixture: ComponentFixture<SideNavComponent>;
  let userSig: ReturnType<typeof signal<CurrentUser | null>>;
  let authStub: Pick<AuthService, 'currentUser' | 'hasPermission'>;

  beforeEach(async () => {
    userSig = signal<CurrentUser | null>(null);
    authStub = {
      currentUser: userSig.asReadonly(),
      hasPermission: (p: string) => userSig()?.permissions.includes(p) ?? false,
    };

    await TestBed.configureTestingModule({
      imports: [SideNavComponent, TranslateModule.forRoot()],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: AuthService, useValue: authStub },
      ],
    }).compileComponents();

    // Stub translate to return the key verbatim
    const translate = TestBed.inject(TranslateService);
    jest.spyOn(translate, 'instant').mockImplementation((key: string | string[]) =>
      Array.isArray(key) ? key[0] : key,
    );
    jest.spyOn(translate, 'get').mockImplementation((key: string | string[]) =>
      of(Array.isArray(key) ? key[0] : key),
    );

    fixture = TestBed.createComponent(SideNavComponent);
    fixture.detectChanges();
  });

  it('renders zero nav links when user has no permissions', () => {
    const links = fixture.nativeElement.querySelectorAll('a[mat-list-item]');
    expect(links).toHaveLength(0);
  });

  it('renders one link per NAV_ITEM when user has every permission', () => {
    const allPermissions = [...new Set(NAV_ITEMS.map((i) => i.permission))];
    TestBed.runInInjectionContext(() =>
      userSig.set({ id: '1', email: 'admin@test.com', userName: 'admin', permissions: allPermissions }),
    );
    fixture.detectChanges();

    const links = fixture.nativeElement.querySelectorAll('a[mat-list-item]');
    expect(links).toHaveLength(NAV_ITEMS.length);
  });

  it('renders only the Users link when user only has User.Read', () => {
    TestBed.runInInjectionContext(() =>
      userSig.set({ id: '1', email: 'admin@test.com', userName: 'admin', permissions: ['User.Read'] }),
    );
    fixture.detectChanges();

    const links = fixture.nativeElement.querySelectorAll('a[mat-list-item]');
    // User.Read covers nav.users only (nav.taxonomies & nav.resources share Resource.Center.Upload but that's not granted)
    expect(links).toHaveLength(1);
  });

  it('reactively removes links when user is signed out', () => {
    TestBed.runInInjectionContext(() =>
      userSig.set({ id: '1', email: 'admin@test.com', userName: 'admin', permissions: ['User.Read', 'Audit.Read'] }),
    );
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('a[mat-list-item]')).toHaveLength(2);

    TestBed.runInInjectionContext(() => userSig.set(null));
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('a[mat-list-item]')).toHaveLength(0);
  });

  it('renders icons for each visible nav item', () => {
    TestBed.runInInjectionContext(() =>
      userSig.set({ id: '1', email: 'admin@test.com', userName: 'admin', permissions: ['User.Read'] }),
    );
    fixture.detectChanges();

    const icon = fixture.nativeElement.querySelector('mat-icon');
    expect(icon).not.toBeNull();
    expect(icon.textContent.trim()).toBe('people');
  });
});
