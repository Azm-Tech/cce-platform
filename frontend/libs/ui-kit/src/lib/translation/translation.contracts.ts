import { InjectionToken } from '@angular/core';

/** Whether the source is plain text or rich HTML — lets a provider preserve markup. */
export type TranslateFormat = 'text' | 'html';

/**
 * Result of a translation attempt. `kind` is a provider-agnostic error category
 * that doubles as the `errors.*` i18n key — keep new kinds generic (not tied to
 * any one engine) so messages stay valid across providers.
 */
export type TranslateResult =
  | { ok: true; text: string }
  | { ok: false; kind: 'translateNotConfigured' | 'translateFailed' };

export interface TranslateOptions {
  format?: TranslateFormat;
}

/**
 * Provider-agnostic translation contract. Implement this to back the feature
 * with ANY engine — Gemini, OpenAI, Azure, a backend proxy, or the browser's
 * built-in Translator — without touching consumers. Direction is Arabic → English.
 *
 * Consumers never depend on this directly; they inject {@link TranslationService}
 * (the stable facade). Apps swap the engine by binding {@link TRANSLATION_PROVIDER}.
 */
export interface TranslationProvider {
  translate(text: string, opts?: TranslateOptions): Promise<TranslateResult>;
}

/**
 * DI token an app binds to choose the active translation engine, e.g.
 * `{ provide: TRANSLATION_PROVIDER, useExisting: OpenAiTranslationProvider }`.
 * When unbound, {@link TranslationService} falls back to the Gemini provider.
 */
export const TRANSLATION_PROVIDER = new InjectionToken<TranslationProvider>('TRANSLATION_PROVIDER');
