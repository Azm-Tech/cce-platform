import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { LocaleService } from '@frontend/i18n';
import { AssistantApiService } from '../assistant-api.service';
import { newMessage, type ThreadMessage } from '../assistant.types';
import { AssistantStore } from './assistant-store.service';
import { MessageListComponent } from './message-list.component';

describe('MessageListComponent', () => {
  let fixture: ComponentFixture<MessageListComponent>;
  let store: AssistantStore;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MessageListComponent, TranslateModule.forRoot()],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        AssistantStore,
        { provide: AssistantApiService, useValue: { query: jest.fn() } },
        { provide: LocaleService, useValue: { locale: signal<'ar' | 'en'>('en').asReadonly() } },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(MessageListComponent);
    store = fixture.debugElement.injector.get(AssistantStore);
  });

  it('renders the empty state when no messages', () => {
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('assistant.empty');
  });

  it('renders one MessageBubble per message', () => {
    const a: ThreadMessage = newMessage('user', 'hi');
    const b: ThreadMessage = newMessage('assistant', 'hello');
    b.status = 'complete';
    store.messages.set([a, b]);
    fixture.detectChanges();
    const bubbles = fixture.nativeElement.querySelectorAll('cce-message-bubble');
    expect(bubbles.length).toBe(2);
  });

  it('shows typing indicator when last message is pending', () => {
    const a = newMessage('user', 'q');
    const b = newMessage('assistant', '');
    // pending by default for assistant
    store.messages.set([a, b]);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('cce-typing-indicator')).toBeTruthy();
  });

  it('hides typing indicator once streaming starts (text arrived)', () => {
    const a = newMessage('user', 'q');
    const b = newMessage('assistant', 'hi');
    b.status = 'streaming';
    store.messages.set([a, b]);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('cce-typing-indicator')).toBeFalsy();
  });

  it('aria-live="polite" set on the scroll container', () => {
    fixture.detectChanges();
    const container = fixture.nativeElement.querySelector('.cce-message-list');
    expect(container?.getAttribute('aria-live')).toBe('polite');
  });
});
