import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { LocaleService } from '@frontend/i18n';
import { AssistantApiService } from './assistant-api.service';
import { AssistantPage } from './assistant.page';
import { AssistantStore } from './thread/assistant-store.service';

describe('AssistantPage', () => {
  function setUp() {
    TestBed.configureTestingModule({
      imports: [AssistantPage, TranslateModule.forRoot()],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: AssistantApiService, useValue: { query: jest.fn() } },
        { provide: LocaleService, useValue: { locale: signal<'ar' | 'en'>('en').asReadonly() } },
      ],
    });
    return TestBed.createComponent(AssistantPage);
  }

  it('renders title + subtitle from i18n keys', () => {
    const fixture = setUp();
    fixture.detectChanges();
    const html = fixture.nativeElement.textContent ?? '';
    expect(html).toContain('assistant.title');
    expect(html).toContain('assistant.subtitle');
  });

  it('mounts MessageList and ComposeBox', () => {
    const fixture = setUp();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('cce-message-list')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('cce-compose-box')).toBeTruthy();
  });

  it('clear button is disabled when thread is empty', () => {
    const fixture = setUp();
    fixture.detectChanges();
    const clearBtn = Array.from(fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>)
      .find((b) => (b.textContent ?? '').includes('assistant.thread.clear'));
    expect(clearBtn?.disabled).toBe(true);
  });

  it('clear() wipes the thread', () => {
    const fixture = setUp();
    fixture.detectChanges();
    const store = fixture.debugElement.injector.get(AssistantStore);
    store.messages.set([{
      id: 'a', role: 'user', content: 'q', citations: [],
      status: 'complete', createdAt: '2026-05-02T00:00:00Z',
    }]);
    fixture.detectChanges();
    fixture.componentInstance.clear();
    expect(store.messages()).toEqual([]);
  });
});
