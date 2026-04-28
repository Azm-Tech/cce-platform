# Phase 00 — Cross-cutting infrastructure

> Parent: [`../2026-04-28-internal-api.md`](../2026-04-28-internal-api.md) · Spec: [`../../specs/2026-04-28-internal-api-design.md`](../../specs/2026-04-28-internal-api-design.md) §3.2

**Phase goal:** Lay the cross-cutting infrastructure every later phase needs:
1. JIT user sync middleware (Keycloak `sub` → `users` row).
2. `ConcurrencyException` → 409 ProblemDetails mapping (extends Foundation's `ExceptionHandlingMiddleware`).
3. `PagedResult<T>` + EF extension.
4. OpenAPI per-API path split (`/swagger/internal/v1/swagger.json`) + drift check.
5. New `Audit.Read` permission seeded into `permissions.yaml`.

After Phase 00, every subsequent feature-area phase has the building blocks it needs.

**Tasks in this phase:** 5
**Working directory:** `/Users/m/CCE/`
**Preconditions:**
- Sub-project 2 closed (`data-domain-v0.1.0` tagged at `21d234b`).
- 376 backend tests + 1 skipped passing.
- Both API hosts running on 5001/5002 (or stoppable for restarts).

---

## Pre-execution sanity checks

1. `git status` clean apart from `.claude/`.
2. `git tag -l | grep data-domain-v0.1.0` → present.
3. `dotnet build backend/CCE.sln --no-restore` 0 errors.
4. `dotnet test backend/CCE.sln --no-build --no-restore --logger "console;verbosity=minimal" | grep -E "Passed!|Failed!"` → 6 result lines, all `Passed!` (Domain 284 / Application 12 / Infrastructure 30 + 1 skipped / Architecture 12 / SourceGen 10 / Api Integration 28).

If any fail, stop and report.

---

## Task 0.1: `Audit.Read` permission added to `permissions.yaml`

**Files:**
- Modify: `backend/permissions.yaml`
- Modify: `backend/tests/CCE.Domain.Tests/PermissionsYamlSchemaTests.cs` (bump count from 41 → 42, add Audit.Read sentinel)

**Rationale:** Phase 8 audit-log query needs a new permission. Adding it now means Phase 1's user-management endpoints can already use `Audit.Read` if they reference it, and the permissions source generator picks up the new entry on the next build.

- [ ] **Step 1: Add the new group + permission to YAML**

Open `backend/permissions.yaml`. Find the `Notification:` group near the bottom; insert the new `Audit:` group immediately after it (before `Report:` for alphabetical-ish ordering). The diff:

```yaml
  Notification:
    TemplateManage:
      description: Manage notification templates
      roles: [SuperAdmin]
+ Audit:
+   Read:
+     description: Query the audit-event log
+     roles: [SuperAdmin]
  Report:
    UserRegistrations:
      ...
```

The full updated `Audit` block to insert:

```yaml
  Audit:
    Read:
      description: Query the audit-event log
      roles: [SuperAdmin]
```

- [ ] **Step 2: Bump the BRD-coverage count from 41 to 42**

Open `backend/tests/CCE.Domain.Tests/PermissionsYamlSchemaTests.cs`. Find the test:

```csharp
[Fact]
public void Permissions_All_count_matches_BRD_matrix()
{
    Permissions.All.Count.Should().Be(41);
}
```

Change `41` to `42`.

Also append `"Audit.Read"` to the `BrdRequiredSentinel` static-readonly array (so the BRD-coverage test asserts the new entry is present):

```csharp
private static readonly string[] BrdRequiredSentinel =
{
    "System.Health.Read",
    ...
    "Report.UserRegistrations",
    "Audit.Read",
};
```

- [ ] **Step 3: Build + run the affected tests**

```bash
dotnet build backend/CCE.sln --nologo --no-restore 2>&1 | tail -5
```

Expected: 0 errors. (The source generator regenerates `Permissions.Audit_Read` automatically.)

```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~PermissionsYamlSchemaTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
```

Expected: `Passed: 7` (existing 7 schema tests, unchanged count, unchanged outcomes).

Run the source-gen tests too to confirm the generator handles the new permission cleanly:

```bash
dotnet test backend/tests/CCE.Domain.SourceGenerators.Tests/CCE.Domain.SourceGenerators.Tests.csproj --nologo --no-restore --logger "console;verbosity=minimal" 2>&1 | tail -3
```

Expected: `Passed: 10`.

- [ ] **Step 4: Re-run the seeder to add the claim** (against dev DB)

```bash
cd /Users/m/CCE/backend
# The seeder isn't wired into a CLI yet; run via a tiny helper test, or simply restart the API hosts
# whose startup will eventually run seeders (sub-project 5 wiring). For Phase 00, regenerating the
# YAML is enough; the new claim row gets created next time RolesAndPermissionsSeeder runs.
# Verify: dotnet test --filter RolesAndPermissionsSeederTests --no-restore --no-build  → still green.
dotnet test backend/tests/CCE.Infrastructure.Tests/CCE.Infrastructure.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~RolesAndPermissionsSeederTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
```

Expected: `Passed: 3`. (The seeder picks up `Audit.Read` from `RolePermissionMap.SuperAdmin` automatically — no seeder code change needed.)

- [ ] **Step 5: Commit**

```bash
git add backend/permissions.yaml backend/tests/CCE.Domain.Tests/PermissionsYamlSchemaTests.cs
git -c commit.gpgsign=false commit -m "feat(permissions): add Audit.Read permission (SuperAdmin only) for sub-project 3 audit-log query"
```

---

## Task 0.2: `PagedResult<T>` + `IQueryable<T>` extension

**Files:**
- Create: `backend/src/CCE.Application/Common/Pagination/PagedResult.cs`
- Create: `backend/tests/CCE.Application.Tests/Common/Pagination/PaginationExtensionsTests.cs`

**Rationale:** Every list endpoint in phases 1–8 returns `PagedResult<T>`. Centralizing the type + the `ToPagedResultAsync` extension keeps every handler short and consistent.

- [ ] **Step 1: Write the failing tests first**

`backend/tests/CCE.Application.Tests/Common/Pagination/PaginationExtensionsTests.cs`:

```csharp
using CCE.Application.Common.Pagination;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.Tests.Common.Pagination;

public class PaginationExtensionsTests
{
    [Fact]
    public async Task ToPagedResultAsync_returns_first_page_with_total()
    {
        var data = Enumerable.Range(1, 25).ToList();
        var query = data.AsQueryable();

        var result = await query.ToPagedResultAsync(page: 1, pageSize: 10, ct: CancellationToken.None);

        result.Total.Should().Be(25);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.Items.Should().HaveCount(10);
        result.Items[0].Should().Be(1);
        result.Items[9].Should().Be(10);
    }

    [Fact]
    public async Task ToPagedResultAsync_clamps_pageSize_to_max_100()
    {
        var data = Enumerable.Range(1, 200).ToList();
        var query = data.AsQueryable();

        var result = await query.ToPagedResultAsync(page: 1, pageSize: 500, ct: CancellationToken.None);

        result.PageSize.Should().Be(100);
        result.Items.Should().HaveCount(100);
    }

    [Fact]
    public async Task ToPagedResultAsync_clamps_pageSize_to_min_1()
    {
        var data = Enumerable.Range(1, 5).ToList();
        var query = data.AsQueryable();

        var result = await query.ToPagedResultAsync(page: 1, pageSize: 0, ct: CancellationToken.None);

        result.PageSize.Should().Be(1);
        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task ToPagedResultAsync_clamps_page_to_min_1()
    {
        var data = Enumerable.Range(1, 5).ToList();
        var query = data.AsQueryable();

        var result = await query.ToPagedResultAsync(page: 0, pageSize: 10, ct: CancellationToken.None);

        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task ToPagedResultAsync_returns_empty_items_for_page_past_end()
    {
        var data = Enumerable.Range(1, 5).ToList();
        var query = data.AsQueryable();

        var result = await query.ToPagedResultAsync(page: 99, pageSize: 10, ct: CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.Total.Should().Be(5);
        result.Page.Should().Be(99);
    }
}
```

(Using an in-memory `IQueryable<int>` as the source — the extension method has to work for any provider. EF behavior is tested in integration tests later.)

- [ ] **Step 2: Run — expect compile error**

```bash
dotnet test backend/tests/CCE.Application.Tests/CCE.Application.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~PaginationExtensionsTests" 2>&1 | tail -8
```

Expected: build failure referencing `PagedResult` / `ToPagedResultAsync` not found.

- [ ] **Step 3: Implement**

`backend/src/CCE.Application/Common/Pagination/PagedResult.cs`:

```csharp
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.Common.Pagination;

/// <summary>
/// Page of <typeparamref name="T"/> entries plus the total count for the unpaged query.
/// </summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long Total);

public static class PaginationExtensions
{
    public const int MaxPageSize = 100;

    /// <summary>
    /// Materialises an <see cref="IQueryable{T}"/> as a <see cref="PagedResult{T}"/>.
    /// <c>page</c> is 1-based, clamped to <c>&gt;= 1</c>. <c>pageSize</c> is clamped to <c>[1, 100]</c>.
    /// </summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var total = query is IAsyncEnumerable<T>
            ? await query.LongCountAsync(ct).ConfigureAwait(false)
            : query.LongCount();
        var items = query is IAsyncEnumerable<T>
            ? await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct).ConfigureAwait(false)
            : query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new PagedResult<T>(items, page, pageSize, total);
    }
}
```

The branch on `IAsyncEnumerable<T>` lets the extension work both with EF (which always implements the interface) and with plain LINQ-to-Objects in unit tests.

- [ ] **Step 4: Run — expect pass**

```bash
dotnet test backend/tests/CCE.Application.Tests/CCE.Application.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~PaginationExtensionsTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
```

Expected: `Passed: 5`.

- [ ] **Step 5: Commit**

```bash
git add backend/src/CCE.Application/Common/Pagination/PagedResult.cs backend/tests/CCE.Application.Tests/Common/Pagination/PaginationExtensionsTests.cs
git -c commit.gpgsign=false commit -m "feat(application): PagedResult<T> + IQueryable.ToPagedResultAsync extension (5 TDD tests)"
```

---

## Task 0.3: Concurrency 409 mapping in `ExceptionHandlingMiddleware`

**Files:**
- Modify: `backend/src/CCE.Api.Common/Middleware/ExceptionHandlingMiddleware.cs`
- Create: `backend/tests/CCE.Api.IntegrationTests/Middleware/ExceptionHandlingMiddlewareConcurrencyTests.cs`

**Rationale:** Sub-project 2 added `ConcurrencyException` (DbExceptionMapper output) and `DuplicateException`. The existing middleware only maps `ValidationException` and treats everything else as 500. Phase 0 adds the two domain exception kinds → 409.

- [ ] **Step 1: Write failing integration test**

`backend/tests/CCE.Api.IntegrationTests/Middleware/ExceptionHandlingMiddlewareConcurrencyTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using CCE.Domain.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CCE.Api.IntegrationTests.Middleware;

public class ExceptionHandlingMiddlewareConcurrencyTests
{
    [Fact]
    public async Task ConcurrencyException_returns_409_problem_details()
    {
        using var host = await CreateHostThatThrowsAsync(new ConcurrencyException("test conflict"));
        using var client = host.GetTestClient();

        var response = await client.GetAsync("/throw");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body!["title"].ToString().Should().Be("Concurrent edit");
        body["status"].ToString().Should().Be("409");
    }

    [Fact]
    public async Task DuplicateException_returns_409_problem_details()
    {
        using var host = await CreateHostThatThrowsAsync(new DuplicateException("dup conflict"));
        using var client = host.GetTestClient();

        var response = await client.GetAsync("/throw");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body!["title"].ToString().Should().Be("Duplicate value");
    }

    [Fact]
    public async Task DomainException_returns_400_problem_details()
    {
        using var host = await CreateHostThatThrowsAsync(new DomainException("invariant violated"));
        using var client = host.GetTestClient();

        var response = await client.GetAsync("/throw");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body!["title"].ToString().Should().Be("Invariant violated");
    }

    private static async Task<IHost> CreateHostThatThrowsAsync(System.Exception toThrow)
    {
        return await new HostBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseMiddleware<CCE.Api.Common.Middleware.CorrelationIdMiddleware>();
                    app.UseMiddleware<CCE.Api.Common.Middleware.ExceptionHandlingMiddleware>();
                    app.Run(async ctx =>
                    {
                        await Task.Yield();
                        throw toThrow;
                    });
                })
                .ConfigureServices(services => services.AddLogging()))
            .StartAsync();
    }
}
```

- [ ] **Step 2: Run — expect failure (fall-through to 500)**

```bash
dotnet test backend/tests/CCE.Api.IntegrationTests/CCE.Api.IntegrationTests.csproj --nologo --no-restore --filter "FullyQualifiedName~ExceptionHandlingMiddlewareConcurrencyTests" --logger "console;verbosity=minimal" 2>&1 | tail -5
```

Expected: 3 fails (currently the middleware lacks `ConcurrencyException`/`DuplicateException`/`DomainException` mapping; everything throws → 500).

- [ ] **Step 3: Extend the middleware**

Open `backend/src/CCE.Api.Common/Middleware/ExceptionHandlingMiddleware.cs`. After the existing `catch (ValidationException ex)` block, add (in order — most-specific first):

```csharp
        catch (CCE.Domain.Common.ConcurrencyException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status409Conflict,
                title: "Concurrent edit",
                detail: ex.Message,
                type: "https://cce.moenergy.gov.sa/problems/concurrency").ConfigureAwait(false);
        }
        catch (CCE.Domain.Common.DuplicateException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status409Conflict,
                title: "Duplicate value",
                detail: ex.Message,
                type: "https://cce.moenergy.gov.sa/problems/duplicate").ConfigureAwait(false);
        }
        catch (CCE.Domain.Common.DomainException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest,
                title: "Invariant violated",
                detail: ex.Message,
                type: "https://cce.moenergy.gov.sa/problems/invariant").ConfigureAwait(false);
        }
```

Add the new helper at the bottom of the class:

```csharp
    private static async Task WriteProblemAsync(
        HttpContext ctx, int statusCode, string title, string detail, string type)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Type = type,
            Title = title,
            Detail = detail,
            Instance = ctx.Request.Path,
        };
        problem.Extensions["correlationId"] = GetCorrelationId(ctx);

        ctx.Response.StatusCode = statusCode;
        ctx.Response.ContentType = "application/problem+json";
        await JsonSerializer.SerializeAsync(ctx.Response.Body, problem).ConfigureAwait(false);
    }
```

(Placement: before `WriteServerErrorAsync`. Reuses the existing `JsonSerializer` import + `ProblemDetails` type from Foundation.)

- [ ] **Step 4: Run — expect pass**

```bash
dotnet test backend/tests/CCE.Api.IntegrationTests/CCE.Api.IntegrationTests.csproj --nologo --no-restore --filter "FullyQualifiedName~ExceptionHandlingMiddlewareConcurrencyTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
```

Expected: `Passed: 3`.

Run the rest of the existing IntegrationTests to confirm no regressions:

```bash
dotnet test backend/tests/CCE.Api.IntegrationTests/CCE.Api.IntegrationTests.csproj --nologo --no-restore --logger "console;verbosity=minimal" 2>&1 | tail -3
```

Expected: `Passed: 31` (28 existing + 3 new).

- [ ] **Step 5: Commit**

```bash
git add backend/src/CCE.Api.Common/Middleware/ExceptionHandlingMiddleware.cs backend/tests/CCE.Api.IntegrationTests/Middleware/ExceptionHandlingMiddlewareConcurrencyTests.cs
git -c commit.gpgsign=false commit -m "feat(api-common): map ConcurrencyException/DuplicateException/DomainException to 409/400 ProblemDetails (3 TDD tests)"
```

---

## Task 0.4: JIT user-sync middleware

**Files:**
- Create: `backend/src/CCE.Api.Common/Identity/UserSyncMiddleware.cs`
- Create: `backend/src/CCE.Api.Common/Identity/UserSyncMiddlewareRegistration.cs`
- Create: `backend/tests/CCE.Api.IntegrationTests/Identity/UserSyncMiddlewareTests.cs`
- Modify: `backend/src/CCE.Api.Common/CCE.Api.Common.csproj` (add `Microsoft.Extensions.Caching.Memory` if missing)
- Modify: `backend/src/CCE.Api.Internal/Program.cs` (mount middleware after `UseAuthentication`)

**Rationale:** First request from an admin needs their `users` row + role assignments. The middleware short-circuits via `IMemoryCache` after the first hit per `sub` claim.

- [ ] **Step 1: Add `Microsoft.Extensions.Caching.Memory` to CPM if not present**

```bash
grep -q "Microsoft.Extensions.Caching.Memory" backend/Directory.Packages.props || \
  echo "    <PackageVersion Include=\"Microsoft.Extensions.Caching.Memory\" Version=\"8.0.1\" />" \
  >> /tmp/_audit.txt && echo "Need to add Caching.Memory to CPM" || echo "Caching.Memory already in CPM"
```

If missing, open `backend/Directory.Packages.props` and add to the "Core framework & Testing" item group (or wherever similar `Microsoft.Extensions.*` packages live):

```xml
<PackageVersion Include="Microsoft.Extensions.Caching.Memory" Version="8.0.1" />
```

Add to `backend/src/CCE.Api.Common/CCE.Api.Common.csproj`:

```xml
<PackageReference Include="Microsoft.Extensions.Caching.Memory" />
```

- [ ] **Step 2: Write the middleware**

`backend/src/CCE.Api.Common/Identity/UserSyncMiddleware.cs`:

```csharp
using System.Security.Claims;
using CCE.Application.Common.Interfaces;
using CCE.Domain.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CCE.Api.Common.Identity;

/// <summary>
/// On every authenticated request, ensures the current user has a row in <c>users</c>.
/// Runs AFTER <c>UseAuthentication</c> + <c>UseAuthorization</c>, BEFORE endpoint dispatch.
/// Idempotent — uses <see cref="IMemoryCache"/> keyed by JWT <c>sub</c> for 5 min so repeat
/// requests skip the DB.
/// </summary>
public sealed class UserSyncMiddleware
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private readonly RequestDelegate _next;
    private readonly ILogger<UserSyncMiddleware> _logger;

    public UserSyncMiddleware(RequestDelegate next, ILogger<UserSyncMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IMemoryCache cache,
        ICceDbContext db,
        IConfiguration configuration)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var subClaim = context.User.FindFirstValue("sub")
            ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!System.Guid.TryParse(subClaim, out var userId))
        {
            _logger.LogWarning("Authenticated request has no parseable sub claim; skipping user sync.");
            await _next(context).ConfigureAwait(false);
            return;
        }

        var cacheKey = $"user-synced:{userId:N}";
        if (cache.TryGetValue(cacheKey, out _))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var dbContext = (Microsoft.EntityFrameworkCore.DbContext)db;
        var existing = await dbContext.Set<User>().FindAsync(new object[] { userId }, context.RequestAborted)
            .ConfigureAwait(false);
        if (existing is null)
        {
            var email = context.User.FindFirstValue("email") ?? $"{userId:N}@unknown.local";
            var preferredUsername = context.User.FindFirstValue("preferred_username") ?? email;

            var user = new User
            {
                Id = userId,
                UserName = preferredUsername,
                NormalizedUserName = preferredUsername.ToUpperInvariant(),
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                EmailConfirmed = true,
            };
            dbContext.Add(user);

            // Map Keycloak group claims to CCE roles. Group → role mapping is configurable.
            var groupToRole = configuration.GetSection("UserSync:GroupToRoleMap")
                .Get<Dictionary<string, string>>() ?? new();
            var groups = context.User.FindAll("groups").Select(c => c.Value)
                .Concat(context.User.FindAll(ClaimTypes.Role).Select(c => c.Value))
                .ToList();

            foreach (var group in groups)
            {
                if (!groupToRole.TryGetValue(group, out var roleName)) continue;
                var role = await dbContext.Set<Role>()
                    .FirstOrDefaultAsync(r => r.Name == roleName, context.RequestAborted)
                    .ConfigureAwait(false);
                if (role is null) continue;
                dbContext.Add(new IdentityUserRole<System.Guid>
                {
                    UserId = userId,
                    RoleId = role.Id,
                });
            }

            await db.SaveChangesAsync(context.RequestAborted).ConfigureAwait(false);
            _logger.LogInformation("Synced new user {UserId} from JWT claims.", userId);
        }

        cache.Set(cacheKey, true, CacheTtl);
        await _next(context).ConfigureAwait(false);
    }
}
```

`backend/src/CCE.Api.Common/Identity/UserSyncMiddlewareRegistration.cs`:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace CCE.Api.Common.Identity;

public static class UserSyncMiddlewareRegistration
{
    public static IServiceCollection AddCceUserSync(this IServiceCollection services)
    {
        services.AddMemoryCache();
        return services;
    }

    public static IApplicationBuilder UseCceUserSync(this IApplicationBuilder app) =>
        app.UseMiddleware<UserSyncMiddleware>();
}
```

- [ ] **Step 3: Wire into `CCE.Api.Internal/Program.cs`**

Insert into the `builder.Services` chain (existing fluent chain):

```csharp
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddCceJwtAuth(builder.Configuration)
    .AddCcePermissionPolicies()
    .AddCceUserSync()                      // ← NEW
    .AddCceHealthChecks(builder.Configuration)
    .AddCceRateLimiter(builder.Configuration)
    .AddCceOpenApi("CCE Internal API");
```

And in the middleware pipeline, after `UseAuthorization`:

```csharp
app.UseAuthentication();
app.UseAuthorization();
app.UseCceUserSync();                       // ← NEW
app.UseRateLimiter();
```

- [ ] **Step 4: Write integration tests**

`backend/tests/CCE.Api.IntegrationTests/Identity/UserSyncMiddlewareTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Headers;
using CCE.Domain.Identity;
using CCE.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CCE.Api.IntegrationTests.Identity;

public class UserSyncMiddlewareTests
{
    [Fact]
    public async Task First_authenticated_request_creates_user_row()
    {
        await using var factory = new InternalApiFactory();
        using var client = factory.CreateClient();

        var token = await factory.IssueAdminTokenAsync(); // helper from Foundation Phase 14
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/health/authenticated");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<CceDbContext>();
        var sub = await factory.GetTokenSubAsync(token);

        var user = await ctx.Set<User>().FindAsync(sub);
        user.Should().NotBeNull();
    }

    [Fact]
    public async Task Repeat_request_uses_cache_and_does_not_duplicate()
    {
        await using var factory = new InternalApiFactory();
        using var client = factory.CreateClient();

        var token = await factory.IssueAdminTokenAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.GetAsync("/health/authenticated");
        await client.GetAsync("/health/authenticated");
        await client.GetAsync("/health/authenticated");

        using var scope = factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<CceDbContext>();
        var sub = await factory.GetTokenSubAsync(token);

        var users = await ctx.Set<User>().Where(u => u.Id == sub).ToListAsync();
        users.Should().HaveCount(1);
    }

    [Fact]
    public async Task Anonymous_request_does_not_invoke_middleware()
    {
        await using var factory = new InternalApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // No assertion needed beyond "no crash" — middleware short-circuits on unauthenticated.
    }
}
```

(`InternalApiFactory` and the token helpers exist from Foundation Phase 14. If they don't expose `IssueAdminTokenAsync` / `GetTokenSubAsync` yet, add minimal helpers next to them — the existing test harness has the Keycloak `Testcontainers.Keycloak` integration already running.)

- [ ] **Step 5: Run — expect pass**

```bash
dotnet test backend/tests/CCE.Api.IntegrationTests/CCE.Api.IntegrationTests.csproj --nologo --no-restore --filter "FullyQualifiedName~UserSyncMiddlewareTests" --logger "console;verbosity=minimal" 2>&1 | tail -5
```

Expected: `Passed: 3`. If the harness extensions are missing, the build error here points to which factory method to add — extend the existing harness rather than rewriting it.

- [ ] **Step 6: Commit**

```bash
git add backend/Directory.Packages.props backend/src/CCE.Api.Common/CCE.Api.Common.csproj backend/src/CCE.Api.Common/Identity/ backend/src/CCE.Api.Internal/Program.cs backend/tests/CCE.Api.IntegrationTests/Identity/UserSyncMiddlewareTests.cs
git -c commit.gpgsign=false commit -m "feat(api-common): JIT user-sync middleware (Keycloak sub → users row, IMemoryCache 5min) (3 TDD tests)"
```

---

## Task 0.5: OpenAPI per-API path split + drift check

**Files:**
- Modify: `backend/src/CCE.Api.Common/OpenApi/CceOpenApiRegistration.cs`
- Modify: `scripts/check-contracts-clean.sh` (Foundation Phase 06's drift script)
- Create: `contracts/internal-api.yaml` (snapshot generated by `scripts/export-internal-openapi.sh`)
- Create: `scripts/export-internal-openapi.sh`

**Rationale:** Foundation exports a single `external-api.yaml`. Internal API needs its own contract file so Admin CMS (sub-project 5) can codegen against it without seeing External API endpoints.

- [ ] **Step 1: Extend `CceOpenApiRegistration.cs`**

Add a parameterised version that takes a "tag" (`internal` / `external`) and uses it in the route + document name:

```csharp
public static IServiceCollection AddCceOpenApi(this IServiceCollection services, string title, string apiTag = "v1")
{
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen(opts =>
    {
        opts.SwaggerDoc(apiTag, new OpenApiInfo
        {
            Title = title,
            Version = apiTag,
            Description = $"CCE Knowledge Center — {title}"
        });
    });
    return services;
}

public static IApplicationBuilder UseCceOpenApi(this IApplicationBuilder app, string apiTag = "v1")
{
    app.UseSwagger(opts =>
    {
        opts.RouteTemplate = $"swagger/{apiTag}/{{documentName}}/swagger.{{json|yaml}}";
    });
    app.UseSwaggerUI(opts =>
    {
        opts.SwaggerEndpoint($"/swagger/{apiTag}/v1/swagger.json", $"CCE {apiTag} API");
    });
    return app;
}
```

Then in `CCE.Api.Internal/Program.cs` change the calls to:

```csharp
.AddCceOpenApi("CCE Internal API", apiTag: "internal")
...
app.UseCceOpenApi(apiTag: "internal");
```

And `CCE.Api.External/Program.cs` similarly with `apiTag: "external"`.

- [ ] **Step 2: Verify both APIs serve their docs**

Restart both API hosts (kill + dotnet run; or Ctrl-C in their terminals + restart):

```bash
pkill -f "CCE.Api.External\|CCE.Api.Internal" 2>/dev/null
sleep 2
dotnet run --project backend/src/CCE.Api.External/CCE.Api.External.csproj --no-build &
dotnet run --project backend/src/CCE.Api.Internal/CCE.Api.Internal.csproj --no-build &
sleep 10
curl -s -o /dev/null -w "external openapi: %{http_code}\n" http://localhost:5001/swagger/external/v1/swagger.json
curl -s -o /dev/null -w "internal openapi: %{http_code}\n" http://localhost:5002/swagger/internal/v1/swagger.json
```

Expected: both 200.

- [ ] **Step 3: Add `scripts/export-internal-openapi.sh`**

```bash
#!/usr/bin/env bash
set -euo pipefail
URL="${INTERNAL_API_URL:-http://localhost:5002}/swagger/internal/v1/swagger.yaml"
OUT="contracts/internal-api.yaml"
curl -fsSL "$URL" -o "$OUT"
echo "Exported internal-api OpenAPI to $OUT"
```

`chmod +x scripts/export-internal-openapi.sh`. Run it:

```bash
chmod +x scripts/export-internal-openapi.sh
./scripts/export-internal-openapi.sh
ls -la contracts/internal-api.yaml
wc -l contracts/internal-api.yaml
```

Expected: file created, ~30+ lines (mostly health endpoints — endpoints land in later phases).

- [ ] **Step 4: Extend `scripts/check-contracts-clean.sh`**

Open the existing script. Find the line that exports `contracts/external-api.yaml` and check its dirty status. Add a parallel block for `contracts/internal-api.yaml`. Concrete diff (insert after the existing external block):

```bash
# Internal API drift check
INTERNAL_TMP="$(mktemp)"
curl -fsSL "${INTERNAL_API_URL:-http://localhost:5002}/swagger/internal/v1/swagger.yaml" -o "$INTERNAL_TMP"
if ! diff -u contracts/internal-api.yaml "$INTERNAL_TMP" > /dev/null; then
    echo "DRIFT: contracts/internal-api.yaml is out of date. Run scripts/export-internal-openapi.sh and commit."
    diff -u contracts/internal-api.yaml "$INTERNAL_TMP" | head -40
    exit 1
fi
echo "internal-api.yaml clean"
rm -f "$INTERNAL_TMP"
```

Run the script:

```bash
./scripts/check-contracts-clean.sh
```

Expected: prints "external-api.yaml clean" + "internal-api.yaml clean".

- [ ] **Step 5: Commit**

```bash
git add backend/src/CCE.Api.Common/OpenApi/CceOpenApiRegistration.cs backend/src/CCE.Api.External/Program.cs backend/src/CCE.Api.Internal/Program.cs scripts/export-internal-openapi.sh scripts/check-contracts-clean.sh contracts/internal-api.yaml
git -c commit.gpgsign=false commit -m "feat(api-common): OpenAPI per-API path split (/swagger/internal vs /swagger/external) + internal-api.yaml drift check"
```

---

## Phase 00 — completion checklist

- [ ] `Audit.Read` permission added to `permissions.yaml` (42 total).
- [ ] `PagedResult<T>` + `ToPagedResultAsync` extension shipped (5 tests).
- [ ] `ConcurrencyException`/`DuplicateException`/`DomainException` mapped to 409/409/400 ProblemDetails (3 tests).
- [ ] `UserSyncMiddleware` mounted in Internal API; first authenticated request creates `users` row (3 tests).
- [ ] OpenAPI split per API; `contracts/internal-api.yaml` exported and drift-checked.
- [ ] `dotnet build backend/CCE.sln` 0 errors / 0 warnings.
- [ ] All previous tests still pass; new test counts ≈ Application +5 / Api Integration +6 = +11.
- [ ] `git status` clean; 5 new commits.

**If all boxes ticked, Phase 00 is complete. Proceed to Phase 01 (Identity admin endpoints).**
