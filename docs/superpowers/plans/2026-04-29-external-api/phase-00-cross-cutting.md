# Phase 00 — Cross-cutting infrastructure

> Parent: [`../2026-04-29-external-api.md`](../2026-04-29-external-api.md) · Spec: [`../../specs/2026-04-29-external-api-design.md`](../../specs/2026-04-29-external-api-design.md) §3.2

**Phase goal:** Stand up the cross-cutting infrastructure that every later phase depends on:
1. BFF cookie auth + Bearer dual-mode middleware (`/auth/login`, `/auth/callback`, `/auth/refresh`, `/auth/logout`).
2. Redis output cache middleware (60-second TTL, anonymous-only).
3. Tiered rate limiter (Anonymous / Authenticated / SearchAndWrite — config-driven).
4. Meilisearch container + `ISearchClient` HTTP wrapper (read-side; indexer is Phase 02).
5. `IHtmlSanitizer` + `HtmlSanitizerWrapper`.
6. `ICountryScopeAccessor` + `HttpContextCountryScopeAccessor` (lands here so Phase 1 reads can use it).

After Phase 00, every later phase has the building blocks it needs.

**Tasks in this phase:** 6
**Working directory:** `/Users/m/CCE/`
**Preconditions:**
- Sub-project 3 closed (`internal-api-v0.1.0` tag at `4b40d2f`).
- 794 + 1 skipped backend tests passing.
- Both API hosts boot to /health/ready.

---

## Pre-execution sanity checks

1. `git status` clean apart from `.claude/`.
2. `git tag -l | grep internal-api-v0.1.0` → present.
3. `dotnet build backend/CCE.sln --no-restore` 0 warnings / 0 errors.
4. `dotnet test backend/CCE.sln --no-build --no-restore --logger "console;verbosity=minimal" 2>&1 | grep -E "Passed!|Failed!"` → 6 result lines, all `Passed!` (Domain 290 / Application 278 / Infrastructure 37 + 1 skipped / Architecture 12 / SourceGen 10 / Api Integration 167).
5. `nc -z localhost 6379 && echo redis-up` (Redis reachable).

If any fail, stop and report.

---

## Task 0.1: Add Meilisearch container + CPM packages + appsettings

**Files:**
- Modify: `docker-compose.yml`
- Modify: `backend/Directory.Packages.props`
- Modify: `backend/src/CCE.Infrastructure/CceInfrastructureOptions.cs`
- Modify: `backend/src/CCE.Api.External/appsettings.Development.json`

**Rationale:** Meilisearch is a new container; the client + sanitizer NuGets are CPM additions. Lock both at the start so subsequent tasks can `dotnet restore` cleanly.

- [ ] **Step 1: Add Meilisearch service to `docker-compose.yml`**

After the existing `clamav:` service block, add:

```yaml
  meilisearch:
    image: getmeili/meilisearch:v1.10
    container_name: cce-meilisearch
    ports:
      - "7700:7700"
    environment:
      MEILI_ENV: development
      MEILI_NO_ANALYTICS: "true"
      MEILI_MASTER_KEY: dev-meili-master-key-change-me
    volumes:
      - meilisearch-data:/meili_data
    networks:
      - cce-net
```

And add `meilisearch-data:` to the existing `volumes:` block (alongside `clamav-data:`).

- [ ] **Step 2: Add the two NuGet packages to CPM**

Open `backend/Directory.Packages.props`. Find the `<ItemGroup>` containing existing `PackageVersion` entries. Add (alphabetical-ish):

```xml
<PackageVersion Include="HtmlSanitizer" Version="9.0.886" />
<PackageVersion Include="Meilisearch" Version="0.15.5" />
```

(Meilisearch's official package is named `Meilisearch` — not `Meilisearch.Dotnet` — verify on nuget.org if version drifts.)

- [ ] **Step 3: Extend `CceInfrastructureOptions`**

Open `backend/src/CCE.Infrastructure/CceInfrastructureOptions.cs`. After the existing `AllowedAssetMimeTypes` property, add:

```csharp
/// <summary>Meilisearch HTTP base URL. Default <c>http://localhost:7700</c>.</summary>
public string MeilisearchUrl { get; init; } = "http://localhost:7700";

/// <summary>Meilisearch master key. Required.</summary>
public string MeilisearchMasterKey { get; init; } = string.Empty;

/// <summary>Output-cache TTL in seconds for anonymous reads. Default 60.</summary>
public int OutputCacheTtlSeconds { get; init; } = 60;
```

- [ ] **Step 4: Extend `appsettings.Development.json` for the External API**

Open `backend/src/CCE.Api.External/appsettings.Development.json`. Add (or extend) the `Infrastructure` section:

```json
"Infrastructure": {
  "MeilisearchUrl": "http://localhost:7700",
  "MeilisearchMasterKey": "dev-meili-master-key-change-me",
  "OutputCacheTtlSeconds": 60
},
"RateLimit": {
  "Anonymous": { "RequestsPerMinute": 120 },
  "Authenticated": { "RequestsPerMinute": 600 },
  "SearchAndWrite": { "RequestsPerMinute": 30 }
},
"Bff": {
  "KeycloakRealm": "cce-public",
  "KeycloakClientId": "cce-public-portal",
  "KeycloakClientSecret": "dev-public-secret-change-me",
  "CookieDomain": "localhost",
  "SessionLifetimeMinutes": 30
}
```

(Preserve any existing keys.)

- [ ] **Step 5: Bring up the new container + restore**

```bash
docker compose up -d meilisearch
sleep 5
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:7700/health
```

Expected: `200`.

```bash
dotnet restore backend/CCE.sln 2>&1 | tail -5
```

Expected: clean restore (no error).

- [ ] **Step 6: Commit**

```bash
git add docker-compose.yml backend/Directory.Packages.props \
  backend/src/CCE.Infrastructure/CceInfrastructureOptions.cs \
  backend/src/CCE.Api.External/appsettings.Development.json
git -c commit.gpgsign=false commit -m "chore(sub-4): add meilisearch container + Meilisearch/HtmlSanitizer CPM + appsettings (Phase 0.1)"
```

---

## Task 0.2: `IHtmlSanitizer` + `HtmlSanitizerWrapper`

**Files:**
- Create: `backend/src/CCE.Application/Common/Sanitization/IHtmlSanitizer.cs`
- Create: `backend/src/CCE.Infrastructure/Sanitization/HtmlSanitizerWrapper.cs`
- Modify: `backend/src/CCE.Infrastructure/CCE.Infrastructure.csproj` (add `<PackageReference Include="HtmlSanitizer" />`)
- Modify: `backend/src/CCE.Infrastructure/DependencyInjection.cs` (register)
- Create: `backend/tests/CCE.Infrastructure.Tests/Sanitization/HtmlSanitizerWrapperTests.cs`

**Rationale:** Every Phase 6 (community write) command's validator pipes user content through `IHtmlSanitizer.Sanitize(...)`. Land the abstraction now.

- [ ] **Step 1: Add the test (TDD)**

`backend/tests/CCE.Infrastructure.Tests/Sanitization/HtmlSanitizerWrapperTests.cs`:

```csharp
using CCE.Infrastructure.Sanitization;

namespace CCE.Infrastructure.Tests.Sanitization;

public class HtmlSanitizerWrapperTests
{
    private readonly HtmlSanitizerWrapper _sut = new();

    [Fact]
    public void Strips_script_tags()
    {
        var input = "<p>safe</p><script>alert('xss')</script>";
        var output = _sut.Sanitize(input);
        output.Should().NotContain("script");
        output.Should().Contain("<p>safe</p>");
    }

    [Fact]
    public void Strips_javascript_href()
    {
        var input = "<a href=\"javascript:alert(1)\">click</a>";
        var output = _sut.Sanitize(input);
        output.Should().NotContain("javascript");
    }

    [Fact]
    public void Allows_https_href()
    {
        var input = "<a href=\"https://example.com\">click</a>";
        var output = _sut.Sanitize(input);
        output.Should().Contain("href=\"https://example.com\"");
    }

    [Fact]
    public void Allows_basic_formatting_tags()
    {
        var input = "<p><strong>bold</strong> <em>italic</em></p><ul><li>item</li></ul>";
        var output = _sut.Sanitize(input);
        output.Should().Contain("<strong>bold</strong>");
        output.Should().Contain("<em>italic</em>");
        output.Should().Contain("<ul><li>item</li></ul>");
    }

    [Fact]
    public void Empty_input_returns_empty_string()
    {
        _sut.Sanitize(string.Empty).Should().Be(string.Empty);
        _sut.Sanitize(null!).Should().Be(string.Empty);
    }

    [Fact]
    public void Preserves_arabic_text()
    {
        var input = "<p>مرحبا بالعالم</p>";
        var output = _sut.Sanitize(input);
        output.Should().Contain("مرحبا بالعالم");
    }
}
```

- [ ] **Step 2: Run — expect compile error (HtmlSanitizerWrapper missing)**

```bash
dotnet test backend/tests/CCE.Infrastructure.Tests/CCE.Infrastructure.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~HtmlSanitizerWrapperTests" 2>&1 | tail -8
```

Expected: build failure referencing `HtmlSanitizerWrapper`.

- [ ] **Step 3: Define interface**

`backend/src/CCE.Application/Common/Sanitization/IHtmlSanitizer.cs`:

```csharp
namespace CCE.Application.Common.Sanitization;

/// <summary>
/// Strips disallowed HTML from user-submitted content. Allowlist:
/// p, br, strong, em, a (https only), ul, ol, li, blockquote, code, pre.
/// </summary>
public interface IHtmlSanitizer
{
    /// <summary>Returns sanitized HTML. Null/empty input returns empty string.</summary>
    string Sanitize(string input);
}
```

- [ ] **Step 4: Implement wrapper**

Add `<PackageReference Include="HtmlSanitizer" />` to `backend/src/CCE.Infrastructure/CCE.Infrastructure.csproj`.

`backend/src/CCE.Infrastructure/Sanitization/HtmlSanitizerWrapper.cs`:

```csharp
using CCE.Application.Common.Sanitization;
using Ganss.Xss;

namespace CCE.Infrastructure.Sanitization;

public sealed class HtmlSanitizerWrapper : IHtmlSanitizer
{
    private static readonly HtmlSanitizer Inner = BuildInner();

    public string Sanitize(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }
        return Inner.Sanitize(input);
    }

    private static HtmlSanitizer BuildInner()
    {
        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedTags.Clear();
        foreach (var tag in new[] { "p", "br", "strong", "em", "a", "ul", "ol", "li", "blockquote", "code", "pre" })
        {
            sanitizer.AllowedTags.Add(tag);
        }
        sanitizer.AllowedAttributes.Clear();
        sanitizer.AllowedAttributes.Add("href");
        sanitizer.AllowedSchemes.Clear();
        sanitizer.AllowedSchemes.Add("https");
        sanitizer.AllowedCssProperties.Clear();
        return sanitizer;
    }
}
```

- [ ] **Step 5: Register in DI**

In `backend/src/CCE.Infrastructure/DependencyInjection.cs`, add inside `AddInfrastructure(...)` after the existing sanitization-adjacent block (after `IClamAvScanner` registration):

```csharp
services.AddSingleton<IHtmlSanitizer, HtmlSanitizerWrapper>();
```

(Add `using CCE.Application.Common.Sanitization;` and `using CCE.Infrastructure.Sanitization;` imports.)

- [ ] **Step 6: Run — expect 6/6 pass**

```bash
dotnet restore backend/CCE.sln 2>&1 | tail -3
dotnet test backend/tests/CCE.Infrastructure.Tests/CCE.Infrastructure.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~HtmlSanitizerWrapperTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
```

Expected: `Passed: 6`.

- [ ] **Step 7: Commit**

```bash
git add backend/src/CCE.Application/Common/Sanitization/IHtmlSanitizer.cs \
  backend/src/CCE.Infrastructure/Sanitization/HtmlSanitizerWrapper.cs \
  backend/src/CCE.Infrastructure/CCE.Infrastructure.csproj \
  backend/src/CCE.Infrastructure/DependencyInjection.cs \
  backend/tests/CCE.Infrastructure.Tests/Sanitization/HtmlSanitizerWrapperTests.cs
git -c commit.gpgsign=false commit -m "feat(infrastructure): IHtmlSanitizer + HtmlSanitizerWrapper (6 TDD tests) (Phase 0.2)"
```

---

## Task 0.3: `ICountryScopeAccessor` + `HttpContextCountryScopeAccessor`

**Files:**
- Create: `backend/src/CCE.Application/Common/CountryScope/ICountryScopeAccessor.cs`
- Create: `backend/src/CCE.Api.External/Identity/HttpContextCountryScopeAccessor.cs`
- Create: `backend/src/CCE.Api.Internal/Identity/HttpContextCountryScopeAccessor.cs` (sibling — Internal API also needs this for Sub-3 deferred work)
- Modify: `backend/src/CCE.Api.External/Program.cs` and `backend/src/CCE.Api.Internal/Program.cs` to register
- Create: `backend/src/CCE.Infrastructure/Identity/SystemCountryScopeAccessor.cs` (fallback returning `null` — no scope; for non-HTTP contexts like seeders).
- Modify: `backend/src/CCE.Infrastructure/DependencyInjection.cs`
- Create: tests under `backend/tests/CCE.Api.IntegrationTests/Identity/HttpContextCountryScopeAccessorTests.cs` (4 tests: super-admin → null, content-manager → null, state-rep → list, anonymous → null)

**Rationale:** Spec §3.2.6 + ADR-0030 (deferred from Sub-3, lands here). Phase 1 country-aware reads (e.g. `/api/resources?countryId=...`) use this. The accessor reads the JWT `sub` and queries `state_representative_assignments` for active rows.

- [ ] **Step 1: Define interface**

`backend/src/CCE.Application/Common/CountryScope/ICountryScopeAccessor.cs`:

```csharp
namespace CCE.Application.Common.CountryScope;

/// <summary>
/// Resolves the set of country IDs the current request is authorized to query.
/// <c>null</c> means no scope (admin / content-manager / anonymous-on-public-route).
/// A non-null list means StateRepresentative — restrict country-scoped reads to those ids.
/// </summary>
public interface ICountryScopeAccessor
{
    Task<IReadOnlyList<System.Guid>?> GetAuthorizedCountryIdsAsync(System.Threading.CancellationToken ct);
}
```

- [ ] **Step 2: Fallback impl in Infrastructure**

`backend/src/CCE.Infrastructure/Identity/SystemCountryScopeAccessor.cs`:

```csharp
using CCE.Application.Common.CountryScope;

namespace CCE.Infrastructure.Identity;

/// <summary>
/// Default registration: returns null (no scope). API hosts override with HttpContext-based impl.
/// </summary>
public sealed class SystemCountryScopeAccessor : ICountryScopeAccessor
{
    public Task<IReadOnlyList<System.Guid>?> GetAuthorizedCountryIdsAsync(System.Threading.CancellationToken ct)
        => Task.FromResult<IReadOnlyList<System.Guid>?>(null);
}
```

Register in `AddInfrastructure(...)`:

```csharp
services.TryAddScoped<ICountryScopeAccessor, SystemCountryScopeAccessor>();
```

(Add `using CCE.Application.Common.CountryScope;` and `using CCE.Infrastructure.Identity;`.)

- [ ] **Step 3: HttpContext impl in `CCE.Api.Common.Identity`** (shared between External + Internal)

Move location to `backend/src/CCE.Api.Common/Identity/HttpContextCountryScopeAccessor.cs` so both API hosts can register it.

```csharp
using System.Security.Claims;
using CCE.Application.Common.CountryScope;
using CCE.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CCE.Api.Common.Identity;

/// <summary>
/// Reads the JWT sub claim and looks up active state-representative assignments.
/// SuperAdmin / ContentManager / anonymous → null (no scope).
/// StateRepresentative → list of CountryId from active assignments.
/// Other roles → empty list (sees nothing in country-scoped queries).
/// </summary>
public sealed class HttpContextCountryScopeAccessor : ICountryScopeAccessor
{
    private static readonly string[] BypassRoles = new[] { "SuperAdmin", "ContentManager" };

    private readonly IHttpContextAccessor _accessor;
    private readonly ICceDbContext _db;

    public HttpContextCountryScopeAccessor(IHttpContextAccessor accessor, ICceDbContext db)
    {
        _accessor = accessor;
        _db = db;
    }

    public async Task<IReadOnlyList<System.Guid>?> GetAuthorizedCountryIdsAsync(System.Threading.CancellationToken ct)
    {
        var user = _accessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return null;
        }
        var groups = user.FindAll("groups").Select(c => c.Value).ToHashSet(System.StringComparer.OrdinalIgnoreCase);
        if (BypassRoles.Any(r => groups.Contains(r)))
        {
            return null;
        }
        if (!groups.Contains("StateRepresentative"))
        {
            return System.Array.Empty<System.Guid>();
        }

        var sub = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!System.Guid.TryParse(sub, out var userId))
        {
            return System.Array.Empty<System.Guid>();
        }

        var ids = await _db.StateRepresentativeAssignments
            .Where(a => a.UserId == userId)
            .Select(a => a.CountryId)
            .Distinct()
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return ids;
    }
}
```

- [ ] **Step 4: Register in both API hosts**

In `backend/src/CCE.Api.External/Program.cs` and `backend/src/CCE.Api.Internal/Program.cs`, after `builder.Services.AddInfrastructure(...)` add:

```csharp
builder.Services.Replace(ServiceDescriptor.Scoped<ICountryScopeAccessor, HttpContextCountryScopeAccessor>());
```

(`using CCE.Api.Common.Identity;` and `using CCE.Application.Common.CountryScope;` and `using Microsoft.Extensions.DependencyInjection.Extensions;` if not already imported.)

- [ ] **Step 5: Run + commit**

```bash
dotnet build backend/CCE.sln --nologo --no-restore 2>&1 | tail -5
dotnet test backend/CCE.sln --no-build --no-restore --logger "console;verbosity=minimal" 2>&1 | grep -E "Passed!|Failed!"
```

Expected: 0/0 build, no test regressions.

(Integration tests for the accessor are deferred to Phase 1 where we have country-scoped endpoints to exercise it.)

```bash
git add backend/src/CCE.Application/Common/CountryScope/ICountryScopeAccessor.cs \
  backend/src/CCE.Infrastructure/Identity/SystemCountryScopeAccessor.cs \
  backend/src/CCE.Infrastructure/DependencyInjection.cs \
  backend/src/CCE.Api.Common/Identity/HttpContextCountryScopeAccessor.cs \
  backend/src/CCE.Api.External/Program.cs \
  backend/src/CCE.Api.Internal/Program.cs
git -c commit.gpgsign=false commit -m "feat(api-common): ICountryScopeAccessor + HttpContextCountryScopeAccessor (Phase 0.3)"
```

---

## Task 0.4: BFF cookie auth + Bearer dual-mode

**Files:**
- Create: `backend/src/CCE.Api.Common/Auth/BffSessionCookie.cs` (Data Protection encrypt/decrypt of session blob)
- Create: `backend/src/CCE.Api.Common/Auth/BffSessionMiddleware.cs`
- Create: `backend/src/CCE.Api.Common/Auth/BffAuthEndpoints.cs` (`MapBffAuthEndpoints(this IEndpointRouteBuilder)`)
- Create: `backend/src/CCE.Api.Common/Auth/BffOptions.cs` (binds `Bff:` config section)
- Modify: `backend/src/CCE.Api.External/Program.cs` (register options + middleware + endpoints)
- Create: `backend/tests/CCE.Api.IntegrationTests/Auth/BffSessionMiddlewareTests.cs` (5 tests: no cookie → next, valid cookie → synthesizes Bearer, expired cookie → refreshes, refresh fails → clears + 401, malformed cookie → next + 401 if route requires auth)

**Rationale:** ADR-0031. Lands the cookie session encryption + decryption + auto-refresh and the 4 BFF endpoints for the SPA.

This is the largest task in Phase 0. Approximate code:

`backend/src/CCE.Api.Common/Auth/BffOptions.cs`:

```csharp
namespace CCE.Api.Common.Auth;

public sealed class BffOptions
{
    public const string SectionName = "Bff";
    public string KeycloakRealm { get; init; } = string.Empty;
    public string KeycloakClientId { get; init; } = string.Empty;
    public string KeycloakClientSecret { get; init; } = string.Empty;
    public string CookieDomain { get; init; } = "localhost";
    public int SessionLifetimeMinutes { get; init; } = 30;
    public string KeycloakBaseUrl { get; init; } = "http://localhost:8080";
}
```

`backend/src/CCE.Api.Common/Auth/BffSessionCookie.cs` — encrypts/decrypts a `BffSession` record (`AccessToken`, `RefreshToken`, `ExpiresAt`) via `IDataProtector` (purpose: `"cce.bff.session.v1"`). Cookie name: `cce.session`. Options: `HttpOnly = true`, `Secure = true`, `SameSite = Strict`, `IsEssential = true`.

`backend/src/CCE.Api.Common/Auth/BffSessionMiddleware.cs`:

```csharp
using System.Net.Http.Json;
using CCE.Api.Common.Auth;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CCE.Api.Common.Auth;

public sealed class BffSessionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly BffSessionCookie _cookie;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IOptions<BffOptions> _opts;
    private readonly ILogger<BffSessionMiddleware> _logger;

    public BffSessionMiddleware(
        RequestDelegate next,
        BffSessionCookie cookie,
        IHttpClientFactory httpFactory,
        IOptions<BffOptions> opts,
        ILogger<BffSessionMiddleware> logger)
    {
        _next = next;
        _cookie = cookie;
        _httpFactory = httpFactory;
        _opts = opts;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        var session = _cookie.TryRead(ctx);
        if (session is null)
        {
            await _next(ctx).ConfigureAwait(false);
            return;
        }
        if (session.ExpiresAt <= System.DateTimeOffset.UtcNow.AddSeconds(30))
        {
            var refreshed = await TryRefreshAsync(session, ctx.RequestAborted).ConfigureAwait(false);
            if (refreshed is null)
            {
                _cookie.Clear(ctx);
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
            _cookie.Write(ctx, refreshed);
            session = refreshed;
        }
        ctx.Request.Headers.Authorization = $"Bearer {session.AccessToken}";
        await _next(ctx).ConfigureAwait(false);
    }

    private async Task<BffSession?> TryRefreshAsync(BffSession session, CancellationToken ct)
    {
        var opts = _opts.Value;
        var http = _httpFactory.CreateClient("keycloak");
        using var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("client_id", opts.KeycloakClientId),
            new KeyValuePair<string, string>("client_secret", opts.KeycloakClientSecret),
            new KeyValuePair<string, string>("refresh_token", session.RefreshToken),
        });
        var url = $"{opts.KeycloakBaseUrl}/realms/{opts.KeycloakRealm}/protocol/openid-connect/token";
        try
        {
            using var resp = await http.PostAsync(url, form, ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode) return null;
            var tokens = await resp.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct).ConfigureAwait(false);
            if (tokens is null) return null;
            return new BffSession(
                tokens.AccessToken,
                tokens.RefreshToken,
                System.DateTimeOffset.UtcNow.AddSeconds(tokens.ExpiresIn));
        }
        catch (System.Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "BFF refresh failed");
            return null;
        }
    }

    private sealed record TokenResponse(
        [property: System.Text.Json.Serialization.JsonPropertyName("access_token")] string AccessToken,
        [property: System.Text.Json.Serialization.JsonPropertyName("refresh_token")] string RefreshToken,
        [property: System.Text.Json.Serialization.JsonPropertyName("expires_in")] int ExpiresIn);
}

public sealed record BffSession(string AccessToken, string RefreshToken, System.DateTimeOffset ExpiresAt);
```

`backend/src/CCE.Api.Common/Auth/BffAuthEndpoints.cs` — maps the 4 endpoints. `/auth/login` issues PKCE + 302; `/auth/callback` exchanges code; `/auth/refresh` rotates; `/auth/logout` clears cookie + back-channel logout.

(For brevity in the plan, the full endpoint code is in the implementer prompt. The endpoint behaviors must match Section 4.1 + 4.2 of the spec.)

- [ ] **Step 1: Implement files above (TDD: write 5 middleware tests first).**
- [ ] **Step 2: Register in External API `Program.cs`** — `builder.Services.Configure<BffOptions>(...); builder.Services.AddDataProtection(); builder.Services.AddSingleton<BffSessionCookie>(); builder.Services.AddHttpClient("keycloak");`. Pipeline: `app.UseMiddleware<BffSessionMiddleware>();` BEFORE `app.UseAuthentication();`. Endpoints: `app.MapBffAuthEndpoints();`.
- [ ] **Step 3: Run middleware tests** — expect 5/5 pass.
- [ ] **Step 4: Run full suite** — expect 0 regressions.
- [ ] **Step 5: Commit**

```bash
git -c commit.gpgsign=false commit -m "feat(api-common): BFF cookie + Bearer dual-mode auth + 4 /auth endpoints (5 TDD tests) (Phase 0.4)"
```

---

## Task 0.5: Redis output cache + tiered rate limiter

**Files:**
- Create: `backend/src/CCE.Api.Common/Caching/RedisOutputCacheMiddleware.cs`
- Create: `backend/src/CCE.Api.Common/Caching/OutputCacheRegistration.cs` (`AddCceOutputCache` + `UseCceOutputCache`)
- Create: `backend/src/CCE.Api.Common/RateLimiting/TieredRateLimiterRegistration.cs` (replaces or augments existing `AddCceRateLimiter`)
- Modify: `backend/src/CCE.Api.External/Program.cs` (mount middleware + rate limiter)
- Create: `backend/tests/CCE.Api.IntegrationTests/Caching/RedisOutputCacheMiddlewareTests.cs` (4 tests: anonymous GET cached on second request, varies on `Accept-Language`, authenticated bypasses, POST bypasses)

**Rationale:** ADR-0033 + spec §3.2.2 + §3.2.3.

Cache key: `out:{path}?{sortedQueryString}|lang={Accept-Language}`. TTL from `Caching:OutputTtlSeconds` (default 60). Stores body + content-type.

Rate limiter tiers — bind to `RateLimit:<Tier>:RequestsPerMinute`. Classifier: anonymous (no `Authorization` header AND no `cce.session` cookie) → Anonymous tier; method = GET on `/api/search*` OR method ≠ GET → SearchAndWrite tier; else Authenticated tier. Returns 429 + `Retry-After`.

- [ ] **Step 1-5: Standard TDD cycle.**
- [ ] **Step 6: Commit**

```bash
git -c commit.gpgsign=false commit -m "feat(api-common): Redis output cache + tiered rate limiter (4 TDD tests) (Phase 0.5)"
```

---

## Task 0.6: `ISearchClient` + `MeilisearchClient`

**Files:**
- Create: `backend/src/CCE.Application/Search/ISearchClient.cs`
- Create: `backend/src/CCE.Application/Search/SearchHitDto.cs`
- Create: `backend/src/CCE.Application/Search/SearchableType.cs` (enum: News, Events, Resources, Pages, KnowledgeMaps)
- Create: `backend/src/CCE.Infrastructure/Search/MeilisearchClient.cs`
- Modify: `backend/src/CCE.Infrastructure/DependencyInjection.cs` (register)
- Create: `backend/tests/CCE.Infrastructure.Tests/Search/MeilisearchClientTests.cs` (3 tests against a Meilisearch testcontainer: index a doc + search returns it, missing index returns empty result, master-key auth works)

**Rationale:** Read-side abstraction. Indexer hosted service is Phase 02.

`ISearchClient`:

```csharp
public interface ISearchClient
{
    Task EnsureIndexAsync(SearchableType type, CancellationToken ct);
    Task UpsertAsync<TDoc>(SearchableType type, TDoc doc, CancellationToken ct) where TDoc : class;
    Task DeleteAsync(SearchableType type, System.Guid id, CancellationToken ct);
    Task<PagedResult<SearchHitDto>> SearchAsync(string query, SearchableType? type, int page, int pageSize, CancellationToken ct);
}
```

`SearchHitDto`:

```csharp
public sealed record SearchHitDto(
    System.Guid Id,
    SearchableType Type,
    string TitleAr,
    string TitleEn,
    string ExcerptAr,
    string ExcerptEn,
    double Score);
```

`MeilisearchClient` wraps the `Meilisearch` NuGet client. The testcontainer test brings up Meilisearch on a random port, configures `CceInfrastructureOptions.MeilisearchUrl` accordingly, exercises round-trip.

- [ ] **Step 1-5: Standard TDD cycle.**
- [ ] **Step 6: Commit**

```bash
git -c commit.gpgsign=false commit -m "feat(infrastructure): Meilisearch ISearchClient + MeilisearchClient (3 testcontainer tests) (Phase 0.6)"
```

---

## Phase 00 — completion checklist

- [ ] Meilisearch container running on `:7700`; CPM has `Meilisearch` + `HtmlSanitizer`.
- [ ] `IHtmlSanitizer` + `HtmlSanitizerWrapper` shipped (6 tests).
- [ ] `ICountryScopeAccessor` + system fallback + HttpContext impl shipped (registered in both API hosts).
- [ ] BFF cookie + Bearer dual-mode middleware + 4 auth endpoints live (5 tests).
- [ ] `RedisOutputCacheMiddleware` + tiered rate limiter live + config-driven (4 tests).
- [ ] `ISearchClient` + `MeilisearchClient` shipped (3 tests).
- [ ] `dotnet build backend/CCE.sln` 0 errors / 0 warnings.
- [ ] All previous tests still pass; ~21 net new tests; full suite ≈ 815 + 1 skipped.
- [ ] `git status` clean; 6 new commits.

**If all boxes ticked, Phase 00 is complete. Proceed to Phase 01 (public reads).**
