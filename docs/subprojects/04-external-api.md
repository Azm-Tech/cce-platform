# Sub-project 04: External API

## Goal

Implement the public-facing REST API (`CCE.Api.External`): published content, search, knowledge maps, interactive city, smart assistant, community endpoints, registration, profile, notifications. Includes the **BFF cookie wiring** for the public Web Portal SPA, completing the OIDC story from [ADR-0015](../adr/0015-oidc-code-flow-pkce-bff-cookies.md). After this sub-project, the Web Portal sub-project (6) has a stable client and a secure session model.

## BRD references

- §4.1.1–4.1.18 — Public functional requirements.
- §6.2.1–6.2.36 — Public user stories (visitor + registered).
- §6.3.1–6.3.8 — Public-facing forms.
- §6.5 — Integration touchpoints (smart assistant, KAPSARC consumer side).

## Dependencies

- Sub-project 2 (Data & Domain).

## Rough estimate

T-shirt size: **L**.

## DoD skeleton

- [ ] Public endpoints for every §4.1.1–4.1.18 requirement.
- [ ] BFF endpoints (`/auth/login`, `/auth/callback`, `/auth/refresh`, `/auth/logout`) issue `httpOnly` `SameSite=Strict` cookies.
- [ ] Rate limiting on public endpoints (per IP + per session).
- [ ] OpenAPI `external-api.yaml` exported and drift-checked.
- [ ] Smart assistant endpoint (§6.2.6–§6.2.9) integrates with provider per sub-project 8 design.
- [ ] Community endpoints (§6.2.19–§6.2.31) include moderation hooks.
- [ ] Output sanitization on user-submitted content.
- [ ] Integration tests + load tests against public endpoints (k6 thresholds).
- [ ] Sentry wired; PII scrubbing rules verified.

Refined at this sub-project's own brainstorm cycle.

## Related

- ADRs: [0009](../adr/0009-openapi-as-contract-source.md), [0015](../adr/0015-oidc-code-flow-pkce-bff-cookies.md).
