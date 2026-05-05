# ADR-0059 — App roles vs security groups for permission mapping

**Date:** 2026-05-04
**Status:** Accepted
**Decision-makers:** CCE Architecture, Sub-11 brainstorm 2026-05-04

## Context

CCE permissions historically mapped from Keycloak realm roles (`SuperAdmin`, `ContentEditor`, `ExpertReviewer`, etc.) into the `permissions.yaml` matrix consumed by `RoleToPermissionClaimsTransformer`. Sub-11 swaps Keycloak for Entra ID, and Entra ID offers two competing mechanisms for the same shape: **app roles** (declared in the app registration's `appRoles[]`) and **security groups** (membership-based, emitted in the `groups` claim).

## Decision

CCE uses **app roles** (`appRoles[]` in the app registration) to drive permissions. The token's `roles` claim is the authoritative input to `RoleToPermissionClaimsTransformer`. Security groups are NOT consumed by the platform.

The 5 app roles are: `cce-admin`, `cce-editor`, `cce-reviewer`, `cce-expert`, `cce-user`.

## Rationale

- **App roles are app-scoped, groups are tenant-scoped.** The `cce-editor` role only means anything inside CCE; a tenant-scoped `Marketing` group might mean unrelated things in other apps. App roles keep authorization semantics local to CCE.
- **Multi-tenant compatibility.** Group membership in a partner tenant doesn't propagate to CCE's app — but app-role assignments do (admin consent + admin assignment in the partner's tenant). Groups would require per-tenant claim-mapping policies.
- **Token size.** Groups emit ALL group memberships into the `groups` claim. For a user in a typical Microsoft Entra tenant, that's 20–200 GUIDs. The token bloats past Entra ID's 6 KB soft cap and spills into a separate `_claim_names` reference, which the BFF then has to dereference via Graph. App roles emit only the 1–5 assigned values into `roles` — no spillover.
- **Existing transformer adapts cleanly.** Phase 03 updates `RoleToPermissionClaimsTransformer` to consume `roles` (was `groups`) with role names matching `appRoles[].value` (was `SuperAdmin`-style names). The mapping table in `permissions.yaml` rewrites accordingly.

## Consequences

- **Operators must assign app roles in the Azure portal** (or via PowerShell / Microsoft Graph Explorer) per user. Group-based assignment via dynamic membership rules is NOT supported.
- **Phase 03 rewrites `permissions.yaml`** with `cce-admin`-style role names instead of `SuperAdmin`-style. The matrix shape is unchanged.
- **Phase 00's `RoleClaimMappingTests` (2 tests, deferred from Phase 00 to Phase 03)** land in Phase 03 alongside the transformer rewrite.

## Status

Accepted.
