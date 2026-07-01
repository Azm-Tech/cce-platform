import { Injectable, inject } from '@angular/core';
import { GeminiTranslationProvider } from './gemini-translation.provider';
import {
  TRANSLATION_PROVIDER,
  type TranslateOptions,
  type TranslateResult,
  type TranslationProvider,
} from './translation.contracts';

/**
 * Stable, provider-agnostic entry point for translation. **Consumers inject
 * THIS** and call `translate()` — its signature is the public API and does not
 * change when the engine does.
 *
 * The active engine is resolved from {@link TRANSLATION_PROVIDER}; when no app
 * binds that token it falls back to {@link GeminiTranslationProvider}. To switch
 * providers later, bind the token in an app's `providers` (e.g.
 * `{ provide: TRANSLATION_PROVIDER, useExisting: OpenAiTranslationProvider }`)
 * — no changes to this facade or to any consumer.
 */
@Injectable({ providedIn: 'root' })
export class TranslationService implements TranslationProvider {
  private readonly provider: TranslationProvider =
    inject(TRANSLATION_PROVIDER, { optional: true }) ?? inject(GeminiTranslationProvider);

  translate(text: string, opts?: TranslateOptions): Promise<TranslateResult> {
    return this.provider.translate(text, opts);
  }
}
