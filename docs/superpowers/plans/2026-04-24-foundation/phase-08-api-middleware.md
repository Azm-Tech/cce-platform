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
    [Fact]
    public async Task Adds_baseline_security_headers()
    {
        using var host = new HostBuilder()
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

## Tasks 8.4 — 8.14 — outline only (full detail when those tasks become next-up)

Plan files this large risk drift; I'll spec each remaining task fully when its predecessor commits land. The shape:

- **8.4 Rate limiting:** ASP.NET Core 8 built-in `RateLimiter` middleware backed by Redis (custom limiter implementation). Anonymous: 60/min. Authenticated: 300/min. Auth endpoints: 10/min. 2 tests (under-limit passes, over-limit returns 429).
- **8.5 Localization:** middleware reads `Accept-Language`, sets `CultureInfo.CurrentCulture`. 1 test for ar/en switching.
- **8.6 Swagger:** `AddSwaggerGen` + `UseSwagger` + `UseSwaggerUI`. 1 test asserting `/swagger/v1/swagger.json` returns valid JSON.
- **8.7 External JWT:** `AddAuthentication().AddJwtBearer()` configured for Keycloak `cce-external` realm; claim mapper for `upn`/`groups`/`preferred_username`. 2 tests (valid token → 200, invalid → 401).
- **8.8 Internal OIDC:** same shape for `cce-internal` realm with full code-flow validation. 2 tests.
- **8.9 `/health` endpoint** (External, anonymous): returns `HealthQuery` result. 1 endpoint test.
- **8.10 `/health/ready`:** dependency probes (SQL, Redis, Keycloak JWKS). 503 if any unhealthy. 2 tests.
- **8.11 `/health/authenticated`:** Internal API; requires `SuperAdmin` policy; returns claims echo. 2 tests (200 with token, 403 without policy).
- **8.12 Permission policies:** `[RequirePermission(Permissions.X)]` attribute or policy registration helper using the source-generated `Permissions` constants. 1 test.
- **8.13 DI composition:** Program.cs of both APIs assembles middleware in correct order. Smoke test (already existing) confirms 200 on root.
- **8.14 E2E integration tests:** 4 tests against `WebApplicationFactory` covering: anonymous health, locale negotiation ar→en, 401 unauthenticated `/health/authenticated`, 200 authenticated.

**Each remaining task will follow the same pattern: failing test → minimal code → run → commit.**

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
