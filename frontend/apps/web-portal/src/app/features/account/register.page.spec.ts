import { ComponentFixture, TestBed } from '@angular/core/testing';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { TranslocoModule } from '@jsverse/transloco';
import { AuthService } from '../../core/auth/auth.service';
import { RegisterPage } from './register.page';

describe('RegisterPage', () => {
  let fixture: ComponentFixture<RegisterPage>;
  let isAuthenticatedSig: ReturnType<typeof signal<boolean>>;
  let http: HttpTestingController;

  const VALID_FORM = {
    firstName: 'Test',
    lastName: 'User',
    emailAddress: 'test.user@example.com',
    jobTitle: 'Engineer',
    organizationName: 'Acme Corp',
    phoneNumber: '+1234567890',
    password: 'Passw0rd!',
    confirmPassword: 'Passw0rd!',
  };

  function fillFormWithValidValues(component: RegisterPage): void {
    component.form.setValue(VALID_FORM);
  }

  beforeEach(async () => {
    isAuthenticatedSig = signal(false);

    await TestBed.configureTestingModule({
      imports: [RegisterPage, TranslocoModule.forRoot()],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: AuthService,
          useValue: {
            isAuthenticated: isAuthenticatedSig.asReadonly(),
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(RegisterPage);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('when authenticated, hides the form + renders an "Open profile" link', () => {
    isAuthenticatedSig.set(true);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('form')).toBeNull();
    const link = fixture.nativeElement.querySelector('a[mat-flat-button]') as HTMLAnchorElement;
    expect(link).not.toBeNull();
    expect(link.getAttribute('href')).toBe('/me/profile');
  });

  it('happy path: submitting valid form POSTs /api/auth/register and shows success', async () => {
    fixture.detectChanges();
    const component = fixture.componentInstance;
    fillFormWithValidValues(component);
    fixture.detectChanges();

    component.submit();

    const req = http.expectOne('/api/auth/register');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(VALID_FORM);
    req.flush(null, { status: 201, statusText: 'Created' });

    expect(component.state().kind).toBe('success');
  });

  it('409 Conflict surfaces the conflict error key', () => {
    fixture.detectChanges();
    const component = fixture.componentInstance;
    fillFormWithValidValues(component);

    component.submit();

    const req = http.expectOne('/api/auth/register');
    req.flush({}, { status: 409, statusText: 'Conflict' });

    const state = component.state();
    expect(state.kind).toBe('error');
    if (state.kind === 'error') {
      expect(state.messageKey).toBe('account.register.errorConflict');
    }
  });

  it('400 Bad Request surfaces the validation error key', () => {
    fixture.detectChanges();
    const component = fixture.componentInstance;
    fillFormWithValidValues(component);

    component.submit();

    const req = http.expectOne('/api/auth/register');
    req.flush({}, { status: 400, statusText: 'Bad Request' });

    const state = component.state();
    expect(state.kind).toBe('error');
    if (state.kind === 'error') {
      expect(state.messageKey).toBe('account.register.errorValidation');
    }
  });

  it('5xx surfaces the generic error key', () => {
    fixture.detectChanges();
    const component = fixture.componentInstance;
    fillFormWithValidValues(component);

    component.submit();

    const req = http.expectOne('/api/auth/register');
    req.flush({}, { status: 500, statusText: 'Server Error' });

    const state = component.state();
    expect(state.kind).toBe('error');
    if (state.kind === 'error') {
      expect(state.messageKey).toBe('account.register.errorGeneric');
    }
  });
});
