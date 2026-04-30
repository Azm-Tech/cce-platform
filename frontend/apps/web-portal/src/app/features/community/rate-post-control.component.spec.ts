import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { ToastService } from '@frontend/ui-kit';
import { AuthService } from '../../core/auth/auth.service';
import { CommunityApiService, type Result } from './community-api.service';
import { RatePostControlComponent } from './rate-post-control.component';
import { MarkAnswerButtonComponent } from './mark-answer-button.component';

function ok<T>(value: T): Result<T> {
  return { ok: true, value };
}

describe('RatePostControlComponent', () => {
  let fixture: ComponentFixture<RatePostControlComponent>;
  let component: RatePostControlComponent;
  let ratePost: jest.Mock;
  let toastSuccess: jest.Mock;
  let isAuthenticatedSig: ReturnType<typeof signal<boolean>>;

  beforeEach(async () => {
    ratePost = jest.fn().mockResolvedValue(ok(undefined));
    toastSuccess = jest.fn();
    isAuthenticatedSig = signal<boolean>(true);

    await TestBed.configureTestingModule({
      imports: [RatePostControlComponent, TranslateModule.forRoot()],
      providers: [
        provideNoopAnimations(),
        { provide: CommunityApiService, useValue: { ratePost } },
        { provide: AuthService, useValue: { isAuthenticated: isAuthenticatedSig.asReadonly(), signIn: jest.fn() } },
        { provide: ToastService, useValue: { success: toastSuccess, error: jest.fn() } },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(RatePostControlComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('postId', 'p1');
    fixture.detectChanges();
  });

  it('5-star click calls ratePost(postId, 5) and toasts success', async () => {
    await component.setRating(5);
    expect(ratePost).toHaveBeenCalledWith('p1', 5);
    expect(toastSuccess).toHaveBeenCalledWith('community.rate.toast');
    expect(component.displayedRating()).toBe(5);
  });

  it('anonymous user renders SignInCta instead of stars', async () => {
    isAuthenticatedSig.set(false);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('cce-sign-in-cta')).not.toBeNull();
    expect(fixture.nativeElement.querySelector('.cce-rate-post')).toBeNull();
  });

  it('Enter key on a star fires the rate call', async () => {
    const event = new KeyboardEvent('keydown', { key: 'Enter' });
    await component.onKey(4, event);
    expect(ratePost).toHaveBeenCalledWith('p1', 4);
  });
});

describe('MarkAnswerButtonComponent', () => {
  let fixture: ComponentFixture<MarkAnswerButtonComponent>;
  let component: MarkAnswerButtonComponent;
  let markAnswer: jest.Mock;
  let toastSuccess: jest.Mock;
  let emitted: number;

  beforeEach(async () => {
    markAnswer = jest.fn().mockResolvedValue(ok(undefined));
    toastSuccess = jest.fn();
    emitted = 0;

    await TestBed.configureTestingModule({
      imports: [MarkAnswerButtonComponent, TranslateModule.forRoot()],
      providers: [
        provideNoopAnimations(),
        { provide: CommunityApiService, useValue: { markAnswer } },
        { provide: ToastService, useValue: { success: toastSuccess, error: jest.fn() } },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(MarkAnswerButtonComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('postId', 'p1');
    fixture.componentRef.setInput('replyId', 'r1');
    component.marked.subscribe(() => { emitted++; });
    fixture.detectChanges();
  });

  it('click calls markAnswer(postId, replyId) and emits marked', async () => {
    await component.onClick();
    expect(markAnswer).toHaveBeenCalledWith('p1', 'r1');
    expect(toastSuccess).toHaveBeenCalledWith('community.markAnswer.toast');
    expect(emitted).toBe(1);
  });

  it('disabled=true makes click a no-op', async () => {
    fixture.componentRef.setInput('disabled', true);
    fixture.detectChanges();
    await component.onClick();
    expect(markAnswer).not.toHaveBeenCalled();
    expect(emitted).toBe(0);
  });
});
