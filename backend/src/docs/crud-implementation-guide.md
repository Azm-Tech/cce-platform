# CRUD Implementation Guide — CCE Project Patterns

## Overview

This document captures the **architectural patterns and conventions** used in the CCE project for implementing CRUD features. It is based on the Service Evaluation (US018) implementation and the team leader's requirements for the FAQ CRUD.

### Core Principles

| Principle | Description |
|---|---|
| **Clean Architecture** | Domain → Application → Infrastructure → API (4 layers) |
| **CQRS** | Separate Command (write) and Query (read) via MediatR |
| **Unit of Work** | Repository tracks, handler commits (`ICceDbContext.SaveChangesAsync`) |
| **Reads → ICceDbContext** | All read operations inject `ICceDbContext` directly, no repository |
| **Writes → Repository** | Write operations use repository interface + domain factory |
| **Response Envelope** | Every endpoint returns `Response<T>` via `MessageFactory` |
| **No validation in endpoints** | All validation is in FluentValidation validators only |

---

## Table of Contents

1. [Step-by-Step: Complete CRUD Creation](#step-by-step-complete-crud-creation)
2. [Pattern: Write-Only Repository + Unit of Work](#pattern-write-only-repository--unit-of-work)
3. [Pattern: Read via ICceDbContext](#pattern-read-via-iccedbcontext)
4. [Pattern: Generic Repository for Write Operations (Update/Delete)](#pattern-generic-repository-for-write-operations-updatedelete)
5. [Pattern: Response\<T\> Envelope + MessageFactory](#pattern-response-t-envelope--messagefactory)
6. [Pattern: FluentValidation + ERR900 Handling](#pattern-fluentvalidation--err900-handling)
7. [Pattern: ToHttpResult for Endpoints](#pattern-tohttpresult-for-endpoints)
8. [Pattern: Pagination with PagedResult\<T\>](#pattern-pagination-with-pagedresultt)
9. [Pattern: Enum Handling (int Request, String Response)](#pattern-enum-handling-int-request-string-response)
10. [Pattern: Anonymous Users + Nullable CreatedById](#pattern-anonymous-users--nullable-createdbyid)
11. [Pattern: Error/Success Codes (SystemCode, ApplicationErrors, Resources.yaml)](#pattern-errorsuccess-codes)
12. [Pattern: LocalizedText Value Object](#pattern-localizedtext-value-object)
13. [Pattern: SuperAdmin Authorization](#pattern-superadmin-authorization)
14. [Pattern: Domain Factory + Mutation Methods](#pattern-domain-factory--mutation-methods)
15. [Pattern: Mapping (DTOs)](#pattern-mapping-dtos)
16. [File Checklist](#file-checklist)
17. [Common Pitfalls](#common-pitfalls)

---

## Step-by-Step: Complete CRUD Creation

### Step 1 — Domain Layer

Create the entity and any value objects/enums.

**Entity:**
```csharp
// CCE.Domain\YourDomain\YourEntity.cs
public sealed class YourEntity : AuditableEntity<Guid>
{
    // Properties — private set for immutability via domain methods
    public string Name { get; private set; }
    public int Order { get; private set; }

    // EF Core materialization constructor
    private YourEntity() : base(Guid.NewGuid()) { }

    // Domain factory
    public static YourEntity Create(string name, int order, Guid by, ISystemClock clock)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Name is required.");

        var entity = new YourEntity { Name = name, Order = order };
        entity.MarkAsCreated(by, clock);
        return entity;
    }

    // Domain mutation
    public void Update(string name, int order, Guid by, ISystemClock clock)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Name is required.");

        Name = name;
        Order = order;
        MarkAsModified(by, clock);
    }
}
```

**Entity with enum:**
```csharp
// CCE.Domain\YourDomain\YourRating.cs
public enum YourRating
{
    None = 0,       // Sentinel — always rejected by validation
    Good = 1,
    Bad = 2,
}
```

**Entity with value object:**
```csharp
// CCE.Domain\YourDomain\YourEntity.cs
public sealed class YourEntity : AuditableEntity<Guid>
{
    public LocalizedText Title { get; private set; }
    public LocalizedText Description { get; private set; }

    private YourEntity() : base(Guid.NewGuid()) { }

    public static YourEntity Create(LocalizedText title, LocalizedText description, Guid by, ISystemClock clock)
    {
        var entity = new YourEntity { Title = title, Description = description };
        entity.MarkAsCreated(by, clock);
        return entity;
    }

    public void Update(LocalizedText title, LocalizedText description, Guid by, ISystemClock clock)
    {
        Title = title;
        Description = description;
        MarkAsModified(by, clock);
    }
}
```

---

### Step 2 — Application Layer: Write-Side (Command)

**Write-only repository interface:**
```csharp
// CCE.Application\YourDomain\IYourEntityRepository.cs
public interface IYourEntityRepository
{
    Task AddAsync(YourEntity entity, CancellationToken ct = default);
}
```

> **Note:** For Update/Delete operations that need to fetch first, use `IRepository<T, TId>` directly instead of creating a repository interface. See [Pattern: Generic Repository](#pattern-generic-repository-for-write-operations-updatedelete).

**Command:**
```csharp
// CCE.Application\YourDomain\Commands\CreateYourEntity\CreateYourEntityCommand.cs
public sealed record CreateYourEntityCommand(
    string Name,
    int Order
) : IRequest<Response<VoidData>>;
```

**Command Handler:**
```csharp
// CCE.Application\YourDomain\Commands\CreateYourEntity\CreateYourEntityCommandHandler.cs
using CCE.Application.Common.Interfaces;
using CCE.Application.Messages;

internal sealed class CreateYourEntityCommandHandler(
    IYourEntityRepository _repo,
    ICceDbContext _db,
    ICurrentUserAccessor _currentUser,
    ISystemClock _clock,
    MessageFactory _msg)
    : IRequestHandler<CreateYourEntityCommand, Response<VoidData>>
{
    public async Task<Response<VoidData>> Handle(CreateYourEntityCommand cmd, CancellationToken ct)
    {
        var userId = _currentUser.GetUserId();
        // For endpoints with [AllowAnonymous], userId may be null
        // Domain factory requires non-null, so handle accordingly:
        if (userId is null)
            return _msg.Unauthorized<VoidData>("NOT_AUTHENTICATED");

        var entity = YourEntity.Create(cmd.Name, cmd.Order, userId.Value, _clock);
        await _repo.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);  // Unit of Work — single commit point

        return _msg.Ok("YOUR_ENTITY_CREATED");
    }
}
```

**For Create with anonymous access (no user required):**
```csharp
public async Task<Response<VoidData>> Handle(CreateYourEntityCommand cmd, CancellationToken ct)
{
    var userId = _currentUser.GetUserId();  // null for anonymous

    var entity = YourEntity.Create(cmd.Name, cmd.Order);  // factory without user
    // OR:
    var entity = YourEntity.Create(cmd.Name, cmd.Order, userId, _clock);
    // where factory handles null userId gracefully

    await _repo.AddAsync(entity, ct);
    await _db.SaveChangesAsync(ct);

    return _msg.Ok("YOUR_ENTITY_CREATED");
}
```

**FluentValidation Validator:**
```csharp
// CCE.Application\YourDomain\Commands\CreateYourEntity\CreateYourEntityCommandValidator.cs
internal sealed class CreateYourEntityCommandValidator : AbstractValidator<CreateYourEntityCommand>
{
    public CreateYourEntityCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode("REQUIRED_FIELD")
            .MaximumLength(200).WithErrorCode("MAX_LENGTH");

        RuleFor(x => x.Order)
            .GreaterThan(0).WithErrorCode("INVALID_VALUE");
    }
}
```

---

### Step 3 — Application Layer: Write-Side (Update/Delete)

**For Update/Delete, use `IRepository<T, TId>` (generic interface):**
```csharp
// No custom repository interface needed — use generic IRepository<T,TId>
// Located at: CCE.Application\Common\Interfaces\IRepository.cs
public interface IRepository<T, TId>
    where T : Entity<TId>
    where TId : IEquatable<TId>
{
    Task<T?> GetByIdAsync(TId id, CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Delete(T entity);
}
```

**Update Command Handler:**
```csharp
internal sealed class UpdateYourEntityCommandHandler(
    IRepository<YourEntity, Guid> _repo,
    ICceDbContext _db,
    ICurrentUserAccessor _currentUser,
    ISystemClock _clock,
    MessageFactory _msg)
    : IRequestHandler<UpdateYourEntityCommand, Response<VoidData>>
{
    public async Task<Response<VoidData>> Handle(UpdateYourEntityCommand cmd, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(cmd.Id, ct);
        if (entity is null)
            return _msg.NotFound<VoidData>("YOUR_ENTITY_NOT_FOUND");

        var userId = _currentUser.GetUserId();
        if (userId is null)
            return _msg.Unauthorized<VoidData>("NOT_AUTHENTICATED");

        entity.Update(cmd.Name, cmd.Order, userId.Value, _clock);
        // No need to call _repo.Update() — EF tracks changes automatically
        // when the entity was fetched via GetByIdAsync (same DbContext)
        await _db.SaveChangesAsync(ct);

        return _msg.Ok("YOUR_ENTITY_UPDATED");
    }
}
```

**Delete Command Handler:**
```csharp
internal sealed class DeleteYourEntityCommandHandler(
    IRepository<YourEntity, Guid> _repo,
    ICceDbContext _db,
    MessageFactory _msg)
    : IRequestHandler<DeleteYourEntityCommand, Response<VoidData>>
{
    public async Task<Response<VoidData>> Handle(DeleteYourEntityCommand cmd, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(cmd.Id, ct);
        if (entity is null)
            return _msg.NotFound<VoidData>("YOUR_ENTITY_NOT_FOUND");

        _repo.Delete(entity);    // Marks for deletion
        await _db.SaveChangesAsync(ct);  // Unit of Work commit

        return _msg.Ok("YOUR_ENTITY_DELETED");
    }
}
```

---

### Step 4 — Application Layer: Read-Side (Queries)

**Queries inject `ICceDbContext` directly — no repository involvement.**

**DTO:**
```csharp
// CCE.Application\YourDomain\DTOs\YourEntityDto.cs
public sealed record YourEntityDto(
    Guid Id,
    string Name,
    int Order,
    DateTimeOffset CreatedOn,
    Guid? CreatedById
);
```

**GetById Query:**
```csharp
// CCE.Application\YourDomain\Queries\GetYourEntityById\GetYourEntityByIdQuery.cs
public sealed record GetYourEntityByIdQuery(Guid Id) : IRequest<Response<YourEntityDto>>;

// Handler:
internal sealed class GetYourEntityByIdQueryHandler(
    ICceDbContext _db,
    MessageFactory _msg)
    : IRequestHandler<GetYourEntityByIdQuery, Response<YourEntityDto>>
{
    public async Task<Response<YourEntityDto>> Handle(GetYourEntityByIdQuery q, CancellationToken ct)
    {
        var entity = await _db.Set<YourEntity>()
            .Where(e => e.Id == q.Id)
            .Select(e => new YourEntityDto(
                e.Id, e.Name, e.Order, e.CreatedOn, e.CreatedById))
            .FirstOrDefaultAsync(ct);

        if (entity is null)
            return _msg.NotFound<YourEntityDto>("YOUR_ENTITY_NOT_FOUND");

        return _msg.Ok(entity, "ITEMS_LISTED");
    }
}
```

**GetAll (paginated) Query:**
```csharp
// CCE.Application\YourDomain\Queries\GetAllYourEntities\GetAllYourEntitiesQuery.cs
public sealed record GetAllYourEntitiesQuery(
    int Page = 1,
    int PageSize = 20
) : IRequest<Response<PagedResult<YourEntityDto>>>;

// Handler:
internal sealed class GetAllYourEntitiesQueryHandler(
    ICceDbContext _db,
    MessageFactory _msg)
    : IRequestHandler<GetAllYourEntitiesQuery, Response<PagedResult<YourEntityDto>>>
{
    public async Task<Response<PagedResult<YourEntityDto>>> Handle(
        GetAllYourEntitiesQuery q, CancellationToken ct)
    {
        var result = await _db.Set<YourEntity>()
            .OrderByDescending(e => e.CreatedOn)
            .Select(e => new YourEntityDto(
                e.Id, e.Name, e.Order, e.CreatedOn, e.CreatedById))
            .ToPagedResultAsync(q.Page, q.PageSize, ct);

        return _msg.Ok(result, "ITEMS_LISTED");
    }
}
```

> `ToPagedResultAsync` is defined in `CCE.Application.Common.Pagination.PaginationExtensions`. It clamps `page >= 1` and `pageSize` to `[1, 100]`. See [Pattern: Pagination](#pattern-pagination-with-pagedresultt).

**GetAll (non-paginated) Query:**
```csharp
public async Task<Response<List<YourEntityDto>>> Handle(
    GetAllYourEntitiesQuery q, CancellationToken ct)
{
    var items = await _db.Set<YourEntity>()
        .OrderBy(e => e.Order)
        .Select(e => new YourEntityDto(
            e.Id, e.Name, e.Order, e.CreatedOn, e.CreatedById))
        .ToListAsync(ct);

    return _msg.Ok(items, "ITEMS_LISTED");
}
```

---

### Step 5 — Infrastructure Layer

**EF Core Configuration:**
```csharp
// CCE.Infrastructure\Persistence\Configurations\YourDomain\YourEntityConfiguration.cs
internal sealed class YourEntityConfiguration : IEntityTypeConfiguration<YourEntity>
{
    public void Configure(EntityTypeBuilder<YourEntity> builder)
    {
        builder.ToTable("your_entities");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Order)
            .IsRequired();

        builder.Property(e => e.CreatedOn)
            .IsRequired();

        // Index on CreatedOn for ordered queries
        builder.HasIndex(e => e.CreatedOn)
            .HasDatabaseName("ix_your_entity_created_on");
    }
}
```

**For LocalizedText (owned entity):**
```csharp
builder.OwnsOne(e => e.Title, nav =>
{
    nav.Property(t => t.Ar).IsRequired().HasColumnName("title_ar");
    nav.Property(t => t.En).IsRequired().HasColumnName("title_en");
});

builder.OwnsOne(e => e.Description, nav =>
{
    nav.Property(t => t.Ar).IsRequired().HasColumnName("description_ar");
    nav.Property(t => t.En).IsRequired().HasColumnName("description_en");
});
```

**For enum (int conversion):**
```csharp
builder.Property(e => e.Rating)
    .IsRequired()
    .HasConversion<int>();
```

**Concrete Repository (for custom write-only repository pattern):**
```csharp
// CCE.Infrastructure\YourDomain\YourEntityRepository.cs
public sealed class YourEntityRepository : IYourEntityRepository
{
    private readonly CceDbContext _db;

    public YourEntityRepository(CceDbContext db) => _db = db;

    public async Task AddAsync(YourEntity entity, CancellationToken ct)
        => await _db.Set<YourEntity>().AddAsync(entity, ct);
    // NOTE: No SaveChangesAsync here — handler calls _db.SaveChangesAsync()
}
```

**When using generic `Repository<T, TId>`, no implementation needed — it's already registered in DI:**
```csharp
// Already in DependencyInjection.cs:
services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
```

**Migration:**
```powershell
dotnet ef migrations add AddYourEntity --context CceDbContext --startup-project ../CCE.Api.Internal
```

---

### Step 6 — Error/Success Codes & Localization

**ApplicationErrors.cs — constant domain keys:**
```csharp
// CCE.Application\Errors\ApplicationErrors.cs
public static class YourEntity
{
    public const string YOUR_ENTITY_NOT_FOUND = "YOUR_ENTITY_NOT_FOUND";
    public const string YOUR_ENTITY_CREATED  = "YOUR_ENTITY_CREATED";
    public const string YOUR_ENTITY_UPDATED  = "YOUR_ENTITY_UPDATED";
    public const string YOUR_ENTITY_DELETED  = "YOUR_ENTITY_DELETED";
}
```

**SystemCode.cs — assign ERR/CON codes:**
```csharp
// CCE.Application\Messages\SystemCode.cs
// Pick next available code:
public const string ERR999 = "ERR999"; // YourEntity not found
public const string CON999 = "CON999"; // YourEntity created/updated/deleted
```

**SystemCodeMap.cs — map domain keys to system codes:**
```csharp
// CCE.Application\Messages\SystemCodeMap.cs
// In the dictionary:
["YOUR_ENTITY_NOT_FOUND"] = SystemCode.ERR999,
["YOUR_ENTITY_CREATED"]   = SystemCode.CON999,
["YOUR_ENTITY_UPDATED"]   = SystemCode.CON999,
["YOUR_ENTITY_DELETED"]   = SystemCode.CON999,
```

**MessageFactory.cs — convenience shortcuts:**
```csharp
// CCE.Application\Messages\MessageFactory.cs
// ─── Convenience shortcuts (YourEntity) ───
public Response<VoidData> YourEntityCreated()   => Ok(ApplicationErrors.YourEntity.YOUR_ENTITY_CREATED);
public Response<VoidData> YourEntityUpdated()   => Ok(ApplicationErrors.YourEntity.YOUR_ENTITY_UPDATED);
public Response<VoidData> YourEntityDeleted()   => Ok(ApplicationErrors.YourEntity.YOUR_ENTITY_DELETED);
public Response<T> YourEntityNotFound<T>()      => NotFound<T>(ApplicationErrors.YourEntity.YOUR_ENTITY_NOT_FOUND);
```

**Resources.yaml — bilingual messages:**
```yaml
YOUR_ENTITY_NOT_FOUND:
  ar: "المنشأة غير موجودة"
  en: "Your entity not found"

YOUR_ENTITY_CREATED:
  ar: "تم إنشاء المنشأة بنجاح"
  en: "Your entity created successfully"

YOUR_ENTITY_UPDATED:
  ar: "تم تحديث المنشأة بنجاح"
  en: "Your entity updated successfully"

YOUR_ENTITY_DELETED:
  ar: "تم حذف المنشأة بنجاح"
  en: "Your entity deleted successfully"
```

---

### Step 7 — API Endpoints

**External API Endpoints (public):**
```csharp
// CCE.Api.External\Endpoints\YourEntityEndpoints.cs
public static class YourEntityEndpoints
{
    public static void MapYourEntityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/your-entities");

        group.MapPost("/", Submit)
            .AllowAnonymous();  // Or use RequireAuthorization() for authenticated-only
    }

    private static async Task<IResult> Submit(
        SubmitYourEntityRequest request,
        ISender sender)
    {
        var cmd = new CreateYourEntityCommand(request.Name, request.Order);
        var result = await sender.Send(cmd);
        return result.ToHttpResult(StatusCodes.Status201Created);
    }
}

// Request DTO — uses primitive types (int for enums)
public sealed record SubmitYourEntityRequest(string Name, int Order);
```

**Internal API Endpoints (admin):**
```csharp
// CCE.Api.Internal\Endpoints\YourEntityEndpoints.cs
public static class YourEntityEndpoints
{
    public static void MapYourEntityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/your-entities")
            .RequireAuthorization(Permissions.Survey_ReadAll);

        group.MapPost("/", Create);
        group.MapPut("/{id:guid}", Update);
        group.MapDelete("/{id:guid}", Delete);
        group.MapGet("/{id:guid}", GetById);
        group.MapGet("/", GetAll);
    }

    private static async Task<IResult> Create(CreateYourEntityRequest request, ISender sender)
    {
        var cmd = new CreateYourEntityCommand(request.Name, request.Order);
        var result = await sender.Send(cmd);
        return result.ToHttpResult(StatusCodes.Status201Created);
    }

    private static async Task<IResult> Update(Guid id, UpdateYourEntityRequest request, ISender sender)
    {
        var cmd = new UpdateYourEntityCommand(id, request.Name, request.Order);
        var result = await sender.Send(cmd);
        return result.ToHttpResult();
    }

    private static async Task<IResult> Delete(Guid id, ISender sender)
    {
        var result = await sender.Send(new DeleteYourEntityCommand(id));
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetById(Guid id, ISender sender)
    {
        var result = await sender.Send(new GetYourEntityByIdQuery(id));
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetAll(
        int page = 1, int pageSize = 20, ISender sender = default!)
    {
        var result = await sender.Send(new GetAllYourEntitiesQuery(page, pageSize));
        return result.ToHttpResult();
    }
}
```

---

### Step 8 — DI Registration

```csharp
// CCE.Infrastructure\DependencyInjection.cs

// Custom write-only repository:
services.AddScoped<IYourEntityRepository, YourEntityRepository>();

// Generic repository (already registered — add only if you need it):
// services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
```

### Step 9 — Program.cs (both APIs)

```csharp
// CCE.Api.External\Program.cs
app.MapYourEntityEndpoints();

// CCE.Api.Internal\Program.cs
app.MapYourEntityEndpoints();
```

---

## Pattern: Write-Only Repository + Unit of Work

```
┌─────────────────────────────────────────────────────┐
│ CommandHandler                                       │
│                                                       │
│  1. Create entity via domain factory                  │
│  2. _repo.AddAsync(entity, ct)     ← tracks only     │
│  3. _db.SaveChangesAsync(ct)       ← single commit   │
│  4. Return MessageFactory result                      │
└─────────────────────────────────────────────────────┘
```

### Rules:
- Repository **never calls** `SaveChangesAsync`
- Handler calls `_db.SaveChangesAsync()` **exactly once**
- Repository only adds/attaches entity to the change tracker
- Use when feature only needs Create (no Update/Delete)

### Key Files:
- `CCE.Application\Evaluation\IEvaluationRepository.cs` — write-only interface
- `CCE.Infrastructure\Evaluation\EvaluationRepository.cs` — tracks only

---

## Pattern: Generic Repository for Write Operations (Update/Delete)

```
┌─────────────────────────────────────────────────────┐
│ CommandHandler (Update/Delete)                       │
│                                                       │
│  1. _repo.GetByIdAsync(id, ct)    ← fetch entity     │
│  2. entity.Update(...)            ← domain mutation  │
│     (or _repo.Delete(entity)      ← mark for removal)│
│  3. _db.SaveChangesAsync(ct)      ← single commit   │
│  4. Return MessageFactory result                      │
└─────────────────────────────────────────────────────┘
```

### Key Points:
- Inject `IRepository<YourEntity, Guid>` (from `CCE.Application.Common.Interfaces`)
- `GetByIdAsync` returns tracked entity — no need to call `Update()` after mutation
- `Delete()` marks for removal
- Same `SaveChangesAsync` pattern
- Generic repository is **already registered** in DI

### Key Files:
- `CCE.Application\Common\Interfaces\IRepository.cs` — interface
- `CCE.Infrastructure\Persistence\Repository.cs` — concrete implementation
- `CCE.Infrastructure\Persistence\EntityRepository.cs` — abstract base (without interface)

---

## Pattern: Read via ICceDbContext

```
┌─────────────────────────────────────────────────────┐
│ QueryHandler                                         │
│                                                       │
│  injects ICceDbContext directly                       │
│  _db.Set<YourEntity>().Where(...)                     │
│  .Select(e => new Dto(...)) — projection              │
│  .FirstOrDefaultAsync / .ToListAsync                  │
│  .ToPagedResultAsync(...) — pagination                │
│                                                       │
│  Returns _msg.Ok(data, "ITEMS_LISTED")                │
└─────────────────────────────────────────────────────┘
```

### Rules:
- **No repository** for read operations
- Use `.Select()` to project to DTO directly in SQL
- Use `.ToPagedResultAsync()` for paginated lists
- Always use `AsNoTracking()` (already set in ICceDbContext implementation)

### Key Files:
- `CCE.Infrastructure\Persistence\ICceDbContext.cs` — `IQueryable<T>` properties
- `CCE.Infrastructure\Persistence\CceDbContext.cs` — `AsNoTracking()` in explicit interface impl

---

## Pattern: Response\<T\> Envelope + MessageFactory

### Response\<T\> structure:
```json
{
  "success": true,
  "code": "CON008",
  "message": "Evaluation submitted successfully",
  "data": { ... },
  "errors": [],
  "traceId": "...",
  "timestamp": "..."
}
```

### MessageFactory usage:

| Method | HTTP Status | When to Use |
|---|---|---|
| `_msg.Ok(data, domainKey)` | 200 | Success with data |
| `_msg.Ok(domainKey)` | 200 | Success, no data |
| `_msg.NotFound<T>(domainKey)` | 404 | Entity not found |
| `_msg.Conflict<T>(domainKey)` | 409 | Duplicate/conflict |
| `_msg.Unauthorized<T>(domainKey)` | 401 | Not authenticated |
| `_msg.Forbidden<T>(domainKey)` | 403 | Not authorized |
| `_msg.BusinessRule<T>(domainKey)` | 422 | Business rule violation |
| `_msg.ValidationError<T>(domainKey, errors)` | 400 | Validation errors |

### How it works:
1. Handler passes a **domain key** (e.g., `"YOUR_ENTITY_CREATED"`)
2. `MessageFactory` calls `SystemCodeMap.ToSystemCode(key)` → e.g., `"CON999"`
3. `MessageFactory` calls `ILocalizationService.GetString(key)` → localized message
4. Returns `Response<T>` with code + message

### Key Files:
- `CCE.Application\Common\Response.cs` — `Response<T>`, `VoidData`, `Response`
- `CCE.Application\Messages\MessageFactory.cs` — factory
- `CCE.Application\Messages\SystemCode.cs` — code constants
- `CCE.Application\Messages\SystemCodeMap.cs` — domain key → code mapping

---

## Pattern: FluentValidation + ERR900 Handling

### Validator Rules:
```csharp
internal sealed class CreateCommandValidator : AbstractValidator<CreateCommand>
{
    public CreateCommandValidator()
    {
        RuleFor(x => x.Field)
            .NotEmpty().WithErrorCode("REQUIRED_FIELD")
            .MaximumLength(500).WithErrorCode("MAX_LENGTH");

        // For enum fields (int in request):
        RuleFor(x => x.Rating)
            .NotEqual(0).WithErrorCode("REQUIRED_FIELD")
            .IsInEnum().WithErrorCode("INVALID_ENUM");

        // For required enums where 0 = None (sentinel):
        RuleFor(x => x.OverallSatisfaction)
            .NotEqual(EvaluationRating.None).WithErrorCode("REQUIRED_FIELD");
        // NOTE: .IsInEnum() is not needed when request uses int 1-5
        // (out-of-range values can't happen from valid request body)
    }
}
```

### ERR900 Fallback Chain:
```
Validator: .WithErrorCode("REQUIRED_FIELD")
    ↓
ResponseValidationBehavior: f.ErrorCode ?? f.ErrorMessage
    ↓
ExceptionHandlingMiddleware: e.ErrorCode ?? e.Message
```

If `SystemCodeMap` doesn't have the domain key → `SystemCode.ERR900` is returned → middleware uses `fallbackMessage`.

### Key Files:
- `CCE.Application\Common\Behaviors\ResponseValidationBehavior.cs`
- `CCE.Api.Common\Middleware\ExceptionHandlingMiddleware.cs`

---

## Pattern: ToHttpResult for Endpoints

```csharp
// CCE.Api.Common.Extensions.ResponseExtensions

public static IResult ToHttpResult<T>(this Response<T> response, int successStatusCode = 200);
public static IResult ToCreatedHttpResult<T>(this Response<T> response);  // → 201
public static IResult ToNoContentHttpResult(this Response<VoidData> response);  // → 204
```

### HTTP Status Mapping:

| MessageType | HTTP Status |
|---|---|
| Success | `successStatusCode` (default 200) |
| NotFound | 404 |
| Validation | 400 |
| Conflict | 409 |
| Unauthorized | 401 |
| Forbidden | 403 |
| BusinessRule | 422 |
| Internal | 500 |

### Key Files:
- `CCE.Api.Common\Extensions\ResponseExtensions.cs`

---

## Pattern: Pagination with PagedResult\<T\>

```csharp
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long Total
);
```

### Usage in Query Handlers:
```csharp
public async Task<Response<PagedResult<YourEntityDto>>> Handle(
    GetAllQuery q, CancellationToken ct)
{
    var result = await _db.Set<YourEntity>()
        .OrderByDescending(e => e.CreatedOn)
        .Select(e => new YourEntityDto(e.Id, e.Name, e.CreatedOn))
        .ToPagedResultAsync(q.Page, q.PageSize, ct);

    return _msg.Ok(result, "ITEMS_LISTED");
}
```

### Extension Methods:
```csharp
// Project to DTO in single SQL round trip:
query.ToPagedResultAsync(page, pageSize, ct)

// Or with explicit projection expression:
query.ToPagedResultAsync(q => new Dto(q.Id, q.Name), page, pageSize, ct)
```

### Behavior:
- `page` clamped to `>= 1`
- `pageSize` clamped to `[1, 100]`
- Returns `PagedResult<T>` with `Items`, `Page`, `PageSize`, `Total`

### Key Files:
- `CCE.Application\Common\Pagination\PagedResult.cs`

---

## Pattern: Enum Handling (int Request, String Response)

### Request (int):
```csharp
public sealed record SubmitEvaluationRequest(
    int OverallSatisfaction,  // 1-5 (not None=0)
    int EaseOfUse,
    int ContentSuitability,
    string Feedback);
```

### Command (enum):
```csharp
public sealed record SubmitEvaluationCommand(
    EvaluationRating OverallSatisfaction,
    EvaluationRating EaseOfUse,
    EvaluationRating ContentSuitability,
    string Feedback) : IRequest<Response<VoidData>>;
```

### MediatR automatically converts int → enum when sending command.

### Response (string — enum name):
`JsonStringEnumConverter` is configured globally in both Program.cs files:
```csharp
.AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
```

So response shows: `"overallSatisfaction": "Excellent"` not `"overallSatisfaction": 1`.

### Validation:
```csharp
// Rejects 0 (sentinel None):
RuleFor(x => x.OverallSatisfaction)
    .NotEqual(EvaluationRating.None).WithErrorCode("REQUIRED_FIELD");

// .IsInEnum() is NOT needed because int→enum conversion at MediatR
// ensures only valid int values (1-5) pass through
```

---

## Pattern: Anonymous Users + Nullable CreatedById

### Background:
- `AuditableEntity<TId>.CreatedById` is `Guid?` (nullable)
- `MarkAsCreated(Guid by, ISystemClock clock)` requires non-null `Guid` (throws on `Guid.Empty`)
- For endpoints with `[AllowAnonymous]`, the user may not be authenticated
- `ICurrentUserAccessor.GetUserId()` returns `null` for unauthenticated requests

### Solution:
- If endpoint is `AllowAnonymous`, **don't pass userId to domain factory** or handle null:
```csharp
var userId = _currentUser.GetUserId();
// Option A: Skip CreatedById for anonymous submissions
var entity = ServiceEvaluation.Submit(cmd.OverallSatisfaction, ...);  // factory without user
// CreatedById stays null

// Option B: Pass even null and let factory handle it
var entity = ServiceEvaluation.Submit(cmd.OverallSatisfaction, ..., userId, _clock);
// factory stores CreatedById = userId (may be null)
```

### Key Files:
- `CCE.Domain\Common\AuditableEntity.cs` — `CreatedById` as `Guid?`
- `CCE.Api.Common.Identity\HttpContextCurrentUserAccessor.cs` — `GetUserId()`

---

## Pattern: LocalizedText Value Object

### Definition:
```csharp
// CCE.Domain.PlatformSettings.ValueObjects.LocalizedText
public sealed class LocalizedText
{
    public string Ar { get; private init; }
    public string En { get; private init; }

    // Factory with validation:
    public static LocalizedText Create(string ar, string en);  // throws if empty

    // Factory without validation:
    public static LocalizedText From(string ar, string en);    // allows empty
}
```

### Usage in Entity:
```csharp
public LocalizedText Question { get; private set; }
public LocalizedText Answer { get; private set; }
```

### EF Core Configuration:
```csharp
builder.OwnsOne(e => e.Question, nav =>
{
    nav.Property(t => t.Ar).IsRequired().HasColumnName("question_ar");
    nav.Property(t => t.En).IsRequired().HasColumnName("question_en");
});
```

### DTO for LocalizedText:
```csharp
public sealed record FaqDto(
    Guid Id,
    string QuestionEn,
    string QuestionAr,
    string AnswerEn,
    string AnswerAr,
    int Order,
    DateTimeOffset CreatedOn,
    Guid? CreatedById);
```

### Mapping LocalizedText to DTO:
```csharp
.Select(e => new FaqDto(
    e.Id,
    e.Question.En,
    e.Question.Ar,
    e.Answer.En,
    e.Answer.Ar,
    e.Order,
    e.CreatedOn,
    e.CreatedById))
```

### Key Files:
- `CCE.Domain.PlatformSettings.ValueObjects.LocalizedText` — value object

---

## Pattern: SuperAdmin Authorization

```csharp
// In Internal API endpoints:
var group = app.MapGroup("/api/admin/faqs")
    .RequireAuthorization(Permissions.Survey_ReadAll);
    // Or use a specific SuperAdmin permission if it exists

// If no dedicated SuperAdmin permission exists, check existing ones:
// Permissions.Survey_ReadAll — used for evaluation admin endpoints
// Or add a new permission constant in Permissions.cs
```

### Key Files:
- Check `CCE.Application\Common\Authorization\Permissions.cs` for available permissions
- Policies are registered in `AddCcePermissionPolicies` (both API Program.cs)

---

## Pattern: Domain Factory + Mutation Methods

### Factory (static Create method):
```csharp
public static YourEntity Create(string name, Guid by, ISystemClock clock)
{
    // Validate
    if (string.IsNullOrWhiteSpace(name))
        throw new DomainException("Name is required.");

    // Create
    var entity = new YourEntity { Name = name };

    // Audit
    entity.MarkAsCreated(by, clock);

    return entity;
}
```

### Mutation (instance Update method):
```csharp
public void Update(string name, Guid by, ISystemClock clock)
{
    // Validate
    if (string.IsNullOrWhiteSpace(name))
        throw new DomainException("Name is required.");

    // Mutate
    Name = name;

    // Audit
    MarkAsModified(by, clock);
}
```

### Rules:
- Factory validates all inputs before creating
- Mutation validates and changes state
- Both call `MarkAsCreated` / `MarkAsModified` with `ISystemClock`
- Private constructor ensures entity is only created via factory
- `MarkAsCreated` throws if `by == Guid.Empty`

---

## Pattern: Mapping (DTOs)

### Manual Projection in Query Handlers:
```csharp
.Select(e => new YourEntityDto(
    e.Id,
    e.Name,
    e.Order,
    e.CreatedOn,
    e.CreatedById))
```

This is the current project convention — **no AutoMapper** is used. DTO projection happens directly in `.Select()` for single SQL round trip.

### Mapping LocalizedText → Flat DTO Fields:
```csharp
.Select(e => new FaqDto(
    e.Id,
    e.Question.En,
    e.Question.Ar,
    e.Answer.En,
    e.Answer.Ar,
    e.Order,
    e.CreatedOn,
    e.CreatedById))
```

---

## File Checklist

Use this checklist when creating a new CRUD feature.

### Domain Layer
- [ ] `CCE.Domain\YourDomain\YourEntity.cs` — entity (inherits `AuditableEntity<Guid>`)
- [ ] `CCE.Domain\YourDomain\YourRating.cs` — enum (if needed)
- [ ] `CCE.Domain\YourDomain\ValueObjects\LocalizedText.cs` — value object (if needed)

### Application Layer — Repository Interface
- [ ] `CCE.Application\YourDomain\IYourEntityRepository.cs` — write-only interface (if creating custom repo)
- [ ] OR use `IRepository<YourEntity, Guid>` from `CCE.Application.Common.Interfaces` (for generic)

### Application Layer — Commands
- [ ] `Commands\CreateYourEntity\CreateYourEntityCommand.cs`
- [ ] `Commands\CreateYourEntity\CreateYourEntityCommandHandler.cs`
- [ ] `Commands\CreateYourEntity\CreateYourEntityCommandValidator.cs`
- [ ] `Commands\UpdateYourEntity\UpdateYourEntityCommand.cs` (if needed)
- [ ] `Commands\UpdateYourEntity\UpdateYourEntityCommandHandler.cs` (if needed)
- [ ] `Commands\UpdateYourEntity\UpdateYourEntityCommandValidator.cs` (if needed)
- [ ] `Commands\DeleteYourEntity\DeleteYourEntityCommand.cs` (if needed)
- [ ] `Commands\DeleteYourEntity\DeleteYourEntityCommandHandler.cs` (if needed)

### Application Layer — Queries
- [ ] `Queries\GetAllYourEntities\GetAllYourEntitiesQuery.cs`
- [ ] `Queries\GetAllYourEntities\GetAllYourEntitiesQueryHandler.cs`
- [ ] `Queries\GetYourEntityById\GetYourEntityByIdQuery.cs`
- [ ] `Queries\GetYourEntityById\GetYourEntityByIdQueryHandler.cs`

### Application Layer — DTOs
- [ ] `DTOs\YourEntityDto.cs`

### Application Layer — Error/Success Codes
- [ ] `Errors\ApplicationErrors.cs` — add `YourEntity` static class with constants
- [ ] `Messages\SystemCode.cs` — add ERR/CON constants
- [ ] `Messages\SystemCodeMap.cs` — map domain keys to codes
- [ ] `Messages\MessageFactory.cs` — add convenience shortcut methods

### Infrastructure Layer
- [ ] `Persistence\Configurations\YourDomain\YourEntityConfiguration.cs` — EF Core config
- [ ] `YourDomain\YourEntityRepository.cs` — concrete repository (if custom)
- [ ] `Persistence\Migrations\…_AddYourEntity.cs` — migration

### API Layer — Endpoints
- [ ] `CCE.Api.External\Endpoints\YourEntityEndpoints.cs` — public endpoints
- [ ] `CCE.Api.Internal\Endpoints\YourEntityEndpoints.cs` — admin endpoints
- [ ] `CCE.Api.External\Program.cs` — add `app.MapYourEntityEndpoints()`
- [ ] `CCE.Api.Internal\Program.cs` — add `app.MapYourEntityEndpoints()`

### Localization
- [ ] `CCE.Api.Common\Localization\Resources.yaml` — add AR/EN messages

### Registration
- [ ] `CCE.Infrastructure\DependencyInjection.cs` — register repository

---

## Common Pitfalls

| Pitfall | Solution |
|---|---|
| Forgetting to add `DbSet` to `ICceDbContext` + `CceDbContext` | Always add both the interface property and the class property |
| Forgetting to register repository in DI | Add `services.AddScoped<IYourRepo, YourRepo>()` |
| Using repository for reads instead of `ICceDbContext` | Inject `ICceDbContext` directly in QueryHandlers |
| Adding validation in endpoint | All validation goes in FluentValidation validators only |
| `SaveChangesAsync` in repository | Repository only tracks — handler commits |
| Using `None=0` enum as valid value | Validator must reject `None` via `.NotEqual(None)` |
| Not updating `created_by_id` to nullable for anonymous entities | `CreatedById` is `Guid?` — migration must reflect that |
| Forgetting to add `MapYourEntityEndpoints()` to Program.cs | Both External and Internal Program.cs need it |
| Using `Result<T>` instead of `Response<T>` | Use `Response<T>` everywhere — `Result<T>` is legacy |
| `.WithErrorCode("REQUIRED_FIELD")` vs `.WithMessage("...")` | Use `.WithErrorCode()` with domain keys, not inline messages |
| Not adding `ISystemClock` to handler DI | Domain factory methods need `ISystemClock` for audit timestamps |
| `IRepository` import from wrong namespace | Import from `CCE.Application.Common.Interfaces` |
| Entity not tracked after `GetByIdAsync` | Entity fetched via same DbContext is auto-tracked — no need for `Update()` |
| `ToHttpResult` missing | Import `CCE.Api.Common.Extensions` |
| `ToPagedResultAsync` missing | Import `CCE.Application.Common.Pagination` |
