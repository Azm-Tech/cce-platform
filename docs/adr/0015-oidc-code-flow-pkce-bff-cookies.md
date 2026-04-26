# ADR-0015: OIDC code-flow + PKCE + BFF cookie pattern

- **Status:** Accepted (Foundation gap noted below)
- **Date:** 2026-04-26
- **Sub-project owner:** Foundation
- **Spec ref:** [Foundation §3, §9](../superpowers/specs/2026-04-24-foundation-design.md#3-stack-decisions)

## Context

The two Angular apps are Single Page Applications federated to Keycloak (dev) / ADFS (prod). The choice of OAuth2/OIDC flow and where tokens live materially affects security posture:

- **Implicit flow** — deprecated; tokens in URL fragment, no refresh.
- **Authorization code without PKCE** — vulnerable to code interception in public clients.
- **Authorization code + PKCE** — current best practice for SPAs.
- **Token storage in `localStorage`** — vulnerable to XSS exfiltration.
- **Token storage in httpOnly cookies via a BFF (backend-for-frontend)** — cookies aren't readable by JS; the BFF holds the refresh token and proxies API calls.

## Decision

**Target design:**

- **Flow:** OIDC authorization code with **PKCE**.
- **Token storage:** Refresh tokens in `httpOnly`, `SameSite=Strict`, `Secure` cookies, scoped to the BFF origin. Access tokens injected by the BFF on every backend call. **Never `localStorage`.**
- **BFF pattern:** A lightweight backend per SPA terminates the OIDC dance, holds refresh tokens, and proxies authenticated calls.

**Foundation gap:**

- Foundation ships the Angular apps with `angular-auth-oidc-client` defaults, which keep tokens in **memory** (not localStorage, not BFF cookies). This is acceptable for dev / Foundation scope but does not match the target design.
- The BFF cookie wiring lands in **sub-project 4** (External API) / **5** (Admin CMS), where the BFF endpoints are added and the SPA OIDC config is reconfigured to point at the BFF.

This ADR records the target design so future sub-projects don't drift.

## Consequences

### Positive

- XSS no longer exfiltrates a long-lived refresh token (it's not in JS-accessible storage).
- Single-origin cookies eliminate CORS friction and CSRF risk (with `SameSite=Strict`).
- PKCE removes the public-client interception risk.

### Negative

- BFF is one more deployable per SPA.
- Cross-origin scenarios (subdomains) require careful cookie scope.

### Neutral / follow-ups

- Sub-project 4 / 5 owns the BFF implementation.
- Foundation security gates (Trivy, CodeQL) cover the BFF when it lands.

## Alternatives considered

### Option A: Tokens in `localStorage`

- Rejected: XSS-prone.

### Option B: Implicit flow

- Rejected: deprecated by OAuth 2.1.

### Option C: Code flow without PKCE

- Rejected: not safe for public clients.

## Related

- [ADR-0006](0006-keycloak-as-adfs-stand-in.md)
- [ADR-0011](0011-security-scanning-pipeline.md)
