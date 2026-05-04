# Environment promotion runbook (Sub-10c)

CCE has 4 environments: `test` → `preprod` → `prod` → (`dr` mirrors prod). Promotion is operator-driven, supported by `deploy/promote-env.ps1` for the mechanical config edits.

## When to promote

| From → To | Trigger | Image tag pattern |
|---|---|---|
| `test` → `preprod` | After test passes; promoting a feature for QA | release-candidate (`app-v1.0.0-rc.1`) |
| `preprod` → `prod` | After QA + stakeholder sign-off | release tag (`app-v1.0.0`) |
| `prod` → `dr` | When DR host needs to mirror prod's tag (e.g. before a known-risky deploy) | exact prod tag |

## Procedure: test → preprod

```powershell
# On the preprod host:
cd C:\path\to\CCE

# 1. Generate the preprod env-file from test's, with the new image tag.
.\deploy\promote-env.ps1 -FromEnv test -ToEnv preprod -ImageTag app-v1.0.0-rc.1
# Output: written to C:\ProgramData\CCE\.env.preprod with <set-me> placeholders.

# 2. Fill in the <set-me> values (preprod-specific secrets).
notepad C:\ProgramData\CCE\.env.preprod

# 3. Validate.
.\deploy\validate-env.ps1 -EnvFile C:\ProgramData\CCE\.env.preprod -Environment preprod
# Expected: OK.

# 4. Lock down ACLs.
icacls C:\ProgramData\CCE\.env.preprod /inheritance:r `
    /grant:r "Administrators:R" "<deploy-user>:R"

# 5. Deploy.
.\deploy\deploy.ps1 -Environment preprod
```

## Procedure: preprod → prod

Identical to the above; substitute `-FromEnv preprod -ToEnv prod` and use the release tag (no `-rc.N` suffix).

The first time prod runs, the `<set-me>` placeholders include the prod-specific `SENTRY_DSN`, prod LDAP bind account, prod backup-share user, etc. — different from preprod's. **`promote-env.ps1` deliberately re-blanks all secrets** so an operator can't accidentally inherit preprod creds into prod.

## Procedure: prod → dr (mirror)

```powershell
# On the DR host:
.\deploy\promote-env.ps1 -FromEnv prod -ToEnv dr -ImageTag <prod's-current-tag>
# ... fill <set-me>, validate, deploy.ps1 -Environment dr
```

DR host stays cold until promoted. Use `prod → dr` to keep the DR env-file's tag aligned before a planned risky deploy, so failover finds the right images.

## Common mistakes

| Mistake | Fix |
|---|---|
| Forgot to fill `<set-me>` | `validate-env.ps1` catches this; re-edit, re-validate. |
| Inherited secrets from prior env | Re-run `promote-env.ps1` (it re-blanks); fill in destination-specific values. |
| Wrong `CCE_IMAGE_TAG` | Edit `.env.<env>` directly, or re-run `promote-env.ps1` with `-Force`. |
| `Sentry_Environment` doesn't match `-Environment` | `validate-env.ps1` catches this; fix the env-file. |
| Used prod's hostnames for preprod (or vice versa) | Fixed automatically by `promote-env.ps1`'s per-env hostname table; if you bypassed, edit `IIS_HOSTNAMES` to match the destination's convention. |

## See also

- [`secret-rotation.md`](secret-rotation.md) — per-secret rotation procedure.
- [`deploy.md`](deploy.md) — green-path deploy.
- [`rollback.md`](rollback.md) — rollback within an env.
- [Sub-10c design spec §Multi-env config](../superpowers/specs/2026-05-04-sub-10c-design.md#multi-env-config--per-env-files).
