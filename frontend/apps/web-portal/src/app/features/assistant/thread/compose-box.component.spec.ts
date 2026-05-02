import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { LocaleService } from '@frontend/i18n';
import { AssistantApiService } from '../assistant-api.service';
import { AssistantStore } from './assistant-store.service';
import { ComposeBoxComponent } from './compose-box.component';

describe('ComposeBoxComponent', () => {
  let fixture: ComponentFixture<ComposeBoxComponent>;
  let store: AssistantStore;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ComposeBoxComponent, TranslateModule.forRoot()],
      providers: [
        provideNoopAnimations(),
        AssistantStore,
        { provide: AssistantApiService, useValue: { query: jest.fn() } },
        { provide: LocaleService, useValue: { locale: signal<'ar' | 'en'>('en').asReadonly() } },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(ComposeBoxComponent);
    store = fixture.debugElement.injector.get(AssistantStore);
  });

  it('disables send when input is empty', () => {
    fixture.detectChanges();
    expect(fixture.componentInstance.canSend()).toBe(false);
  });

  it('enables send when input has content', () => {
    fixture.detectChanges();
    fixture.componentInstance.textControl.setValue('Hello');
    fixture.detectChanges();
    expect(fixture.componentInstance.canSend()).toBe(true);
  });

  it('whitespace-only is not sendable', () => {
    fixture.detectChanges();
    fixture.componentInstance.textControl.setValue('   ');
    fixture.detectChanges();
    expect(fixture.componentInstance.canSend()).toBe(false);
  });

  it('send() calls store.sendMessage and clears the input', async () => {
    const spy = jest.spyOn(store, 'sendMessage').mockResolvedValue();
    fixture.detectChanges();
    fixture.componentInstance.textControl.setValue('What is CCE?');
    await fixture.componentInstance.send();
    expect(spy).toHaveBeenCalledWith('What is CCE?');
    expect(fixture.componentInstance.textControl.value).toBe('');
  });

  it('Enter (no Shift) triggers send via onKeydown', async () => {
    const spy = jest.spyOn(store, 'sendMessage').mockResolvedValue();
    fixture.detectChanges();
    fixture.componentInstance.textControl.setValue('Q');
    const event = new KeyboardEvent('keydown', { key: 'Enter' });
    fixture.componentInstance.onKeydown(event);
    expect(spy).toHaveBeenCalledWith('Q');
  });

  it('Shift+Enter does not send', () => {
    const spy = jest.spyOn(store, 'sendMessage');
    fixture.detectChanges();
    fixture.componentInstance.textControl.setValue('Q');
    const event = new KeyboardEvent('keydown', { key: 'Enter', shiftKey: true });
    fixture.componentInstance.onKeydown(event);
    expect(spy).not.toHaveBeenCalled();
  });

  it('cancel button shows when streaming and calls store.cancel', () => {
    store.streaming.set(true);
    fixture.detectChanges();
    const spy = jest.spyOn(store, 'cancel');
    const buttons = Array.from(fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>);
    const cancelBtn = buttons.find((b) => (b.textContent ?? '').includes('assistant.compose.cancel'));
    expect(cancelBtn).toBeTruthy();
    cancelBtn?.click();
    expect(spy).toHaveBeenCalled();
  });

  it('send button replaced by cancel during streaming', () => {
    store.streaming.set(true);
    fixture.detectChanges();
    const buttons = Array.from(fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>);
    expect(buttons.find((b) => (b.textContent ?? '').includes('assistant.compose.send'))).toBeFalsy();
    expect(buttons.find((b) => (b.textContent ?? '').includes('assistant.compose.cancel'))).toBeTruthy();
  });

  it('shows char count when value reaches warn threshold', () => {
    fixture.detectChanges();
    fixture.componentInstance.textControl.setValue('x'.repeat(1500));
    fixture.detectChanges();
    expect(fixture.componentInstance.showCharCount()).toBe(true);
  });
});
