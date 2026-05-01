# ADR-0041 — Same-origin scoped HTTP interceptors

**Status:** Accepted
**Date:** 2026-05-01
**Deciders:** CCE frontend team

---

## Context

CCE frontend apps use functional HTTP interceptors for three concerns:

1. **Correlation-ID** — stamps `X-Correlation-Id: <uuid>` on every outbound request so logs across SPA, BFF, and downstream services can be joined.
2. **BFF credentials** — adds `withCredentials: true` so HttpOnly session cookies travel with same-origin requests.
3. **Server-error toast** — surfaces 5xx + 403 errors via the global ToastService.

In Sub-5 (admin-cms), we initially stamped these on **every** outbound request. That broke when the OIDC client tried to fetch the Keycloak discovery document — the cross-origin request still picked up `withCredentials: true` and `X-Correlation-Id`, which CORS rejected. The fix (mid-Sub-5) was a small `isInternalUrl(url)` helper that lets the interceptors pass cross-origin requests through untouched.

Sub-6 inherits this lesson from day 1 and codifies it.

---

## Decision

**All three HTTP interceptors are scoped same-origin from day one. Cross-origin requests pass through without modification.**

Concretely (`core/http/`):

- `correlation-id.interceptor.ts` — only stamps `X-Correlation-Id` on internal URLs.
- `bff-credentials.interceptor.ts` — only sets `withCredentials: true` on internal URLs.
- `server-error.interceptor.ts` — only translates 5xx/403 to toasts on internal URLs (cross-origin errors are surfaced by the per-feature `*ApiService` `Result<T>` mapping).

`isInternalUrl(url)` definition: a URL is internal if either:
- it's a relative path (starts with `/` and not `//`), or
- it's an absolute URL whose origin matches `window.location.origin`.

All three interceptors share this helper from `core/http/is-internal-url.ts`.

## Consequences

**Positive:**
- Cross-origin requests (Keycloak discovery, third-party CDN fetches, embedded analytics) never trip CORS due to our headers.
- The pattern is explicit and easy to audit — every interceptor calls `isInternalUrl(req.url)` at the top.
- Future cross-origin integrations (CDN previews, embedded widgets) won't need interceptor edits.

**Negative:**
- One extra helper to maintain.
- Subtle: relative URLs that resolve to a different origin (e.g., `<base href="https://other.example/">` in index.html) would currently be treated as internal. We don't use `<base href>` in CCE so this is a theoretical risk.

**Neutral:**
- admin-cms (Sub-5) was retroactively patched to use the same pattern after the original CORS bug. Both apps now share the helper shape.
