# Contributing to CCE

Thanks for working on CCE. This document covers branches, commit format, the PR checklist, and the local pre-commit / a11y / security expectations.

## Branch model

- **`main`** is protected. Only fast-forward merges via reviewed PRs. Direct pushes are rejected.
- **Feature branches** are short-lived: `feat/<scope>`, `fix/<scope>`, `docs/<scope>`, `chore/<scope>`. One coherent change per branch.
- Long-running branches (release stabilization, etc.) are the exception, not the rule.

## Commit format

Conventional Commits-style, single line for the subject:

```
<type>(<scope>): <subject>
```

- `<type>` ∈ `feat | fix | docs | chore | refactor | test | perf | ci | build | style | revert`.
- `<scope>` is a short noun: `phase-XX`, `api`, `web-portal`, `admin-cms`, `infra`, `permissions`, etc.
- `<subject>` is imperative, no trailing period, ≤ 72 chars.

Body and footer are optional; use them for context, BRD references, and `Co-Authored-By` lines.

Examples:

```
feat(api): add /users/{id}/permissions endpoint
docs(phase-18): add ADR-0007 TDD policy (strict backend, test-after Angular UI)
fix(web-portal): correct RTL flip on community feed
```

## PR checklist

Mirrors [`.github/pull_request_template.md`](.github/pull_request_template.md). Every PR must satisfy:

- [ ] `dotnet test backend/CCE.sln` green.
- [ ] `pnpm nx run-many -t lint,test` green.
- [ ] If API surface changed: `./scripts/check-contracts-clean.sh` green.
- [ ] If UI changed: `pnpm nx run-many -t e2e` (web-portal-e2e + admin-cms-e2e) green.
- [ ] No new secrets / credentials in code.
- [ ] AuthN / AuthZ impact considered.
- [ ] Input validation on new endpoints.
- [ ] Audit-log entry on new state-changing operations.
- [ ] BRD section IDs covered listed (or "n/a").

## Local pre-commit setup

The repo uses Husky for git hooks and Gitleaks for secret detection.

```bash
pnpm install --frozen-lockfile
pnpm prepare        # installs Husky hooks (auto-runs on first install)
```

After install, `git commit` will run:

- **Gitleaks** scan on staged content.
- **Prettier** on staged Markdown / YAML / JSON.

Failures block the commit. Don't bypass with `--no-verify` — fix the underlying issue. If you have a legitimate reason, raise it in the PR description.

To run the same checks manually:

```bash
gitleaks protect --staged --redact -v
pnpm prettier --check .
```

## Accessibility review

UI-touching PRs must:

- Pass the axe-core gate baked into the E2E suites (zero `critical` / `serious` WCAG 2.1 AA violations).
- Be checked against the manual list in [`docs/a11y-checklist.md`](docs/a11y-checklist.md) for the screens you changed.
- Verify both `dir="ltr"` (English) and `dir="rtl"` (Arabic) layouts.

If you suppress an axe rule, link to the violating third-party widget or a tracking issue. Bare suppressions are rejected in review.

## Security review

PRs that touch security-relevant surface (auth, file uploads, external calls, deserialization, SQL, env vars) require:

- A note in the PR summary explaining the security-relevant change.
- The Security checklist items in the PR template ticked.
- A reviewer with security context approving.

Suppression of a security tool finding (CodeQL, Semgrep, SonarCloud, Trivy, Dependency-Check) follows the policy in [`security/README.md`](security/README.md): max 30 days, ADR or tracked issue required.

## Working with phase plans

This project follows a brainstorm → spec → plan → execute workflow under `docs/superpowers/`. New planned work goes through:

1. A **brainstorm** that explores intent and scope.
2. A **spec** in `docs/superpowers/specs/` once requirements are clear.
3. A **plan** in `docs/superpowers/plans/<date>-<slug>/` with one task at a time.
4. **Execution** with one commit per task, exact commit messages from the plan.

If you're picking up unfinished work, start by reading the parent plan file and the most recent phase plan.

## Questions

Open an issue, or ping the maintainers in the repo's discussion channel.
