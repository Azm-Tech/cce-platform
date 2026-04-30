/**
 * Returns true when the URL targets the CCE backend (relative path or
 * absolute URL whose origin matches the SPA). Cross-origin URLs (third-party
 * APIs, Sentry, etc.) MUST NOT receive same-origin headers/credentials —
 * those services do not declare them in `Access-Control-Allow-Headers`,
 * so the browser preflight rejects the request before it fires.
 */
export function isInternalUrl(url: string): boolean {
  if (url.startsWith('/')) return true;
  try {
    return new URL(url).origin === window.location.origin;
  } catch {
    return false;
  }
}
