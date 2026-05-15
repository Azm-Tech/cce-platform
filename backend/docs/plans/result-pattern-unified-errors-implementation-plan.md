# Result Pattern & Unified Localized Errors — Implementation Plan

## Problem Statement

The current codebase uses **three different patterns** to signal errors from handlers:

### 1. Return `null` → Endpoint checks for 404
```csharp
// Handler returns NewsDto? → null means not found
var dto = await mediator.Send(new UpdateNewsCommand(...), ct);
return dto is null ? Results.NotFound() : Results.Ok(dto);
```
**Problems:**
- Endpoint must guess that `null` means "not found" (no error code, no message)
- Client gets an empty `404` with no localized explanation
- Inconsistent — some handlers throw, others return null

### 2. Throw `KeyNotFoundException` → Middleware maps to 404
```csharp
// Handler throws for not-found
throw new KeyNotFoundException($"News {request.Id} not found.");
```
**Problems:**
- Using **exceptions for control flow** — not-found is an expected outcome, not an exceptional one
- Error messages are English-only hardcoded strings
- No error code for frontend to switch on

### 3. Throw `DomainException` → Middleware maps to 400
```csharp
throw new DomainException("TitleAr is required.");
```
**Problems:**
- English-only messages leaked to API clients
- No structured error code
- Client can't distinguish between different domain failures

### 4. No Unified API Response Envelope
```
GET  /news      → 200 { items: [...], page: 1, ... }     (raw DTO)
GET  /news/{id} → 200 { id: ..., titleAr: ... }          (raw DTO)
GET  /news/{id} → 404  (empty body)
POST /news      → 400  ProblemDetails { title: "..." }    (RFC 7807)
```
**Frontend must handle 4 different response shapes.**

---

## Target Architecture

### Unified Response Shape
```json
// Success
{
  "isSuccess": true,
  "data": { "id": "...", "titleAr": "..." },
  "error": null
}

// Failure
{
  "isSuccess": false,
  "data": null,
  "error": {
    "code": "CONTENT_NEWS_NOT_FOUND",
    "messageAr": "الخبر غير موجود",
    "messageEn": "News not found",
    "type": "NotFound",
    "details": null
  }
}

// Validation Failure
{
  "isSuccess": false,
  "data": null,
  "error": {
    "code": "GENERAL_VALIDATION_ERROR",
    "messageAr": "عذرًا، البيانات المدخلة غير صحيحة",
    "messageEn": "Sorry, the entered data is invalid",
    "type": "Validation",
    "details": {
      "TitleAr": ["REQUIRED_FIELD"],
      "Slug": ["INVALID_FORMAT"]
    }
  }
}
```

### Flow

```
┌──────────────────────────────────────────────────────────┐
│  Handler                                                 │
│                                                          │
│  return Result<NewsDto>.Success(dto);                    │
│  return Result<NewsDto>.Failure(Errors.Content.NewsNotFound); │
│  (never throw for expected failures)                     │
└───────────────────────┬──────────────────────────────────┘
                        │
                        ▼
┌──────────────────────────────────────────────────────────┐
│  ResultBehavior<TRequest, TResponse> (MediatR Pipeline)  │
│  (optional — wraps unhandled exceptions into Result)     │
└───────────────────────┬──────────────────────────────────┘
                        │
                        ▼
┌──────────────────────────────────────────────────────────┐
│  Endpoint                                                │
│                                                          │
│  var result = await mediator.Send(cmd, ct);              │
│  return result.ToHttpResult();  // one-liner             │
│                                                          │
│  Maps ErrorType → HTTP status automatically:             │
│    NotFound    → 404                                     │
│    Validation  → 400                                     │
│    Conflict    → 409                                     │
│    Forbidden   → 403                                     │
│    BusinessRule→ 422                                     │
└──────────────────────────────────────────────────────────┘
```

---

## Inventory: What Already Exists (Reuse)

| Component | Status | Location |
|---|---|---|
| `Error` record (Code, MessageAr, MessageEn, ErrorType, Details) | ✅ Exists | `Domain/Common/Error.cs` |
| `ErrorType` enum (None, Validation, NotFound, Conflict, ...) | ✅ Exists | `Domain/Common/Error.cs` |
| `ApplicationErrors` constants (per domain) | ✅ Exists | `Application/Errors/ApplicationErrors.cs` |
| `Resources.yaml` with bilingual keys | ✅ Exists | `Api.Common/Localization/Resources.yaml` |
| `ILocalizationService` + `LocalizedMessage` | ✅ Exists | `Application/Localization/` |
| `ExceptionHandlingMiddleware` (ProblemDetails) | ✅ Exists (keep as safety net) | `Api.Common/Middleware/` |
| `Result<T>` wrapper | ❌ Missing | Needs creation |
| Error factory methods | ❌ Missing | Needs creation |
| `Result → IResult` mapper for endpoints | ❌ Missing | Needs creation |
| `ValidationBehavior` → `Result<T>` integration | ❌ Needs update | Currently throws `ValidationException` |

---

## Phase 1 — Core `Result<T>` Type (Application Layer)

### Step 1.1 — Create `Result<T>`

**File:** `src/CCE.Application/Common/Result.cs` (new)

```csharp
using CCE.Domain.Common;

namespace CCE.Application.Common;

/// <summary>
/// Discriminated result type for handler returns. Replaces returning null (not-found)
/// and throwing exceptions for expected business failures.
/// </summary>
public sealed record Result<T>
{
    public bool IsSuccess { get; private init; }
    public T? Data { get; private init; }
    public Error? Error { get; private init; }

    private Result() { }

    public static Result<T> Success(T data) => new() { IsSuccess = true, Data = data };
    public static Result<T> Failure(Error error) => new() { IsSuccess = false, Error = error };

    /// <summary>Allow implicit conversion from T for clean handler returns.</summary>
    public static implicit operator Result<T>(T data) => Success(data);

    /// <summary>Allow implicit conversion from Error for clean handler returns.</summary>
    public static implicit operator Result<T>(Error error) => Failure(error);
}

/// <summary>
/// Non-generic companion for void commands that return no data on success.
/// </summary>
public static class Result
{
    private static readonly Result<Unit> SuccessUnit = Result<Unit>.Success(Unit.Value);

    public static Result<Unit> Success() => SuccessUnit;
    public static Result<Unit> Failure(Error error) => Result<Unit>.Failure(error);
}

/// <summary>Unit type for commands that return no data.</summary>
public readonly record struct Unit
{
    public static readonly Unit Value = default;
}
```

> **Note:** We define our own `Unit` instead of using MediatR's `Unit` so the Application layer doesn't need MediatR for this type.

---

### Step 1.2 — Create Localized Error Factory

**File:** `src/CCE.Application/Common/Errors.cs` (new)

This bridges `ApplicationErrors` constants with `ILocalizationService` to produce fully localized `Error` records.

```csharp
using CCE.Application.Errors;
using CCE.Application.Localization;
using CCE.Domain.Common;

namespace CCE.Application.Common;

/// <summary>
/// Factory for creating localized <see cref="Error"/> instances.
/// Each method looks up the bilingual message from Resources.yaml.
/// </summary>
public sealed class Errors
{
    private readonly ILocalizationService _l;

    public Errors(ILocalizationService l) => _l = l;

    // ─── General ───
    public Error NotFound(string code)
        => Build(code, ErrorType.NotFound);
    public Error Conflict(string code)
        => Build(code, ErrorType.Conflict);
    public Error BusinessRule(string code)
        => Build(code, ErrorType.BusinessRule);
    public Error Validation(string code, IDictionary<string, string[]>? details = null)
        => Build(code, ErrorType.Validation, details);
    public Error Forbidden(string code)
        => Build(code, ErrorType.Forbidden);

    // ─── Convenience: Content domain ───
    public Error NewsNotFound()      => NotFound($"CONTENT_{ApplicationErrors.Content.NEWS_NOT_FOUND}");
    public Error EventNotFound()     => NotFound($"CONTENT_{ApplicationErrors.Content.EVENT_NOT_FOUND}");
    public Error ResourceNotFound()  => NotFound($"CONTENT_{ApplicationErrors.Content.RESOURCE_NOT_FOUND}");
    public Error PageNotFound()      => NotFound($"CONTENT_{ApplicationErrors.Content.PAGE_NOT_FOUND}");
    public Error CategoryNotFound()  => NotFound($"CONTENT_{ApplicationErrors.Content.CATEGORY_NOT_FOUND}");
    public Error AssetNotFound()     => NotFound($"CONTENT_{ApplicationErrors.Content.ASSET_NOT_FOUND}");

    // ─── Convenience: Identity domain ───
    public Error UserNotFound()      => NotFound($"IDENTITY_{ApplicationErrors.Identity.USER_NOT_FOUND}");
    public Error ExpertRequestNotFound() => NotFound($"IDENTITY_{ApplicationErrors.Identity.EXPERT_REQUEST_NOT_FOUND}");

    // ─── Convenience: Community domain ───
    public Error TopicNotFound()     => NotFound($"COMMUNITY_{ApplicationErrors.Community.TOPIC_NOT_FOUND}");
    public Error PostNotFound()      => NotFound($"COMMUNITY_{ApplicationErrors.Community.POST_NOT_FOUND}");
    public Error ReplyNotFound()     => NotFound($"COMMUNITY_{ApplicationErrors.Community.REPLY_NOT_FOUND}");

    // ─── Convenience: Country domain ───
    public Error CountryNotFound()   => NotFound($"COUNTRY_{ApplicationErrors.Country.COUNTRY_NOT_FOUND}");

    private Error Build(string code, ErrorType type, IDictionary<string, string[]>? details = null)
    {
        var msg = _l.GetLocalizedMessage(code);
        return new Error(code, msg.Ar, msg.En, type, details);
    }
}
```

**Registration:** `services.AddScoped<Errors>();` in `Application/DependencyInjection.cs`.

---

### Step 1.3 — Create `ResultExtensions` for Minimal API Endpoints

**File:** `src/CCE.Api.Common/Extensions/ResultExtensions.cs` (new)

```csharp
using CCE.Application.Common;
using CCE.Domain.Common;
using Microsoft.AspNetCore.Http;

namespace CCE.Api.Common.Extensions;

public static class ResultExtensions
{
    /// <summary>
    /// Maps a <see cref="Result{T}"/> to an <see cref="IResult"/> with the correct HTTP status.
    /// </summary>
    public static IResult ToHttpResult<T>(
        this Result<T> result,
        int successStatusCode = StatusCodes.Status200OK)
    {
        if (result.IsSuccess)
        {
            return successStatusCode switch
            {
                StatusCodes.Status201Created => Results.Created((string?)null, result),
                StatusCodes.Status204NoContent => Results.NoContent(),
                _ => Results.Json(result, statusCode: successStatusCode)
            };
        }

        var statusCode = result.Error!.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.BusinessRule => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status500InternalServerError,
        };

        return Results.Json(result, statusCode: statusCode);
    }

    /// <summary>Shorthand for 201 Created.</summary>
    public static IResult ToCreatedHttpResult<T>(this Result<T> result)
        => result.ToHttpResult(StatusCodes.Status201Created);

    /// <summary>Shorthand for 204 NoContent (void commands).</summary>
    public static IResult ToNoContentHttpResult(this Result<Unit> result)
        => result.ToHttpResult(StatusCodes.Status204NoContent);
}
```

---

## Phase 2 — Update `ValidationBehavior` to Return `Result<T>`

### Step 2.1 — Create `ResultValidationBehavior`

The current `ValidationBehavior` throws `ValidationException`. For handlers that return `Result<T>`, we need a behavior that returns a `Result<T>.Failure(validationError)` instead.

**File:** `src/CCE.Application/Common/Behaviors/ResultValidationBehavior.cs` (new)

```csharp
using CCE.Application.Localization;
using CCE.Domain.Common;
using FluentValidation;
using MediatR;

namespace CCE.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior for requests returning <see cref="Result{T}"/>.
/// Instead of throwing <see cref="ValidationException"/>, it returns a failure Result
/// with localized messages and structured field-level details.
/// </summary>
public sealed class ResultValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : class
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILocalizationService _localization;

    public ResultValidationBehavior(
        IEnumerable<IValidator<TRequest>> validators,
        ILocalizationService localization)
    {
        _validators = validators;
        _localization = localization;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Only intercept when TResponse is Result<T>
        if (!IsResultType(typeof(TResponse)))
        {
            // Fall through to existing ValidationBehavior for non-Result handlers
            return await next().ConfigureAwait(false);
        }

        if (!_validators.Any())
            return await next().ConfigureAwait(false);

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)))
            .ConfigureAwait(false);

        var failures = results.SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
            return await next().ConfigureAwait(false);

        // Build structured details: { "TitleAr": ["REQUIRED_FIELD"], "Slug": ["INVALID_FORMAT"] }
        var details = failures
            .GroupBy(f => f.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => f.ErrorMessage).ToArray());

        var msg = _localization.GetLocalizedMessage("GENERAL_VALIDATION_ERROR");
        var error = new Error(
            "GENERAL_VALIDATION_ERROR",
            msg.Ar, msg.En,
            ErrorType.Validation,
            details);

        // Use reflection to call Result<T>.Failure(error)
        var innerType = typeof(TResponse).GetGenericArguments()[0];
        var failureMethod = typeof(Result<>)
            .MakeGenericType(innerType)
            .GetMethod("Failure")!;

        return (TResponse)failureMethod.Invoke(null, [error])!;
    }

    private static bool IsResultType(Type type)
        => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Result<>);
}
```

### Step 2.2 — Register the Behavior

**File:** `src/CCE.Application/DependencyInjection.cs` (edit existing)

```csharp
services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(assembly);
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(ResultValidationBehavior<,>)); // NEW — before old one
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));        // existing — for non-Result handlers
});
```

> **Important:** `ResultValidationBehavior` runs first for `Result<T>` handlers. `ValidationBehavior` still runs for legacy handlers that haven't been migrated yet. This allows **gradual migration**.

---

## Phase 3 — Migrate Handlers (Per Domain)

### Migration Recipe Per Handler

#### Command Handler (was: throw or return null)

**Before:**
```csharp
public sealed class DeleteNewsCommandHandler : IRequestHandler<DeleteNewsCommand, MediatR.Unit>
{
    public async Task<MediatR.Unit> Handle(DeleteNewsCommand request, CancellationToken ct)
    {
        var news = await _service.FindAsync(request.Id, ct);
        if (news is null)
            throw new KeyNotFoundException($"News {request.Id} not found.");
        // ...
        return MediatR.Unit.Value;
    }
}
```

**After:**
```csharp
public sealed class DeleteNewsCommandHandler : IRequestHandler<DeleteNewsCommand, Result<Unit>>
{
    private readonly INewsRepository _repo;
    private readonly Errors _errors;
    // ...

    public async Task<Result<Unit>> Handle(DeleteNewsCommand request, CancellationToken ct)
    {
        var news = await _repo.FindAsync(request.Id, ct);
        if (news is null)
            return _errors.NewsNotFound();   // ← localized, typed, no exception

        var deletedById = _currentUser.GetUserId()
            ?? throw new DomainException("Cannot delete news without user identity.");

        news.SoftDelete(deletedById, _clock);
        await _repo.UpdateAsync(news, news.RowVersion, ct);
        return Result.Success();
    }
}
```

**Command record:**
```csharp
// Before
public sealed record DeleteNewsCommand(Guid Id) : IRequest<MediatR.Unit>;

// After
public sealed record DeleteNewsCommand(Guid Id) : IRequest<Result<Unit>>;
```

#### Query Handler — GetById (was: return null)

**Before:**
```csharp
// Handler returns NewsDto?
// Endpoint: return dto is null ? Results.NotFound() : Results.Ok(dto);
```

**After:**
```csharp
public sealed class GetNewsByIdQueryHandler : IRequestHandler<GetNewsByIdQuery, Result<NewsDto>>
{
    private readonly ICceDbContext _db;
    private readonly Errors _errors;

    public async Task<Result<NewsDto>> Handle(GetNewsByIdQuery request, CancellationToken ct)
    {
        var news = await _db.News
            .Where(n => n.Id == request.Id)
            .ToListAsyncEither(ct);

        var entity = news.SingleOrDefault();
        if (entity is null)
            return _errors.NewsNotFound();

        return MapToDto(entity);  // implicit conversion to Result<NewsDto>.Success
    }
}
```

#### Endpoint (simplified)

**Before:**
```csharp
news.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
{
    var dto = await mediator.Send(new GetNewsByIdQuery(id), ct);
    return dto is null ? Results.NotFound() : Results.Ok(dto);
});
```

**After:**
```csharp
news.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
{
    var result = await mediator.Send(new GetNewsByIdQuery(id), ct);
    return result.ToHttpResult();
});
```

**Every endpoint becomes a one-liner.** The `ErrorType` → HTTP status mapping is automatic.

---

### 3.1 — Content Domain Commands

| # | Handler | Current Return | New Return | Not-Found Pattern |
|---|---|---|---|---|
| 1 | `CreateNewsCommandHandler` | `NewsDto` | `Result<NewsDto>` | N/A (always creates) |
| 2 | `UpdateNewsCommandHandler` | `NewsDto?` | `Result<NewsDto>` | `_errors.NewsNotFound()` |
| 3 | `DeleteNewsCommandHandler` | `MediatR.Unit` | `Result<Unit>` | `_errors.NewsNotFound()` |
| 4 | `PublishNewsCommandHandler` | `NewsDto?` | `Result<NewsDto>` | `_errors.NewsNotFound()` |
| 5 | `CreateEventCommandHandler` | `EventDto` | `Result<EventDto>` | N/A |
| 6 | `UpdateEventCommandHandler` | `EventDto?` | `Result<EventDto>` | `_errors.EventNotFound()` |
| 7 | `DeleteEventCommandHandler` | `MediatR.Unit` | `Result<Unit>` | `_errors.EventNotFound()` |
| 8 | `RescheduleEventCommandHandler` | `EventDto?` | `Result<EventDto>` | `_errors.EventNotFound()` |
| 9 | `CreateResourceCommandHandler` | `ResourceDto` | `Result<ResourceDto>` | N/A |
| 10 | `UpdateResourceCommandHandler` | `ResourceDto?` | `Result<ResourceDto>` | `_errors.ResourceNotFound()` |
| 11 | `PublishResourceCommandHandler` | `ResourceDto?` | `Result<ResourceDto>` | `_errors.ResourceNotFound()` |
| 12 | `CreatePageCommandHandler` | `PageDto` | `Result<PageDto>` | N/A |
| 13 | `UpdatePageCommandHandler` | `PageDto?` | `Result<PageDto>` | `_errors.PageNotFound()` |
| 14 | `DeletePageCommandHandler` | `MediatR.Unit` | `Result<Unit>` | `_errors.PageNotFound()` |
| 15 | `CreateResourceCategoryCommandHandler` | `ResourceCategoryDto` | `Result<ResourceCategoryDto>` | N/A |
| 16 | `UpdateResourceCategoryCommandHandler` | `ResourceCategoryDto?` | `Result<ResourceCategoryDto>` | `_errors.CategoryNotFound()` |
| 17 | `DeleteResourceCategoryCommandHandler` | `MediatR.Unit` | `Result<Unit>` | `_errors.CategoryNotFound()` |
| 18 | `CreateHomepageSectionCommandHandler` | `HomepageSectionDto` | `Result<HomepageSectionDto>` | N/A |
| 19 | `UpdateHomepageSectionCommandHandler` | `HomepageSectionDto?` | `Result<HomepageSectionDto>` | `_errors.HomepageSectionNotFound()` |
| 20 | `DeleteHomepageSectionCommandHandler` | `MediatR.Unit` | `Result<Unit>` | `_errors.HomepageSectionNotFound()` |
| 21 | `ReorderHomepageSectionsCommandHandler` | `MediatR.Unit` | `Result<Unit>` | N/A |
| 22 | `UploadAssetCommandHandler` | `AssetFileDto` | `Result<AssetFileDto>` | N/A |
| 23 | `ApproveCountryResourceRequestCommandHandler` | varies | `Result<...>` | `_errors.NotFound(...)` |
| 24 | `RejectCountryResourceRequestCommandHandler` | varies | `Result<...>` | `_errors.NotFound(...)` |

### 3.2 — Content Domain Queries

| # | Handler | Current Return | New Return |
|---|---|---|---|
| 1 | `ListNewsQueryHandler` | `PagedResult<NewsDto>` | `Result<PagedResult<NewsDto>>` |
| 2 | `GetNewsByIdQueryHandler` | `NewsDto?` | `Result<NewsDto>` |
| 3 | `ListEventsQueryHandler` | `PagedResult<EventDto>` | `Result<PagedResult<EventDto>>` |
| 4 | `GetEventByIdQueryHandler` | `EventDto?` | `Result<EventDto>` |
| ... | (all other query handlers) | `T?` or `PagedResult<T>` | `Result<T>` or `Result<PagedResult<T>>` |

> **Note on List queries:** List queries never "fail" — an empty list is a valid success. `Result<PagedResult<T>>` wrapping is still valuable for **consistency** so the frontend always sees the same envelope. However, you could choose to keep list queries returning `PagedResult<T>` directly (unwrapped) if you prefer less ceremony on reads. **Pick one convention and stick to it.**

### 3.3 — Identity Domain

Same pattern. Replace `KeyNotFoundException` throws with `_errors.UserNotFound()`, `_errors.ExpertRequestNotFound()` etc.

### 3.4 — Community Domain

Same pattern. Replace `KeyNotFoundException` throws with `_errors.TopicNotFound()`, `_errors.PostNotFound()`, `_errors.ReplyNotFound()`.

### 3.5 — Other Domains (Country, Notifications, KnowledgeMaps, InteractiveCity, Surveys)

Same recipe. Each domain already has error constants in `ApplicationErrors` and YAML keys in `Resources.yaml`.

---

## Phase 4 — DomainException Integration

### Keep `DomainException` for TRUE invariant violations

`DomainException` is thrown from **Domain entity methods** (`News.Draft()`, `News.UpdateContent()`) where you cannot return a `Result<T>`. These are **programming errors** (caller passed bad data past validation), not expected user-facing failures.

**Do not change Domain entities.** The `ExceptionHandlingMiddleware` stays as a safety net for:
- `DomainException` → 400
- `ConcurrencyException` → 409
- `DuplicateException` → 409
- Unhandled `Exception` → 500

But now the middleware also localizes these:

### Step 4.1 — Enhance Middleware to Use Localization

**File:** `src/CCE.Api.Common/Middleware/ExceptionHandlingMiddleware.cs` (edit)

```csharp
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    // ...existing constructor...

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (ValidationException ex)
        {
            var l = context.RequestServices.GetService<ILocalizationService>();
            await WriteValidationResultAsync(context, ex, l).ConfigureAwait(false);
        }
        catch (ConcurrencyException ex)
        {
            var l = context.RequestServices.GetService<ILocalizationService>();
            await WriteErrorResultAsync(context, StatusCodes.Status409Conflict,
                "CONCURRENCY_CONFLICT", ErrorType.Conflict, ex.Message, l).ConfigureAwait(false);
        }
        catch (DuplicateException ex)
        {
            var l = context.RequestServices.GetService<ILocalizationService>();
            await WriteErrorResultAsync(context, StatusCodes.Status409Conflict,
                "DUPLICATE_VALUE", ErrorType.Conflict, ex.Message, l).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            var l = context.RequestServices.GetService<ILocalizationService>();
            await WriteErrorResultAsync(context, StatusCodes.Status400BadRequest,
                "GENERAL_BAD_REQUEST", ErrorType.BusinessRule, ex.Message, l).ConfigureAwait(false);
        }
        catch (KeyNotFoundException ex)
        {
            // Legacy — still caught for non-migrated handlers
            var l = context.RequestServices.GetService<ILocalizationService>();
            await WriteErrorResultAsync(context, StatusCodes.Status404NotFound,
                "GENERAL_NOT_FOUND", ErrorType.NotFound, ex.Message, l).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            var l = context.RequestServices.GetService<ILocalizationService>();
            await WriteErrorResultAsync(context, StatusCodes.Status500InternalServerError,
                "GENERAL_INTERNAL_ERROR", ErrorType.Internal, null, l).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Writes a unified error response matching the Result{T} shape,
    /// so clients always see the same JSON structure regardless of
    /// whether the error came from a handler or the middleware.
    /// </summary>
    private static async Task WriteErrorResultAsync(
        HttpContext ctx, int statusCode, string code, ErrorType type,
        string? fallbackMessage, ILocalizationService? l)
    {
        var msg = l?.GetLocalizedMessage(code);
        var error = new Error(
            code,
            msg?.Ar ?? fallbackMessage ?? "خطأ",
            msg?.En ?? fallbackMessage ?? "Error",
            type);

        var envelope = new { isSuccess = false, data = (object?)null, error };

        ctx.Response.StatusCode = statusCode;
        ctx.Response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(ctx.Response.Body, envelope,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            .ConfigureAwait(false);
    }
}
```

Now **every response** — success or failure, from handler or middleware — uses the same JSON shape.

---

## Phase 5 — Add Missing YAML Keys

**File:** `src/CCE.Api.Common/Localization/Resources.yaml` (append)

```yaml
CONCURRENCY_CONFLICT:
  ar: "تم تعديل هذا السجل من قبل مستخدم آخر. يرجى تحديث الصفحة والمحاولة مرة أخرى"
  en: "This record was modified by another user. Please refresh and try again"

DUPLICATE_VALUE:
  ar: "القيمة موجودة بالفعل"
  en: "Value already exists"

NOTIFICATION_TEMPLATE_NOT_FOUND:
  ar: "قالب الإشعار غير موجود"
  en: "Notification template not found"

KNOWLEDGE_MAP_NOT_FOUND:
  ar: "خريطة المعرفة غير موجودة"
  en: "Knowledge map not found"

SCENARIO_NOT_FOUND:
  ar: "السيناريو غير موجود"
  en: "Scenario not found"
```

---

## Phase 6 — Update Endpoints (Per API)

### Recipe Per Endpoint

**Before:**
```csharp
news.MapPut("/{id:guid}", async (Guid id, UpdateNewsRequest body,
    IMediator mediator, CancellationToken ct) =>
{
    var cmd = new UpdateNewsCommand(id, body.TitleAr, ...);
    var dto = await mediator.Send(cmd, ct);
    return dto is null ? Results.NotFound() : Results.Ok(dto);
});
```

**After:**
```csharp
news.MapPut("/{id:guid}", async (Guid id, UpdateNewsRequest body,
    IMediator mediator, CancellationToken ct) =>
{
    var cmd = new UpdateNewsCommand(id, body.TitleAr, ...);
    var result = await mediator.Send(cmd, ct);
    return result.ToHttpResult();
});
```

Every endpoint becomes **the same 3 lines**: build command/query → send → `.ToHttpResult()`.

---

## Execution Order & Risk Assessment

| Phase | Effort | Risk | Can Ship Independently |
|---|---|---|---|
| **Phase 1** — `Result<T>`, `Errors` factory, `ResultExtensions` | 1 day | None — additive | ✅ Yes |
| **Phase 2** — `ResultValidationBehavior` | 0.5 day | Low — new behavior, old one still works | ✅ Yes |
| **Phase 3.1** — Content handlers | 2 days | Medium — changes handler + command + endpoint signatures | ✅ Per handler |
| **Phase 3.2–3.5** — Other domains | 2 days | Medium | ✅ Per domain |
| **Phase 4** — Middleware localization | 0.5 day | Low — changes error format | ✅ Yes |
| **Phase 5** — YAML keys | 0.5 day | None — additive | ✅ Yes |
| **Phase 6** — Endpoint cleanup | 1 day | Low — 1:1 mapping | ✅ Per API |

**Total:** ~7.5 days

---

## Gradual Migration Strategy

This plan is designed for **zero big-bang**:

1. **Phase 1–2** are purely additive — no existing code breaks
2. **Phase 3** is per-handler:
   - Change `DeleteNewsCommand : IRequest<MediatR.Unit>` → `IRequest<Result<Unit>>`
   - Change handler return type
   - Change endpoint to use `.ToHttpResult()`
   - **All three happen atomically per feature** — one PR per handler group
3. **Old handlers** (`IRequest<NewsDto?>`) still work with the existing `ValidationBehavior` and middleware
4. **New handlers** (`IRequest<Result<NewsDto>>`) use `ResultValidationBehavior` automatically
5. Once all handlers are migrated, delete the old `ValidationBehavior` (throwing) and `MediatR.Unit` usages

---

## Validation Checklist (Per Handler Migration)

- [ ] Command/Query record uses `IRequest<Result<T>>` not `IRequest<T>`
- [ ] Handler injects `Errors` factory
- [ ] Handler returns `_errors.XxxNotFound()` instead of `throw new KeyNotFoundException` or `return null`
- [ ] Handler returns implicit `Result<T>` on success (e.g., `return dto;`)
- [ ] Endpoint uses `result.ToHttpResult()` — no manual `Results.NotFound()` / `Results.Ok()`
- [ ] FluentValidation validator unchanged (still uses same rules)
- [ ] Tests updated: assert `result.IsSuccess` / `result.Error.Code` instead of catching exceptions
- [ ] `dotnet build CCE.sln` — zero warnings
- [ ] `dotnet test CCE.sln` — all green
- [ ] API response shape matches the unified envelope

---

## Files Changed Summary

### New Files
| File | Layer | Purpose |
|---|---|---|
| `Application/Common/Result.cs` | Application | `Result<T>` + `Unit` |
| `Application/Common/Errors.cs` | Application | Localized error factory |
| `Application/Common/Behaviors/ResultValidationBehavior.cs` | Application | Validation → Result (no throw) |
| `Api.Common/Extensions/ResultExtensions.cs` | API | `Result<T>` → `IResult` HTTP mapper |

### Modified Files
| File | Change |
|---|---|
| `Application/DependencyInjection.cs` | Register `Errors` + `ResultValidationBehavior` |
| `Api.Common/Middleware/ExceptionHandlingMiddleware.cs` | Localized error envelope format |
| `Api.Common/Localization/Resources.yaml` | Add missing YAML keys |
| All command/query records | `IRequest<T?>` → `IRequest<Result<T>>` |
| All handlers | Return `Result<T>` instead of throw/null |
| All endpoint files | Use `.ToHttpResult()` |
| All handler test files | Assert on `result.IsSuccess` / `result.Error.Code` |

### Deleted Files (after full migration)
| File | When |
|---|---|
| `Application/Common/Behaviors/ValidationBehavior.cs` | After ALL handlers are migrated to `Result<T>` |
