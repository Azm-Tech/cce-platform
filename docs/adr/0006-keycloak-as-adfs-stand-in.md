# ADR-0006: Keycloak as ADFS stand-in via OIDC

- **Status:** Accepted
- **Date:** 2026-04-26
- **Sub-project owner:** Foundation
- **Spec ref:** [Foundation §3](../superpowers/specs/2026-04-24-foundation-design.md#3-stack-decisions)

## Context

Production CCE federates with the ministry's **ADFS** for authentication via OIDC. ADFS is not realistic to run locally for every contributor: it requires Active Directory, Windows Server, and per-laptop trust setup. Foundation needs a dev-time identity provider that produces OIDC tokens with the **same claim shapes** ADFS will produce, so the swap to ADFS is configuration-only — not code change.

## Decision

- **Dev / Foundation:** Keycloak 25 in `docker-compose.yml`, exposing OIDC endpoints on `localhost:8080`.
- **Prod:** ADFS, identical OIDC contract (authorization-code + PKCE), identical claim shape: `upn`, `groups`, `preferred_username`, plus standard `sub`/`iss`/`aud`/`exp`.
- The .NET backend's OIDC config (`appsettings.json` overrides) and the Angular `angular-auth-oidc-client` config differ only in `Authority`/`ClientId` between environments.

## Consequences

### Positive

- Local devs run a single `docker compose up -d` and get a working OIDC IdP.
- Claim-shape parity means JWT validation, role mapping, and middleware logic don't branch per environment.
- Keycloak is tracked in git via realm export ([phase 02 plan](../superpowers/plans/2026-04-24-foundation/phase-02-keycloak.md)) so realm state is reproducible.
- ADFS swap is a config change, not a code change.

### Negative

- Two systems' quirks to know (Keycloak admin console, ADFS claim rules); engineers must understand the contract, not just one implementation.
- Realm export drift requires diligence — Keycloak admin UI changes that aren't exported don't survive `docker compose down -v`.

### Neutral / follow-ups

- Production realm/issuer/clientId values are environment variables, never committed.
- Any new claim required by a feature must be added to **both** Keycloak realm export and the ADFS claim-rule documentation (sub-project 8).

## Alternatives considered

### Option A: Mock OIDC server (e.g., handcrafted JWT issuer)

- Rejected: doesn't exercise real authorization-code + PKCE; behavior diverges from ADFS in subtle ways.

### Option B: Use ADFS in dev too

- Rejected: requires AD + Windows Server + per-laptop trust; impractical.

### Option C: Use a hosted IdP (Auth0, Azure AD)

- Rejected: per-developer accounts, costs, network dependency; doesn't model the on-prem ADFS topology.

## Related

- [ADR-0015](0015-oidc-code-flow-pkce-bff-cookies.md)
- `keycloak/cce-realm.json`
- `docker-compose.yml`
