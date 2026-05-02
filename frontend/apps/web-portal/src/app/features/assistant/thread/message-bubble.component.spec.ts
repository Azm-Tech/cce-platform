import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TranslateModule } from '@ngx-translate/core';
import { newMessage, type ThreadMessage } from '../assistant.types';
import { MessageBubbleComponent } from './message-bubble.component';

function buildAssistantMessage(content: string, status: ThreadMessage['status']): ThreadMessage {
  const m = newMessage('assistant', content);
  m.status = status;
  return m;
}

describe('MessageBubbleComponent', () => {
  let fixture: ComponentFixture<MessageBubbleComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MessageBubbleComponent, TranslateModule.forRoot()],
      providers: [provideRouter([]), provideNoopAnimations()],
    }).compileComponents();
    fixture = TestBed.createComponent(MessageBubbleComponent);
  });

  it('renders user message content with user role class', () => {
    fixture.componentRef.setInput('message', newMessage('user', 'Hello there'));
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Hello there');
    expect(fixture.nativeElement.querySelector('.cce-bubble--user')).toBeTruthy();
  });

  it('renders assistant message with assistant role class', () => {
    fixture.componentRef.setInput('message', buildAssistantMessage('Hi back', 'complete'));
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.cce-bubble--assistant')).toBeTruthy();
  });

  it('shows blinking cursor while streaming', () => {
    fixture.componentRef.setInput('message', buildAssistantMessage('partial', 'streaming'));
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.cce-bubble__cursor')).toBeTruthy();
  });

  it('omits cursor on complete', () => {
    fixture.componentRef.setInput('message', buildAssistantMessage('done', 'complete'));
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.cce-bubble__cursor')).toBeFalsy();
  });

  it('shows error block + retry button on error when isLast', () => {
    const m = buildAssistantMessage('partial', 'error');
    m.errorKind = 'server';
    fixture.componentRef.setInput('message', m);
    fixture.componentRef.setInput('isLast', true);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('assistant.errors.server');
    expect(fixture.componentInstance.showRetry()).toBe(true);
  });

  it('hides retry when not last', () => {
    const m = buildAssistantMessage('partial', 'error');
    fixture.componentRef.setInput('message', m);
    fixture.componentRef.setInput('isLast', false);
    fixture.detectChanges();
    expect(fixture.componentInstance.showRetry()).toBe(false);
  });

  it('shows regenerate on the last completed assistant message', () => {
    fixture.componentRef.setInput('message', buildAssistantMessage('done', 'complete'));
    fixture.componentRef.setInput('isLast', true);
    fixture.detectChanges();
    expect(fixture.componentInstance.showRegenerate()).toBe(true);
  });

  it('emits retry event when retry button clicked', () => {
    const m = buildAssistantMessage('partial', 'error');
    fixture.componentRef.setInput('message', m);
    fixture.componentRef.setInput('isLast', true);
    fixture.detectChanges();
    const spy = jest.fn();
    fixture.componentInstance.retry.subscribe(spy);
    const buttons = fixture.nativeElement.querySelectorAll('button');
    const retryBtn = Array.from(buttons as NodeListOf<HTMLButtonElement>).find(
      (b) => (b.textContent ?? '').includes('assistant.message.retry'),
    );
    expect(retryBtn).toBeTruthy();
    retryBtn?.click();
    expect(spy).toHaveBeenCalled();
  });

  it('renders citation chips when message has citations', () => {
    const m = buildAssistantMessage('see source', 'complete');
    m.citations = [{
      id: 'r1', kind: 'resource', title: 'A', href: '/x',
    }];
    fixture.componentRef.setInput('message', m);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('cce-citation-chip').length).toBe(1);
  });
});
