import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TranslateModule } from '@ngx-translate/core';
import { PortalShellComponent } from './portal-shell.component';
import { AuthService } from '../auth/auth.service';

describe('PortalShellComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PortalShellComponent, TranslateModule.forRoot()],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        provideHttpClient(),
        provideHttpClientTesting(),
        AuthService,
      ],
    }).compileComponents();
  });

  it('creates the component', () => {
    const fixture = TestBed.createComponent(PortalShellComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('renders cce-header', () => {
    const fixture = TestBed.createComponent(PortalShellComponent);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('cce-header')).toBeTruthy();
  });

  it('renders cce-footer', () => {
    const fixture = TestBed.createComponent(PortalShellComponent);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('cce-footer')).toBeTruthy();
  });

  it('renders main element with router-outlet', () => {
    const fixture = TestBed.createComponent(PortalShellComponent);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('main')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('router-outlet')).toBeTruthy();
  });
});
