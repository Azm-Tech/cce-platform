# ADR-0024: ASP.NET Identity tables coexist with CCE entities

- **Status:** Accepted
- **Date:** 2026-04-28
- **Sub-project owner:** Data & Domain
- **Spec ref:** [Data & Domain §3.1, §4.1](../../project-plan/specs/2026-04-27-data-domain-design.md)

## Context

External users authenticate via Keycloak (dev) / ADFS (prod) and the app stores their profile in our DB. Two options: maintain Identity in a separate connection / DB, or co-locate Identity tables with CCE entities.

## Decision

ASP.NET Identity tables (`asp_net_users`, `asp_net_roles`, `asp_net_user_roles`, etc.) live in the same DB as CCE entities, mapped via `IdentityDbContext<User, Role, Guid>`. The `User` entity in CCE.Domain extends `IdentityUser<Guid>` and adds CCE profile fields (LocalePreference, KnowledgeLevel, Interests, CountryId, AvatarUrl).

## Consequences

### Positive
- One DB connection, one transaction, one migration history.
- FK from CCE entities to `users` is a real referential constraint (not a stale Guid).
- AuditingInterceptor catches Identity-related state changes (role assignments, claim changes).

### Negative
- CCE.Domain references `Microsoft.Extensions.Identity.Stores` (extends Clean Architecture — see ADR-0014 / ADR-0019).
- Identity table names use `asp_net_*` prefix because Identity's defaults dominate the snake-case naming convention.
