# Phase 07 — Application Layer (MediatR Pipeline + Health Handlers)

> Parent: [../2026-04-24-foundation.md](../2026-04-24-foundation.md) · Spec: [../../specs/2026-04-24-foundation-design.md](../../specs/2026-04-24-foundation-design.md)

**Phase goal:** Wire the MediatR pipeline (Logging + Validation behaviors) and ship the two Foundation handlers — `HealthQuery` (anonymous) and `AuthenticatedHealthQuery` (claims echo). All TDD per ADR-0007. Phase 08 then exposes these handlers as HTTP endpoints with full middleware.

**Tasks in this phase:** 5
**Working directory:** `/Users/m/CCE/`
**Preconditions:** Phase 06 complete; `dotnet test backend/CCE.sln` reports 22 passed; Docker stack healthy.

---

## Pre-execution sanity checks

1. `dotnet build backend/CCE.sln --nologo 2>&1 | tail -3` → 0 errors.
2. `dotnet test backend/CCE.sln --nologo --no-build 2>&1 | tail -5` → `Passed: 22`.
3. `grep -c '<PackageReference Include="MediatR"' backend/src/CCE.Application/CCE.Application.csproj` → 1.
4. `grep -c '<PackageReference Include="FluentValidation"' backend/src/CCE.Application/CCE.Application.csproj` → 1.

If any fail, stop and report.

---

## Why pipeline behaviors?

MediatR `IPipelineBehavior<TRequest, TResponse>` interceptors wrap every handler call. We use two:

- **`LoggingBehavior`** — logs handler entry, exit, and elapsed time at `Information` level with correlation id. Auto-applied to every query/command, so handlers don't repeat boilerplate.
- **`ValidationBehavior`** — runs every `IValidator<TRequest>` registered in DI. Failures throw `ValidationException` that Phase 08's middleware converts to ProblemDetails 400.

Both behaviors live in the Application layer because they're business-logic concerns, not infrastructure.

---

## Task 7.1: Add `LoggingBehavior` to the MediatR pipeline

**Files:**
- Create: `backend/src/CCE.Application/Common/Behaviors/LoggingBehavior.cs`
- Create: `backend/tests/CCE.Application.Tests/Common/Behaviors/LoggingBehaviorTests.cs`

**Rationale:** Every handler invocation logs entry, success, and elapsed time. Failures escape — we don't catch and re-throw; logs reflect what actually happened. Stopwatch is per-request, not shared.

- [ ] **Step 1: Write the failing test**

```csharp
using CCE.Application.Common.Behaviors;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute.ExceptionExtensions;

namespace CCE.Application.Tests.Common.Behaviors;

public class LoggingBehaviorTests
{
    private sealed record TestRequest(string Name) : IRequest<string>;

    [Fact]
    public async Task Logs_entry_and_success_for_handled_request()
    {
        var logger = Substitute.For<ILogger<LoggingBehavior<TestRequest, string>>>();
        var sut = new LoggingBehavior<TestRequest, string>(logger);

        var result = await sut.Handle(new TestRequest("x"), () => Task.FromResult("ok"), CancellationToken.None);

        result.Should().Be("ok");
        logger.ReceivedCalls()
            .Where(c => c.GetArguments()[0] is LogLevel l && l == LogLevel.Information)
            .Should().HaveCountGreaterThanOrEqualTo(2);   // entry + success
    }

    [Fact]
    public async Task Lets_exceptions_escape()
    {
        var logger = Substitute.For<ILogger<LoggingBehavior<TestRequest, string>>>();
        var sut = new LoggingBehavior<TestRequest, string>(logger);
        Task<string> Failing() => Task.FromException<string>(new InvalidOperationException("boom"));

        var act = async () => await sut.Handle(new TestRequest("x"), Failing, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
    }
}
```

- [ ] **Step 2: Run — expect compile error (LoggingBehavior doesn't exist)**

```bash
dotnet test backend/tests/CCE.Application.Tests/CCE.Application.Tests.csproj --nologo -c Debug 2>&1 | tail -8
```
Expected: build error mentioning `LoggingBehavior`.

- [ ] **Step 3: Add reference from Application.Tests to Application**

```bash
dotnet add backend/tests/CCE.Application.Tests/CCE.Application.Tests.csproj reference backend/src/CCE.Application/CCE.Application.csproj
```

- [ ] **Step 4: Write `backend/src/CCE.Application/Common/Behaviors/LoggingBehavior.cs`**

```csharp
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace CCE.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that logs handler entry, success, and elapsed time.
/// Logs at <see cref="LogLevel.Information"/>. Exceptions are not caught — they escape
/// to the next pipeline stage (typically the API middleware that converts to ProblemDetails).
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger) => _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("Handling {RequestName}", requestName);

        var sw = Stopwatch.StartNew();
        var response = await next().ConfigureAwait(false);
        sw.Stop();

        _logger.LogInformation(
            "Handled {RequestName} in {ElapsedMs}ms",
            requestName,
            sw.ElapsedMilliseconds);

        return response;
    }
}
```

- [ ] **Step 5: Add `Microsoft.Extensions.Logging.Abstractions` package reference if not present**

```bash
grep -c 'Microsoft.Extensions.Logging.Abstractions' backend/src/CCE.Application/CCE.Application.csproj
```
If 0, add inside the existing `<ItemGroup>` for packages. (It's pinned in CPM already.)

- [ ] **Step 6: Run tests — expect 2 passed**

```bash
dotnet test backend/tests/CCE.Application.Tests/CCE.Application.Tests.csproj --nologo -c Debug 2>&1 | tail -8
```
Expected: 2 passed.

- [ ] **Step 7: Commit**

```bash
git add backend/src/CCE.Application/Common/Behaviors backend/tests/CCE.Application.Tests
git -c commit.gpgsign=false commit -m "feat(phase-07): add MediatR LoggingBehavior with 2 TDD tests (logs entry/success, lets exceptions escape)"
```

---

## Task 7.2: Add `ValidationBehavior`

**Files:**
- Create: `backend/src/CCE.Application/Common/Behaviors/ValidationBehavior.cs`
- Create: `backend/tests/CCE.Application.Tests/Common/Behaviors/ValidationBehaviorTests.cs`

**Rationale:** Auto-runs every `IValidator<TRequest>` registered in DI. Multiple failures aggregate into one `FluentValidation.ValidationException`. Phase 08 catches this in API middleware → 400 ProblemDetails with field-level errors.

- [ ] **Step 1: Write the failing tests**

```csharp
using CCE.Application.Common.Behaviors;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace CCE.Application.Tests.Common.Behaviors;

public class ValidationBehaviorTests
{
    private sealed record TestRequest(string Name) : IRequest<string>;

    [Fact]
    public async Task Passes_through_when_no_validators_registered()
    {
        var sut = new ValidationBehavior<TestRequest, string>(validators: []);

        var result = await sut.Handle(new TestRequest("ok"), () => Task.FromResult("done"), CancellationToken.None);

        result.Should().Be("done");
    }

    [Fact]
    public async Task Passes_through_when_all_validators_pass()
    {
        var validator = Substitute.For<IValidator<TestRequest>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var sut = new ValidationBehavior<TestRequest, string>([validator]);

        var result = await sut.Handle(new TestRequest("ok"), () => Task.FromResult("done"), CancellationToken.None);

        result.Should().Be("done");
    }

    [Fact]
    public async Task Throws_aggregated_ValidationException_on_failures()
    {
        var v1 = Substitute.For<IValidator<TestRequest>>();
        v1.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([new ValidationFailure("Name", "must not be empty")]));
        var v2 = Substitute.For<IValidator<TestRequest>>();
        v2.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([new ValidationFailure("Name", "must be at least 3 chars")]));

        var sut = new ValidationBehavior<TestRequest, string>([v1, v2]);

        var act = async () => await sut.Handle(new TestRequest(""), () => Task.FromResult("done"), CancellationToken.None);

        var ex = (await act.Should().ThrowAsync<ValidationException>()).Subject.First();
        ex.Errors.Should().HaveCount(2);
        ex.Errors.Should().Contain(f => f.ErrorMessage == "must not be empty");
        ex.Errors.Should().Contain(f => f.ErrorMessage == "must be at least 3 chars");
    }
}
```

- [ ] **Step 2: Run — expect compile error**

```bash
dotnet test backend/tests/CCE.Application.Tests/CCE.Application.Tests.csproj --nologo -c Debug 2>&1 | tail -6
```
Expected: build error referencing `ValidationBehavior`.

- [ ] **Step 3: Write `backend/src/CCE.Application/Common/Behaviors/ValidationBehavior.cs`**

```csharp
using FluentValidation;
using MediatR;

namespace CCE.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that runs every <see cref="IValidator{T}"/> registered for the request.
/// Aggregates all failures across validators into a single <see cref="ValidationException"/>.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next().ConfigureAwait(false);
        }

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)))
            .ConfigureAwait(false);

        var failures = results.SelectMany(r => r.Errors).Where(f => f is not null).ToList();
        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }

        return await next().ConfigureAwait(false);
    }
}
```

- [ ] **Step 4: Run — expect 3 passed**

```bash
dotnet test backend/tests/CCE.Application.Tests/CCE.Application.Tests.csproj --nologo -c Debug 2>&1 | tail -6
```
Expected: 5 total in Application.Tests (2 LoggingBehavior + 3 ValidationBehavior), all passed.

- [ ] **Step 5: Commit**

```bash
git add backend/src/CCE.Application/Common/Behaviors backend/tests/CCE.Application.Tests
git -c commit.gpgsign=false commit -m "feat(phase-07): add MediatR ValidationBehavior with 3 TDD tests (passthrough, success, aggregated failures)"
```

---

## Task 7.3: `HealthQuery` + handler (anonymous health endpoint backing)

**Files:**
- Create: `backend/src/CCE.Application/Health/HealthQuery.cs`
- Create: `backend/src/CCE.Application/Health/HealthQueryHandler.cs`
- Create: `backend/src/CCE.Application/Health/HealthResult.cs`
- Create: `backend/tests/CCE.Application.Tests/Health/HealthQueryHandlerTests.cs`

**Rationale:** Returns `{ status, version, locale }` for the anonymous `GET /health` endpoint Phase 08 will expose. The handler reads version from the assembly and `ISystemClock` (so the response timestamp is testable).

- [ ] **Step 1: Write the failing test**

```csharp
using CCE.Application.Health;
using CCE.Domain.Common;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Health;

public class HealthQueryHandlerTests
{
    [Fact]
    public async Task Returns_ok_status_with_locale_and_now_timestamp()
    {
        var clock = new FakeSystemClock();
        var sut = new HealthQueryHandler(clock);
        var query = new HealthQuery(Locale: "ar");

        var result = await sut.Handle(query, CancellationToken.None);

        result.Status.Should().Be("ok");
        result.Locale.Should().Be("ar");
        result.UtcNow.Should().Be(FakeSystemClock.DefaultStart);
        result.Version.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Defaults_locale_to_ar_when_unspecified()
    {
        var sut = new HealthQueryHandler(new FakeSystemClock());

        var result = await sut.Handle(new HealthQuery(Locale: null), CancellationToken.None);

        result.Locale.Should().Be("ar");
    }

    [Fact]
    public async Task Echoes_explicit_en_locale()
    {
        var sut = new HealthQueryHandler(new FakeSystemClock());

        var result = await sut.Handle(new HealthQuery(Locale: "en"), CancellationToken.None);

        result.Locale.Should().Be("en");
    }
}
```

- [ ] **Step 2: Run — expect compile error**

```bash
dotnet test backend/tests/CCE.Application.Tests --nologo -c Debug 2>&1 | tail -6
```

- [ ] **Step 3: Write `backend/src/CCE.Application/Health/HealthQuery.cs`**

```csharp
using MediatR;

namespace CCE.Application.Health;

/// <summary>
/// Anonymous health query. Returns <see cref="HealthResult"/>.
/// Locale falls back to <c>"ar"</c> (Arabic, the default per spec) when null/empty.
/// </summary>
/// <param name="Locale">Requested locale (typically from <c>Accept-Language</c> header in the API).</param>
public sealed record HealthQuery(string? Locale) : IRequest<HealthResult>;
```

- [ ] **Step 4: Write `backend/src/CCE.Application/Health/HealthResult.cs`**

```csharp
namespace CCE.Application.Health;

public sealed record HealthResult(
    string Status,
    string Version,
    string Locale,
    DateTimeOffset UtcNow);
```

- [ ] **Step 5: Write `backend/src/CCE.Application/Health/HealthQueryHandler.cs`**

```csharp
using CCE.Domain.Common;
using MediatR;
using System.Reflection;

namespace CCE.Application.Health;

public sealed class HealthQueryHandler : IRequestHandler<HealthQuery, HealthResult>
{
    private readonly ISystemClock _clock;

    public HealthQueryHandler(ISystemClock clock) => _clock = clock;

    public Task<HealthResult> Handle(HealthQuery request, CancellationToken cancellationToken)
    {
        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
            ?? "0.0.0";

        var locale = string.IsNullOrWhiteSpace(request.Locale) ? "ar" : request.Locale;

        var result = new HealthResult(
            Status: "ok",
            Version: version,
            Locale: locale,
            UtcNow: _clock.UtcNow);

        return Task.FromResult(result);
    }
}
```

- [ ] **Step 6: Run — expect 3 new passes**

```bash
dotnet test backend/tests/CCE.Application.Tests --nologo -c Debug 2>&1 | tail -6
```
Expected: 8 total in Application.Tests (5 prior + 3 HealthQuery), all passed.

- [ ] **Step 7: Commit**

```bash
git add backend/src/CCE.Application/Health backend/tests/CCE.Application.Tests/Health
git -c commit.gpgsign=false commit -m "feat(phase-07): add HealthQuery + handler returning {status, version, locale, utcNow} with 3 TDD tests"
```

---

## Task 7.4: `AuthenticatedHealthQuery` + handler (claims echo)

**Files:**
- Create: `backend/src/CCE.Application/Health/AuthenticatedHealthQuery.cs`
- Create: `backend/src/CCE.Application/Health/AuthenticatedHealthQueryHandler.cs`
- Create: `backend/src/CCE.Application/Health/AuthenticatedHealthResult.cs`
- Create: `backend/tests/CCE.Application.Tests/Health/AuthenticatedHealthQueryHandlerTests.cs`

**Rationale:** For the authenticated `GET /health/authenticated` endpoint Phase 08 exposes on the Internal API. Takes a list of claims (extracted from JWT by the API layer) and echoes them in a structured response. Includes the seeded `SuperAdmin` group so the test can assert it's present after a real login.

- [ ] **Step 1: Write the failing test**

```csharp
using CCE.Application.Health;
using CCE.TestInfrastructure.Time;

namespace CCE.Application.Tests.Health;

public class AuthenticatedHealthQueryHandlerTests
{
    [Fact]
    public async Task Echoes_user_id_and_groups_in_result()
    {
        var clock = new FakeSystemClock();
        var sut = new AuthenticatedHealthQueryHandler(clock);
        var query = new AuthenticatedHealthQuery(
            UserId: "test-user-id",
            PreferredUsername: "admin@cce.local",
            Email: "admin@cce.local",
            Upn: "admin@cce.local",
            Groups: ["SuperAdmin", "default-roles-cce-internal"],
            Locale: "en");

        var result = await sut.Handle(query, CancellationToken.None);

        result.Status.Should().Be("ok");
        result.User.Id.Should().Be("test-user-id");
        result.User.PreferredUsername.Should().Be("admin@cce.local");
        result.User.Upn.Should().Be("admin@cce.local");
        result.User.Groups.Should().Contain("SuperAdmin");
        result.Locale.Should().Be("en");
        result.UtcNow.Should().Be(FakeSystemClock.DefaultStart);
    }

    [Fact]
    public async Task Defaults_locale_to_ar_when_unspecified()
    {
        var sut = new AuthenticatedHealthQueryHandler(new FakeSystemClock());
        var query = new AuthenticatedHealthQuery(
            UserId: "x", PreferredUsername: "x", Email: "x", Upn: "x",
            Groups: [], Locale: null);

        var result = await sut.Handle(query, CancellationToken.None);

        result.Locale.Should().Be("ar");
    }
}
```

- [ ] **Step 2: Run — expect compile error**

- [ ] **Step 3: Write `backend/src/CCE.Application/Health/AuthenticatedHealthQuery.cs`**

```csharp
using MediatR;

namespace CCE.Application.Health;

/// <summary>
/// Authenticated health query — exercised by the Internal API after JWT validation.
/// Caller (the API endpoint) extracts claims from the validated JWT and passes them in.
/// Handler echoes them with status + timestamp.
/// </summary>
public sealed record AuthenticatedHealthQuery(
    string UserId,
    string PreferredUsername,
    string Email,
    string Upn,
    IReadOnlyList<string> Groups,
    string? Locale) : IRequest<AuthenticatedHealthResult>;
```

- [ ] **Step 4: Write `backend/src/CCE.Application/Health/AuthenticatedHealthResult.cs`**

```csharp
namespace CCE.Application.Health;

public sealed record AuthenticatedHealthResult(
    string Status,
    AuthenticatedUserInfo User,
    string Locale,
    DateTimeOffset UtcNow);

public sealed record AuthenticatedUserInfo(
    string Id,
    string PreferredUsername,
    string Email,
    string Upn,
    IReadOnlyList<string> Groups);
```

- [ ] **Step 5: Write `backend/src/CCE.Application/Health/AuthenticatedHealthQueryHandler.cs`**

```csharp
using CCE.Domain.Common;
using MediatR;

namespace CCE.Application.Health;

public sealed class AuthenticatedHealthQueryHandler
    : IRequestHandler<AuthenticatedHealthQuery, AuthenticatedHealthResult>
{
    private readonly ISystemClock _clock;

    public AuthenticatedHealthQueryHandler(ISystemClock clock) => _clock = clock;

    public Task<AuthenticatedHealthResult> Handle(
        AuthenticatedHealthQuery request,
        CancellationToken cancellationToken)
    {
        var locale = string.IsNullOrWhiteSpace(request.Locale) ? "ar" : request.Locale;

        var result = new AuthenticatedHealthResult(
            Status: "ok",
            User: new AuthenticatedUserInfo(
                Id: request.UserId,
                PreferredUsername: request.PreferredUsername,
                Email: request.Email,
                Upn: request.Upn,
                Groups: request.Groups),
            Locale: locale,
            UtcNow: _clock.UtcNow);

        return Task.FromResult(result);
    }
}
```

- [ ] **Step 6: Run — expect 2 new passes**

```bash
dotnet test backend/tests/CCE.Application.Tests --nologo -c Debug 2>&1 | tail -6
```
Expected: 10 total in Application.Tests (5 behaviors + 3 HealthQuery + 2 AuthenticatedHealthQuery).

- [ ] **Step 7: Commit**

```bash
git add backend/src/CCE.Application/Health backend/tests/CCE.Application.Tests/Health
git -c commit.gpgsign=false commit -m "feat(phase-07): add AuthenticatedHealthQuery + handler (claims echo) with 2 TDD tests"
```

---

## Task 7.5: Wire pipeline behaviors into `AddApplication` + integration smoke test

**Files:**
- Modify: `backend/src/CCE.Application/DependencyInjection.cs`
- Create: `backend/tests/CCE.Application.Tests/DependencyInjectionTests.cs`

**Rationale:** Foundation's `AddApplication` already calls `cfg.RegisterServicesFromAssembly(...)` for handlers. We add explicit pipeline-behavior registrations and a verification test that resolves a handler through DI to confirm the pipeline is in place.

- [ ] **Step 1: Overwrite `backend/src/CCE.Application/DependencyInjection.cs`**

```csharp
using CCE.Application.Common.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CCE.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            // Pipeline behavior order matters — first registered runs outermost.
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
```

- [ ] **Step 2: Write `backend/tests/CCE.Application.Tests/DependencyInjectionTests.cs`**

```csharp
using CCE.Application.Health;
using CCE.Domain.Common;
using CCE.TestInfrastructure.Time;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CCE.Application.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public async Task Mediator_resolves_HealthQuery_handler_through_pipeline()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ISystemClock>(new FakeSystemClock());
        services.AddApplication();

        await using var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var result = await mediator.Send(new HealthQuery(Locale: "en"));

        result.Status.Should().Be("ok");
        result.Locale.Should().Be("en");
    }

    [Fact]
    public async Task Mediator_resolves_AuthenticatedHealthQuery_handler_through_pipeline()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ISystemClock>(new FakeSystemClock());
        services.AddApplication();

        await using var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var result = await mediator.Send(new AuthenticatedHealthQuery(
            UserId: "u",
            PreferredUsername: "u@local",
            Email: "u@local",
            Upn: "u@local",
            Groups: ["SuperAdmin"],
            Locale: "ar"));

        result.Status.Should().Be("ok");
        result.User.Groups.Should().Contain("SuperAdmin");
    }
}
```

- [ ] **Step 3: Run — expect 2 new passes**

```bash
dotnet test backend/tests/CCE.Application.Tests --nologo -c Debug 2>&1 | tail -6
```
Expected: 12 total in Application.Tests, all passed.

- [ ] **Step 4: Final solution-wide test run**

```bash
dotnet build backend/CCE.sln --nologo 2>&1 | tail -5
dotnet test backend/CCE.sln --nologo --no-build 2>&1 | tail -10
```
Expected: build 0 errors, total 34 passed (16 Domain + 12 Application + 6 Infrastructure).

- [ ] **Step 5: Commit**

```bash
git add backend/src/CCE.Application/DependencyInjection.cs backend/tests/CCE.Application.Tests/DependencyInjectionTests.cs
git -c commit.gpgsign=false commit -m "feat(phase-07): wire LoggingBehavior + ValidationBehavior into AddApplication with 2 DI integration tests"
```

---

## Phase 07 — completion checklist

- [ ] `LoggingBehavior<TRequest, TResponse>` logs entry + success + elapsed-ms.
- [ ] `ValidationBehavior<TRequest, TResponse>` runs validators and aggregates failures.
- [ ] `HealthQuery` + handler returns `{ status, version, locale, utcNow }`; locale defaults to `"ar"`.
- [ ] `AuthenticatedHealthQuery` + handler echoes claims in structured shape.
- [ ] `AddApplication` registers MediatR with both pipeline behaviors and FluentValidation scanning.
- [ ] `dotnet build backend/CCE.sln` succeeds with 0 errors.
- [ ] `dotnet test backend/CCE.sln` reports 34 passed (16 Domain + 12 Application + 6 Infrastructure).
- [ ] `git log --oneline | head -7` shows 5 new Phase-07 commits.
- [ ] `git status` clean.

**If all boxes ticked, phase 07 is complete. Proceed to phase 08 (API middleware + endpoints).**
