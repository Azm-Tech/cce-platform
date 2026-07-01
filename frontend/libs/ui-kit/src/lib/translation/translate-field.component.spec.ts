import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { LocaleService } from '@frontend/i18n';
import { ConfirmDialogService } from '../feedback/confirm-dialog.service';
import { ToastService } from '../feedback/toast.service';
import { TranslateFieldComponent } from './translate-field.component';
import { TranslationService } from './translation.service';
import type { TranslateResult } from './translation.contracts';

describe('TranslateFieldComponent', () => {
  let fixture: ComponentFixture<TranslateFieldComponent>;
  let component: TranslateFieldComponent;
  const locale = signal<'ar' | 'en'>('ar');
  let translate: jest.Mock<Promise<TranslateResult>, [string, { format?: string }?]>;
  let confirm: jest.Mock<Promise<boolean>, [unknown]>;
  let toastError: jest.Mock;

  beforeEach(async () => {
    locale.set('ar');
    translate = jest.fn();
    confirm = jest.fn();
    toastError = jest.fn();
    await TestBed.configureTestingModule({
      imports: [
        TranslateFieldComponent,
        TranslocoTestingModule.forRoot({
          langs: { en: {}, ar: {} },
          translocoConfig: { availableLangs: ['en', 'ar'], defaultLang: 'ar' },
        }),
      ],
      providers: [
        { provide: LocaleService, useValue: { locale } },
        { provide: TranslationService, useValue: { translate } },
        { provide: ConfirmDialogService, useValue: { confirm } },
        { provide: ToastService, useValue: { error: toastError } },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(TranslateFieldComponent);
    component = fixture.componentInstance;
  });

  function button(): HTMLButtonElement | null {
    return fixture.nativeElement.querySelector('.cce-translate-field__btn');
  }

  it('is shown but disabled when the source is empty', () => {
    fixture.componentRef.setInput('source', '   ');
    fixture.detectChanges();
    expect(button()).not.toBeNull();
    expect(button()?.disabled).toBe(true);
  });

  it('is hidden when the UI locale is English', () => {
    locale.set('en');
    fixture.componentRef.setInput('source', 'مرحبا');
    fixture.detectChanges();
    expect(button()).toBeNull();
  });

  it('is enabled in Arabic UI with a non-empty source', () => {
    fixture.componentRef.setInput('source', 'مرحبا');
    fixture.detectChanges();
    expect(button()).not.toBeNull();
    expect(button()?.disabled).toBe(false);
  });

  it('translates and emits on success (no confirm when target empty)', async () => {
    translate.mockResolvedValueOnce({ ok: true, text: 'Hello' });
    fixture.componentRef.setInput('source', 'مرحبا');
    fixture.componentRef.setInput('targetHasContent', false);
    fixture.detectChanges();

    let emitted: string | undefined;
    component.translated.subscribe((v) => (emitted = v));
    await component.run();

    expect(confirm).not.toHaveBeenCalled();
    expect(translate).toHaveBeenCalledWith('مرحبا', { format: 'text' });
    expect(emitted).toBe('Hello');
  });

  it('confirms before overwriting a non-empty target and aborts if declined', async () => {
    confirm.mockResolvedValueOnce(false);
    fixture.componentRef.setInput('source', 'مرحبا');
    fixture.componentRef.setInput('targetHasContent', true);
    fixture.detectChanges();

    await component.run();
    expect(confirm).toHaveBeenCalled();
    expect(translate).not.toHaveBeenCalled();
  });

  it('shows an error toast when translation fails', async () => {
    translate.mockResolvedValueOnce({ ok: false, kind: 'translateFailed' });
    fixture.componentRef.setInput('source', 'مرحبا');
    fixture.detectChanges();

    await component.run();
    expect(toastError).toHaveBeenCalledWith('errors.translateFailed');
  });
});
