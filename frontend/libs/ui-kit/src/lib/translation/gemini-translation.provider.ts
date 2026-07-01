import { HttpBackend, HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { TranslationConfigService } from './translation-config.service';
import type {
  TranslateFormat,
  TranslateOptions,
  TranslateResult,
  TranslationProvider,
} from './translation.contracts';

interface GeminiResponse {
  candidates?: { content?: { parts?: { text?: string }[] } }[];
}

/** Model used when the config file omits one. */
const GEMINI_DEFAULT_MODEL = 'gemini-2.5-flash';

/**
 * {@link TranslationProvider} backed by Google AI Studio (Gemini), called
 * directly from the browser. Swappable: bind a different `TRANSLATION_PROVIDER`
 * to replace it without changing any consumer. Uses an `HttpBackend`-built
 * client so the cross-origin call carries no CCE bearer token or `/api` handling.
 */
@Injectable({ providedIn: 'root' })
export class GeminiTranslationProvider implements TranslationProvider {
  private readonly http = new HttpClient(inject(HttpBackend));
  private readonly config = inject(TranslationConfigService);

  async translate(text: string, opts: TranslateOptions = {}): Promise<TranslateResult> {
    const format = opts.format ?? 'text';
    const cfg = await this.config.load();
    if (!cfg) return { ok: false, kind: 'translateNotConfigured' };

    const model = cfg.model || GEMINI_DEFAULT_MODEL;
    const url =
      `https://generativelanguage.googleapis.com/v1beta/models/${model}` +
      `:generateContent?key=${encodeURIComponent(cfg.apiKey)}`;
    try {
      const res = await firstValueFrom(
        this.http.post<GeminiResponse>(url, {
          contents: [{ parts: [{ text: this.buildPrompt(text, format) }] }],
          generationConfig: { temperature: 0.2 },
        }),
      );
      const out = res?.candidates?.[0]?.content?.parts?.[0]?.text;
      if (!out) return { ok: false, kind: 'translateFailed' };
      return { ok: true, text: this.clean(out) };
    } catch {
      return { ok: false, kind: 'translateFailed' };
    }
  }

  private buildPrompt(text: string, format: TranslateFormat): string {
    if (format === 'html') {
      return (
        'Translate the Arabic text in the following HTML into English. Preserve ALL HTML tags, ' +
        'attributes and structure exactly; translate only the human-readable text nodes; do not ' +
        'add or remove markup or wrap the output in code fences. Return only the resulting HTML.' +
        `\n\n${text}`
      );
    }
    return (
      'Translate the following Arabic text into English. Return ONLY the translation — no quotes, ' +
      `no notes.\n\n${text}`
    );
  }

  /** Strip a ```html / ``` code fence the model sometimes wraps output in. */
  private clean(out: string): string {
    const s = out.trim();
    const fence = /^```(?:html|json|text)?\s*([\s\S]*?)\s*```$/i.exec(s);
    return (fence ? fence[1] : s).trim();
  }
}
