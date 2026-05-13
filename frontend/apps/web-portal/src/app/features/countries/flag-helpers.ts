/**
 * Flag URL + emoji helpers.
 *
 * The backend seeds country records with placeholder flag URLs like
 * `https://flags.example.com/sa.svg` — that domain doesn't exist, so
 * every <img> request fails. We detect the placeholder and substitute
 * a real CDN URL keyed off the ISO alpha-2 code; if even that fails
 * we fall back to a Unicode flag emoji.
 */

import type { Country } from './country.types';

/**
 * Public flag CDN. flagcdn.com serves SVG flags by lowercase ISO 3166-1
 * alpha-2 codes (e.g. `sa`, `ae`, `gb`). Free, no API key, fast.
 * Reference: https://flagcdn.com/
 */
const FLAG_CDN_BASE = 'https://flagcdn.com';

/** Domains we know to be placeholders that won't resolve. */
const PLACEHOLDER_DOMAINS = ['flags.example.com', 'example.com', 'localhost'];

export function flagUrlFor(country: Pick<Country, 'flagUrl' | 'isoAlpha2'>): string {
  const cc = (country.isoAlpha2 || '').toLowerCase();
  const url = country.flagUrl ?? '';

  // If the seeded URL is a known placeholder OR empty, build a real one.
  if (!url || isPlaceholder(url)) {
    return cc ? `${FLAG_CDN_BASE}/${cc}.svg` : '';
  }
  return url;
}

function isPlaceholder(url: string): boolean {
  try {
    const host = new URL(url).hostname;
    return PLACEHOLDER_DOMAINS.some((placeholder) => host === placeholder || host.endsWith(`.${placeholder}`));
  } catch {
    return true; // unparseable URL → treat as placeholder
  }
}

/**
 * Convert a 2-letter country code to its Unicode flag emoji using
 * regional-indicator characters. Falls back to a generic flag if the
 * code isn't 2 letters.
 */
export function flagEmojiFor(isoAlpha2: string): string {
  const cc = (isoAlpha2 || '').toUpperCase();
  if (cc.length !== 2 || !/^[A-Z]{2}$/.test(cc)) return '🏳️';
  const A = 0x1f1e6; // regional indicator A
  return String.fromCodePoint(
    A + (cc.charCodeAt(0) - 'A'.charCodeAt(0)),
    A + (cc.charCodeAt(1) - 'A'.charCodeAt(0)),
  );
}
