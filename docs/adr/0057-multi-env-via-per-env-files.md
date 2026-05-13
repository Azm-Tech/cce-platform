# ADR-0057 — Multi-env via per-env files; Vault graduation deferred

**Status:** Accepted
**Date:** 2026-05-04
**Deciders:** Sub-10c brainstorm (kilany113@gmail.com)
**Sub-project:** [Sub-10c — Production infra + DR](../../project-plan/specs/2026-05-04-sub-10c-design.md)

## Context

Sub-10b shipped one-environment deployment (`.env.prod` only). Sub-10c targets four environments (test, preprod, prod, dr) sharing the same hosts-and-Linux-containers shape. Multi-env config is the foundation every other Sub-10c phase consumes (identity binds to env-specific Keycloak realms, IIS provisions env-specific hostnames, backup writes to env-specific UNC subdirectories).

## Decision

Use **per-env env-files** at `C:\ProgramData\CCE\.env.<env>` (one per environment), with `deploy.ps1 -Environment <env>` resolving the env-file. NTFS ACLs lock each env-file to the deploy user + Administrators. Per-env audit trail via `deploy-history-${env}.tsv`. Secrets stay in env-files; rotation is operator-driven via the documented procedure (see `docs/runbooks/secret-rotation.md`).

Vault / Azure Key Vault / AWS Secrets Manager **graduation is explicitly deferred** to Sub-10d+ if it ever happens.

**Considered alternatives:**

- **Compose profiles per env**: rejected. Compose profiles opt services in/out, not parameterize their env-vars. Doesn't solve the per-env config problem.
- **Helm-style overlay (`docker-compose.<env>.yml` per env)**: rejected. Overlays shine when service shapes vary between envs; CCE's service shape is identical across envs (only env-vars differ). Adds complexity without value.
- **Vault graduation**: rejected for Sub-10c. Single-host single-tenant scale means env-files + NTFS ACLs are sufficient. Vault adds a new infrastructure component (server, unseal procedure, root-token storage) and a new failure mode for marginal benefit at this scale. Path to Vault stays open — env-files are config-from-anywhere; nothing in the deploy flow forecloses graduation later.

## Implementation

`deploy.ps1 -Environment <test|preprod|prod|dr>` resolves to `C:\ProgramData\CCE\.env.<env>`. Default `prod` preserves Sub-10b backward compat (existing call sites work unchanged).

`rollback.ps1` mirrors the switch.

`deploy/validate-env.ps1` provides canary integrity check: rejects placeholder values + known-leaked secrets + suspicious whitespace + cross-key inconsistencies.

`deploy/promote-env.ps1` does mechanical promotion (test → preprod → prod) — rewrites per-env knobs (DB name, hostnames, Sentry environment, AUTO_ROLLBACK default, log level, image tag stream); **re-blanks all secrets** to enforce per-env isolation.

Per-env `deploy-history-${env}.tsv` audit trail prevents test deploys from cluttering prod history.

## Consequences

**Positive:**
- Zero new infra. Operates entirely on the host filesystem.
- Backward-compat: Sub-10b deploys keep working with the default `-Environment prod`.
- Operator workflow is consistent across envs (same scripts, different `-Environment` value).
- Promotion is mechanical (`promote-env.ps1`) with security-by-default secret re-blanking.
- Canary check + cross-key consistency catches placeholder values + leaked-secret canaries before deploy.

**Negative / accepted:**
- Secret rotation is manual via the runbook procedure. Operator must touch each env-file individually.
- No central audit of secret values across envs; the deploy-history.tsv shows which tag was deployed when, not what was in the env-file.
- File-based secrets are a recognized risk; mitigated by NTFS ACL + canary check, but not eliminated.

**Out of scope (Sub-10d+):**
- Vault / Key Vault graduation.
- Automated rotation.
- Multi-host (each env on multiple hosts) — per-host env-files would diverge; that's a Sub-10c+ HA topology question.

## References

- [Sub-10c design spec §Multi-env config](../../project-plan/specs/2026-05-04-sub-10c-design.md#multi-env-config--per-env-files)
- [Secret-rotation runbook](../runbooks/secret-rotation.md)
- [Env-promotion runbook](../runbooks/env-promotion.md)
- ADR-0054 — IIS reverse proxy on Windows Server (Sub-10c)
- ADR-0055 — AD federation via Keycloak LDAP (Sub-10c)
- ADR-0056 — Backup strategy (Sub-10c)
