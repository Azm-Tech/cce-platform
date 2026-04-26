# CCE Threat Model — v1 (STRIDE)

> Scope: Foundation (sub-project 1) topology. Refined per sub-project as feature surface grows.
> Reference architecture: HLD §3.1, §3.3.

This is a **STRIDE** pass against the CCE reference architecture: SPA frontends → API gateway → Internal/External APIs → Domain → Infrastructure (SQL, Redis, ClamAV) → Integrations (KAPSARC, ADFS, Email, SMS, SIEM, iCal). Each category gets one paragraph and links to mitigation ADRs / code paths. This is a living document.

## S — Spoofing

Attackers may attempt to impersonate users (stolen session), services (rogue API client), or the IdP (DNS spoof). Mitigations:

- OIDC code-flow + PKCE + httpOnly cookies via BFF — no JS-accessible refresh tokens, see [ADR-0015](adr/0015-oidc-code-flow-pkce-bff-cookies.md).
- Same claim shape across Keycloak (dev) and ADFS (prod) — [ADR-0006](adr/0006-keycloak-as-adfs-stand-in.md) — so JWT validation is identical.
- TLS everywhere (mandated in prod; dev is loopback-only by default).
- Service-to-service calls authenticated via signed JWTs; no plain shared secrets in code.

## T — Tampering

Attackers may modify data in transit, in storage, or in build artifacts. Mitigations:

- TLS in prod terminates external tampering risk.
- Database integrity via transactional writes + audit-log entries on every state change ([ADR-0014](adr/0014-clean-architecture-layering.md)).
- File uploads scanned by ClamAV ([ADR-0018](adr/0018-clamav-debian-for-arm64.md)) before persistence.
- Build supply chain: pinned Docker images (Trivy), pinned NuGet/npm via lockfiles, Dependency-Check + Dependency-Review block tampered or vulnerable transitive deps ([ADR-0011](adr/0011-security-scanning-pipeline.md)).
- CycloneDX SBOM published per release for downstream verification.

## R — Repudiation

Users may deny actions; admins may deny changes. Mitigations:

- Append-only audit log on every state-changing operation, written from the Application layer. Includes user id, permission asserted, before/after where relevant.
- Logs are structured (Serilog) — fields are queryable, not free-text.
- SIEM shipping (real, in sub-project 8; file stub in Foundation per [ADR-0017](adr/0017-serilog-file-sink-for-siem-stub.md)) preserves events outside the application's own DB.

## I — Information disclosure

Sensitive data may leak via responses, logs, error pages, or storage. Mitigations:

- Permissions enforced at the Application layer ([ADR-0013](adr/0013-permissions-source-generated-enum.md)) — no endpoint without a permission check.
- Sentry PII scrubbing rules; DSN-empty no-op in dev ([ADR-0010](adr/0010-sentry-error-tracking.md)).
- Error responses sanitized in prod (no stack traces; correlation IDs instead).
- Gitleaks pre-commit + CI history scan blocks credentials in code.
- TLS in prod; SQL/Redis encrypted at rest per ministry policy.

## D — Denial of service

Public endpoints may be flooded; expensive operations (smart-assistant queries) may be abused. Mitigations:

- Rate limiting on public endpoints (per IP + per session; tightened in sub-project 4).
- k6 load thresholds in CI catch performance regressions before merge ([ADR-0012](adr/0012-a11y-axe-and-k6-loadtest.md)).
- ClamAV scans bounded in size; uploads have hard limits.
- Smart-assistant + community endpoints throttled per session.
- Health checks + Sentry alerting surface degradation early.

## E — Elevation of privilege

Users may attempt to bypass permission checks (mass assignment, IDOR, missing authorization). Mitigations:

- Source-generated `Permissions` static class — typos and missing references caught at compile time ([ADR-0013](adr/0013-permissions-source-generated-enum.md)).
- OpenAPI `x-permission` extension projects permissions into client guard generation (sub-project 5/6).
- Architecture tests assert layer boundaries — no Domain bypass via direct Infrastructure access ([ADR-0014](adr/0014-clean-architecture-layering.md)).
- DTO mapping is explicit; no automatic mass-assignment from request body to entity.
- Integration tests cover auth-fail / wrong-permission paths for every endpoint.

## Out of scope (Foundation)

- Hosting-target threats (cloud IAM, network ACLs, secrets manager) — owned by Ops cycle.
- Physical / personnel security — ministry policy.
- Third-party LLM data residency for Smart Assistant — assessed in sub-project 7/8.

## Update cadence

This document is re-reviewed at the start of every sub-project brainstorm cycle. New components add new STRIDE entries. Suppression / accepted-risk decisions require an ADR.
