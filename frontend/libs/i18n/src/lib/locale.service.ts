import { Injectable, signal, type Signal } from '@angular/core';

export const SUPPORTED_LOCALES = ['ar', 'en'] as const;
export type SupportedLocale = (typeof SUPPORTED_LOCALES)[number];

const STORAGE_KEY = 'cce.locale';
const DEFAULT_LOCALE: SupportedLocale = 'ar';

/**
 * Source of truth for the user's locale. Writes `dir="rtl"|"ltr"` and `lang` to
 * `<html>` so CSS `[dir="rtl"]` selectors and screen readers see the right value.
 * Persists to localStorage so the choice survives reload.
 */
@Injectable({ providedIn: 'root' })
export class LocaleService {
  private readonly _locale = signal<SupportedLocale>(this.readPersisted());

  constructor() {
    this.applyToDom(this._locale());
  }

  readonly locale: Signal<SupportedLocale> = this._locale.asReadonly();

  setLocale(next: SupportedLocale): void {
    const safe = this.coerce(next);
    this._locale.set(safe);
    this.applyToDom(safe);
    try {
      localStorage.setItem(STORAGE_KEY, safe);
    } catch {
      // localStorage unavailable (private mode, SSR) — no-op.
    }
  }

  private readPersisted(): SupportedLocale {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      return this.coerce(raw as SupportedLocale | null);
    } catch {
      return DEFAULT_LOCALE;
    }
  }

  private coerce(value: SupportedLocale | null | undefined): SupportedLocale {
    return value && (SUPPORTED_LOCALES as readonly string[]).includes(value) ? value : DEFAULT_LOCALE;
  }

  private applyToDom(locale: SupportedLocale): void {
    if (typeof document === 'undefined') {
      return;
    }
    const html = document.documentElement;
    html.setAttribute('lang', locale);
    html.setAttribute('dir', locale === 'ar' ? 'rtl' : 'ltr');
  }
}
