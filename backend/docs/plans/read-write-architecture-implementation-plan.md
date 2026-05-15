# Read/Write Architecture — Implementation Plan

## Problem Statement

The current codebase has **three Clean Architecture violations** and **two performance issues**:

### Clean Architecture Violations

1. **Infrastructure knows Application DTOs** — `ContentReadService`, `IdentityReadService`, `CommunityReadService` (Infrastructure) import and construct Application-layer DTOs (`NewsDto`, `UserListItemDto`, etc.). DTO mapping is Application logic.
2. **Query handlers are empty pass-throughs** — e.g. `ListNewsQueryHandler` does nothing except call `_readService.ListNewsAsync()` and return the result. The handler has no reason to exist.
3. **God interfaces** — `IContentReadService` has **21 methods** spanning News, Events, Pages, Resources, HomepageSections, and Assets. `ICommunityReadService` has **10 methods**. `IIdentityReadService` has **8 methods**. These grow with every feature.

### Performance Issues

4. **No `AsNoTracking()` on reads** — All queries go through `ICceDbContext` (which returns tracked `IQueryable<T>`). Read services never call `.AsNoTracking()`, so EF Core builds change-tracking snapshots for entities that are immediately mapped to DTOs and discarded.
5. **No server-side DTO projection** — All queries materialise full domain entities (`.ToListAsync()`), then map to DTOs in memory. This fetches ALL columns from SQL (including `ContentAr`, `ContentEn` — large text blobs) even for list endpoints that only need `Id`, `Title`, `Slug`.

---

## Target Architecture

```
┌──────────────────────────────────────────────────────┐
│  QUERIES (Reads)                                     │
│                                                      │
│  Endpoint → MediatR → QueryHandler → ICceDbContext   │
│                        ▪ .AsNoTracking()             │
│                        ▪ .WhereIf() filters          │
│                        ▪ .Select() → DTO projection  │
│                        ▪ .ToPagedResultAsync()       │
│                        ▪ mapping lives HERE           │
│                                                      │
│  ICceDbContext stays in Application layer (IQueryable)│
│  No ReadService. No DTO leak to Infrastructure.      │
└──────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────┐
│  COMMANDS (Writes)                                   │
│                                                      │
│  Endpoint → MediatR → CommandHandler → IXxxRepository│
│                        ▪ FluentValidation (pipeline) │
│                        ▪ Domain entity factory/method │
│                        ▪ repo.SaveAsync / UpdateAsync │
│                                                      │
│  Specific repos per aggregate (no generic base).     │
│  RowVersion via small extension helper.              │
└──────────────────────────────────────────────────────┘
```

---

## Phase 1 — Foundation (No Behaviour Changes)

### Step 1.1 — Add `AsNoTracking()` to `ICceDbContext` queryables

**Why:** Every query currently creates change-tracking snapshots that are never used. This is free perf.

**File:** `src/CCE.Infrastructure/Persistence/CceDbContext.cs`

Add a new explicit interface implementation block that wraps every `DbSet<T>` in `.AsNoTracking()` for the `ICceDbContext` contract:

```csharp
// ─── ICceDbContext (read-only queryables — no tracking) ───
IQueryable<News> ICceDbContext.News => Set<News>().AsNoTracking();
IQueryable<Event> ICceDbContext.Events => Set<Event>().AsNoTracking();
IQueryable<Resource> ICceDbContext.Resources => Set<Resource>().AsNoTracking();
IQueryable<Page> ICceDbContext.Pages => Set<Page>().AsNoTracking();
// ... all other IQueryable<T> properties
```

> **Important:** Write repositories must keep using the concrete `CceDbContext` (with tracked `DbSet<T>`), NOT `ICceDbContext`. This is already the case — all repos inject `CceDbContext`, not `ICceDbContext`.

**Impact:** Zero code changes in handlers or read services. All reads become no-tracking automatically.

**Verify:** Run full test suite — `dotnet test CCE.sln`. All tests should pass because test mocks return in-memory queryables (untracked anyway).

---

### Step 1.2 — Add `WhereIf` extension method

**Why:** Removes repetitive `if (x != null) { query = query.Where(...); }` blocks.

**File:** `src/CCE.Application/Common/Pagination/QueryableExtensions.cs` (new)

```csharp
using System.Linq.Expressions;

namespace CCE.Application.Common.Pagination;

public static class QueryableExtensions
{
    /// <summary>
    /// Conditionally appends a Where clause. When <paramref name="condition"/> is false
    /// the original query is returned unmodified.
    /// </summary>
    public static IQueryable<T> WhereIf<T>(
        this IQueryable<T> query,
        bool condition,
        Expression<Func<T, bool>> predicate)
        => condition ? query.Where(predicate) : query;
}
```

**Impact:** No behaviour change. Used in Phase 2.

---

### Step 1.3 — Add `PagedResult<T>.Map()` helper

**Why:** After `ToPagedResultAsync()` materialises entities, we need to map items to DTOs while preserving pagination metadata.

**File:** `src/CCE.Application/Common/Pagination/PagedResult.cs` (edit existing)

```csharp
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long Total)
{
    /// <summary>
    /// Projects each item into a new shape while preserving pagination metadata.
    /// </summary>
    public PagedResult<TOut> Map<TOut>(Func<T, TOut> selector) =>
        new(Items.Select(selector).ToList(), Page, PageSize, Total);
}
```

**Impact:** No behaviour change. Used in Phase 2.

---

### Step 1.4 — Add `DbContextExtensions.SetExpectedRowVersion()` helper

**Why:** Removes duplicated RowVersion boilerplate from the 4 repos that use it.

**File:** `src/CCE.Infrastructure/Persistence/DbContextExtensions.cs` (new)

```csharp
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Persistence;

internal static class DbContextExtensions
{
    /// <summary>
    /// Sets the expected RowVersion for optimistic concurrency on a tracked entity.
    /// </summary>
    public static void SetExpectedRowVersion<T>(
        this DbContext db, T entity, byte[] expectedRowVersion)
        where T : class
    {
        db.Entry(entity).OriginalValues["RowVersion"] = expectedRowVersion;
    }
}
```

**Impact:** Optional. Simplifies `NewsRepository`, `ResourceRepository`, `EventRepository`, `PageRepository`.

---

### Step 1.5 — Add server-side projection `ToPagedResultAsync<T, TDto>()` overload

**Why:** The current `ToPagedResultAsync()` always materialises full entities. We need an overload that accepts a `Select` expression so SQL only fetches the columns needed for the DTO.

**File:** `src/CCE.Application/Common/Pagination/PagedResult.cs` (edit existing, add to `PaginationExtensions`)

```csharp
/// <summary>
/// Paginates and projects in a single query — SQL only fetches DTO columns.
/// Use for list endpoints where you don't need the full entity.
/// </summary>
public static async Task<PagedResult<TDto>> ToPagedResultAsync<T, TDto>(
    this IQueryable<T> query,
    Expression<Func<T, TDto>> projection,
    int page, int pageSize, CancellationToken ct)
{
    page = Math.Max(1, page);
    pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

    var total = query is IAsyncEnumerable<T>
        ? await query.LongCountAsync(ct).ConfigureAwait(false)
        : query.LongCount();

    var projected = query.Select(projection);
    var items = projected is IAsyncEnumerable<TDto>
        ? await projected.Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct).ConfigureAwait(false)
        : projected.Skip((page - 1) * pageSize).Take(pageSize).ToList();

    return new PagedResult<TDto>(items, page, pageSize, total);
}
```

**Impact:** No behaviour change. Used in Phase 2 for performance-critical list endpoints.

---

## Phase 2 — Migrate Query Handlers (Per-Domain Module)

Migrate one domain at a time. Each domain follows the same 4-step recipe.

### Recipe: Migrating a Query Handler

For each query handler that currently delegates to a ReadService:

1. **Inject `ICceDbContext`** instead of `IXxxReadService`
2. **Move the query + filter logic** from ReadService into the handler
3. **Move the DTO mapping** from ReadService into the handler (or use `.Select()` projection)
4. **Use `WhereIf`** for conditional filters
5. **Delete the ReadService method** once all callers are migrated

### Before (current):
```csharp
// Application/Content/Queries/ListNews/ListNewsQueryHandler.cs
public sealed class ListNewsQueryHandler : IRequestHandler<ListNewsQuery, PagedResult<NewsDto>>
{
    private readonly IContentReadService _readService;

    public ListNewsQueryHandler(IContentReadService readService)
        => _readService = readService;

    public async Task<PagedResult<NewsDto>> Handle(ListNewsQuery request, CancellationToken ct)
        => await _readService.ListNewsAsync(
            request.Search, request.IsFeatured, request.IsPublished,
            request.Page, request.PageSize, ct).ConfigureAwait(false);
}
```

### After (target):
```csharp
// Application/Content/Queries/ListNews/ListNewsQueryHandler.cs
public sealed class ListNewsQueryHandler : IRequestHandler<ListNewsQuery, PagedResult<NewsDto>>
{
    private readonly ICceDbContext _db;

    public ListNewsQueryHandler(ICceDbContext db) => _db = db;

    public async Task<PagedResult<NewsDto>> Handle(ListNewsQuery request, CancellationToken ct)
    {
        var query = _db.News
            .WhereIf(!string.IsNullOrWhiteSpace(request.Search),
                n => n.TitleAr.Contains(request.Search!) ||
                     n.TitleEn.Contains(request.Search!) ||
                     n.Slug.Contains(request.Search!))
            .WhereIf(request.IsPublished == true,  n => n.PublishedOn != null)
            .WhereIf(request.IsPublished == false, n => n.PublishedOn == null)
            .WhereIf(request.IsFeatured.HasValue,  n => n.IsFeatured == request.IsFeatured!.Value)
            .OrderByDescending(n => n.PublishedOn ?? DateTimeOffset.MinValue)
            .ThenByDescending(n => n.Id);

        var result = await query.ToPagedResultAsync(page: request.Page,
            pageSize: request.PageSize, ct).ConfigureAwait(false);
        return result.Map(MapToDto);
    }

    internal static NewsDto MapToDto(News n) => new(
        n.Id, n.TitleAr, n.TitleEn, n.ContentAr, n.ContentEn,
        n.Slug, n.AuthorId, n.FeaturedImageUrl,
        n.PublishedOn, n.IsFeatured, n.IsPublished,
        Convert.ToBase64String(n.RowVersion));
}
```

---

### 2.1 — Content Domain (21 methods → 0)

| # | Query Handler | ReadService Method to Absorb | Priority |
|---|---|---|---|
| 1 | `ListNewsQueryHandler` | `ListNewsAsync` | High |
| 2 | `GetNewsByIdQueryHandler` | `GetNewsByIdAsync` | High |
| 3 | `ListEventsQueryHandler` | `ListEventsAsync` | High |
| 4 | `GetEventByIdQueryHandler` | `GetEventByIdAsync` | High |
| 5 | `ListResourcesQueryHandler` | `ListResourcesAsync` | High |
| 6 | `GetResourceByIdQueryHandler` | `GetResourceByIdAsync` | High |
| 7 | `ListPagesQueryHandler` | `ListPagesAsync` | High |
| 8 | `GetPageByIdQueryHandler` | `GetPageByIdAsync` | High |
| 9 | `ListResourceCategoriesQueryHandler` | `ListResourceCategoriesAsync` | Medium |
| 10 | `GetResourceCategoryByIdQueryHandler` | `GetResourceCategoryByIdAsync` | Medium |
| 11 | `ListHomepageSectionsQueryHandler` | `ListHomepageSectionsAsync` | Medium |
| 12 | `GetAssetByIdQueryHandler` | `GetAssetByIdAsync` | Medium |
| 13 | `ListPublicNewsQueryHandler` | `ListPublicNewsAsync` | High |
| 14 | `GetPublicNewsBySlugQueryHandler` | `GetPublicNewsBySlugAsync` | High |
| 15 | `ListPublicEventsQueryHandler` | `ListPublicEventsAsync` | High |
| 16 | `GetPublicEventByIdQueryHandler` | `GetPublicEventByIdAsync` | High |
| 17 | `ListPublicResourcesQueryHandler` | `ListPublicResourcesAsync` | High |
| 18 | `GetPublicResourceByIdQueryHandler` | `GetPublicResourceByIdAsync` | High |
| 19 | `ListPublicResourceCategoriesQueryHandler` | `ListPublicResourceCategoriesAsync` | Medium |
| 20 | `ListPublicHomepageSectionsQueryHandler` | `ListPublicHomepageSectionsAsync` | Medium |
| 21 | `GetPublicPageBySlugQueryHandler` | `GetPublicPageBySlugAsync` | Medium |

**After all 21 are migrated:**
- Delete `IContentReadService.cs` from Application
- Delete `ContentReadService.cs` from Infrastructure
- Remove registration from `DependencyInjection.cs`

---

### 2.2 — Identity Domain (8 methods → 0)

| # | Query Handler | ReadService Method |
|---|---|---|
| 1 | `ListUsersQueryHandler` | `ListUsersAsync` |
| 2 | `GetUserByIdQueryHandler` | `GetUserByIdAsync` |
| 3 | `ListExpertProfilesQueryHandler` | `ListExpertProfilesAsync` |
| 4 | `ListExpertRequestsQueryHandler` | `ListExpertRequestsAsync` |
| 5 | `ListStateRepAssignmentsQueryHandler` | `ListStateRepAssignmentsAsync` |
| 6 | `GetExpertStatusQueryHandler` | `GetExpertStatusAsync` |
| 7 | Internal callers of `GetUserNamesAsync` | `GetUserNamesAsync` |
| 8 | Internal callers of `UsersExistAsync` | `UsersExistAsync` |

> **Note:** `GetUserNamesAsync` and `UsersExistAsync` may be called from Command handlers (for validation). If so, keep them as a thin `IUserLookupService` interface with just those 2 methods — that's a legitimate cross-cutting lookup, not a God interface.

**After migration:**
- Delete `IIdentityReadService.cs` from Application
- Delete `IdentityReadService.cs` from Infrastructure
- Optionally create `IUserLookupService` with only `GetUserNamesAsync` + `UsersExistAsync`

---

### 2.3 — Community Domain (10 methods → 0)

| # | Query Handler | ReadService Method |
|---|---|---|
| 1 | `ListTopicsQueryHandler` | `ListTopicsAsync` |
| 2 | `GetTopicByIdQueryHandler` | `GetTopicByIdAsync` |
| 3 | `ListAdminPostsQueryHandler` | `ListAdminPostsAsync` |
| 4 | `ListPublicTopicsQueryHandler` | `ListPublicTopicsAsync` |
| 5 | `GetPublicTopicBySlugQueryHandler` | `GetPublicTopicBySlugAsync` |
| 6 | `ListPublicPostsInTopicQueryHandler` | `ListPublicPostsInTopicAsync` |
| 7 | `ListPublicPostRepliesQueryHandler` | `ListPublicPostRepliesAsync` |
| 8 | `GetPublicPostByIdQueryHandler` | `GetPublicPostByIdAsync` |
| 9 | `GetMyFollowsQueryHandler` | `GetMyFollowsAsync` |
| 10 | Any other callers | — |

**After migration:**
- Delete `ICommunityReadService.cs` from Application
- Delete `CommunityReadService.cs` from Infrastructure

---

## Phase 3 — Performance Optimisations

After Phase 2, all reads flow through handlers with `ICceDbContext`. Now optimise hot paths.

### Step 3.1 — Server-Side DTO Projection for List Endpoints

For list endpoints that return summaries (not full content), use `.Select()` to project at the SQL level:

```csharp
// BEFORE — fetches ALL columns including ContentAr, ContentEn (large text)
var result = await query.ToPagedResultAsync(request.Page, request.PageSize, ct);
return result.Map(MapToDto);

// AFTER — SQL only fetches the 5 columns needed for the list DTO
var result = await query.ToPagedResultAsync(
    n => new NewsListItemDto(n.Id, n.TitleAr, n.TitleEn, n.Slug, n.PublishedOn, n.IsFeatured),
    request.Page, request.PageSize, ct);
```

**Apply to these high-traffic list endpoints first:**
- `ListPublicNewsAsync` → `PublicNewsDto` (does NOT need `ContentAr`/`ContentEn`)
- `ListPublicEventsAsync` → `PublicEventDto` (does NOT need full description)
- `ListPublicResourcesAsync` → `PublicResourceDto` (does NOT need description blobs)
- `ListUsersAsync` → `UserListItemDto` (does NOT need full profile)

**By-Id endpoints keep full entity load** — they need all columns for detail views.

### Step 3.2 — Split List DTOs from Detail DTOs

Where a list endpoint and a detail endpoint currently share the same DTO, split them:

| Endpoint Type | DTO | Columns |
|---|---|---|
| `GET /news` (list) | `NewsListItemDto` | Id, TitleAr, TitleEn, Slug, PublishedOn, IsFeatured |
| `GET /news/{id}` (detail) | `NewsDetailDto` | All columns including ContentAr, ContentEn |

This enables server-side projection for lists while keeping full data for detail views.

---

## Phase 4 — Cleanup & DI

### Step 4.1 — Remove Dead ReadService Registrations

**File:** `src/CCE.Infrastructure/DependencyInjection.cs`

Remove these lines:
```csharp
// DELETE these
services.AddScoped<IContentReadService, ContentReadService>();
services.AddScoped<IIdentityReadService, IdentityReadService>();
services.AddScoped<ICommunityReadService, CommunityReadService>();
```

### Step 4.2 — Delete Dead Files

```
DELETE  src/CCE.Application/Content/IContentReadService.cs
DELETE  src/CCE.Application/Identity/IIdentityReadService.cs
DELETE  src/CCE.Application/Community/ICommunityReadService.cs
DELETE  src/CCE.Infrastructure/Content/ContentReadService.cs
DELETE  src/CCE.Infrastructure/Identity/IdentityReadService.cs
DELETE  src/CCE.Infrastructure/Community/CommunityReadService.cs
```

### Step 4.3 — Update Tests

Existing tests mock `IXxxReadService`. After migration:
- Query handler tests mock `ICceDbContext` (return in-memory `IQueryable<T>`) — this pattern already exists in `ListMyNotificationsQueryHandlerTests.cs` and `GetMyUnreadCountQueryHandlerTests.cs`.
- Pattern: `db.News.Returns(testList.AsQueryable())`

---

## Phase 5 — Write Repos (Simplify, Don't Change Pattern)

Write repos stay as-is (specific interfaces, specific implementations). Only small cleanup:

### Step 5.1 — Use `SetExpectedRowVersion` helper in RowVersion repos

Apply to: `NewsRepository`, `ResourceRepository`, `EventRepository`, `PageRepository`

```csharp
// Before
public async Task UpdateAsync(News news, byte[] expectedRowVersion, CancellationToken ct)
{
    var entry = _db.Entry(news);
    entry.OriginalValues[nameof(News.RowVersion)] = expectedRowVersion;
    await _db.SaveChangesAsync(ct).ConfigureAwait(false);
}

// After
public async Task UpdateAsync(News news, byte[] expectedRowVersion, CancellationToken ct)
{
    _db.SetExpectedRowVersion(news, expectedRowVersion);
    await _db.SaveChangesAsync(ct).ConfigureAwait(false);
}
```

---

## Execution Order & Risk Assessment

| Phase | Effort | Risk | Can Ship Independently |
|---|---|---|---|
| **Phase 1** — Foundation helpers | 1 day | None — additive only | ✅ Yes |
| **Phase 2.1** — Content queries | 2 days | Low — 1:1 logic move | ✅ Yes |
| **Phase 2.2** — Identity queries | 1 day | Low | ✅ Yes |
| **Phase 2.3** — Community queries | 1 day | Low | ✅ Yes |
| **Phase 3** — DTO projections | 1 day | Medium — new DTOs, endpoint contract may change | ✅ Yes |
| **Phase 4** — Cleanup | 0.5 day | None — only deleting dead code | ✅ Yes (after Phase 2) |
| **Phase 5** — Write repo cleanup | 0.5 day | None — internal refactor | ✅ Yes |

**Total:** ~7 days

---

## Validation Checklist (Per Handler Migration)

- [ ] Handler injects `ICceDbContext`, NOT a ReadService
- [ ] `ICceDbContext` queryables return `.AsNoTracking()` data (Phase 1.1)
- [ ] Filters use `WhereIf` for clean conditional composition
- [ ] DTO mapping is in the handler (Application layer), NOT Infrastructure
- [ ] List endpoints use `.Select()` projection where possible (Phase 3)
- [ ] `dotnet build CCE.sln` — zero warnings
- [ ] `dotnet test CCE.sln` — all green
- [ ] Swagger response shape unchanged (no API breaking changes)

---

## Files Changed Summary

### New Files
| File | Layer | Purpose |
|---|---|---|
| `Application/Common/Pagination/QueryableExtensions.cs` | Application | `WhereIf` extension |
| `Infrastructure/Persistence/DbContextExtensions.cs` | Infrastructure | `SetExpectedRowVersion` helper |

### Modified Files
| File | Change |
|---|---|
| `Application/Common/Pagination/PagedResult.cs` | Add `Map<TOut>()` method + projection `ToPagedResultAsync` overload |
| `Infrastructure/Persistence/CceDbContext.cs` | Explicit `ICceDbContext` impl with `AsNoTracking()` |
| `Infrastructure/DependencyInjection.cs` | Remove 3 ReadService registrations |
| All 39 query handler files | Inject `ICceDbContext`, own query logic + mapping |
| 4 write repo files | Use `SetExpectedRowVersion` helper |

### Deleted Files
| File | Reason |
|---|---|
| `Application/Content/IContentReadService.cs` | God interface eliminated |
| `Application/Identity/IIdentityReadService.cs` | God interface eliminated |
| `Application/Community/ICommunityReadService.cs` | God interface eliminated |
| `Infrastructure/Content/ContentReadService.cs` | Logic moved to handlers |
| `Infrastructure/Identity/IdentityReadService.cs` | Logic moved to handlers |
| `Infrastructure/Community/CommunityReadService.cs` | Logic moved to handlers |
