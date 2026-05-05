# ADR-0058 — Entra ID multi-tenant with Graph writes

**Date:** 2026-05-04
**Status:** Accepted (supersedes ADR-0055)
**Decision-makers:** CCE Architecture, Sub-11 brainstorm 2026-05-04

## Context

Sub-1 through Sub-9 ran the entire CCE platform on Keycloak as the IdP, synced from on-prem AD via Keycloak's LDAP user-federation provider (see ADR-0055). Sub-11 retires Keycloak and adopts Microsoft Entra ID in its place.

The choice of *which* Entra ID surface — single-tenant, multi-tenant (`AzureADMultipleOrgs`), or B2C (`PersonalMicrosoftAccount`) — drives how partner organizations sign in and whether CCE has authority to create users.

## Decision

CCE uses **multi-tenant Entra ID** (`signInAudience: AzureADMultipleOrgs`) with the CCE backend writing to its own home tenant via **Microsoft Graph SDK** (app-only `User.ReadWrite.All` permission).

- **Tenant model:** multi-tenant. CCE's home tenant is `cce.onmicrosoft.com` (or verified custom domain). Partner organization users sign in with their own Entra ID tenant accounts; their tokens carry an `iss` claim matching `https://login.microsoftonline.com/<their-tenant>/v2.0`.
- **Issuer validation:** custom `EntraIdIssuerValidator` accepts any issuer matching `https://login.microsoftonline.com/<tenant>/v2.0` — no per-tenant allow-list.
- **User write path:** registration calls Graph `POST /v1.0/users` from the runtime CCE app (which has `User.ReadWrite.All` admin-consented application permission). CCE only ever writes users into its own home tenant — partner-tenant users are created by their own admins, not by CCE.

## Rationale

- **Multi-tenant unblocks partner orgs.** Single-tenant would force every partner user to be a guest invitation in CCE's tenant — a manual per-user gate that doesn't scale.
- **B2C was ruled out.** B2C is for consumer accounts; CCE serves organizations (employees, government partners). B2C also can't sync from on-prem AD via Entra ID Connect, which is decisive here because cce.local is the existing source of truth.
- **Graph writes (option b) chosen over Graph reads-only (option a).** CCE has self-service registration. Without write access, every new user requires an out-of-band IT ticket. The trade-off — CCE backend holds a Graph client secret with `User.ReadWrite.All` — is mitigated by storing the secret only on prod hosts in env-files locked down via ICACLS.
- **Entra ID Connect handles cce.local sync.** Existing on-prem AD identities flow into CCE's home tenant automatically; no Keycloak LDAP-federation surface needed (ADR-0055 is now superseded).

## Consequences

- **Phase 04 cutover deletes `infra/keycloak/`** and the `KeycloakLdapFederationTests` (3 tests) and the `Testcontainers.Keycloak` reference.
- **Outbound internet access required** from prod hosts to `login.microsoftonline.com` and `graph.microsoft.com`. This is a network-policy change documented in `docs/runbooks/entra-id-cutover.md`.
- **Multi-tenant means CCE can't enforce per-tenant policies.** A partner tenant could disable an account via their own admin; CCE's `EntraIdUserResolver` keeps stale objectIds linked but the next Graph call returns 404 / 401, surfacing the cleanup naturally.
- **Custom branding only renders for CCE-tenant users.** Partner-tenant users see their own home-tenant sign-in page. ADR documents this is by Microsoft design, not a configuration choice.

## Status

Accepted. **Supersedes ADR-0055** (`ad-federation-via-keycloak-ldap`).
