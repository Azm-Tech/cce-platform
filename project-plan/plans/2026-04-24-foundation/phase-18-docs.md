# Phase 18 — Documentation: ADRs, Roadmap, Briefs, Traceability

> Parent: [../2026-04-24-foundation.md](../2026-04-24-foundation.md) · Spec: [../../specs/2026-04-24-foundation-design.md](../../specs/2026-04-24-foundation-design.md)

**Phase goal:** Capture every architectural decision in ADRs, map the BRD to sub-projects, write all 8 sub-project briefs, seed `requirements-trace.csv`, ship the threat model + a11y checklist, and produce the full `README.md` + `CONTRIBUTING.md`. After Phase 18, anyone joining the project can read `docs/` and reconstruct the why behind every choice.

**Tasks in this phase:** 20
**Working directory:** `/Users/m/CCE/`
**Preconditions:** Phase 17 complete; `docs/` already has `../../specs/` + `../`.

---

## Pre-execution sanity checks

1. `git status` clean.
2. `ls docs/` — `superpowers/` exists.
3. `test ! -d docs/adr && test ! -d docs/subprojects && echo OK` → `OK` (empty target dirs).

---

## ADR template (used by every ADR file)

Each ADR follows MADR (Markdown Any Decision Record) format:

```markdown
# ADR-XXXX: <Short title>

- **Status:** Accepted
- **Date:** 2026-04-26
- **Sub-project owner:** Foundation
- **Spec ref:** [Foundation §X](../../specs/2026-04-24-foundation-design.md#section-x)

## Context

What problem are we solving? What constraints apply? What forces are at play?
2–4 paragraphs.

## Decision

The choice we made, stated definitively. 1–3 sentences.

## Consequences

### Positive

- Bullet
- Bullet

### Negative

- Bullet
- Bullet

### Neutral / follow-ups

- Bullet

## Alternatives considered

### Option A: <name>

- One sentence summary
- Why rejected

### Option B: <name>

- Same shape

## Related

- Linked ADRs
- Linked code paths
```

Subagent: use this template for every ADR. Keep each ADR under 400 words.

---

## Tasks 18.1 — 18.5: Foundational ADRs (1–5)

Each task creates **one** ADR file under `docs/adr/`.

- [ ] **Task 18.1:** `docs/adr/0001-decomposition-9-subprojects.md`

  - **Decision:** Foundation is sub-project 1 of 9; remaining 8 sub-projects (Data/Domain, Internal API, External API, Admin CMS, Web Portal, Feature Modules, Integration Gateway, Mobile) each get their own brainstorm → spec → plan → implementation cycle.
  - **Why over a single big-bang plan:** spec §10 + brainstorming-skill scope-check rule.
  - Commit: `docs(phase-18): add ADR-0001 decomposition into 9 sub-projects`

- [ ] **Task 18.2:** `docs/adr/0002-angular-over-react.md`

  - **Decision:** Angular 18 over React 18 for the two frontend apps.
  - **Why:** opinionated stack with built-in i18n/RTL, Reactive Forms for admin-heavy UI, mature Angular Material + ARIA out-of-box, single source of truth for long-lived government projects.
  - **Reject:** React 18 (assemble-your-own approach, more decisions per feature).
  - Commit: `docs(phase-18): add ADR-0002 Angular over React`

- [ ] **Task 18.3:** `docs/adr/0003-material-bootstrap-grid-dga-tokens.md`

  - **Decision:** Angular Material 18 for components/theming + Bootstrap 5 grid + utilities ONLY (no Bootstrap components/theme) + DGA tokens layered on top.
  - **Why:** Material gives mature RTL + a11y + density; Bootstrap grid is the most economical responsive layout; DGA tokens align with Saudi gov UX standard.
  - Commit: `docs(phase-18): add ADR-0003 Material components + Bootstrap grid + DGA tokens`

- [ ] **Task 18.4:** `docs/adr/0004-single-repo-backend-frontend.md`

  - **Decision:** Single git repo, idiomatic ecosystems side-by-side: `backend/` (.NET solution) + `frontend/` (Nx workspace).
  - **Why:** OpenAPI contract bridge stays atomic; one PR can span the API + UI; keeps each ecosystem on its native conventions.
  - **Reject:** Nx-monorepo-everything (Nx .NET plugin is second-class) and four-separate-repos (heavy NuGet/npm publish overhead).
  - Commit: `docs(phase-18): add ADR-0004 single repo with backend + frontend workspaces`

- [ ] **Task 18.5:** `docs/adr/0005-local-first-docker-compose.md`
  - **Decision:** Foundation targets local Docker Compose dev; production hosting target deferred.
  - **Why:** moves the build forward without committing to ministry hosting choices; everything stays portable / containerizable.
  - Commit: `docs(phase-18): add ADR-0005 local-first Docker Compose dev environment`

---

## Tasks 18.6 — 18.10: Auth, testing, version pins, contract bridge, observability

- [ ] **Task 18.6:** `docs/adr/0006-keycloak-as-adfs-stand-in.md`

  - **Decision:** Keycloak in dev, ADFS in prod, both via OIDC.
  - **Why:** identical claim shapes (`upn`, `groups`, `preferred_username`) — swap is config-only.
  - Commit: `docs(phase-18): add ADR-0006 Keycloak as ADFS stand-in via OIDC`

- [ ] **Task 18.7:** `docs/adr/0007-tdd-strict-backend-test-after-ui.md`

  - **Decision:** Strict TDD for `Domain`/`Application`/`Infrastructure` critical paths/`Api.*`; test-after for Angular UI.
  - **Why:** UI component tests for trivial templates are low ROI; domain logic must be test-first or it rots.
  - Coverage gates: ≥90% Domain/App, ≥70% Infra/API, ≥60% Angular overall.
  - Commit: `docs(phase-18): add ADR-0007 TDD policy (strict backend, test-after Angular UI)`

- [ ] **Task 18.8:** `docs/adr/0008-version-pins.md`

  - **Decision:** .NET 8 LTS, Angular 18.2, ngx-translate (vs `@angular/localize` build-time), Signals + services (no NgRx in Foundation).
  - **Why:** LTS stability for gov procurement; ngx-translate handles dynamic content from CMS; NgRx adds boilerplate without payback for a Foundation-scope app.
  - Commit: `docs(phase-18): add ADR-0008 version pins (.NET 8, Angular 18.2, ngx-translate, Signals)`

- [ ] **Task 18.9:** `docs/adr/0009-openapi-as-contract-source.md`

  - **Decision:** OpenAPI specs in `contracts/` are the single source of truth between backend + frontend. Backend exports via Swashbuckle on every build; frontend regenerates `api-client` lib via `@hey-api/openapi-ts`. Drift detected by `scripts/check-contracts-clean.sh`.
  - Commit: `docs(phase-18): add ADR-0009 OpenAPI as single contract source with drift check`

- [ ] **Task 18.10:** `docs/adr/0010-sentry-error-tracking.md`
  - **Decision:** Sentry for both backend + frontend error tracking; SDK no-ops when DSN env var is empty.
  - **Why:** uniform error pipeline dev → prod, no self-hosted Sentry container in Foundation.
  - Commit: `docs(phase-18): add ADR-0010 Sentry for error tracking (no self-hosted in Foundation)`

---

## Tasks 18.11 — 18.15: Security pipeline, a11y/load, permissions, Clean Arch, OIDC PKCE

- [ ] **Task 18.11:** `docs/adr/0011-security-scanning-pipeline.md`

  - **Decision:** Layered security: Gitleaks (pre-commit + CI), CodeQL, Semgrep, SonarCloud, OWASP ZAP, Trivy, Dependency-Check, Dependency-Review, CycloneDX SBOM. Each tool's role and trigger documented.
  - Commit: `docs(phase-18): add ADR-0011 security scanning pipeline (CodeQL/Semgrep/SonarCloud/ZAP/Trivy/...)`

- [ ] **Task 18.12:** `docs/adr/0012-a11y-axe-and-k6-loadtest.md`

  - **Decision:** axe-core enforced in Playwright E2E (zero critical/serious WCAG 2.1 AA violations); k6 thresholds enforced on `/health` p95 < 100ms (anonymous) + 200ms (authenticated).
  - Commit: `docs(phase-18): add ADR-0012 a11y gate (axe-core) + k6 load thresholds`

- [ ] **Task 18.13:** `docs/adr/0013-permissions-source-generated-enum.md`

  - **Decision:** `permissions.yaml` is the single source of truth; Roslyn source generator emits `Permissions` static class consumed by backend policies + (via OpenAPI extension, future) frontend guards.
  - Commit: `docs(phase-18): add ADR-0013 permissions source-generated from permissions.yaml`

- [ ] **Task 18.14:** `docs/adr/0014-clean-architecture-layering.md`

  - **Decision:** Clean Architecture layers: Domain → Application → Infrastructure → Api.\* + Integration. Domain has zero deps; Application depends on Domain only; Infra implements Application interfaces.
  - Commit: `docs(phase-18): add ADR-0014 Clean Architecture layering`

- [ ] **Task 18.15:** `docs/adr/0015-oidc-code-flow-pkce-bff-cookies.md`
  - **Decision:** OIDC authorization code flow + PKCE; refresh tokens in httpOnly SameSite=Strict secure cookies via the BFF (backend-for-frontend) pattern; never localStorage.
  - **Note on Foundation gap:** the actual BFF cookie wiring lands in sub-project 4; Foundation has the Angular SPA holding tokens via `angular-auth-oidc-client` defaults (in-memory). ADR documents the target design.
  - Commit: `docs(phase-18): add ADR-0015 OIDC code-flow + PKCE + BFF cookie pattern`

---

## Tasks 18.16 — 18.18: Phase 01 divergence ADRs

- [ ] **Task 18.16:** `docs/adr/0016-azure-sql-edge-for-arm64-dev.md`

  - **Decision:** Use `mcr.microsoft.com/azure-sql-edge:1.0.7` in local Docker Compose instead of SQL Server 2022 (no arm64 image from Microsoft).
  - **Prod unchanged:** SQL Server 2022 per HLD §3.3.4.
  - Commit: `docs(phase-18): add ADR-0016 Azure SQL Edge for arm64 dev (prod unchanged)`

- [ ] **Task 18.17:** `docs/adr/0017-serilog-file-sink-for-siem-stub.md`

  - **Decision:** Drop Papercut from Phase 01 stack (it's SMTP, not SIEM); use Serilog's File sink writing structured JSON to `logs/siem-events.log` for the dev SIEM stub. Real SIEM shipping is sub-project 8.
  - Commit: `docs(phase-18): add ADR-0017 Serilog file sink as dev SIEM stub (drop Papercut)`

- [ ] **Task 18.18:** `docs/adr/0018-clamav-debian-for-arm64.md`
  - **Decision:** Use `clamav/clamav-debian:stable` (multi-arch) instead of `clamav/clamav:stable` (amd64 only). Identical clamd protocol on TCP 3310.
  - Commit: `docs(phase-18): add ADR-0018 clamav-debian image for arm64 multi-arch`

---

## Task 18.19: Roadmap

**Files:**

- Create: `docs/roadmap.md`

- [ ] **Step 1: Write the roadmap**

```markdown
# CCE Roadmap

## Sub-projects

| #   | Sub-project         | Status                               | Goal                                                         | BRD refs                                                                | Depends on |
| --- | ------------------- | ------------------------------------ | ------------------------------------------------------------ | ----------------------------------------------------------------------- | ---------- |
| 1   | Foundation          | **In progress (this is Foundation)** | Scaffold + CI + dev infra                                    | NFR §4.1.32                                                             | —          |
| 2   | Data & Domain       | Pending                              | Full EF schema, migrations, seed data, permission matrix     | §4.1.31, §4.1.32                                                        | 1          |
| 3   | Internal API        | Pending                              | Admin endpoints + reports                                    | §4.1.19–4.1.29, §6.2.37–6.2.63, §6.4.1–6.4.9                            | 2          |
| 4   | External API        | Pending                              | Public endpoints + smart-assistant + community               | §4.1.1–4.1.18, §6.2.1–6.2.36                                            | 2          |
| 5   | Admin / CMS Portal  | Pending                              | Angular admin app                                            | §4.1.19–4.1.29, §6.3.9–6.3.16, §6.4                                     | 3          |
| 6   | External Web Portal | Pending                              | Angular public app                                           | §4.1.1–4.1.18, §6.3.1–6.3.8                                             | 4          |
| 7   | Feature Modules     | Pending                              | Knowledge Maps, Interactive City, Smart Assistant, Community | §4.1.4, §4.1.5, §4.1.11, §4.1.12, §4.1.13, §6.2.6–6.2.9, §6.2.19–6.2.31 | 6          |
| 8   | Integration Gateway | Pending                              | KAPSARC, ADFS, Email, SMS, SIEM, iCal                        | §6.5, §7.1, §7.2, HLD §3.1.2–3.1.8                                      | 3, 4       |
| 9   | Mobile (Flutter)    | Pending                              | WebView shell for iOS/Android/Huawei                         | HLD §3.2.2                                                              | 6          |

## Foundation completion (sub-project 1)

See [`subprojects/01-foundation.md`](subprojects/01-foundation.md) for the brief.
DoD tracked in [Foundation spec §11](../../specs/2026-04-24-foundation-design.md#11-definition-of-done).

## Per-sub-project briefs

- [02 Data & Domain](subprojects/02-data-domain.md)
- [03 Internal API](subprojects/03-internal-api.md)
- [04 External API](subprojects/04-external-api.md)
- [05 Admin / CMS Portal](subprojects/05-admin-cms.md)
- [06 External Web Portal](subprojects/06-web-portal.md)
- [07 Feature Modules](subprojects/07-feature-modules.md)
- [08 Integration Gateway](subprojects/08-integrations.md)
- [09 Mobile (Flutter)](subprojects/09-mobile-flutter.md)

## Traceability

- [`requirements-trace.csv`](requirements-trace.csv) — every BRD section → sub-project mapping.
```

- [ ] **Step 2: Commit**

```bash
git add docs/roadmap.md
git -c commit.gpgsign=false commit -m "docs(phase-18): add docs/roadmap.md with 9-sub-project map + BRD references"
```

---

## Task 18.20: 8 sub-project briefs + traceability + threat model + a11y checklist + README + CONTRIBUTING

**Combined task** — these are all short documents, written together to keep commit count under 20.

**Files:**

- Create: `docs/subprojects/01-foundation.md` (back-reference to spec)
- Create: `docs/subprojects/02-data-domain.md` through `docs/subprojects/09-mobile-flutter.md` (8 files)
- Create: `docs/requirements-trace.csv`
- Create: `docs/threat-model.md`
- Create: `docs/a11y-checklist.md`
- Modify: `README.md` (replace Phase 00 stub)
- Create: `CONTRIBUTING.md`

Each sub-project brief uses this template (200–400 words each):

```markdown
# Sub-project NN: <Title>

## Goal

One paragraph outcome statement.

## BRD references

- §X.Y — <description>
- §X.Z — <description>

## Dependencies

- Sub-project NN must be complete before this one starts.

## Rough estimate

T-shirt size: S / M / L / XL.

## DoD skeleton

- [ ] Item 1
- [ ] Item 2

Refined at this sub-project's own brainstorm cycle.
```

`requirements-trace.csv` columns: `brd_ref,title_ar,title_en,subproject_id,subproject_name,dod_anchor,status`. Seed every BRD section from the index in [/tmp/brd.txt](/tmp/brd.txt) (extracted in Phase 0). Status: `pending` for everything, `in-progress` for Foundation NFRs.

`threat-model.md` follows STRIDE against the HLD architecture: Spoofing / Tampering / Repudiation / Info disclosure / DoS / Elevation. One paragraph per category, mitigation refs to ADRs/code.

`a11y-checklist.md` lists items axe-core can't catch: keyboard nav flow, screen-reader narration order in ar/en, focus trap on dialogs, color use beyond contrast (e.g., red+green pairs), animations + prefers-reduced-motion.

Full `README.md` replaces the Phase 00 stub with: project intro, link to roadmap, quickstart commands (Docker + dotnet + pnpm), test commands, key directories, link list to ADRs.

`CONTRIBUTING.md`: branch model (`main`-protected, feature branches), commit format (`<type>(<scope>): <subject>`), PR checklist (mirrors `.github/pull_request_template.md`), local pre-commit setup, a11y review, security review.

- [ ] **Step 1: Write all the files (~10 files in this batch)**

Subagent decision: write each file with content that reflects the project state established by Phases 00–17. Don't invent new constraints.

- [ ] **Step 2: Commit as one atomic batch**

```bash
git add docs/subprojects/ docs/requirements-trace.csv docs/threat-model.md docs/a11y-checklist.md README.md CONTRIBUTING.md
git -c commit.gpgsign=false commit -m "docs(phase-18): add 9 sub-project briefs + traceability CSV + threat model + a11y checklist + full README + CONTRIBUTING"
```

---

## Phase 18 — completion checklist

- [ ] 18 ADRs in `docs/adr/0001-...md` through `0018-...md`.
- [ ] `docs/roadmap.md` with sub-project table + BRD refs.
- [ ] `docs/subprojects/01-...09-*.md` (9 briefs).
- [ ] `docs/requirements-trace.csv` seeded with every BRD section.
- [ ] `docs/threat-model.md` v1 (STRIDE).
- [ ] `docs/a11y-checklist.md`.
- [ ] `README.md` is the full version (not the Phase 00 stub).
- [ ] `CONTRIBUTING.md`.
- [ ] All Markdown lint clean (Prettier passes).
- [ ] `git status` clean.
- [ ] ~20 new commits (18 ADRs + roadmap + combined batch).

**If all boxes ticked, phase 18 is complete. Proceed to phase 19 (DoD verification + release tag).**
