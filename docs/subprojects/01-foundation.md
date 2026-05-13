# Sub-project 01: Foundation

## Goal

Stand up the repo, CI, dev infrastructure, contract pipeline, security gates, and documentation that every later sub-project will rely on. After Foundation, a new contributor can clone, run `docker compose up -d`, build the .NET solution, run the Angular apps, and have a working end-to-end auth + health-check stack with all CI gates green. **This is the canonical starting point** — all other sub-projects depend on Foundation.

## BRD references

- §4.1.32 — Non-functional: stack and tooling baseline.
- HLD §3.1, §3.3 — Reference architecture (Compose stack mirrors the runtime topology).

## Dependencies

- None. Foundation is sub-project 1 of 9.

## Rough estimate

T-shirt size: **XL** (already in flight — 18 phases).

## DoD skeleton

- [ ] All 18 phases complete; phase-18 docs in place.
- [ ] `docker compose up -d` brings infra healthy (SQL, Redis, Keycloak, MailDev, ClamAV).
- [ ] `dotnet test backend/CCE.sln` green.
- [ ] `pnpm nx run-many -t lint,test` green.
- [ ] `pnpm nx run-many -t e2e` green (web-portal-e2e + admin-cms-e2e).
- [ ] axe-core: zero critical/serious violations on smoke E2E.
- [ ] k6 thresholds met: `/health` p95 < 100 ms anonymous, < 200 ms authenticated.
- [ ] All security workflows green on `main`: Gitleaks, CodeQL, Semgrep, SonarCloud, Trivy, Dependency-Check, Dependency-Review, CycloneDX SBOM.
- [ ] OpenAPI drift check (`scripts/check-contracts-clean.sh`) green.
- [ ] All 18 ADRs committed; `docs/roadmap.md`, briefs, traceability, threat model, a11y checklist, README, CONTRIBUTING shipped.
- [ ] Release tag `foundation-v1.0.0`.

Refined at the Foundation spec § 11 — see [`project-plan/specs/2026-04-24-foundation-design.md`](../../project-plan/specs/2026-04-24-foundation-design.md#11-definition-of-done).

## Related

- Spec: [`project-plan/specs/2026-04-24-foundation-design.md`](../../project-plan/specs/2026-04-24-foundation-design.md)
- Plan: [`project-plan/plans/2026-04-24-foundation/`](../../project-plan/plans/2026-04-24-foundation)
- ADRs: [`docs/adr/`](../adr/)
