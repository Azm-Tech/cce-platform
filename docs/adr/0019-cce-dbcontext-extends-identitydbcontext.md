# ADR-0019: Single CceDbContext extending IdentityDbContext

- **Status:** Accepted
- **Date:** 2026-04-28
- **Sub-project owner:** Data & Domain
- **Spec ref:** [Data & Domain §3.1, §3.2](../superpowers/specs/2026-04-27-data-domain-design.md)

## Context

The CCE platform needs ASP.NET Identity tables (users, roles, claims) AND ~33 CCE-specific entities to coexist in one schema with one transactional boundary. Two patterns existed: split DbContext (IdentityDbContext + CceDbContext) sharing a connection, or single DbContext extending IdentityDbContext.

## Decision

`CceDbContext : IdentityDbContext<User, Role, Guid>`. All 36 entities live in one DbContext, one connection, one SaveChanges transaction.

## Consequences

### Positive
- Single migration, single transaction — atomic schema across Identity + CCE.
- AuditingInterceptor sees every change (Identity + CCE) in one ChangeTracker.
- Less DI plumbing.

### Negative
- Domain layer references `Microsoft.Extensions.Identity.Stores` (User extends IdentityUser<Guid>). Trade-off accepted in ADR-0014 (Clean Architecture exemption).
- `CceDbContext` is a large class (35 DbSets). Mitigated by per-entity `IEntityTypeConfiguration<T>` files.
