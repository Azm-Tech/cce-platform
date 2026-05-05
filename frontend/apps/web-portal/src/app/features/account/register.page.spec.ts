import { ComponentFixture, TestBed } from '@angular/core/testing';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth.service';
import { RegisterPage } from './register.page';

describe('RegisterPage', () => {
  let fixture: ComponentFixture<RegisterPage>;
  let isAuthenticatedSig: ReturnType<typeof signal<boolean>>;
  let signInMock: jest.Mock;
  let http: HttpTestingController;

  function fillFormWithValidValues(component: RegisterPage): void {
    component.model = {
      givenName: 'Test',
      surname: 'User',
      email: 'test.user@example.com',
      mailNickname: 'test.user',
    };
  }

  beforeEach(async () => {
    isAuthenticatedSig = signal(false);
    signInMock = jest.fn();

    await TestBed.configureTestingModule({
      imports: [RegisterPage, TranslateModule.forRoot()],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: AuthService,
          useValue: {
            isAuthenticated: isAuthenticatedSig.asReadonly(),
            signIn: signInMock,
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

  it('Sign-in-existing CTA calls auth.signIn(/me/profile)', () => {
    fixture.detectChanges();
    const signInExistingBtn = Array.from(
      fixture.nativeElement.querySelectorAll('button[mat-button]') as NodeListOf<HTMLButtonElement>,
    ).find((b: HTMLButtonElement) =>
      (b.textContent ?? '').includes('signInExistingButton'),
    );
    expect(signInExistingBtn).toBeDefined();
    signInExistingBtn!.click();
    expect(signInMock).toHaveBeenCalledWith('/me/profile');
  });

  it('when authenticated, hides the form + renders an "Open profile" link', () => {
    isAuthenticatedSig.set(true);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('form')).toBeNull();
    const link = fixture.nativeElement.querySelector('a[mat-flat-button]') as HTMLAnchorElement;
    expect(link).not.toBeNull();
    expect(link.getAttribute('href')).toBe('/me/profile');
  });

  it('happy path: submitting valid form POSTs /api/users/register and shows success', async () => {
    fixture.detectChanges();
    const component = fixture.componentInstance;
    fillFormWithValidValues(component);
    fixture.detectChanges();

    const form = fixture.nativeElement.querySelector('form') as HTMLFormElement;
    expect(form).not.toBeNull();

    component.submit({ invalid: false, valid: true } as never);

    const req = http.expectOne('/api/users/register');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      givenName: 'Test',
      surname: 'User',
      email: 'test.user@example.com',
      mailNickname: 'test.user',
    });
    req.flush({
      entraIdObjectId: '11111111-1111-1111-1111-111111111111',
      userPrincipalName: 'test.user@example.com',
      displayName: 'Test User',
    });

    expect(component.state().kind).toBe('success');
  });

  it('409 Conflict surfaces the conflict error key', () => {
    fixture.detectChanges();
    const component = fixture.componentInstance;
    fillFormWithValidValues(component);
    fixture.detectChanges();

    component.submit({ invalid: false, valid: true } as never);

    const req = http.expectOne('/api/users/register');
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
    fixture.detectChanges();

    component.submit({ invalid: false, valid: true } as never);

    const req = http.expectOne('/api/users/register');
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
    fixture.detectChanges();

    component.submit({ invalid: false, valid: true } as never);

    const req = http.expectOne('/api/users/register');
    req.flush({}, { status: 500, statusText: 'Server Error' });

    const state = component.state();
    expect(state.kind).toBe('error');
    if (state.kind === 'error') {
      expect(state.messageKey).toBe('account.register.errorGeneric');
    }
  });
});
