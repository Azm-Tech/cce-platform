# CCE — Security Tooling

## Layered defenses

| Layer | Tool | When |
|---|---|---|
| Pre-commit | Gitleaks (Phase 00) | every commit, fast-path |
| PR gate | CI (build/test/lint), CodeQL, Semgrep, Trivy fs+config, Dependency Review, Gitleaks history | every PR |
| Nightly / weekly | OWASP ZAP baseline (nightly), Dependency-Check (weekly), Semgrep / Trivy / Gitleaks (weekly cron) | scheduled |
| Per-release | CycloneDX SBOM | tag push |
| Quality | SonarCloud | every PR (gated on `SONAR_TOKEN` secret) |

## Files in this directory

- `gitleaks.toml` — Gitleaks config + allowlist (Phase 00).
- `semgrep.yml` — project-specific Semgrep rules.
- `trivyignore` — Trivy CVE suppression list.
- `zap-rules.tsv` — ZAP baseline rule overrides.
- `dependency-check-suppression.xml` — OWASP DC suppressions.
- `README.md` — this file.

## How to add a CVE suppression

Each suppression must be **time-bounded** and **explained**.

1. Open the right file (`trivyignore` for Trivy, `dependency-check-suppression.xml` for OWASP DC, etc.).
2. Add the CVE id with a comment containing:
   - CVE id
   - Why suppression is justified (false positive / inapplicable / mitigated elsewhere)
   - Expiry date (max 90 days)
   - Issue link tracking the upstream fix
3. Open a PR — the security reviewer signs off before merge.

## Manual security review

Before merging any PR that touches:
- AuthN/AuthZ code paths
- File upload paths
- External integration code
- Cryptography
- Persistence layer

run `./scripts/check-contracts-clean.sh` AND a manual review against `docs/threat-model.md` (added in Phase 18).
