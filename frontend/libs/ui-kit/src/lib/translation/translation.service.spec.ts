import { TestBed } from '@angular/core/testing';
import { GeminiTranslationProvider } from './gemini-translation.provider';
import { TRANSLATION_PROVIDER } from './translation.contracts';
import { TranslationService } from './translation.service';

describe('TranslationService (provider-agnostic facade)', () => {
  it('delegates to the bound TRANSLATION_PROVIDER', async () => {
    const translate = jest.fn().mockResolvedValue({ ok: true, text: 'X' });
    TestBed.configureTestingModule({
      providers: [{ provide: TRANSLATION_PROVIDER, useValue: { translate } }],
    });

    const svc = TestBed.inject(TranslationService);
    await expect(svc.translate('y', { format: 'html' })).resolves.toEqual({ ok: true, text: 'X' });
    expect(translate).toHaveBeenCalledWith('y', { format: 'html' });
  });

  it('falls back to the Gemini provider when no token is bound', async () => {
    const translate = jest.fn().mockResolvedValue({ ok: false, kind: 'translateFailed' });
    TestBed.configureTestingModule({
      providers: [{ provide: GeminiTranslationProvider, useValue: { translate } }],
    });

    const svc = TestBed.inject(TranslationService);
    await svc.translate('z');
    expect(translate).toHaveBeenCalledWith('z', undefined);
  });
});
