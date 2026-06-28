# Unit of Work & Repository Implementation Plan

## How to Adopt in Another Solution

1. Replace all `[YourAppName]` occurrences with your root namespace.
2. Ensure your DbContext (`AppDbContext`) inherits from `DbContext` and is registered in DI.
3. All entities must inherit from `BaseEntity` (or adjust the `where T : BaseEntity` constraint to your own base type).
4. Install `AutoMapper` and `AutoMapper.Extensions.Microsoft.DependencyInjection` if you want the projection-based paging methods.
5. Register `IUnitOfWork`, `IRepository<>`, and `AutoMapper` in your Infrastructure DI module.

---

## Overview

This plan implements the **Unit of Work** and **Generic Repository** patterns using EF Core. The repository is read-optimized (`AsNoTracking` by default) and supports paging, filtering, projection, and eager loading. The Unit of Work wraps the DbContext and exposes explicit transaction control.

**Packages required:** `AutoMapper`, `AutoMapper.Extensions.Microsoft.DependencyInjection`, `Microsoft.EntityFrameworkCore`

---

### 1. Create `IBaseEntity` Interface (Domain Layer)

**File:** `Domain/Entities/IBaseEntity.cs`

```csharp
namespace [YourAppName].Domain.Entities;

public interface IBaseEntity
{
    Guid Id { get; set; }
    DateTime CreatedAt { get; set; }
    Guid? CreatedBy { get; set; }
    DateTime? UpdatedAt { get; set; }
    Guid? UpdatedBy { get; set; }
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
}
```

---

### 2. Create `BaseEntity` Abstract Class (Domain Layer)

**File:** `Domain/Entities/BaseEntity.cs`

```csharp
using [YourAppName].Domain.Events;

namespace [YourAppName].Domain.Entities;

public abstract class BaseEntity : IBaseEntity
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public void MarkUpdated() => UpdatedAt = DateTime.UtcNow;
    public void SoftDelete() { IsDeleted = true; DeletedAt = DateTime.UtcNow; }
}
```

> **Note:** If you do not use domain events, remove the `IDomainEvent` references and the `_domainEvents` list.

---

### 3. Create `BasePagedQuery` (Domain Layer)

**File:** `Domain/Common/BasePagedQuery.cs`

```csharp
namespace [YourAppName].Domain.Common;

public abstract class BasePagedQuery
{
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; }
    public string? SortDirection { get; set; } = "asc";
}
```

---

### 4. Create `PaginatedList<T>` (Domain Layer)

**File:** `Domain/PaginatedList.cs`

```csharp
namespace [YourAppName].Domain;

public class PaginatedList<T>
{
    public IReadOnlyList<T> Items { get; }
    public int PageIndex { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;

    private PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
    {
        Items = items.AsReadOnly();
        PageIndex = Math.Max(1, pageIndex);
        PageSize = Math.Max(1, pageSize);
        TotalCount = count;
    }

    public static PaginatedList<T> Create(IEnumerable<T> items, int count, int pageIndex, int pageSize)
    {
        var itemList = items.ToList();
        return new PaginatedList<T>(itemList, count, pageIndex, pageSize);
    }
}
```

---

### 5. Create `ApplyOrdering` Extension (Domain Layer)

**File:** `Domain/Common/LinqExtensions.cs`

```csharp
using System.Linq.Expressions;
using System.Reflection;

namespace [YourAppName].Domain.Common;

public static class LinqExtensions
{
    public static IQueryable<T> ApplyOrdering<T>(this IQueryable<T> source, string propertyPath, bool isDescending)
    {
        if (string.IsNullOrWhiteSpace(propertyPath))
            return source;

        var param = Expression.Parameter(typeof(T), "e");
        Expression? body = param;

        foreach (var member in propertyPath.Split('.'))
        {
            body = Expression.PropertyOrField(body!, member);
        }

        var lambdaType = typeof(Func<,>).MakeGenericType(typeof(T), body!.Type);
        var lambda = Expression.Lambda(lambdaType, body, param);

        var methodName = isDescending ? "OrderByDescending" : "OrderBy";

        var resultExp = Expression.Call(
            typeof(Queryable),
            methodName,
            [typeof(T), body.Type],
            source.Expression,
            Expression.Quote(lambda));

        return source.Provider.CreateQuery<T>(resultExp);
    }
}
```

---

### 6. Create `IUnitOfWork` Interface (Domain Layer)

**File:** `Domain/Interfaces/IUnitOfWork.cs`

```csharp
namespace [YourAppName].Domain.Interfaces;

public interface IUnitOfWork : IAsyncDisposable
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitTransactionAsync(CancellationToken ct = default);
    Task RollbackTransactionAsync(CancellationToken ct = default);
}
```

---

### 7. Create `IRepository<T>` Interface (Domain Layer)

**File:** `Domain/Interfaces/IRepository.cs`

```csharp
using [YourAppName].Domain.Common;
using [YourAppName].Domain.Entities;
using System.Linq.Expressions;

namespace [YourAppName].Domain.Interfaces;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<IReadOnlyList<T>> ListAllAsync(CancellationToken ct = default);
    IQueryable<T> Query(Expression<Func<T, bool>>? predicate = null, bool asNoTracking = true);
    IQueryable<T> QueryInclude(string includeProperties, Expression<Func<T, bool>>? predicate = null, bool asNoTracking = true);
    Task<PaginatedList<TDto>> GetPagedAsync<TDto>(BasePagedQuery pagedQuery, Expression<Func<T, bool>>? filter, CancellationToken ct = default);
    Task<PaginatedList<TDto>> GetPagedAsync<TDto>(BasePagedQuery pagedQuery, Expression<Func<T, bool>>? filter, Expression<Func<T, TDto>> selectExpression, CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);
    void Update(T entity);
    void Remove(T entity);
    void RemoveRange(IEnumerable<T> entities);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default);
}
```

---

### 8. Create `UnitOfWork` Implementation (Infrastructure Layer)

**File:** `Infrastructure/Persistence/UnitOfWork.cs`

```csharp
using [YourAppName].Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace [YourAppName].Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _currentTx;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        if (_currentTx != null) return;
        _currentTx = await _context.Database.BeginTransactionAsync(ct);
    }

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_currentTx == null) return;
        await _context.SaveChangesAsync(ct);
        await _currentTx.CommitAsync(ct);
        await _currentTx.DisposeAsync();
        _currentTx = null;
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_currentTx == null) return;
        try
        {
            await _currentTx.RollbackAsync(ct);
        }
        finally
        {
            await _currentTx.DisposeAsync();
            _currentTx = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_currentTx != null)
        {
            await _currentTx.DisposeAsync();
            _currentTx = null;
        }
    }
}
```

---

### 9. Create `BaseRepository<T>` Implementation (Infrastructure Layer)

**File:** `Infrastructure/Persistence/BaseRepository.cs`

```csharp
using AutoMapper;
using AutoMapper.QueryableExtensions;
using [YourAppName].Domain;
using [YourAppName].Domain.Common;
using [YourAppName].Domain.Entities;
using [YourAppName].Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace [YourAppName].Infrastructure.Persistence;

public class BaseRepository<T>(AppDbContext context, IConfigurationProvider config) : IRepository<T> where T : BaseEntity
{
    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Set<T>().AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);

    public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => await context.Set<T>().AsNoTracking().FirstOrDefaultAsync(predicate, ct);

    public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => await context.Set<T>().AnyAsync(predicate, ct);

    public virtual async Task<IReadOnlyList<T>> ListAllAsync(CancellationToken ct = default)
        => await context.Set<T>().AsNoTracking().ToListAsync(ct);

    public virtual IQueryable<T> Query(Expression<Func<T, bool>>? predicate = null, bool asNoTracking = true)
    {
        IQueryable<T> query = context.Set<T>();
        if (asNoTracking) query = query.AsNoTracking();
        if (predicate != null) query = query.Where(predicate);
        return query;
    }

    public virtual IQueryable<T> QueryInclude(
        string includeProperties,
        Expression<Func<T, bool>>? predicate = null,
        bool asNoTracking = true)
    {
        IQueryable<T> query = context.Set<T>();
        if (asNoTracking) query = query.AsNoTracking();
        if (predicate != null) query = query.Where(predicate);

        if (!string.IsNullOrWhiteSpace(includeProperties))
        {
            foreach (var includeProperty in includeProperties.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                query = query.Include(includeProperty.Trim());
            }
        }

        return query;
    }

    public virtual async Task<PaginatedList<TDto>> GetPagedAsync<TDto>(
        BasePagedQuery pagedQuery,
        Expression<Func<T, bool>>? filter,
        CancellationToken ct = default)
    {
        if (pagedQuery == null) throw new ArgumentNullException(nameof(pagedQuery));

        var query = context.Set<T>().AsQueryable();
        query = query.AsNoTracking();
        if (filter != null) query = query.Where(filter);

        var total = await query.CountAsync(ct);

        var pageIndex = Math.Max(pagedQuery.PageIndex, 1);
        var pageSize = Math.Max(pagedQuery.PageSize, 1);
        var skip = (pageIndex - 1) * pageSize;

        var sortBy = string.IsNullOrWhiteSpace(pagedQuery.SortBy) ? null : pagedQuery.SortBy;
        var sortDir = string.IsNullOrWhiteSpace(pagedQuery.SortDirection) ? "asc" : pagedQuery.SortDirection.ToLowerInvariant();

        if (!string.IsNullOrEmpty(sortBy))
        {
            try
            {
                query = query.ApplyOrdering(sortBy, sortDir == "desc");
            }
            catch
            {
                // Fallback: ignore invalid sort
            }
        }

        var items = await query
            .Skip(skip)
            .Take(pageSize)
            .ProjectTo<TDto>(config)
            .ToListAsync(ct);

        return PaginatedList<TDto>.Create(items, total, pageIndex, pageSize);
    }

    public virtual async Task<PaginatedList<TDto>> GetPagedAsync<TDto>(
        BasePagedQuery pagedQuery,
        Expression<Func<T, bool>>? filter,
        Expression<Func<T, TDto>> selectExpression,
        CancellationToken ct = default)
    {
        if (pagedQuery == null) throw new ArgumentNullException(nameof(pagedQuery));
        if (selectExpression == null) throw new ArgumentNullException(nameof(selectExpression));

        var query = context.Set<T>().AsQueryable().AsNoTracking();

        if (filter != null)
            query = query.Where(filter);

        var total = await query.CountAsync(ct);

        var pageIndex = Math.Max(pagedQuery.PageIndex, 1);
        var pageSize = Math.Max(pagedQuery.PageSize, 1);
        var skip = (pageIndex - 1) * pageSize;

        var sortBy = string.IsNullOrWhiteSpace(pagedQuery.SortBy) ? null : pagedQuery.SortBy;
        var sortDir = string.IsNullOrWhiteSpace(pagedQuery.SortDirection) ? "asc" : pagedQuery.SortDirection.ToLowerInvariant();

        if (!string.IsNullOrEmpty(sortBy))
        {
            try
            {
                query = query.ApplyOrdering(sortBy, sortDir == "desc");
            }
            catch
            {
                // Fallback: ignore invalid sort
            }
        }

        var items = await query
            .Skip(skip)
            .Take(pageSize)
            .Select(selectExpression)
            .ToListAsync(ct);

        return PaginatedList<TDto>.Create(items, total, pageIndex, pageSize);
    }

    public virtual async Task AddAsync(T entity, CancellationToken ct = default)
        => await context.Set<T>().AddAsync(entity, ct);

    public virtual async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default)
        => await context.Set<T>().AddRangeAsync(entities, ct);

    public virtual void Update(T entity)
        => context.Set<T>().Update(entity);

    public virtual void Remove(T entity)
        => context.Set<T>().Remove(entity);

    public virtual void RemoveRange(IEnumerable<T> entities)
        => context.Set<T>().RemoveRange(entities);

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
        => predicate == null ? await context.Set<T>().CountAsync(ct) : await context.Set<T>().CountAsync(predicate, ct);
}
```

---

### 10. Register in DI (Infrastructure Layer)

**File:** `Infrastructure/ServiceCollectionExtensions.cs` (or your own registration class)

```csharp
using [YourAppName].Domain.Interfaces;
using [YourAppName].Infrastructure.Persistence;
using System.Reflection;

namespace [YourAppName].Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAutoMapper(Assembly.GetExecutingAssembly());
        services.AddScoped(typeof(IRepository<>), typeof(BaseRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // ... other registrations

        return services;
    }
}
```

---

### 11. Handler Usage Pattern (Application Layer)

Inject both `IRepository<T>` and `IUnitOfWork`. Use the repository for queries and mutations, then call `_unitOfWork.SaveChangesAsync(ct)` once at the end of the handler.

```csharp
public class CreateContentCommandHandler : IRequestHandler<CreateContentCommand, Result<CreateSuccessDto>>
{
    private readonly IRepository<Content> _contentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateContentCommandHandler> _logger;

    public CreateContentCommandHandler(
        IRepository<Content> contentRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateContentCommandHandler> logger)
    {
        _contentRepository = contentRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<CreateSuccessDto>> Handle(CreateContentCommand request, CancellationToken ct)
    {
        var exists = await _contentRepository.ExistsAsync(c => c.Title == request.Title, ct);
        if (exists)
            return Result<CreateSuccessDto>.Failure(new Error(
                ApplicationErrors.Content.ALREADY_EXISTS,
                "...", "...", ErrorType.Conflict));

        var content = Content.Create(request.Title, request.Body, ...);
        await _contentRepository.AddAsync(content, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Content {ContentId} created", content.Id);
        return Result<CreateSuccessDto>.Success(new CreateSuccessDto(content.Id));
    }
}
```

---

### 12. Explicit Transaction Usage Pattern (Application Layer)

Use `BeginTransactionAsync`, `CommitTransactionAsync`, and `RollbackTransactionAsync` when you need to coordinate multiple operations atomically.

```csharp
public async Task<Result<Unit>> Handle(ComplexCommand request, CancellationToken ct)
{
    await _unitOfWork.BeginTransactionAsync(ct);
    try
    {
        await _repositoryA.AddAsync(entityA, ct);
        await _repositoryB.AddAsync(entityB, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        await _unitOfWork.CommitTransactionAsync(ct);

        return Result.Success();
    }
    catch
    {
        await _unitOfWork.RollbackTransactionAsync(ct);
        throw;
    }
}
```

---

## Lifetime Reference

| Service | Interface | Implementation | Lifetime | Reason |
|---------|-----------|----------------|----------|--------|
| `IUnitOfWork` | `Domain/Interfaces` | `Infrastructure/Persistence/UnitOfWork` | Scoped | Bound to request DbContext |
| `IRepository<T>` | `Domain/Interfaces` | `Infrastructure/Persistence/BaseRepository<T>` | Scoped | Bound to request DbContext |
| `AppDbContext` | — | `Infrastructure/Persistence/AppDbContext` | Scoped | EF Core default |

---

## Read-Optimized Defaults

| Method | Tracking | Notes |
|--------|----------|-------|
| `GetByIdAsync` | `AsNoTracking` | For reads only |
| `FirstOrDefaultAsync` | `AsNoTracking` | For reads only |
| `ListAllAsync` | `AsNoTracking` | For reads only |
| `Query` | `asNoTracking = true` | Override when updating queried entities |
| `QueryInclude` | `asNoTracking = true` | Override when updating queried entities |
| `GetPagedAsync` | `AsNoTracking` | Always read-only |
| `AddAsync` | N/A | Marks entity Added |
| `Update` | N/A | Marks entity Modified |
| `Remove` | N/A | Marks entity Deleted |

> **Rule:** If you need to mutate an entity after querying it, call `Query(predicate, asNoTracking: false)` or attach the entity manually.
