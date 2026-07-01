import { HttpBackend, HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

/**
 * Runtime config loaded from a gitignored asset file. Intentionally generic
 * (`apiKey` + optional `model`) so most cloud providers can reuse it; a provider
 * that needs nothing (e.g. the browser's built-in Translator) can ignore it.
 */
export interface TranslationConfig {
  apiKey: string;
  model: string | null;
}

const CONFIG_URL = 'assets/translation-config.json';

/**
 * Loads `assets/translation-config.json` once (cached) so the secret never
 * enters source control. A missing/empty key resolves to `null` ("not
 * configured"). Uses an `HttpBackend`-built client so no app interceptor
 * (auth token, locale, `/api` envelope) touches the request.
 */
@Injectable({ providedIn: 'root' })
export class TranslationConfigService {
  private readonly http = new HttpClient(inject(HttpBackend));
  private cached?: Promise<TranslationConfig | null>;

  load(): Promise<TranslationConfig | null> {
    this.cached ??= firstValueFrom(this.http.get<Partial<TranslationConfig>>(CONFIG_URL))
      .then((c) => {
        const apiKey = c?.apiKey?.trim();
        if (!apiKey) return null;
        return { apiKey, model: c?.model?.trim() || null };
      })
      .catch(() => null);
    return this.cached;
  }
}
