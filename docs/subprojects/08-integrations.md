# Sub-project 08: Integration Gateway

## Goal

Implement every external integration: KAPSARC content sync, ADFS prod federation, transactional email, SMS, real SIEM shipping, iCal feed publishing. Consolidate adapter code under `CCE.Integration.*` projects with consistent retry / circuit-breaker / observability patterns. After this sub-project, prod-grade external dependencies are wired and the dev SIEM stub ([ADR-0017](../adr/0017-serilog-file-sink-for-siem-stub.md)) is replaced by real shipping in non-dev environments.

## BRD references

- §6.5 — Integration touchpoints.
- §7.1, §7.2 — Internal messages, alerts.
- HLD §3.1.2–§3.1.8 — Reference architecture for each integration.

## Dependencies

- Sub-project 3 (Internal API) — owns the calling sites.
- Sub-project 4 (External API) — consumer side for some integrations.

## Rough estimate

T-shirt size: **L**.

## DoD skeleton

- [ ] **KAPSARC:** scheduled content sync; idempotent upserts; conflict resolution; backfill.
- [ ] **ADFS:** prod realm wiring; claim-rule documentation; swap from Keycloak validated end-to-end in a staging environment.
- [ ] **Email:** transactional templates (registration, password reset, content notifications); MailDev still works in dev.
- [ ] **SMS:** provider abstraction; rate limiting; DLR handling.
- [ ] **SIEM:** real network shipping (HEC / syslog); same event schema as the dev file stub.
- [ ] **iCal:** event feed endpoint(s); RFC 5545 compliant.
- [ ] Each integration: retry policy, circuit breaker, structured logging, Sentry integration, dashboard alert if down.
- [ ] Secret management: every credential via env var / vault, never committed.

Refined at this sub-project's own brainstorm cycle.

## Related

- ADRs: [0006](../adr/0006-keycloak-as-adfs-stand-in.md), [0010](../adr/0010-sentry-error-tracking.md), [0017](../adr/0017-serilog-file-sink-for-siem-stub.md).
