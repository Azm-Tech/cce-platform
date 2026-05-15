# WhereIf & Paged DTO List Implementation Plan

## How to Adopt in Another Solution

1. Replace all `[YourAppName]` occurrences with your root namespace.
2. Copy `PredicateBuilder.cs` into your Domain layer (no external dependencies).
3. Ensure `BasePagedQuery`, `PaginatedList<T>`, `IRepository<T>`, and `BaseRepository<T>` are already in place (see the Unit of Work plan).
4. Ensure `AutoMapper` and `AutoMapper.Extensions.Microsoft.DependencyInjection` are installed and configured.
5. For every paged list query, create a `Query` inheriting from `BasePagedQuery`, a `Dto` record, and a `QueryHandler`.

---

## Overview

This plan implements two complementary patterns:

1. **`PredicateBuilder.WhereIf`** — A lightweight expression-tree builder that lets you compose conditional `Where` clauses without branching `if` statements.
2. **`GetPagedAsync<TDto>`** — A generic repository method that projects, filters, sorts, and paginates entity data into DTOs in a single database round-trip.

Together they produce clean, readable query handlers like this:

```csharp
var filter = PredicateBuilder.True<Content>()
    .WhereIf(!string.IsNullOrWhiteSpace(request.SearchTerm),
        c => c.Title.Contains(request.SearchTerm!))
    .WhereIf(request.AuthorId.HasValue,
        c => c.AuthorId == request.AuthorId!.Value);

var result = await _repository.GetPagedAsync<ContentDto>(request, filter, ct);
```

**Packages required:** `AutoMapper`, `AutoMapper.Extensions.Microsoft.DependencyInjection`

---

### 1. Create `PredicateBuilder` (Domain Layer)

**File:** `Domain/Common/PredicateBuilder.cs`

```csharp
using System.Linq.Expressions;

namespace [YourAppName].Domain.Common;

public static class PredicateBuilder
{
    public static Expression<Func<T, bool>> True<T>() => _ => true;
    public static Expression<Func<T, bool>> False<T>() => _ => false;

    public static Expression<Func<T, bool>> And<T>(
        this Expression<Func<T, bool>> expr1,
        Expression<Func<T, bool>> expr2)
    {
        var parameter = Expression.Parameter(typeof(T));
        var body = Expression.AndAlso(
            Expression.Invoke(expr1, parameter),
            Expression.Invoke(expr2, parameter));
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    public static Expression<Func<T, bool>> Or<T>(
        this Expression<Func<T, bool>> expr1,
        Expression<Func<T, bool>> expr2)
    {
        var parameter = Expression.Parameter(typeof(T));
        var body = Expression.OrElse(
            Expression.Invoke(expr1, parameter),
            Expression.Invoke(expr2, parameter));
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    public static Expression<Func<T, bool>> WhereIf<T>(
        this Expression<Func<T, bool>> query,
        bool condition,
        Expression<Func<T, bool>> predicate)
    {
        return condition ? query.And(predicate) : query;
    }
}
```

---

### 2. How `WhereIf` Works

| Step | Code | Result Expression |
|------|------|-----------------|
| 1 | `PredicateBuilder.True<Content>()` | `c => true` |
| 2 | `.WhereIf(hasSearch, c => c.Title.Contains(term))` | `c => true && c.Title.Contains(term)` (if true) or `c => true` (if false) |
| 3 | `.WhereIf(hasAuthor, c => c.AuthorId == id)` | Composed `And` of all active predicates |

**Benefits:**
- No imperative `if` blocks polluting the handler.
- The entire filter is a single `Expression<Func<T, bool>>` ready for EF Core translation.
- Easy to read: each filter condition is one fluent line.

---

### 3. Repository Paging Methods (Infrastructure Layer)

These methods are part of `BaseRepository<T>` (see the Unit of Work plan). They are repeated here for reference.

**Projection-based paging** (requires AutoMapper configuration):

```csharp
public virtual async Task<PaginatedList<TDto>> GetPagedAsync<TDto>(
    BasePagedQuery pagedQuery,
    Expression<Func<T, bool>>? filter,
    CancellationToken ct = default)
```

**Manual-select paging** (no AutoMapper required, explicit projection):

```csharp
public virtual async Task<PaginatedList<TDto>> GetPagedAsync<TDto>(
    BasePagedQuery pagedQuery,
    Expression<Func<T, bool>>? filter,
    Expression<Func<T, TDto>> selectExpression,
    CancellationToken ct = default)
```

Both methods:
1. Apply `AsNoTracking`.
2. Apply the `filter` expression.
3. Execute `CountAsync` for total records.
4. Apply dynamic sorting via `ApplyOrdering(sortBy, isDescending)`.
5. Skip/Take for pagination.
6. Project to `TDto` (AutoMapper `ProjectTo` or manual `Select`).
7. Return `PaginatedList<TDto>`.

---

### 4. AutoMapper Profile (Application Layer)

When using the projection-based `GetPagedAsync<TDto>`, AutoMapper must know how to map `TEntity` → `TDto`.

**File:** `Application/Features/Contents/Mapping/ContentProfile.cs`

```csharp
using AutoMapper;
using [YourAppName].Application.Features.Contents.Dtos;
using [YourAppName].Domain.Entities.Content;

namespace [YourAppName].Application.Features.Contents.Mapping;

public class ContentProfile : Profile
{
    public ContentProfile()
    {
        CreateMap<Content, ContentDto>();
    }
}
```

> **Note:** AutoMapper scans the Assembly for `Profile` classes at startup if you call `services.AddAutoMapper(Assembly.GetExecutingAssembly())` in DI.

---

### 5. Create the DTO (Application Layer)

**File:** `Application/Features/Contents/Dtos/ContentDto.cs`

```csharp
namespace [YourAppName].Application.Features.Contents.Dtos;

public record ContentDto(
    Guid Id,
    string Title,
    string Body,
    string? Summary,
    string ContentType,
    Guid AuthorId,
    string Status,
    string? FeaturedImageUrl,
    int ViewCount,
    int LikeCount,
    string[] Tags,
    string? Category,
    DateTime? PublishedAt,
    DateTime? ExpiresAt,
    bool IsFeatured,
    DateTime CreatedAt
);
```

---

### 6. Create the Paged Query (Application Layer)

**File:** `Application/Features/Contents/Queries/GetContents/GetContentsQuery.cs`

```csharp
using [YourAppName].Application.Contracts;
using [YourAppName].Application.Features.Contents.Dtos;
using [YourAppName].Domain;
using [YourAppName].Domain.Common;
using MediatR;

namespace [YourAppName].Application.Features.Contents.Queries.GetContents;

public class GetContentsQuery : BasePagedQuery, IQuery<Result<PaginatedList<ContentDto>>>
{
    public string? SearchTerm { get; init; }
    public string? Status { get; init; }
    public Guid? AuthorId { get; init; }

    public GetContentsQuery()
    {
        PageIndex = 1;
        PageSize = 10;
    }
}
```

> **Pattern:** The query inherits from `BasePagedQuery` (provides `PageIndex`, `PageSize`, `SortBy`, `SortDirection`) and implements `IQuery<Result<PaginatedList<TDto>>>`. Default page values are set in the constructor.

---

### 7. Create the Query Handler (Application Layer)

**File:** `Application/Features/Contents/Queries/GetContents/GetContentsQueryHandler.cs`

```csharp
using [YourAppName].Application.Contracts;
using [YourAppName].Application.Features.Contents.Dtos;
using [YourAppName].Domain;
using [YourAppName].Domain.Common;
using [YourAppName].Domain.Entities.Content;
using [YourAppName].Domain.Interfaces;
using MediatR;

namespace [YourAppName].Application.Features.Contents.Queries.GetContents;

public class GetContentsQueryHandler(IRepository<Content> contentRepository) 
    : IQueryHandler<GetContentsQuery, Result<PaginatedList<ContentDto>>>
{
    public async Task<Result<PaginatedList<ContentDto>>> Handle(GetContentsQuery request, CancellationToken ct)
    {
        var filter = PredicateBuilder.True<Content>()
            .WhereIf(!string.IsNullOrWhiteSpace(request.SearchTerm),
                c => c.Title.Contains(request.SearchTerm!) || c.Body.Contains(request.SearchTerm!))
            .WhereIf(!string.IsNullOrWhiteSpace(request.Status),
                c => c.Status == request.Status)
            .WhereIf(request.AuthorId.HasValue,
                c => c.AuthorId == request.AuthorId!.Value);

        var result = await contentRepository.GetPagedAsync<ContentDto>(request, filter, ct);
        return Result<PaginatedList<ContentDto>>.Success(result);
    }
}
```

---

### 8. Alternative: Manual Select Paging

If you prefer not to use AutoMapper projection, use the overload with an explicit `Select` expression:

```csharp
var filter = PredicateBuilder.True<Content>()
    .WhereIf(!string.IsNullOrWhiteSpace(request.Status),
        c => c.Status == request.Status);

var result = await _repository.GetPagedAsync(
    request,
    filter,
    c => new ContentDto(
        c.Id,
        c.Title,
        c.Body,
        c.Summary,
        c.ContentType,
        c.AuthorId,
        c.Status,
        c.FeaturedImageUrl,
        c.ViewCount,
        c.LikeCount,
        c.Tags,
        c.Category,
        c.PublishedAt,
        c.ExpiresAt,
        c.IsFeatured,
        c.CreatedAt),
    ct);
```

> **Trade-off:** AutoMapper projection is less code and keeps DTO mapping centralized in Profiles. Manual `Select` is more explicit and avoids AutoMapper configuration overhead for simple cases.

---

### 9. More `WhereIf` Examples

**Notifications — multiple nullable filters:**

```csharp
var filter = PredicateBuilder.True<Notification>()
    .WhereIf(request.UserId.HasValue, n => n.UserId == request.UserId!.Value)
    .WhereIf(!string.IsNullOrWhiteSpace(request.Status), n => n.Status == request.Status)
    .WhereIf(!string.IsNullOrWhiteSpace(request.NotificationType), n => n.NotificationType == request.NotificationType)
    .WhereIf(request.IsRead.HasValue, n => (request.IsRead!.Value ? n.ReadAt != null : n.ReadAt == null));
```

**Platform Settings — boolean flag + string filters:**

```csharp
var filter = PredicateBuilder.True<PlatformSetting>()
    .WhereIf(!string.IsNullOrWhiteSpace(request.Category), s => s.Category == request.Category)
    .WhereIf(!string.IsNullOrWhiteSpace(request.Key), s => s.Key.Contains(request.Key!))
    .WhereIf(!request.IncludePrivate, s => s.IsPublic);
```

---

## Paged Response Shape Reference

When returned through `Result<T>`, the JSON response looks like this:

```json
{
  "isSuccess": true,
  "data": {
    "items": [
      { "id": "...", "title": "...", ... }
    ],
    "pageIndex": 1,
    "pageSize": 10,
    "totalCount": 47,
    "totalPages": 5,
    "hasPreviousPage": false,
    "hasNextPage": true
  },
  "error": null
}
```

| Property | Type | Description |
|----------|------|-------------|
| `Items` | `IReadOnlyList<TDto>` | The page of data |
| `PageIndex` | `int` | Current page (1-based) |
| `PageSize` | `int` | Items per page |
| `TotalCount` | `int` | Total records matching filter |
| `TotalPages` | `int` | Computed ceiling of TotalCount / PageSize |
| `HasPreviousPage` | `bool` | True if PageIndex > 1 |
| `HasNextPage` | `bool` | True if PageIndex < TotalPages |

---

## Sorting Reference

| `SortBy` | `SortDirection` | Behavior |
|----------|-----------------|----------|
| `null` or empty | any | No sorting applied |
| `Title` | `asc` | `OrderBy(e => e.Title)` |
| `Title` | `desc` | `OrderByDescending(e => e.Title)` |
| `Author.Name` | `asc` | `OrderBy(e => e.Author.Name)` (nested property) |
| `invalid` | any | Silently ignored (try/catch fallback) |

> **Note:** `ApplyOrdering` uses reflection to build the expression tree, so nested properties like `Author.Name` are supported via dot notation.
