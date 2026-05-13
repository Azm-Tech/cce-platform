# ADR-0055 — AD federation via Keycloak LDAP user federation

> **STATUS: Superseded by [ADR-0058](./0058-entra-id-multi-tenant-graph-writes.md) on 2026-05-04.**
> Sub-11 retires Keycloak and replaces it with Entra ID multi-tenant + Entra ID Connect from on-prem AD. The decisions in this ADR no longer apply; see ADR-0058 for the current architecture. The Phase 04 cutover (Sub-11) deletes the Keycloak surface (`infra/keycloak/`, `KeycloakLdapFederationTests`, `Testcontainers.Keycloak`).

**Status:** Superseded by ADR-0058
**Date:** 2026-05-04
**Deciders:** Sub-10c brainstorm (kilany113@gmail.com)
**Sub-project:** [Sub-10c — Production infra + DR](../../project-plan/specs/2026-05-04-sub-10c-design.md)

## Context

CCE has used Keycloak as its IdP since Sub-1 (foundation). Both APIs (`Api.External`, `Api.Internal`) authorize via Keycloak roles; backend permissions are enforced via the `permissions.yaml` matrix from CCE.Domain.

IDD v1.2 specifies AD on `cce.local` ports 389/636 (raw LDAP). Sub-10c's task: federate Keycloak with AD so users keep their AD credentials and AD security groups drive Keycloak roles.

## Decision

Use Keycloak's built-in **LDAP user federation provider**, **read-only**, against `ldaps://${LDAP_HOST}:636`. AD security groups → Keycloak roles via Keycloak's group mapper (`group-ldap-mapper`).

**Considered alternatives:**

- **LDAP user federation + Kerberos SSO (SPNEGO):** rejected for Sub-10c. SPNEGO adds AD service-principal + keytab management + AD-side SPN registration — separate ops team usually owns those. Documented as a Sub-10c+ enhancement; the federation provider config supports flipping `allowKerberosAuthentication=true` later without breaking changes.
- **AD FS / Azure AD as an OIDC broker:** rejected. IDD specifies raw LDAP (389/636); brokering through AD FS or Azure AD would introduce a federation hop the IDD doesn't call for and a cloud dependency the IDD doesn't pre-provision.
- **Read-write LDAP federation:** rejected. Keycloak writing to AD requires elevated bind credentials and creates a second source of truth for user data. Read-only is the safe default; user creation stays in AD admin tooling.

**Why read-only:**

- AD is the system of record for users. Keycloak imports + caches; password validation hits AD on each login.
- One-way trust simplifies recovery: a Keycloak failure doesn't corrupt AD.
- Aligns with the "least-privilege bind credential" principle — `cce-keycloak-svc` only needs read on the user/group OUs.

**Group → role mapping** (committed in `infra/keycloak/realm-cce-ldap-federation.json`):

| AD security group | Keycloak role | Backend permission |
|---|---|---|
| `CCE-Admins` | `cce-admin` | full content + admin |
| `CCE-Editors` | `cce-editor` | content edit / publish |
| `CCE-Reviewers` | `cce-reviewer` | content review / approve |
| `CCE-Experts` | `cce-expert` | expert-profile self-service |
| `CCE-Users` | `cce-user` | read-only public surfaces |

The mapping is the contract. AD admins create/manage groups; CCE doesn't write to AD.

## Provisioning

`infra/keycloak/apply-realm.ps1` is the idempotent provisioner. Reads `KEYCLOAK_ADMIN_*` + `LDAP_*` from the env-file, acquires a master-admin token, looks up the federation provider by name, PUTs to update or POSTs to create. Re-runnable; CI tests prove this against a Testcontainers Keycloak.

## Consequences

**Positive:**
- Users keep AD credentials; zero workflow change.
- Group → role mapping is declarative + committed; review-able in PRs.
- Provisioning is idempotent; re-deploys re-apply without duplicating components.
- Read-only stance simplifies the security review (Keycloak can't corrupt AD).
- Path to SPNEGO/Kerberos SSO is open (config flip + keytab provisioning); doesn't require schema changes.

**Negative / accepted:**
- AD service-account password (`LDAP_BIND_PASSWORD`) is a long-lived secret. Rotation is documented in `secret-rotation.md`; Sub-10d may graduate to managed identities.
- LDAPS cert validation requires the AD CA to be trusted by Keycloak's truststore. Operator runbook documents the import procedure.
- AD outage halts new logins (cached tokens still work until expiry). Sub-10c+ HA could mirror federation state, but at single-host scale we accept this.
- `validatePasswordPolicy` must be `false` with `editMode=READ_ONLY` (Keycloak rejects `true` in this combination). Documented in the realm JSON.

**Out of scope (Sub-10c+):**
- SPNEGO/Kerberos SSO for AD-joined clients.
- Group-attribute mappers beyond name → role (e.g., dept → tenant).
- AD writes from Keycloak.
- Federation against AD FS or Azure AD.

## References

- [Sub-10c design spec §Identity](../../project-plan/specs/2026-05-04-sub-10c-design.md#identity--keycloak-ldap-user-federation)
- [AD federation runbook](../runbooks/ad-federation.md)
- [Keycloak User Federation docs](https://www.keycloak.org/docs/latest/server_admin/#_user-storage-federation)
- ADR-0054 — IIS reverse proxy on Windows Server (Sub-10c)
- ADR-0056 — Backup strategy: Ola Hallengren (Sub-10c)
