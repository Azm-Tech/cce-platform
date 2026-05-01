# ADR-0039 — BFF cookie auth in web-portal, anonymous-first browsing

**Status:** Accepted
**Date:** 2026-05-01
**Deciders:** CCE frontend team

---

## Context

Sub-project 6 ships the public External Web Portal at `apps/web-portal`. Most of the surface — Knowledge Center, News, Events, Country profiles, Search, Community browse, Maps/City/Assistant skeletons — is **publicly readable**. A subset (account, expert request, community write, follows, notifications) requires authentication.

Sub-5 (admin-cms) used `angular-auth-oidc-client` with PKCE because every admin route is gated. That model doesn't fit web-portal:

| Constraint | Implication |
|---|---|
| Anonymous-first browsing must work without an OIDC handshake | OIDC client library would still bootstrap on every page load |
| BFF (External API) already issues HttpOnly session cookies | SPA-side token handling is redundant + leaks tokens to JS |
| Mobile browsers + privacy-blockers are common | Adding 80kb of OIDC machinery to anonymous flows is wasteful |
| Sub-5 already proved BFF cookies via `cce-admin-cms-bff` (ADR-0031) | The contract is stable |

We considered three options:

| Option | Pros | Cons |
|---|---|---|
| **Reuse `angular-auth-oidc-client`** | Familiar from Sub-5 | Adds 80kb to anonymous bundle; bootstraps on every load; the SPA must handle tokens |
| **Custom MSAL-style integration** | Fine-grained control | New dependency, untested in CCE |
| **BFF cookie auth, full-page redirect for sign-in** | Zero SPA-side auth library; HttpOnly cookies; anonymous browsing has no auth cost | New pattern in CCE frontend; full-page redirect on sign-in instead of SPA-side popup |

---

## Decision

**Use BFF cookie auth (per ADR-0031) with full-page redirect for sign-in. No `angular-auth-oidc-client` dependency in `apps/web-portal`.**

Concretely:

- `AuthService` (in `core/auth/auth.service.ts`) calls `GET /api/me` to bootstrap the current user; tolerates `401` silently for anonymous users.
- `signIn(returnUrl)` calls `window.location.assign('/auth/login?returnUrl=...')` — a full-page redirect to the BFF, which handles the Keycloak round-trip and lands the user back at `/auth/callback?...`.
- `signOut()` POSTs `/auth/logout` (clears the cookie server-side) then `window.location.assign('/')`.
- Routes that require auth use a functional `authGuard` (CanActivateFn) which calls `auth.signIn(state.url)` on miss.

## Consequences

**Positive:**
- Anonymous users pay zero auth library overhead.
- Tokens never reach the browser JS heap; HttpOnly cookies cannot be exfiltrated by XSS.
- Sign-in is a single full-page redirect — same as every other portal in the ministry.
- BFF cookie pattern is already proven in admin-cms (ADR-0031).

**Negative:**
- Sign-in requires a full-page reload. Users coming back from the IdP land at the original return URL but the SPA fully re-bootstraps.
- The `/auth/callback` route on the SPA is a no-op — it exists only so the SPA mounts after the redirect; the cookie is set by the BFF before the browser hits the URL.
- The `authGuard` cold-start case (cookie valid but `/api/me` not yet called) needs a one-time `auth.refresh()` await — addressed in Phase 6.7.

**Neutral:**
- Sub-5 (admin-cms) keeps its `angular-auth-oidc-client` setup. The two apps coexist; this ADR applies only to web-portal.
