# DDD Implementation Plan

## Overview

This document defines the architecture, patterns, and rules for implementing Domain-Driven Design in a blog/social media platform with moderation. Every decision here was made based on the specific needs of this project — not theory for theory's sake.

---

## Layer Structure

```
Domain          → Aggregates, Entities, Value Objects, Events, Repository Interfaces
Application     → Commands, Queries, DTOs, IAppDbContext
Infrastructure  → Repository Implementations, AppDbContext, EF Configuration
API             → Controllers, minimal pass-through to handlers
```

### Dependency Direction
```
API → Application → Domain ← Infrastructure
```
Infrastructure points inward toward Domain — never the other way around.

---

## Base Class Hierarchy

```
Entity<TId>                     → Id + equality
    └── AuditableEntity<TId>    → + CreatedAt/By, UpdatedAt/By
            └── SoftDeleteEntity<TId>   → + IsDeleted, DeletedAt/By, Restore()
                    └── AggregateRoot<TId>   → + DomainEvents
```

### What each level adds

| Class | Responsibility |
|---|---|
| `Entity<TId>` | Identity and equality only |
| `AuditableEntity<TId>` | Who created/updated and when |
| `SoftDeleteEntity<TId>` | Soft delete + restore logic |
| `AggregateRoot<TId>` | Domain event dispatching |

### Rules
- Every layer adds **one responsibility only** — this is intentional SRP
- `TId` is constrained to `IEquatable<TId>` — no unconstrained generic ids
- `SoftDeleteEntity.Delete()` automatically calls `SetUpdated()` — no manual audit on delete
- `SoftDeleteEntity.Restore()` clears delete fields and calls `SetUpdated()` — full consistency

---

## Domain Layer

### Aggregates → inherit `AggregateRoot<Guid>`

Use when the entity:
- Has its own lifecycle with meaningful stages
- Has its own repository
- Raises domain events
- Can be fetched independently

```
Post            → Draft → UnderReview → Approved/Rejected → SoftDeleted
Comment         → UnderReview → Approved/Rejected → SoftDeleted
Form            → Created → Published → Archived → SoftDeleted
FormSubmission  → Submitted → Reviewed → Closed
User            → Registered → Activated → Deactivated
```

### Child Entities → inherit `AuditableEntity<Guid>`

Use when the entity:
- Only exists inside an aggregate
- Has no lifecycle of its own
- Is never fetched independently
- Is created/removed by the aggregate

```
PostTag         → owned by Post
PostImage       → owned by Post
PostLike        → owned by Post
FormField       → owned by Form
UserRole        → owned by User
UserFollow      → owned by User
```

### Special Case — ApplicationUser

Cannot inherit `AggregateRoot` due to `IdentityUser` base class. Implements interfaces manually:

```csharp
public class ApplicationUser : IdentityUser, ISoftDeletable, IAuditable
{
    // manual implementation — isolated exception, not a pattern
}
```

### Moderation Status

Every content aggregate uses `ModerationStatus`:

```csharp
public enum ModerationStatus
{
    Draft,
    UnderReview,
    Approved,
    Rejected
}
```

### Domain Events

Every meaningful state change raises a domain event:

```
PostCreatedEvent
PostSubmittedEvent
PostApprovedEvent
PostRejectedEvent
PostDeletedEvent
```

Events are dispatched automatically by the EF Core interceptor after `SaveChangesAsync` — handlers never dispatch manually.

### Aggregate Rules

- **Private setters** on all properties — domain owns its state
- **Factory method** (`Post.Create(...)`) instead of public constructor
- **Guard conditions** inside domain methods — fail fast, fail explicitly
- **Child entities created through aggregate** — never `new PostTag()` from outside
- **Reference other aggregates by Id** — never by navigation property

```csharp
// ✅ Correct
public Guid AuthorId { get; private set; }

// ❌ Wrong
public User Author { get; private set; }
```

---

## Repository Pattern

### Generic Repository — kills duplication

```csharp
public interface IRepository<T, TId>
    where T : AggregateRoot<TId>
    where TId : IEquatable<TId>
{
    Task<T?> GetByIdAsync(TId id);
    Task AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
}
```

### Specific Repository — only when aggregate needs extra queries

```csharp
public interface IPostRepository : IRepository<Post, Guid>
{
    Task<List<Post>> GetPendingModerationAsync();
    Task<bool> ExistsByTitleAsync(string title);
}
```

### Decision tree

```
Does the aggregate need custom queries?
    Yes → create specific repo extending generic
    No  → inject IRepository<T, TId> directly, no specific repo needed
```

### Rules
- **Repositories for Aggregates only** — never for child entities
- **Repository returns domain objects** — never DTOs
- **Repository has zero business logic** — fetch and save only
- **No `SaveChangesAsync` inside repository** — that belongs to the handler

---

## Application Layer

### CQRS Split

```
Write side  →  Command Handlers  →  use Repository
Read side   →  Query Handlers    →  use IAppDbContext directly
```

### Command Handler Pattern

```
1. Fetch aggregate via repository
2. Guard — throw if not found
3. Call domain method — business logic stays in domain
4. Persist via repository
5. SaveChangesAsync — commits everything
```

Domain events are dispatched automatically after step 5 — no manual dispatch.

### Query Handler Pattern

```
1. Inject IAppDbContext directly — no repository
2. Write optimized LINQ with Select projection
3. Return DTO — never a domain object
```

### Rules

- **Commands** use repository, return nothing or an Id
- **Queries** use `IAppDbContext` directly, return DTOs
- **No business logic in handlers** — handlers orchestrate, domain decides
- **No domain objects returned from queries** — always project to DTO
- **No service layer** — handlers call domain methods directly

---

## Why No Service Layer

A service layer between handler and domain adds indirection with zero value when logic touches a single aggregate:

```
❌ Handler → Service → Domain → Repository  (pass-through service)
✅ Handler → Domain → Repository            (direct, clean)
```

Domain Services are only justified when:
- Logic spans **multiple aggregates**
- No single aggregate owns the coordination

```csharp
// ✅ Legitimate domain service — two aggregates involved
public class ModerationDomainService
{
    public void Approve(Post post, AdminProfile admin)
    {
        post.Approve(admin.Id);
        admin.RecordModeration(post.Id);
    }
}
```

---

## Infrastructure Layer

### IAppDbContext — is the Unit of Work

```csharp
public interface IAppDbContext
{
    DbSet<Post> Posts { get; }
    DbSet<Comment> Comments { get; }
    DbSet<Form> Forms { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

`DbContext` already implements `IDisposable` — do not add it to `IAppDbContext`. DI handles disposal at end of request automatically.

### EF Core Interceptor — auto audit + soft delete

Interceptor runs on every `SaveChangesAsync`:
- Sets `CreatedAt/By` on new entities
- Sets `UpdatedAt/By` on modified entities
- Intercepts hard deletes and converts to soft delete
- Dispatches domain events after commit

### Global Query Filters

```csharp
// Applied to every query automatically
modelBuilder.Entity<Post>().HasQueryFilter(p => !p.IsDeleted);
```

No manual `!p.IsDeleted` in every query.

---

## Audit Trail — How It Works

Every admin action is automatically recorded:

```
Post created by author    → CreatedBy = authorId,  CreatedAt = timestamp
Post approved by admin    → UpdatedBy = adminId,   UpdatedAt = timestamp
Post deleted by admin     → DeletedBy = adminId,   DeletedAt = timestamp
                          → UpdatedBy = adminId,   UpdatedAt = timestamp (automatic)
```

`SetUpdated` is called automatically inside `Delete()` and `Restore()` — no manual calls needed anywhere.

---

## What Inherits What — Full Map

```csharp
// Full chain — lifecycle + soft delete + audit + events
public class Post           : AggregateRoot<Guid> { }
public class Comment        : AggregateRoot<Guid> { }
public class Form           : AggregateRoot<Guid> { }
public class FormSubmission : AggregateRoot<Guid> { }
public class User           : AggregateRoot<Guid> { }

// Audit only — no lifecycle, no soft delete, no events
public class PostTag        : AuditableEntity<Guid> { }
public class PostImage      : AuditableEntity<Guid> { }
public class PostLike       : AuditableEntity<Guid> { }
public class FormField      : AuditableEntity<Guid> { }
public class UserRole       : AuditableEntity<Guid> { }
public class UserFollow     : AuditableEntity<Guid> { }

// Special case
public class ApplicationUser : IdentityUser, ISoftDeletable, IAuditable { }
```

---

## Rules Summary

| Rule | Reason |
|---|---|
| Repository for Aggregates only | Child entities have no independent lifecycle |
| Handler calls domain methods directly | No pass-through service layer |
| Queries use DbContext directly | Optimized projection, no full aggregate load |
| Domain objects never leave application layer | Queries always return DTOs |
| Business logic lives in domain only | Prevents scatter across services |
| Private setters on all aggregate properties | Domain owns its state |
| Factory methods instead of public constructors | Enforces invariants on creation |
| Guard conditions in every domain method | Fail fast, fail explicitly |
| Domain events raised in domain methods | Automatic dispatch, no manual wiring |
| SaveChangesAsync in handler only | Repository never commits |

---

## Anti-Patterns to Avoid

| Anti-Pattern | Why |
|---|---|
| Public setters on domain objects | Anyone sets anything, logic scatters |
| Business logic in services | Anemic domain, service becomes god class |
| Returning domain objects from queries | Couples read side to write model |
| Repository returning DTOs | Breaks separation of read/write |
| `new ChildEntity()` outside aggregate | Bypasses aggregate consistency boundary |
| Navigation properties to other aggregates | Creates hidden coupling between aggregates |
| SaveChangesAsync inside repository | Loses transactional control in handler |
| Hard delete on any aggregate | Loses audit trail and recoverability |
