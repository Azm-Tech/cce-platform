# ADR-0011: Layered security scanning pipeline

- **Status:** Accepted
- **Date:** 2026-04-26
- **Sub-project owner:** Foundation
- **Spec ref:** [Foundation §9](../../project-plan/specs/2026-04-24-foundation-design.md#9-security)

## Context

Government data, bilingual content from external contributors, and integrations with KAPSARC/email/SMS push CCE into a higher-than-typical security bar. No single tool covers SAST + secrets + container CVEs + dynamic scanning + dependency CVEs + license risk + SBOM. We need defense in depth, with each tool's role and trigger documented so engineers know what failed and why.

## Decision

A layered pipeline, configured in Phase 17:

| Tool                         | Role                                 | Trigger                                |
| ---------------------------- | ------------------------------------ | -------------------------------------- |
| **Gitleaks**                 | Secret detection (regex + entropy)   | Pre-commit hook + CI full-history scan |
| **CodeQL**                   | SAST (semantic)                      | CI on push + PR                        |
| **Semgrep**                  | SAST (rule-based, fast)              | CI on push + PR                        |
| **SonarCloud**               | Code smells, coverage, SAST overlay  | CI on push + PR                        |
| **OWASP ZAP**                | DAST (baseline scan)                 | Scheduled / on-demand workflow         |
| **Trivy**                    | Container CVE scan                   | CI on Dockerfile change + image build  |
| **OWASP Dependency-Check**   | NuGet + npm CVE scan                 | CI weekly + on lockfile change         |
| **GitHub Dependency Review** | Action that blocks risky deps in PRs | PR check                               |
| **CycloneDX SBOM**           | SBOM publication                     | CI on release                          |

Findings are deduped via SonarCloud + GitHub Security tab; suppression policy lives in `security/README.md`.

## Consequences

### Positive

- Defense in depth — a leak escapes Gitleaks but trips CodeQL or SonarCloud.
- Each tool is independently failable in CI, so a flake in one doesn't block all PRs (configured per-job).
- SBOM provides downstream auditability for ministry procurement.

### Negative

- CI minutes go up; some tools (CodeQL) are slow.
- Suppression management is real work; without discipline, the suppression file becomes a graveyard of "we'll fix it later".

### Neutral / follow-ups

- Suppression policy: max 30 days, ADR-or-issue link required ([security/README.md](../../security/README.md)).
- ZAP runs against a deployed staging URL — not against `localhost` in CI.

## Alternatives considered

### Option A: SonarCloud only

- Rejected: weak on secrets and container CVEs.

### Option B: Single commercial all-in-one (e.g., Snyk)

- Rejected: cost, vendor lock-in, less coverage of SAST + DAST + SBOM together.

## Related

- [ADR-0012](0012-a11y-axe-and-k6-loadtest.md)
- `.github/workflows/`
- [`security/README.md`](../../security/README.md)
