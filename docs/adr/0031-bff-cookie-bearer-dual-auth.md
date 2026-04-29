# ADR-0031 — BFF Cookie + Bearer Dual-Mode Auth

**Status:** Accepted
**Date:** 2026-04-29
**Deciders:** CCE backend team

---

## Context

ADR-0015 mandates the BFF (Backend-For-Frontend) cookie pattern for the public Web Portal SPA: the SPA receives an encrypted `HttpOnly` cookie that carries an OIDC access token. This prevents JavaScript token theft (XSS).

However, mobile clients (Android/iOS CCE apps) and third-party API integrations cannot participate in the BFF cookie flow. They authenticate via standard OAuth 2.0 bearer tokens.

The External API needs to accept both authentication modes. Early designs required separate middleware branches, doubling the security logic.

---

## Decision

Use **dual-mode auth**: a single middleware pipeline that handles both methods transparently.

1. `BffSessionMiddleware` runs early in the pipeline.
2. If the incoming request has a valid BFF session cookie:
   - The middleware decrypts the cookie, extracts the embedded access token, and synthesises an `Authorization: Bearer <access>` header on the server-side request object.
3. All downstream middleware (authentication, authorization, handlers) sees only standard Bearer tokens, regardless of whether the original request used a cookie or a header.
4. Mobile clients and direct API callers attach `Authorization: Bearer <token>` normally — `BffSessionMiddleware` is a no-op for them.

The External API's `Program.cs` pipeline order (confirmed in ADR-0015's middleware order spec §7.1):

```
CorrelationId → Exception → SecurityHeaders → RateLimit → BFF → OutputCache → Auth → Authz → UserSync → Locale
```

---

## Consequences

- Simpler downstream code: every handler, policy, and claim transformer works the same way regardless of client type.
- One auth pipeline to reason about and test.
- BFF cookie and Bearer token are both valid simultaneously — a power user with both (e.g., dev tooling) gets the cookie path served by the BFF middleware.
- Cookie rotation and sliding-session refresh remain the BFF's responsibility; backend handlers are unaffected.
- Mobile OIDC flow (PKCE without cookie) is fully supported without additional backend changes.
