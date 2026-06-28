/**
 * Locale-aware date formatter producing "4 ديسمبر 2022" / "December 4, 2022".
 *
 * Uses Intl directly because Angular's `ar` locale data isn't registered
 * in this app (DatePipe with 'ar' would throw "Missing locale data").
 * The `nu-latn` + `ca-gregory` extensions force Western digits and the
 * Gregorian calendar — plain `ar` renders Hijri dates with Arabic-Indic
 * numerals, which is not what the product wants.
 */
export function formatLocaleDate(
  iso: string | null | undefined,
  locale: 'ar' | 'en',
  opts: Intl.DateTimeFormatOptions = { day: 'numeric', month: 'long', year: 'numeric' },
): string {
  if (!iso) return '—';
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return '—';
  const tag = locale === 'ar' ? 'ar-u-nu-latn-ca-gregory' : 'en-US';
  return new Intl.DateTimeFormat(tag, opts).format(d);
}
