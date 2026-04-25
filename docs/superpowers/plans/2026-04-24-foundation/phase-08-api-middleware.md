# Phase 08 — API Middleware & Endpoints

> Parent: [../2026-04-24-foundation.md](../2026-04-24-foundation.md) · Spec: [../../specs/2026-04-24-foundation-design.md](../../specs/2026-04-24-foundation-design.md)

**Phase goal:** Make the External + Internal APIs production-shaped: correlation IDs, RFC 7807 errors, security headers, Redis rate limiting, locale plumbing, JWT bearer (external) + OIDC code-flow validation (internal), Swagger, and the four Foundation endpoints (`/health`, `/health/ready`, `/health/authenticated`, `/swagger`). Integration tests via `WebApplicationFactory` hit real Keycloak + SQL + Redis containers.

**Tasks in this phase:** 14
**Working directory:** `/Users/m/CCE/`
**Preconditions:** Phase 07 complete; `dotnet test backend/CCE.sln` reports 34 passed; Docker stack healthy (sqlserver, redis, keycloak, maildev, clamav).

---

## Pre-execution sanity checks

1. `dotnet build backend/CCE.sln --nologo 2>&1 | tail -3` → 0 errors.
2. `dotnet test backend/CCE.sln --nologo --no-build 2>&1 | tail -5` → `Passed: 34`.
3. `curl -s http://localhost:8080/realms/cce-internal/.well-known/openid-configuration | jq -r .issuer` → `http://localhost:8080/realms/cce-internal`.
4. `curl -s http://localhost:8080/realms/cce-external/.well-known/openid-configuration | jq -r .issuer` → `http://localhost:8080/realms/cce-external`.
5. `nc -z localhost 1433 && nc -z localhost 6379 && echo OK` → `OK`.

If any fail, stop and report.

---

## Phase 08 task index

| # | Task | Tests added |
|---|---|---|
| 8.1 | Correlation-ID middleware | 2 |
| 8.2 | ProblemDetails / exception handler middleware | 3 |
| 8.3 | Security headers middleware | 1 |
| 8.4 | Redis-backed rate limiting | 2 |
| 8.5 | Localization middleware (Accept-Language → CultureInfo) | 1 |
| 8.6 | Add Swashbuckle + Swagger UI | 1 |
| 8.7 | External API: JWT bearer auth + claim mappings | 2 |
| 8.8 | Internal API: OIDC code-flow validation + claim mappings | 2 |
| 8.9 | `/health` endpoint (External) backed by HealthQuery | 1 |
| 8.10 | `/health/ready` endpoint (both APIs) — SQL + Redis + JWKS probes | 2 |
| 8.11 | `/health/authenticated` endpoint (Internal) requiring `SuperAdmin` policy | 2 |
| 8.12 | Permission policy registration helpers | 1 |
| 8.13 | API DI composition: wire all middleware in correct order | 0 (smoke only) |
| 8.14 | End-to-end integration tests via `WebApplicationFactory` | 4 |

**Estimated total tests added: ~24** (Application.Tests + Api.IntegrationTests). Final solution test count after Phase 08: ~58.

---

## Common API project setup

Tasks 8.1–8.6 add packages and code to **both** `CCE.Api.External` and `CCE.Api.Internal`. To avoid copy-paste, we'll add a shared `CCE.Api.Common` library project that both APIs reference.

### Task 8.0 (preliminary): Create `CCE.Api.Common` library

**Files:**
- Create: `backend/src/CCE.Api.Common/CCE.Api.Common.csproj`

```bash
dotnet new classlib -n CCE.Api.Common -o backend/src/CCE.Api.Common --framework net8.0 --force
rm -f backend/src/CCE.Api.Common/Class1.cs
dotnet sln backend/CCE.sln add backend/src/CCE.Api.Common/CCE.Api.Common.csproj
dotnet add backend/src/CCE.Api.Common/CCE.Api.Common.csproj reference backend/src/CCE.Application/CCE.Application.csproj
dotnet add backend/src/CCE.Api.External/CCE.Api.External.csproj reference backend/src/CCE.Api.Common/CCE.Api.Common.csproj
dotnet add backend/src/CCE.Api.Internal/CCE.Api.Internal.csproj reference backend/src/CCE.Api.Common/CCE.Api.Common.csproj
```

Overwrite `backend/src/CCE.Api.Common/CCE.Api.Common.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <!-- CA1031: catch (Exception) is the documented pattern for global exception handlers
         that convert all unhandled errors into RFC 7807 ProblemDetails. Suppressing here
         (project-scoped) so the rule keeps firing in business-logic projects where it's
         a real smell.
         CA1308: BCP 47 language tags are conventionally lowercase ("ar", "en"). The rule
         prefers ToUpperInvariant for security-sensitive normalization (Turkish-i edge case),
         but locale matching against a lowercase array is the correct lowercase use case. -->
    <NoWarn>$(NoWarn);CA1031;CA1308</NoWarn>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" />
    <PackageReference Include="Microsoft.AspNetCore.Authentication.OpenIdConnect" />
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" />
    <PackageReference Include="Swashbuckle.AspNetCore" />
    <PackageReference Include="Swashbuckle.AspNetCore.Annotations" />
    <PackageReference Include="StackExchange.Redis" />
    <PackageReference Include="Hellang.Middleware.ProblemDetails" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\CCE.Application\CCE.Application.csproj" />
  </ItemGroup>

</Project>
```

Build to verify:
```bash
dotnet build backend/CCE.sln --nologo -c Debug 2>&1 | tail -5
```
Expected: 0 errors.

Commit:
```bash
git add backend/src/CCE.Api.Common backend/src/CCE.Api.External backend/src/CCE.Api.Internal backend/CCE.sln
git -c commit.gpgsign=false commit -m "feat(phase-08): add CCE.Api.Common shared library + wire both API projects to reference it"
```

---

## Task 8.1: Correlation-ID middleware

**Files:**
- Create: `backend/src/CCE.Api.Common/Middleware/CorrelationIdMiddleware.cs`
- Create: `backend/tests/CCE.Api.IntegrationTests/Middleware/CorrelationIdMiddlewareTests.cs`

**Rationale:** Every HTTP request gets a `X-Correlation-Id` header — generated if absent, echoed if present. Logger scope is enriched so all log lines for the request carry the id. Phase 08 of spec §6.3 mandates this for end-to-end debugging.

- [ ] **Step 1: Add reference and write the failing test**

```bash
dotnet add backend/tests/CCE.Api.IntegrationTests/CCE.Api.IntegrationTests.csproj reference backend/src/CCE.Api.Common/CCE.Api.Common.csproj
```

`backend/tests/CCE.Api.IntegrationTests/Middleware/CorrelationIdMiddlewareTests.cs`:

```csharp
using CCE.Api.Common.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CCE.Api.IntegrationTests.Middleware;

public class CorrelationIdMiddlewareTests
{
    private static IHost BuildTestHost() =>
        new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.Configure(app =>
                {
                    app.UseMiddleware<CorrelationIdMiddleware>();
                    app.Run(async ctx =>
                    {
                        await ctx.Response.WriteAsync(ctx.Items[CorrelationIdMiddleware.ItemKey]?.ToString() ?? "missing");
                    });
                });
            })
            .Start();

    [Fact]
    public async Task Generates_correlation_id_when_header_absent()
    {
        using var host = BuildTestHost();
        var client = host.GetTestClient();

        var resp = await client.GetAsync(new Uri("/", UriKind.Relative));

        resp.Headers.TryGetValues("X-Correlation-Id", out var values).Should().BeTrue();
        var id = values!.Single();
        Guid.TryParse(id, out _).Should().BeTrue();
        (await resp.Content.ReadAsStringAsync()).Should().Be(id);
    }

    [Fact]
    public async Task Echoes_provided_correlation_id()
    {
        using var host = BuildTestHost();
        var client = host.GetTestClient();
        var sent = Guid.NewGuid().ToString();
        client.DefaultRequestHeaders.Add("X-Correlation-Id", sent);

        var resp = await client.GetAsync(new Uri("/", UriKind.Relative));

        resp.Headers.GetValues("X-Correlation-Id").Single().Should().Be(sent);
        (await resp.Content.ReadAsStringAsync()).Should().Be(sent);
    }
}
```

- [ ] **Step 2: Run — expect compile error**

- [ ] **Step 3: Write `backend/src/CCE.Api.Common/Middleware/CorrelationIdMiddleware.cs`**

```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CCE.Api.Common.Middleware;

public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemKey = "CorrelationId";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var id = context.Request.Headers.TryGetValue(HeaderName, out var existing) && !string.IsNullOrWhiteSpace(existing)
            ? existing.ToString()
            : Guid.NewGuid().ToString();

        context.Items[ItemKey] = id;
        context.Response.Headers[HeaderName] = id;

        using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = id }))
        {
            await _next(context).ConfigureAwait(false);
        }
    }
}
```

- [ ] **Step 4: Run — expect 2 passes**

- [ ] **Step 5: Commit**

```bash
git add backend/src/CCE.Api.Common/Middleware backend/tests/CCE.Api.IntegrationTests/Middleware
git -c commit.gpgsign=false commit -m "feat(phase-08): add CorrelationIdMiddleware (generates or echoes X-Correlation-Id, enriches log scope)"
```

---

## Task 8.2: ProblemDetails / exception-handler middleware

**Files:**
- Create: `backend/src/CCE.Api.Common/Middleware/ExceptionHandlingMiddleware.cs`
- Create: `backend/tests/CCE.Api.IntegrationTests/Middleware/ExceptionHandlingMiddlewareTests.cs`

**Rationale:** Converts every unhandled exception into RFC 7807 ProblemDetails with `correlationId` in body + header. FluentValidation's `ValidationException` → 400 with field errors; everything else → 500.

- [ ] **Step 1: Write the failing tests**

```csharp
using System.Net;
using System.Text.Json;
using CCE.Api.Common.Middleware;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace CCE.Api.IntegrationTests.Middleware;

public class ExceptionHandlingMiddlewareTests
{
    private static IHost BuildHost(RequestDelegate handler) =>
        new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.Configure(app =>
                {
                    app.UseMiddleware<CorrelationIdMiddleware>();
                    app.UseMiddleware<ExceptionHandlingMiddleware>();
                    app.Run(handler);
                });
            })
            .Start();

    [Fact]
    public async Task Returns_500_problem_details_on_unhandled_exception()
    {
        using var host = BuildHost(_ => throw new InvalidOperationException("boom"));
        var client = host.GetTestClient();

        var resp = await client.GetAsync(new Uri("/", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        resp.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var body = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body).RootElement;
        doc.GetProperty("status").GetInt32().Should().Be(500);
        doc.GetProperty("correlationId").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Returns_400_problem_details_on_validation_exception()
    {
        var failures = new List<ValidationFailure>
        {
            new("Name", "must not be empty"),
            new("Age", "must be positive")
        };
        using var host = BuildHost(_ => throw new ValidationException(failures));
        var client = host.GetTestClient();

        var resp = await client.GetAsync(new Uri("/", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body).RootElement;
        doc.GetProperty("status").GetInt32().Should().Be(400);
        doc.GetProperty("errors").GetProperty("Name").EnumerateArray().First().GetString().Should().Be("must not be empty");
        doc.GetProperty("errors").GetProperty("Age").EnumerateArray().First().GetString().Should().Be("must be positive");
    }

    [Fact]
    public async Task Includes_correlation_id_in_response_body()
    {
        using var host = BuildHost(_ => throw new InvalidOperationException("x"));
        var client = host.GetTestClient();
        var sent = Guid.NewGuid().ToString();
        client.DefaultRequestHeaders.Add("X-Correlation-Id", sent);

        var resp = await client.GetAsync(new Uri("/", UriKind.Relative));

        var body = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body).RootElement;
        doc.GetProperty("correlationId").GetString().Should().Be(sent);
    }
}
```

- [ ] **Step 2: Write `backend/src/CCE.Api.Common/Middleware/ExceptionHandlingMiddleware.cs`**

```csharp
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CCE.Api.Common.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (ValidationException ex)
        {
            await WriteValidationProblemAsync(context, ex).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteServerErrorAsync(context, ex).ConfigureAwait(false);
        }
    }

    private static string GetCorrelationId(HttpContext ctx) =>
        ctx.Items[CorrelationIdMiddleware.ItemKey]?.ToString() ?? Guid.NewGuid().ToString();

    private static async Task WriteValidationProblemAsync(HttpContext ctx, ValidationException ex)
    {
        var errors = ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        var problem = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            Title = "One or more validation errors occurred."
        };
        problem.Extensions["correlationId"] = GetCorrelationId(ctx);

        ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
        ctx.Response.ContentType = "application/problem+json";
        await JsonSerializer.SerializeAsync(ctx.Response.Body, problem).ConfigureAwait(false);
    }

    private static async Task WriteServerErrorAsync(HttpContext ctx, Exception ex)
    {
        _ = ex; // intentionally unused — never expose to clients
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
            Title = "An unexpected error occurred.",
            Detail = "See server logs by correlation id for details."
        };
        problem.Extensions["correlationId"] = GetCorrelationId(ctx);

        ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
        ctx.Response.ContentType = "application/problem+json";
        await JsonSerializer.SerializeAsync(ctx.Response.Body, problem).ConfigureAwait(false);
    }
}
```

- [ ] **Step 3: Run — expect 3 passes**

- [ ] **Step 4: Commit**

```bash
git add backend/src/CCE.Api.Common/Middleware backend/tests/CCE.Api.IntegrationTests/Middleware
git -c commit.gpgsign=false commit -m "feat(phase-08): add ExceptionHandlingMiddleware (RFC 7807 ProblemDetails + ValidationException → 400 + correlation id in body)"
```

---

## Task 8.3: Security headers middleware

**Files:**
- Create: `backend/src/CCE.Api.Common/Middleware/SecurityHeadersMiddleware.cs`
- Create: `backend/tests/CCE.Api.IntegrationTests/Middleware/SecurityHeadersMiddlewareTests.cs`

**Rationale:** Per spec §9.1, every response carries CSP, X-Content-Type-Options, Referrer-Policy, Permissions-Policy. HSTS is feature-flag-gated (off in dev).

- [ ] **Step 1: Write the failing test**

```csharp
using CCE.Api.Common.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace CCE.Api.IntegrationTests.Middleware;

public class SecurityHeadersMiddlewareTests
{
    // Synchronous helper — keeps `.Start()` out of async test bodies (CA1849).
    private static IHost BuildTestHost() =>
        new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.Configure(app =>
                {
                    app.UseMiddleware<SecurityHeadersMiddleware>();
                    app.Run(c => c.Response.WriteAsync("ok"));
                });
            })
            .Start();

    [Fact]
    public async Task Adds_baseline_security_headers()
    {
        using var host = BuildTestHost();
        var client = host.GetTestClient();

        var resp = await client.GetAsync(new Uri("/", UriKind.Relative));

        resp.Headers.GetValues("X-Content-Type-Options").Single().Should().Be("nosniff");
        resp.Headers.GetValues("Referrer-Policy").Single().Should().Be("strict-origin-when-cross-origin");
        resp.Headers.GetValues("Permissions-Policy").Single().Should().Contain("camera=()");
        resp.Headers.GetValues("Content-Security-Policy").Single().Should().Contain("default-src 'self'");
        resp.Headers.Contains("Strict-Transport-Security").Should().BeFalse(); // off by default in dev
    }
}
```

- [ ] **Step 2: Write `SecurityHeadersMiddleware.cs`**

```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace CCE.Api.Common.Middleware;

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly bool _hstsEnabled;

    public SecurityHeadersMiddleware(RequestDelegate next, IConfiguration? configuration = null)
    {
        _next = next;
        _hstsEnabled = configuration?.GetValue<bool>("FEATURE_HSTS") ?? false;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var h = context.Response.Headers;
            h["X-Content-Type-Options"] = "nosniff";
            h["Referrer-Policy"] = "strict-origin-when-cross-origin";
            h["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
            h["Cross-Origin-Opener-Policy"] = "same-origin";
            h["Content-Security-Policy"] =
                "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; " +
                "img-src 'self' data:; connect-src 'self'; frame-ancestors 'none';";
            if (_hstsEnabled)
            {
                h["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
            }
            return Task.CompletedTask;
        });
        await _next(context).ConfigureAwait(false);
    }
}
```

- [ ] **Step 3: Run + commit**

```bash
dotnet test backend/tests/CCE.Api.IntegrationTests --nologo -c Debug 2>&1 | tail -6
```
Expected: 6 passed cumulative (2 correlation + 3 exception + 1 security).

```bash
git add backend/src/CCE.Api.Common/Middleware backend/tests/CCE.Api.IntegrationTests/Middleware
git -c commit.gpgsign=false commit -m "feat(phase-08): add SecurityHeadersMiddleware (CSP, X-Content-Type-Options, Referrer-Policy, Permissions-Policy; HSTS feature-flagged)"
```

---

## Task 8.4: Rate limiting (in-memory fixed-window)

**Files:**
- Create: `backend/src/CCE.Api.Common/RateLimiting/CceRateLimiterRegistration.cs`
- Create: `backend/tests/CCE.Api.IntegrationTests/RateLimiting/RateLimiterTests.cs`

**Rationale:** Foundation uses ASP.NET Core 8's built-in `AddRateLimiter` with fixed-window in-memory counters. Spec §9.6 calls for Redis-backed for multi-instance prod — that's deferred to sub-project 8 with an ADR. Foundation proves rate limiting wires correctly at the limit boundary.

**Limits (per spec §9.6):** 60/min anonymous, 300/min authenticated, 10/min auth endpoints. Foundation tests just verify the "anonymous" policy works — Tasks 8.7–8.8 add authenticated policies once auth lands.

- [ ] **Step 1: Write the failing test**

```csharp
using System.Net;
using CCE.Api.Common.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CCE.Api.IntegrationTests.RateLimiting;

public class RateLimiterTests
{
    // Build a host with a tight 3-per-window limit for deterministic tests.
    private static IHost BuildTestHost() =>
        new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(s => s.AddCceRateLimiter(testLimit: 3));
                web.Configure(app =>
                {
                    app.UseRateLimiter();
                    app.MapWhen(_ => true, branch =>
                    {
                        branch.Run(c => c.Response.WriteAsync("ok"));
                    });
                });
            })
            .Start();

    [Fact]
    public async Task Allows_requests_under_the_limit()
    {
        using var host = BuildTestHost();
        var client = host.GetTestClient();

        for (var i = 0; i < 3; i++)
        {
            var resp = await client.GetAsync(new Uri("/", UriKind.Relative));
            resp.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task Returns_429_after_exceeding_the_limit()
    {
        using var host = BuildTestHost();
        var client = host.GetTestClient();

        for (var i = 0; i < 3; i++)
        {
            await client.GetAsync(new Uri("/", UriKind.Relative));
        }
        var over = await client.GetAsync(new Uri("/", UriKind.Relative));

        over.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}
```

- [ ] **Step 2: Write `backend/src/CCE.Api.Common/RateLimiting/CceRateLimiterRegistration.cs`**

```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.RateLimiting;

namespace CCE.Api.Common.RateLimiting;

public static class CceRateLimiterRegistration
{
    public const string AnonymousPolicy = "anonymous";

    /// <summary>
    /// Registers the CCE rate limiter with the anonymous policy applied as the global limiter.
    /// </summary>
    /// <param name="testLimit">Override the per-window limit (test/dev only). Production uses 60.</param>
    public static IServiceCollection AddCceRateLimiter(this IServiceCollection services, int testLimit = 60) =>
        services.AddRateLimiter(opts =>
        {
            opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            opts.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = testLimit,
                        QueueLimit = 0,
                        Window = TimeSpan.FromMinutes(1)
                    }));
        });
}
```

- [ ] **Step 3: Run tests + commit**

```bash
dotnet test backend/tests/CCE.Api.IntegrationTests --nologo -c Debug 2>&1 | tail -8
# Expected: 8 cumulative (2 correlation + 3 ProblemDetails + 1 security + 2 rate-limit)
git add backend/src/CCE.Api.Common/RateLimiting backend/tests/CCE.Api.IntegrationTests/RateLimiting
git -c commit.gpgsign=false commit -m "feat(phase-08): add fixed-window rate limiter (60/min anon by default; testable override; 429 on exceed)"
```

---

## Task 8.5: Localization middleware (Accept-Language → CultureInfo)

**Files:**
- Create: `backend/src/CCE.Api.Common/Middleware/LocalizationMiddleware.cs`
- Create: `backend/tests/CCE.Api.IntegrationTests/Middleware/LocalizationMiddlewareTests.cs`

**Rationale:** Reads `Accept-Language`, picks the best match from `[ar, en]` (default `ar`), sets `CultureInfo.CurrentCulture` for the request scope. Downstream code reads `CultureInfo.CurrentCulture.Name`.

- [ ] **Step 1: Write the failing test**

```csharp
using System.Globalization;
using CCE.Api.Common.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace CCE.Api.IntegrationTests.Middleware;

public class LocalizationMiddlewareTests
{
    private static IHost BuildTestHost() =>
        new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.Configure(app =>
                {
                    app.UseMiddleware<LocalizationMiddleware>();
                    app.Run(c => c.Response.WriteAsync(CultureInfo.CurrentCulture.Name));
                });
            })
            .Start();

    [Theory]
    [InlineData("ar", "ar")]
    [InlineData("en", "en")]
    [InlineData("en-US,en;q=0.9,ar;q=0.8", "en")]
    [InlineData("fr", "ar")]                    // unsupported → default ar
    [InlineData("", "ar")]                       // empty → default ar
    public async Task Selects_supported_locale_or_falls_back_to_ar(string acceptLanguage, string expected)
    {
        using var host = BuildTestHost();
        var client = host.GetTestClient();
        if (!string.IsNullOrEmpty(acceptLanguage))
        {
            client.DefaultRequestHeaders.Add("Accept-Language", acceptLanguage);
        }

        var resp = await client.GetAsync(new Uri("/", UriKind.Relative));

        var body = await resp.Content.ReadAsStringAsync();
        body.Should().Be(expected);
    }
}
```

- [ ] **Step 2: Write `LocalizationMiddleware.cs`**

```csharp
using Microsoft.AspNetCore.Http;
using System.Globalization;

namespace CCE.Api.Common.Middleware;

public sealed class LocalizationMiddleware
{
    private static readonly string[] Supported = ["ar", "en"];
    private const string DefaultLocale = "ar";

    private readonly RequestDelegate _next;

    public LocalizationMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var locale = PickLocale(context.Request.Headers.AcceptLanguage.ToString());
        var culture = CultureInfo.GetCultureInfo(locale);

        var prevCulture = CultureInfo.CurrentCulture;
        var prevUiCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        finally
        {
            CultureInfo.CurrentCulture = prevCulture;
            CultureInfo.CurrentUICulture = prevUiCulture;
        }
    }

    private static string PickLocale(string acceptLanguage)
    {
        if (string.IsNullOrWhiteSpace(acceptLanguage))
        {
            return DefaultLocale;
        }
        // Parse comma-separated entries, trim quality factors, take first matching supported tag.
        foreach (var entry in acceptLanguage.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var tag = entry.Split(';', 2)[0].Trim();
            // "en-US" → "en"
            var primary = tag.Split('-', 2)[0].ToLowerInvariant();
            if (Array.IndexOf(Supported, primary) >= 0)
            {
                return primary;
            }
        }
        return DefaultLocale;
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add backend/src/CCE.Api.Common/Middleware/LocalizationMiddleware.cs backend/tests/CCE.Api.IntegrationTests/Middleware/LocalizationMiddlewareTests.cs
git -c commit.gpgsign=false commit -m "feat(phase-08): add LocalizationMiddleware (Accept-Language → CultureInfo, ar default)"
```

---

## Task 8.6: Swagger UI + OpenAPI export

**Files:**
- Create: `backend/src/CCE.Api.Common/OpenApi/CceOpenApiRegistration.cs`
- Modify: `backend/src/CCE.Api.External/Program.cs`
- Modify: `backend/src/CCE.Api.Internal/Program.cs`
- Create: `backend/tests/CCE.Api.IntegrationTests/OpenApi/SwaggerEndpointTests.cs`

**Rationale:** Swashbuckle generates OpenAPI from controllers + minimal-API endpoint metadata. Phase 13 hooks the export into a contract-bridge step. Foundation just verifies `/swagger/v1/swagger.json` returns valid JSON with at least the root endpoint described.

- [ ] **Step 1: Write the failing test**

```csharp
using System.Net;
using System.Text.Json;
using CCE.Api.External;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CCE.Api.IntegrationTests.OpenApi;

public class SwaggerEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SwaggerEndpointTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Swagger_json_is_served_and_well_formed()
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync(new Uri("/swagger/v1/swagger.json", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body).RootElement;
        doc.GetProperty("openapi").GetString().Should().StartWith("3.");
        doc.GetProperty("info").GetProperty("title").GetString().Should().NotBeNullOrWhiteSpace();
    }
}
```

The test references `CCE.Api.External.Program` — that's available because Phase 03 added `public partial class Program;` to the API project.

- [ ] **Step 2: Add reference from IntegrationTests to External and Internal API projects**

(Already added in Phase 03; verify with `grep` and add if missing.)

- [ ] **Step 3: Write `backend/src/CCE.Api.Common/OpenApi/CceOpenApiRegistration.cs`**

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace CCE.Api.Common.OpenApi;

public static class CceOpenApiRegistration
{
    public static IServiceCollection AddCceOpenApi(this IServiceCollection services, string title)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(opts =>
        {
            opts.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = title,
                Version = "v1",
                Description = "CCE Knowledge Center API — Foundation"
            });
        });
        return services;
    }

    public static IApplicationBuilder UseCceOpenApi(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI();
        return app;
    }
}
```

- [ ] **Step 4: Update `backend/src/CCE.Api.External/Program.cs`**

```csharp
using CCE.Api.Common.OpenApi;
using CCE.Application;
using CCE.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddCceOpenApi("CCE External API");

var app = builder.Build();

app.UseCceOpenApi();
app.MapGet("/", () => "CCE.Api.External — Foundation");

app.Run();

public partial class Program;
```

- [ ] **Step 5: Update `backend/src/CCE.Api.Internal/Program.cs` (same shape, different title)**

Replace `"CCE External API"` with `"CCE Internal API"`.

- [ ] **Step 6: Run tests + commit**

```bash
dotnet test backend/tests/CCE.Api.IntegrationTests --nologo -c Debug 2>&1 | tail -8
# Expected: 11 cumulative (2 correlation + 3 ProblemDetails + 1 security + 2 rate-limit + 1 locale theory expansion = 5 + 1 swagger = 12)
git add backend/src/CCE.Api.Common/OpenApi backend/src/CCE.Api.External/Program.cs backend/src/CCE.Api.Internal/Program.cs backend/tests/CCE.Api.IntegrationTests/OpenApi
git -c commit.gpgsign=false commit -m "feat(phase-08): add Swagger UI + OpenAPI export on /swagger for both APIs"
```

---

## Tasks 8.7 — 8.14 — outline only (full detail when those become next-up)

- **8.7 External JWT:** `AddAuthentication().AddJwtBearer()` configured for Keycloak `cce-external` realm; claim mapper for `upn`/`groups`/`preferred_username`. 2 tests (valid token → 200, invalid → 401).
- **8.8 Internal OIDC:** same shape for `cce-internal` realm. 2 tests.
- **8.9 `/health` endpoint** (External, anonymous): returns `HealthQuery` result. 1 endpoint test.
- **8.10 `/health/ready`:** dependency probes (SQL, Redis, Keycloak JWKS). 503 if any unhealthy. 2 tests.
- **8.11 `/health/authenticated`:** Internal; requires `SuperAdmin` policy; returns claims echo. 2 tests.
- **8.12 Permission policies:** policy registration helper using source-generated `Permissions` constants. 1 test.
- **8.13 DI composition:** Program.cs assembles middleware in correct order. Smoke test confirms 200 on root.
- **8.14 E2E integration tests:** 4 tests via `WebApplicationFactory`.

---

## Phase 08 — completion checklist

- [ ] `CCE.Api.Common` shared library wired into both APIs.
- [ ] `CorrelationIdMiddleware`, `ExceptionHandlingMiddleware`, `SecurityHeadersMiddleware` in Common.
- [ ] Rate limiting (Redis-backed) and Localization middleware in place.
- [ ] Swagger UI on `/swagger` for both APIs.
- [ ] External API: JWT bearer with Keycloak `cce-external` audience.
- [ ] Internal API: OIDC code-flow with Keycloak `cce-internal` audience + `SuperAdmin` policy.
- [ ] Endpoints: `/health` (external + internal), `/health/ready` (both, 503 on dep failure), `/health/authenticated` (internal, requires SuperAdmin).
- [ ] Permission policy helper registers source-generated `Permissions` constants.
- [ ] `dotnet build backend/CCE.sln` 0 errors, `dotnet test backend/CCE.sln` ~58 passed.
- [ ] `git status` clean.
- [ ] ~14 new commits.

**If all boxes ticked, phase 08 is complete. Proceed to phase 09 (Nx workspace bootstrap — first frontend phase).**
