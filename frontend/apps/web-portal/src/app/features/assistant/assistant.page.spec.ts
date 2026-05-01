import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { LocaleService } from '@frontend/i18n';
import { TranslateModule } from '@ngx-translate/core';
import { AssistantApiService, type Result, type AssistantReply } from './assistant-api.service';
import { AssistantPage } from './assistant.page';

function ok<T>(value: T): Result<T> {
  return { ok: true, value };
}

describe('AssistantPage', () => {
  let fixture: ComponentFixture<AssistantPage>;
  let page: AssistantPage;
  let query: jest.Mock;

  beforeEach(async () => {
    query = jest.fn().mockResolvedValue(ok<AssistantReply>({ reply: 'CCE is a platform.' }));

    await TestBed.configureTestingModule({
      imports: [AssistantPage, TranslateModule.forRoot()],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: AssistantApiService, useValue: { query } },
        { provide: LocaleService, useValue: { locale: signal<'ar' | 'en'>('en').asReadonly() } },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(AssistantPage);
    page = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('valid send calls query with the typed payload + renders reply', async () => {
    page.question.setValue('What is CCE?');
    await page.send();
    expect(query).toHaveBeenCalledWith({ question: 'What is CCE?', locale: 'en' });
    expect(page.reply()).toBe('CCE is a platform.');
  });

  it('empty input blocks send (no API call)', async () => {
    page.question.setValue('');
    await page.send();
    expect(query).not.toHaveBeenCalled();
  });

  it('error path sets errorKind, reply stays null', async () => {
    query.mockResolvedValueOnce({ ok: false, error: { kind: 'server' } });
    page.question.setValue('q');
    await page.send();
    expect(page.errorKind()).toBe('server');
    expect(page.reply()).toBeNull();
  });

  it('renders the "coming in Sub-7" notice', () => {
    const html = fixture.nativeElement.textContent ?? '';
    expect(html).toContain('assistant.comingSoon');
  });
});
