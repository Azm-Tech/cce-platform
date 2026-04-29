# ADR-0027: JIT user sync from Keycloak claims

**Status:** Accepted
**Date:** 2026-04-29
**Sub-project:** 03 — Internal API

## Context

The Internal API authenticates users via JWTs issued by Keycloak (dev) or ADFS (prod). The JWT carries a `sub` claim (Guid) and group/role memberships. CCE has its own `users` table for foreign keys (audit log, content authorship, role assignments). We need each authenticated admin's `users` row to exist before any handler runs that joins on UserId.

## Decision

A `UserSyncMiddleware` runs after `UseAuthentication` + `UseAuthorization` on every authenticated request. On first hit per `sub`, it inserts a `User` row (Email, UserName, default LocalePreference) and maps Keycloak `groups` claims to CCE roles via `IConfiguration` (`UserSync:GroupToRoleMap`). An `IMemoryCache` (5-minute TTL, keyed by `sub`) gates subsequent requests so we don't hit the DB every time.

The middleware delegates DB work to `IUserSyncService` (Application interface, Infrastructure implementation) — keeping `CCE.Api.Common` out of EF tracker territory and respecting Clean Architecture layering.

## Consequences

- ✅ Admins can log in without a manual provisioning step; the first authenticated request creates their row.
- ✅ Layering preserved: middleware in Api.Common, DB writes in Infrastructure.
- ✅ Memory cache dramatically reduces DB load (one DB hit per user per 5 minutes).
- ⚠ The 5-minute cache window means a role change in Keycloak doesn't take effect for up to 5 minutes — acceptable for Phase 0.
- ⚠ If two requests for a brand-new user race, both might attempt INSERT. The DB unique constraint on `Id` makes the second one a no-op (idempotent failure). The middleware logs the conflict but doesn't surface a 500.
