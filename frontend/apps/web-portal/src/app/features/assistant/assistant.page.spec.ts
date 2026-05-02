import { TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TranslateModule } from '@ngx-translate/core';
import { AssistantPage } from './assistant.page';

describe('AssistantPage (Phase 00 stub)', () => {
  it('renders title and subtitle from i18n keys', () => {
    TestBed.configureTestingModule({
      imports: [AssistantPage, TranslateModule.forRoot()],
      providers: [provideNoopAnimations()],
    });
    const fixture = TestBed.createComponent(AssistantPage);
    fixture.detectChanges();
    const html = fixture.nativeElement.textContent ?? '';
    expect(html).toContain('assistant.title');
    expect(html).toContain('assistant.subtitle');
  });
});
