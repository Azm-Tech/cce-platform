/**
 * Tiny helpers for rendering posts / replies in the FB / IG / X-style
 * feed layout used on `/community/posts/:id` and inside topic pages.
 *
 * - `timeAgo` produces locale-aware relative-time strings ("2h ago",
 *   "yesterday", "منذ ٣ أيام") via the platform `Intl.RelativeTimeFormat`.
 * - `authorHandle` turns a raw author guid into a friendly display
 *   handle (`@d70d6a`). Real display names are not exposed by the
 *   public community API today; this is a stable visual placeholder.
 */

const RTF_CACHE = new Map<string, Intl.RelativeTimeFormat>();
function rtf(locale: string): Intl.RelativeTimeFormat {
  const key = locale || 'en';
  let f = RTF_CACHE.get(key);
  if (!f) {
    f = new Intl.RelativeTimeFormat(key, { numeric: 'auto' });
    RTF_CACHE.set(key, f);
  }
  return f;
}

/** Returns a relative-time string for the given ISO timestamp.
 *  Falls back to an empty string if the date is invalid. */
export function timeAgo(iso: string | null | undefined, locale: string): string {
  if (!iso) return '';
  const t = new Date(iso).getTime();
  if (Number.isNaN(t)) return '';
  const diff = (Date.now() - t) / 1000; // seconds since
  const f = rtf(locale);
  // Future timestamps render as the plain "now" / "in N units" — but we
  // typically only hit this for clock skew, so the negative diff branch
  // is acceptable.
  const abs = Math.abs(diff);
  const sign = diff >= 0 ? -1 : 1;
  if (abs < 60) return f.format(sign * Math.floor(abs), 'second');
  if (abs < 3600) return f.format(sign * Math.floor(abs / 60), 'minute');
  if (abs < 86400) return f.format(sign * Math.floor(abs / 3600), 'hour');
  if (abs < 604800) return f.format(sign * Math.floor(abs / 86400), 'day');
  if (abs < 2592000) return f.format(sign * Math.floor(abs / 604800), 'week');
  if (abs < 31536000) return f.format(sign * Math.floor(abs / 2592000), 'month');
  return f.format(sign * Math.floor(abs / 31536000), 'year');
}

/** Stable visual handle from a guid — first 6 hex chars prefixed with @. */
export function authorHandle(authorId: string | null | undefined): string {
  if (!authorId) return '@anon';
  return '@' + authorId.replace(/-/g, '').slice(0, 6);
}

/** Initial used inside the brand-gradient avatar tile (uppercase first
 *  hex char of the id). Keeps the avatar visually stable per author. */
export function authorInitial(authorId: string | null | undefined): string {
  if (!authorId) return '?';
  const ch = authorId.replace(/-/g, '').charAt(0);
  return ch ? ch.toUpperCase() : '?';
}
