# Sub-11 — Entra ID migration completion

**Status:** Done. Tagged `entra-id-v1.0.0` on 2026-05-05.
**Predecessor:** [Sub-10c — Production infra + DR](./sub-10c-production-infra-completion.md) (`infra-v1.0.0`).

## Goal

Replace Keycloak (Sub-1's IdP, wired through Sub-2/3/4/5/6/9 and Sub-10c's LDAP federation) with **multi-tenant Microsoft Entra ID** as the IdP for the entire CCE platform. Sync from on-prem AD via **Entra ID Connect**. CCE backend writes to Entra ID via **Microsoft Graph SDK** for self-service registration. **Conditional Access** enforces MFA at the Entra ID side; the CCE backend stays MFA-agnostic.

## Architectural decisions

- **ADR-0058** — Entra ID multi-tenant + Graph writes (supersedes ADR-0055).
- **ADR-0059** — App roles vs security groups (app roles chosen).
- **ADR-0060** — Conditional Access for MFA enforcement.
- **ADR-0055** — `ad-federation-via-keycloak-ldap`: status changed to **Superseded by ADR-0058**.

## Phase summary

| Phase | Title | Outcome |
|---|---|---|
| 00 | Backend foundation (auth library swap) | `Microsoft.Identity.Web` 3.5.0 + `Microsoft.Graph` 5.65.0 + `Azure.Identity` 1.13.2 + `WireMock.Net` 1.7.0 wired. EF migration `AddEntraIdObjectIdToUser`. `EntraIdOptions` + `EntraIdIssuerValidator` + `EntraIdUserResolver`. `CceJwtAuthRegistration` + `BffRegistration` rewired against M.I.W. |
| 01 | Self-service registration via Microsoft Graph | `EntraIdGraphClientFactory` + `EntraIdRegistrationService` + `RegistrationContracts`. `ProfileEndpoints./api/users/register` POST gated to `cce-admin`. WireMock-based `EntraIdFixture` + 4 PII-scrubbed Graph fixture JSONs replace Testcontainers Keycloak. |
| 02 | App registration + branding provisioning | `infra/entra/app-registration-manifest.json` (5 app roles + 10 redirect URIs + 3 Graph permissions). PowerShell scripts: `apply-app-registration.ps1` + `Configure-Branding.ps1`. Branding placeholders + operator README. ADRs 0058 / 0059 / 0060 + ADR-0055 → Superseded. |
| 03 | Frontend OIDC swap + permission-transformer rewrite | Frontend OIDC config + `env.json` files point at `login.microsoftonline.com/common/v2.0`. `register.page` rewired to info page. `RoleToPermissionClaimsTransformer` reads Entra ID `roles` claim with `cce-*` values. `permissions.yaml` + `PermissionsGenerator.KnownRoles` renamed; `ToRoleMemberName` helper. |
| 04 | Cutover + Keycloak deletion | Custom BFF cluster (7 src files) deleted. `infra/keycloak/` deleted. `KeycloakLdapFederationTests` (3) deleted. `Testcontainers.Keycloak` reference removed. `KEYCLOAK_*` + `LDAP_*` env-keys deleted. `ad-federation.md` deleted. `entra-id-cutover.md` + `entra-id-troubleshooting.md` shipped. Tag `entra-id-v1.0.0`. |

## Test deltas

| Suite | Pre-Sub-11 | Post-Sub-11 | Delta |
|---|---:|---:|---:|
| `CCE.Domain.Tests` | 290 | 290 | 0 |
| `CCE.Application.Tests` | 439 | 439 | 0 |
| `CCE.ArchitectureTests` | 12 | 12 | 0 |
| `CCE.Infrastructure.Tests` | 75 | 84 | **+9** |
| `CCE.Api.IntegrationTests` (transformer) | 5 | 5 | 0 (3 retargeted, 2 added, 5 removed via BFF deletion) |
| Frontend (`web-portal` + `admin-cms`) | 720 | 720 | 0 |

**Infrastructure breakdown** (75 → 84): −3 KeycloakLdap + +5 EntraIdIssuerValidator + +3 EntraIdObjectIdLazyResolution + +1 EntraIdFixture smoke + +3 EntraIdRegistration = +9 net.

**IntegrationTests delta**: −5 BffSessionMiddlewareTests (custom BFF cluster deleted) + transformer tests held steady at 5 after Phase 04 simplification (−2 legacy `groups`-claim tests + −2 retargeted from legacy → roles claim, +2 net new RoleClaimMapping tests deferred from Phase 00).

## Net file changes

**Created (~30 files):**
- 5 new src/CCE.Infrastructure/Identity/ files: `EntraIdGraphClientFactory.cs`, `EntraIdOptions.cs`, `EntraIdRegistrationService.cs`, `RegistrationContracts.cs` (relocated from CCE.Api.Common.Auth + 1 new)
- 3 new src/CCE.Api.Common/Auth/ files: `EntraIdIssuerValidator.cs`, `EntraIdUserResolver.cs`, `CceAuthCookies.cs`
- 4 new test files: `EntraIdFixture.cs`, `EntraIdIssuerValidatorTests.cs`, `EntraIdObjectIdLazyResolutionTests.cs`, `EntraIdRegistrationTests.cs`
- 4 Graph fixture JSONs under `tests/.../Identity/Fixtures/entra-id-fixtures/`
- 1 EF migration: `20260504182534_AddEntraIdObjectIdToUser`
- 5 infra/entra/ files: `app-registration-manifest.json`, `apply-app-registration.ps1`, `Configure-Branding.ps1`, `README.md`, `branding/{README.md, custom.css.example, .gitkeep}`
- 4 docs: ADR-0058 + ADR-0059 + ADR-0060 + sub-11 completion doc
- 2 runbooks: `entra-id-cutover.md`, `entra-id-troubleshooting.md`
- 5 plan docs: master + 4 phase details

**Deleted (~13 files):**
- 7 custom-BFF cluster files: `BffSessionMiddleware`, `BffAuthEndpoints`, `BffTokenRefresher`, `BffSessionCookie`, `BffSession`, `BffOptions`, `BffTokenResponse`
- 3 Keycloak test/infra files: `KeycloakLdapFixture`, `KeycloakLdapFederationTests`, `BffSessionMiddlewareTests`
- 2 infra/keycloak/ files: `apply-realm.ps1`, `realm-cce-ldap-federation.json`
- 1 runbook: `ad-federation.md`

## Deferred items (Sub-11d backlog)

- **Anonymous self-service registration.** `/api/users/register` is admin-only in Sub-11 (gated to `cce-admin`). Anonymous public sign-up needs an `IEmailSender` abstraction to deliver the temp password — neither exists yet. Sub-11d adds both; the existing `RegisterUserRequest` DTO + service shape already match.
- **Email-on-create hook in `EntraIdRegistrationService.CreateUserAsync`.** Currently returns the temp password to the calling admin who communicates it out-of-band. Sub-11d wires `IEmailSender.SendWelcomeAsync(...)`.
- **Manual Graph sync endpoint (`/api/admin/users/sync`).** Phase 04's cutover runbook references this for the day-of-cutover backfill flow but the endpoint isn't implemented. `EntraIdUserResolver` does the work lazily on each user's first sign-in; a batch admin endpoint is a Sub-11d nicety.
- **Cutover automation.** The `entra-id-cutover.md` runbook is operator-driven (12 steps). Sub-11e or Sub-12 could fold key steps into `deploy.ps1 -Environment <env> -CutoverIdp` for reproducibility.

## Known limitations

- **Multi-tenant Conditional Access boundary.** CCE has no authority over partner tenants' CA policies; partner-tenant users are bound by **their own** tenant's MFA rules. Documented in ADR-0060.
- **Branding renders only for CCE-tenant users.** Multi-tenant partner users see their own home-tenant sign-in branding. Hard Entra ID security boundary, not configurable.
- **`Configure-Branding.ps1` requires Entra ID P1 or P2.** Tenant without P1/P2 → script logs warning and exits 0. Branding step is skipped without failing the deploy pipeline.
- **App-role assignment is manual.** Operators assign users to `cce-admin`/`cce-editor`/etc. via Azure portal or `Microsoft.Graph.PowerShell`. No dynamic-group-based assignment per ADR-0059.

## Key reference links

- **Plan files**:
  - [Master plan](./superpowers/plans/2026-05-04-sub-11.md)
  - [Phase 00 detail](./superpowers/plans/2026-05-04-sub-11/phase-00-backend-foundation.md)
  - [Phase 01 detail](./superpowers/plans/2026-05-04-sub-11/phase-01-graph-registration.md)
  - [Phase 02 detail](./superpowers/plans/2026-05-04-sub-11/phase-02-app-registration.md)
  - [Phase 03 detail](./superpowers/plans/2026-05-04-sub-11/phase-03-frontend-changes.md)
  - [Phase 04 detail](./superpowers/plans/2026-05-04-sub-11/phase-04-cutover.md)
- **ADRs**: [0058](./adr/0058-entra-id-multi-tenant-graph-writes.md), [0059](./adr/0059-app-roles-vs-security-groups.md), [0060](./adr/0060-conditional-access-for-mfa.md), [0055 (Superseded)](./adr/0055-ad-federation-via-keycloak-ldap.md).
- **Runbooks**: [entra-id-cutover.md](./runbooks/entra-id-cutover.md), [entra-id-troubleshooting.md](./runbooks/entra-id-troubleshooting.md).
- **Operator docs**: [`infra/entra/README.md`](../infra/entra/README.md).
- **Spec**: [`docs/superpowers/specs/2026-05-04-sub-11-design.md`](./superpowers/specs/2026-05-04-sub-11-design.md).
