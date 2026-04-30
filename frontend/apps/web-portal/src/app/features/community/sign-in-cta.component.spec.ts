import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth.service';
import { SignInCtaComponent } from './sign-in-cta.component';

describe('SignInCtaComponent', () => {
  let fixture: ComponentFixture<SignInCtaComponent>;
  let signIn: jest.Mock;

  beforeEach(async () => {
    signIn = jest.fn();

    await TestBed.configureTestingModule({
      imports: [SignInCtaComponent, TranslateModule.forRoot()],
      providers: [
        provideNoopAnimations(),
        { provide: AuthService, useValue: { signIn } },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(SignInCtaComponent);
  });

  it('"Sign in" button click calls auth.signIn(currentUrl)', () => {
    fixture.detectChanges();
    const btn = fixture.nativeElement.querySelector('button[mat-flat-button]') as HTMLButtonElement;
    expect(btn).not.toBeNull();
    btn.click();
    expect(signIn).toHaveBeenCalled();
    // Argument is window.location.pathname + window.location.search (jsdom default '/').
    const arg = signIn.mock.calls[0][0];
    expect(typeof arg).toBe('string');
    expect(arg.startsWith('/')).toBe(true);
  });

  it('default messageKey renders community.signInToPost', () => {
    fixture.detectChanges();
    const html = fixture.nativeElement.textContent ?? '';
    expect(html).toContain('community.signInToPost');
  });

  it('messageKey input override changes the rendered key', () => {
    fixture.componentRef.setInput('messageKey', 'community.signInToReply');
    fixture.detectChanges();
    const html = fixture.nativeElement.textContent ?? '';
    expect(html).toContain('community.signInToReply');
    expect(html).not.toContain('community.signInToPost');
  });
});
