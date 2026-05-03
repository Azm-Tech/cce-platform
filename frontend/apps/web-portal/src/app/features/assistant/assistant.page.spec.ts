import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { TranslateModule } from '@ngx-translate/core';
import { LocaleService } from '@frontend/i18n';
import { AssistantApiService } from './assistant-api.service';
import { AssistantPage } from './assistant.page';
import { AssistantStore } from './thread/assistant-store.service';

describe('AssistantPage', () => {
  function setUp(queryParams: Record<string, string> = {}) {
    TestBed.configureTestingModule({
      imports: [AssistantPage, TranslateModule.forRoot()],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: AssistantApiService, useValue: { query: jest.fn() } },
        { provide: LocaleService, useValue: { locale: signal<'ar' | 'en'>('en').asReadonly() } },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { queryParamMap: convertToParamMap(queryParams) },
          },
        },
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

  it('?q= deep-link auto-sends when thread is empty', () => {
    const fixture = setUp({ q: 'What is CCE?' });
    const store = fixture.debugElement.injector.get(AssistantStore);
    const sendSpy = jest.spyOn(store, 'sendMessage').mockResolvedValue();
    const router = fixture.debugElement.injector.get(Router);
    const navSpy = jest.spyOn(router, 'navigate').mockResolvedValue(true);
    fixture.detectChanges();
    expect(sendSpy).toHaveBeenCalledWith('What is CCE?');
    expect(navSpy).toHaveBeenCalled();
    // Verify q is being stripped from URL
    expect(navSpy.mock.calls[0][1]?.queryParams).toEqual({ q: null });
  });

  it('?q= is ignored when thread already has messages', () => {
    const fixture = setUp({ q: 'auto' });
    const store = fixture.debugElement.injector.get(AssistantStore);
    store.messages.set([{
      id: 'a', role: 'user', content: 'existing', citations: [],
      status: 'complete', createdAt: '2026-05-02T00:00:00Z',
    }]);
    const sendSpy = jest.spyOn(store, 'sendMessage').mockResolvedValue();
    fixture.detectChanges();
    expect(sendSpy).not.toHaveBeenCalled();
  });

  it('clear() opens confirm dialog when thread is non-empty', async () => {
    const fixture = setUp();
    fixture.detectChanges();
    const store = fixture.debugElement.injector.get(AssistantStore);
    store.messages.set([{
      id: 'a', role: 'user', content: 'q', citations: [],
      status: 'complete', createdAt: '2026-05-02T00:00:00Z',
    }]);
    const dialog = TestBed.inject(MatDialog);
    const openSpy = jest.spyOn(dialog, 'open').mockReturnValue({
      afterClosed: () => ({
        subscribe(observer: { next: (v: boolean) => void; complete: () => void }) {
          observer.next(true);
          observer.complete();
          return { unsubscribe: () => undefined };
        },
      }),
    } as unknown as ReturnType<MatDialog['open']>);
    await fixture.componentInstance.clear();
    expect(openSpy).toHaveBeenCalled();
    expect(store.messages()).toEqual([]);
  });

  it('clear() with cancel does not wipe the thread', async () => {
    const fixture = setUp();
    fixture.detectChanges();
    const store = fixture.debugElement.injector.get(AssistantStore);
    store.messages.set([{
      id: 'a', role: 'user', content: 'q', citations: [],
      status: 'complete', createdAt: '2026-05-02T00:00:00Z',
    }]);
    const dialog = TestBed.inject(MatDialog);
    jest.spyOn(dialog, 'open').mockReturnValue({
      afterClosed: () => ({
        subscribe(observer: { next: (v: boolean) => void; complete: () => void }) {
          observer.next(false);
          observer.complete();
          return { unsubscribe: () => undefined };
        },
      }),
    } as unknown as ReturnType<MatDialog['open']>);
    await fixture.componentInstance.clear();
    expect(store.messages()).toHaveLength(1);
  });
});
