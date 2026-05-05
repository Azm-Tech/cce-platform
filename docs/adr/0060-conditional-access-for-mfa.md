# ADR-0060 — Conditional Access for MFA enforcement

**Date:** 2026-05-04
**Status:** Accepted
**Decision-makers:** CCE Architecture, Sub-11 brainstorm 2026-05-04

## Context

Pre-Sub-11, MFA was an aspirational policy — Keycloak supported TOTP/WebAuthn flows but CCE never wired them up. Sub-11 brings MFA into scope, and Entra ID offers two enforcement points: **app-side** (CCE checks for an `amr` claim and rejects tokens without `mfa`) and **Entra ID-side** (Conditional Access policy refuses to issue a token unless MFA was satisfied during sign-in).

## Decision

MFA is enforced via **Entra ID Conditional Access** policies, not by the CCE app. The CCE backend stays MFA-agnostic — it does not inspect the `amr` claim and does not refuse tokens that lack `mfa`.

The Conditional Access policy targets the CCE app registration with the rule: "All users → require multi-factor authentication".

## Rationale

- **Conditional Access is the canonical Entra ID surface for MFA.** Microsoft's documentation, support, and tooling all assume CA. App-side MFA enforcement is a niche pattern reserved for legacy IdPs that don't support CA.
- **CA covers all tokens uniformly.** A user signing into CCE's web portal, admin CMS, or any other CCE-app-scoped surface gets the same MFA requirement. App-side enforcement would require wiring the same check into every entry point.
- **CCE stays simple.** No `RequireMfaPolicy`, no `amr` claim inspection, no fallback flow for users on devices that can't do MFA. The CA policy is the single source of truth — operators modify it without redeploying CCE.
- **Operationally proven.** Sub-10c (production infra) already assumes CA for MFA in the IIS/443 layer.

## Consequences

- **Operators MUST configure a CA policy** scoped to the CCE app before users can sign in. Without one, MFA is effectively disabled. `docs/runbooks/entra-id-cutover.md` (Phase 04) includes the step-by-step.
- **The CCE backend has no MFA-related test surface** — testing MFA enforcement happens against a real Entra ID tenant in a manual security review, not in CI.
- **Partner-tenant users** are subject to **their own tenant's** CA policies, not CCE's. CCE has no authority to enforce MFA on partner tenants. This is a multi-tenant trade-off accepted at brainstorm (decision 4).
- **Service accounts** (CCE-internal apps that call the CCE API app-to-app) bypass MFA via the client-credentials flow. The CA policy targets users only, not service principals. This is correct Entra ID semantics.

## Status

Accepted.
