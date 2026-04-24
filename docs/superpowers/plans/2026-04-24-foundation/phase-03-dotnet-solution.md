# Phase 03 — .NET 8 Solution Skeleton

> Parent: [../2026-04-24-foundation.md](../2026-04-24-foundation.md) · Spec: [../../specs/2026-04-24-foundation-design.md](../../specs/2026-04-24-foundation-design.md)

**Phase goal:** Scaffold the Clean Architecture .NET solution per spec §4.2: six production projects (`Domain`, `Application`, `Infrastructure`, `Api.External`, `Api.Internal`, `Integration`), four test projects, Central Package Management (CPM) with pinned versions, shared build properties (nullable enabled, warnings-as-errors, LangVersion 12), correct inter-project references. Ends when `dotnet build` + `dotnet test` both pass with one green test proving xUnit wiring.

**Tasks in this phase:** 10
**Working directory:** `/Users/m/CCE/`
**Preconditions:** Phase 02 complete; Docker stack healthy; `.env` has required vars.

---

## Pre-execution sanity checks

1. **.NET SDK 8.0.x installed.** Run: `dotnet --list-sdks | grep '^8\.'` → must print at least one 8.0.x SDK. If missing, `brew install --cask dotnet-sdk@8` (macOS) or use the installer. Stop and report if not available.
2. **.NET 8.0 runtime for test host.** Run: `dotnet --list-runtimes | grep 'Microsoft.NETCore.App 8\.'` → must print at least one. Usually bundled with SDK.
3. **No existing `backend/` directory with conflicting content.** Run: `test ! -e backend/CCE.sln && echo OK` → prints `OK`. If `backend/CCE.sln` already exists, a prior partial run happened — stop and report.
4. **Git identity configured or auto-detected.** Any existing commits from Phase 00–02 will show the committer identity; no change needed. Just confirm `git config user.email` returns *something* non-empty.

If any check fails, stop and report.

---

## Task 3.1: Create the backend directory and empty solution

**Files:**
- Create: `backend/CCE.sln`
- Create: `backend/.gitkeep` (if dotnet doesn't create anything else)

- [ ] **Step 1: Create the backend directory and initialize the solution**

Run:
```bash
mkdir -p backend
cd backend
dotnet new sln --name CCE
cd ..
ls -la backend/
```
Expected: `backend/CCE.sln` exists. `ls` shows at least `CCE.sln`.

- [ ] **Step 2: Validate the solution file is parseable**

Run:
```bash
dotnet sln backend/CCE.sln list
```
Expected: prints header `Project(s)` followed by "No projects found in the solution." (or similar — an empty solution).

- [ ] **Step 3: Commit**

```bash
git add backend/CCE.sln
git -c commit.gpgsign=false commit -m "feat(phase-03): initialize .NET solution (backend/CCE.sln, empty)"
```

---

## Task 3.2: Add `Directory.Build.props` — shared build settings

**Files:**
- Create: `backend/Directory.Build.props`

**Rationale:** A single file at the backend root injects MSBuild properties into every `.csproj` under `backend/`. Eliminates boilerplate duplication across 10 projects and locks discipline (nullable, analyzers, warnings-as-errors) centrally.

- [ ] **Step 1: Write `backend/Directory.Build.props`**

```xml
<Project>

  <PropertyGroup>
    <!-- Language and target framework -->
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>12.0</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>

    <!-- Discipline: every warning is an error. Use [SuppressMessage] sparingly and with justification. -->
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <WarningsAsErrors />
    <WarningsNotAsErrors />

    <!-- Analyzers: full set enabled. EnforceCodeStyleInBuild runs .editorconfig C# rules at build time. -->
    <AnalysisLevel>latest-all</AnalysisLevel>
    <AnalysisMode>AllEnabledByDefault</AnalysisMode>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <CodeAnalysisTreatWarningsAsErrors>true</CodeAnalysisTreatWarningsAsErrors>

    <!-- Deterministic builds for reproducibility -->
    <Deterministic>true</Deterministic>
    <ContinuousIntegrationBuild Condition="'$(CI)' == 'true'">true</ContinuousIntegrationBuild>

    <!-- Project-level defaults — override per project if needed -->
    <GenerateDocumentationFile>true</GenerateDocumentationFile>

    <!-- 1591 = "Missing XML comment for publicly visible type/member" — too noisy pre-API-docs -->
    <NoWarn>$(NoWarn);1591;CS1591</NoWarn>

    <!-- Output organization -->
    <BaseOutputPath>$(MSBuildThisFileDirectory)artifacts/bin/$(MSBuildProjectName)/</BaseOutputPath>
    <BaseIntermediateOutputPath>$(MSBuildThisFileDirectory)artifacts/obj/$(MSBuildProjectName)/</BaseIntermediateOutputPath>

    <!-- Root namespace mirrors project name (e.g., CCE.Domain → namespace CCE.Domain) -->
    <RootNamespace>$(MSBuildProjectName)</RootNamespace>

    <!-- Product metadata — shows up in assembly manifests + NuGet packages -->
    <Authors>Ministry of Energy — CCE Team</Authors>
    <Company>Saudi Ministry of Energy</Company>
    <Product>CCE Knowledge Center</Product>
    <Copyright>© Saudi Ministry of Energy</Copyright>
  </PropertyGroup>

  <!-- Test projects opt into a different base namespace via per-project overrides if needed -->
  <PropertyGroup Condition="$(MSBuildProjectName.EndsWith('.Tests')) Or $(MSBuildProjectName.EndsWith('.IntegrationTests'))">
    <IsPackable>false</IsPackable>
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);CA1707</NoWarn>
    <!-- Test methods commonly use underscores: Should_do_X_when_Y -->
  </PropertyGroup>

</Project>
```

- [ ] **Step 2: Verify MSBuild parses it without error (no projects yet, so nothing to build)**

Run:
```bash
cd backend && dotnet msbuild -nologo -version >/dev/null && echo "MSBuild OK" && cd ..
```
Expected: `MSBuild OK`.

- [ ] **Step 3: Commit**

```bash
git add backend/Directory.Build.props
git -c commit.gpgsign=false commit -m "feat(phase-03): add backend/Directory.Build.props (nullable, warnings-as-errors, analyzers, CPM-friendly)"
```

---

## Task 3.3: Add `Directory.Packages.props` — Central Package Management

**Files:**
- Create: `backend/Directory.Packages.props`

**Rationale:** Central Package Management (MSBuild 17+ / .NET 8) pins every package version in one place. Individual `.csproj` files use `<PackageReference Include="X" />` without a version. Prevents the "wait, which project pulled in FluentValidation 11.10 vs 11.9?" drift that plagues multi-project .NET solutions.

The file pins every package we know we'll need across Foundation (Phases 05–08). Later phases reference packages without versions; only this file changes when a version bumps.

- [ ] **Step 1: Write `backend/Directory.Packages.props`**

```xml
<Project>

  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>

  <ItemGroup Label="Core framework & Testing">
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageVersion Include="xunit" Version="2.9.2" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageVersion Include="xunit.analyzers" Version="1.16.0" />
    <PackageVersion Include="FluentAssertions" Version="6.12.2" />
    <PackageVersion Include="NSubstitute" Version="5.3.0" />
    <PackageVersion Include="NSubstitute.Analyzers.CSharp" Version="1.0.17" />
    <PackageVersion Include="coverlet.collector" Version="6.0.2" />
    <PackageVersion Include="Bogus" Version="35.6.1" />
  </ItemGroup>

  <ItemGroup Label="Application layer">
    <PackageVersion Include="MediatR" Version="12.4.1" />
    <PackageVersion Include="FluentValidation" Version="11.10.0" />
    <PackageVersion Include="FluentValidation.DependencyInjectionExtensions" Version="11.10.0" />
    <PackageVersion Include="Mapster" Version="7.4.0" />
    <PackageVersion Include="Mapster.DependencyInjection" Version="1.0.1" />
  </ItemGroup>

  <ItemGroup Label="Persistence (EF Core + SQL Server)">
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="8.0.10" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.10" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.10" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.10" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Relational" Version="8.0.10" />
    <PackageVersion Include="EFCore.NamingConventions" Version="8.0.3" />
  </ItemGroup>

  <ItemGroup Label="Redis">
    <PackageVersion Include="StackExchange.Redis" Version="2.8.16" />
    <PackageVersion Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="8.0.10" />
  </ItemGroup>

  <ItemGroup Label="ASP.NET Core web API">
    <PackageVersion Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.10" />
    <PackageVersion Include="Microsoft.AspNetCore.Authentication.OpenIdConnect" Version="8.0.10" />
    <PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="8.0.10" />
    <PackageVersion Include="Swashbuckle.AspNetCore" Version="6.8.1" />
    <PackageVersion Include="Swashbuckle.AspNetCore.Annotations" Version="6.8.1" />
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Versioning" Version="5.1.0" />
  </ItemGroup>

  <ItemGroup Label="Identity (external users)">
    <PackageVersion Include="Microsoft.AspNetCore.Identity" Version="2.3.1" />
    <PackageVersion Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="8.0.10" />
    <PackageVersion Include="Microsoft.AspNetCore.Identity.UI" Version="8.0.10" />
  </ItemGroup>

  <ItemGroup Label="Observability (Serilog + Sentry + Prometheus)">
    <PackageVersion Include="Serilog" Version="4.1.0" />
    <PackageVersion Include="Serilog.AspNetCore" Version="8.0.3" />
    <PackageVersion Include="Serilog.Sinks.Console" Version="6.0.0" />
    <PackageVersion Include="Serilog.Sinks.File" Version="6.0.0" />
    <PackageVersion Include="Serilog.Formatting.Compact" Version="3.0.0" />
    <PackageVersion Include="Serilog.Enrichers.CorrelationId" Version="3.0.1" />
    <PackageVersion Include="Serilog.Enrichers.Environment" Version="3.0.1" />
    <PackageVersion Include="Serilog.Exceptions" Version="8.4.0" />
    <PackageVersion Include="Sentry.AspNetCore" Version="4.13.0" />
    <PackageVersion Include="Sentry.Serilog" Version="4.13.0" />
    <PackageVersion Include="prometheus-net" Version="8.2.1" />
    <PackageVersion Include="prometheus-net.AspNetCore" Version="8.2.1" />
  </ItemGroup>

  <ItemGroup Label="Integration tests (Testcontainers)">
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.10" />
    <PackageVersion Include="Testcontainers" Version="4.0.0" />
    <PackageVersion Include="Testcontainers.MsSql" Version="4.0.0" />
    <PackageVersion Include="Testcontainers.Redis" Version="4.0.0" />
    <PackageVersion Include="Testcontainers.Keycloak" Version="4.0.0" />
  </ItemGroup>

  <ItemGroup Label="Rate limiting (built-in 8+, no package) and problem-details helpers">
    <PackageVersion Include="Hellang.Middleware.ProblemDetails" Version="6.5.1" />
  </ItemGroup>

  <ItemGroup Label="MediatR pipeline behaviors">
    <PackageVersion Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.2" />
  </ItemGroup>

  <ItemGroup Label="Roslyn source generators (used by Domain.SourceGenerators)">
    <PackageVersion Include="Microsoft.CodeAnalysis.CSharp" Version="4.11.0" />
    <PackageVersion Include="Microsoft.CodeAnalysis.Analyzers" Version="3.11.0" />
    <PackageVersion Include="YamlDotNet" Version="16.2.0" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Verify NuGet accepts the CPM file (will re-verify after first project is added)**

Run:
```bash
cd backend && dotnet msbuild -nologo /t:_CheckForInvalidConfigurationAndPlatform 2>&1 | head -20 && cd ..
```
Expected: either no output or a clean build-check output. Any version parse errors would surface as MSBuild warnings — none should appear since no `.csproj` yet references these packages.

- [ ] **Step 3: Commit**

```bash
git add backend/Directory.Packages.props
git -c commit.gpgsign=false commit -m "feat(phase-03): add backend/Directory.Packages.props (CPM with all Foundation-phase package versions pinned)"
```

---

## Task 3.4: Create `CCE.Domain` — base classes only

**Files:**
- Create: `backend/src/CCE.Domain/CCE.Domain.csproj`
- Create: `backend/src/CCE.Domain/Common/Entity.cs`
- Create: `backend/src/CCE.Domain/Common/AggregateRoot.cs`
- Create: `backend/src/CCE.Domain/Common/ValueObject.cs`
- Create: `backend/src/CCE.Domain/Common/IDomainEvent.cs`
- Create: `backend/src/CCE.Domain/Common/ISystemClock.cs`

**Rationale:** Domain is the innermost layer with zero dependencies. Foundation seeds the abstract DDD building blocks that all future entities inherit. No business entities land here until sub-project 2 (Data & Domain).

- [ ] **Step 1: Create the project**

Run:
```bash
dotnet new classlib -n CCE.Domain -o backend/src/CCE.Domain --framework net8.0 --force
rm -f backend/src/CCE.Domain/Class1.cs
dotnet sln backend/CCE.sln add backend/src/CCE.Domain/CCE.Domain.csproj
```
Expected: `Project ... added to the solution.`

- [ ] **Step 2: Overwrite `backend/src/CCE.Domain/CCE.Domain.csproj` with minimal content**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

</Project>
```

No `TargetFramework` here — it's inherited from `Directory.Build.props`. No `<PackageReference>` — Domain depends on nothing. This is deliberate and enforced by review.

- [ ] **Step 3: Write `backend/src/CCE.Domain/Common/Entity.cs`**

```csharp
namespace CCE.Domain.Common;

/// <summary>
/// Base class for entities identified by a strongly-typed ID.
/// Entities have identity that persists across state changes.
/// </summary>
/// <typeparam name="TId">The ID type (e.g., Guid, int, or a strongly-typed wrapper).</typeparam>
public abstract class Entity<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected Entity(TId id) => Id = id;

    public TId Id { get; protected set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode() => EqualityComparer<TId>.Default.GetHashCode(Id);

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) =>
        ReferenceEquals(left, right) || (left is not null && left.Equals(right));

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !(left == right);
}
```

- [ ] **Step 4: Write `backend/src/CCE.Domain/Common/AggregateRoot.cs`**

```csharp
namespace CCE.Domain.Common;

/// <summary>
/// Base class for DDD aggregate roots — entities that serve as the consistency boundary
/// for a cluster of related entities and value objects. Repositories are per-aggregate.
/// </summary>
/// <typeparam name="TId">The aggregate root's ID type.</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    protected AggregateRoot(TId id) : base(id) { }
}
```

- [ ] **Step 5: Write `backend/src/CCE.Domain/Common/ValueObject.cs`**

```csharp
namespace CCE.Domain.Common;

/// <summary>
/// Base class for DDD value objects — immutable, identityless, compared by structural equality
/// over their atomic components.
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>
    /// Return the atomic components that define equality. Include every field that distinguishes
    /// one value from another; exclude cached/derived fields.
    /// </summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public bool Equals(ValueObject? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override bool Equals(object? obj) => obj is ValueObject other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var component in GetEqualityComponents())
        {
            hash.Add(component);
        }
        return hash.ToHashCode();
    }

    public static bool operator ==(ValueObject? left, ValueObject? right) =>
        ReferenceEquals(left, right) || (left is not null && left.Equals(right));

    public static bool operator !=(ValueObject? left, ValueObject? right) => !(left == right);
}
```

- [ ] **Step 6: Write `backend/src/CCE.Domain/Common/IDomainEvent.cs`**

```csharp
namespace CCE.Domain.Common;

/// <summary>
/// Marker interface for domain events raised by aggregate roots.
/// Dispatched by the infrastructure layer post-persistence (see Phase 06).
/// </summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredOn { get; }
}
```

- [ ] **Step 7: Write `backend/src/CCE.Domain/Common/ISystemClock.cs`**

```csharp
namespace CCE.Domain.Common;

/// <summary>
/// Abstraction over wall-clock time. Implementations in <c>CCE.Infrastructure</c> use
/// <see cref="DateTimeOffset.UtcNow"/>; tests supply a fake that advances time explicitly.
/// Every domain/application operation that needs 'now' takes <see cref="ISystemClock"/>.
/// Never call <c>DateTimeOffset.UtcNow</c> directly from domain or application layers.
/// </summary>
public interface ISystemClock
{
    DateTimeOffset UtcNow { get; }
}
```

- [ ] **Step 8: Build to verify no errors**

Run:
```bash
dotnet build backend/src/CCE.Domain/CCE.Domain.csproj --nologo -c Debug
```
Expected: `Build succeeded.` with 0 errors. Analyzer warnings-as-errors are enabled — any analyzer violation will fail the build. If the build fails, read the error carefully: it's usually a missing override equals/gethashcode (CA1067/CA1815) or similar. Fix per the analyzer hint before proceeding.

- [ ] **Step 9: Commit**

```bash
git add backend/src/CCE.Domain backend/CCE.sln
git -c commit.gpgsign=false commit -m "feat(phase-03): add CCE.Domain project with Entity/AggregateRoot/ValueObject/IDomainEvent/ISystemClock base types"
```

---

## Task 3.5: Create `CCE.Application` — MediatR host + abstractions

**Files:**
- Create: `backend/src/CCE.Application/CCE.Application.csproj`
- Create: `backend/src/CCE.Application/DependencyInjection.cs`
- Create: `backend/src/CCE.Application/Common/Interfaces/ICceDbContext.cs`

**Rationale:** Application layer hosts MediatR handlers, FluentValidation validators, DTOs, and infrastructure interfaces. Depends on Domain only. No references to ASP.NET Core, EF Core, or Keycloak SDKs.

- [ ] **Step 1: Create project and add solution reference + project reference to Domain**

```bash
dotnet new classlib -n CCE.Application -o backend/src/CCE.Application --framework net8.0 --force
rm -f backend/src/CCE.Application/Class1.cs
dotnet sln backend/CCE.sln add backend/src/CCE.Application/CCE.Application.csproj
dotnet add backend/src/CCE.Application/CCE.Application.csproj reference backend/src/CCE.Domain/CCE.Domain.csproj
```

- [ ] **Step 2: Overwrite `backend/src/CCE.Application/CCE.Application.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="MediatR" />
    <PackageReference Include="FluentValidation" />
    <PackageReference Include="FluentValidation.DependencyInjectionExtensions" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />
    <PackageReference Include="Mapster" />
    <PackageReference Include="Mapster.DependencyInjection" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\CCE.Domain\CCE.Domain.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Write `backend/src/CCE.Application/Common/Interfaces/ICceDbContext.cs`**

```csharp
namespace CCE.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the EF Core <c>DbContext</c> for application-layer use.
/// Phase 06 defines a concrete <c>CceDbContext</c> in <c>CCE.Infrastructure</c> that implements
/// this interface and adds the real <c>DbSet&lt;T&gt;</c> properties. Foundation ships only
/// the interface contract — so far just <see cref="SaveChangesAsync"/>.
/// </summary>
public interface ICceDbContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

- [ ] **Step 4: Write `backend/src/CCE.Application/DependencyInjection.cs`**

```csharp
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CCE.Application;

/// <summary>
/// Extension methods to register the Application layer's services on the DI container.
/// Web API composition roots (External/Internal APIs) call <see cref="AddApplication"/> from their
/// <c>Program.cs</c>. Phase 07 adds real handlers (HealthQuery etc.); Foundation wires the infrastructure.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // MediatR — scans this assembly for IRequestHandler<,> implementations
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
        });

        // FluentValidation — scans this assembly for AbstractValidator<T> implementations
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
```

- [ ] **Step 5: Build**

```bash
dotnet build backend/src/CCE.Application/CCE.Application.csproj --nologo -c Debug
```
Expected: `Build succeeded.` with 0 errors.

- [ ] **Step 6: Commit**

```bash
git add backend/src/CCE.Application backend/CCE.sln
git -c commit.gpgsign=false commit -m "feat(phase-03): add CCE.Application project with MediatR + FluentValidation wired, ICceDbContext contract"
```

---

## Task 3.6: Create `CCE.Infrastructure` — stub only

**Files:**
- Create: `backend/src/CCE.Infrastructure/CCE.Infrastructure.csproj`
- Create: `backend/src/CCE.Infrastructure/SystemClock.cs`
- Create: `backend/src/CCE.Infrastructure/DependencyInjection.cs`

**Rationale:** Infrastructure holds EF Core, Redis, OIDC/JWT config, Serilog, etc. — all the "plumbing" concrete implementations. In Phase 03 we ship only an `ISystemClock` implementation because it's the canonical tiny non-trivial infrastructure service; everything else (DbContext, Redis factory, OIDC JWT, Serilog sinks) lands in Phase 06. No EF Core migrations in this phase.

- [ ] **Step 1: Create project + references**

```bash
dotnet new classlib -n CCE.Infrastructure -o backend/src/CCE.Infrastructure --framework net8.0 --force
rm -f backend/src/CCE.Infrastructure/Class1.cs
dotnet sln backend/CCE.sln add backend/src/CCE.Infrastructure/CCE.Infrastructure.csproj
dotnet add backend/src/CCE.Infrastructure/CCE.Infrastructure.csproj reference backend/src/CCE.Application/CCE.Application.csproj
dotnet add backend/src/CCE.Infrastructure/CCE.Infrastructure.csproj reference backend/src/CCE.Domain/CCE.Domain.csproj
```

- [ ] **Step 2: Overwrite `backend/src/CCE.Infrastructure/CCE.Infrastructure.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\CCE.Application\CCE.Application.csproj" />
    <ProjectReference Include="..\CCE.Domain\CCE.Domain.csproj" />
  </ItemGroup>

</Project>
```

Note: `Microsoft.Extensions.DependencyInjection.Abstractions` isn't yet in `Directory.Packages.props` — add it now.

- [ ] **Step 3: Add missing package to CPM**

Append to `backend/Directory.Packages.props` inside the `<ItemGroup Label="Core framework & Testing">` block (after `Microsoft.NET.Test.Sdk`):

```xml
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="8.0.2" />
```

- [ ] **Step 4: Write `backend/src/CCE.Infrastructure/SystemClock.cs`**

```csharp
using CCE.Domain.Common;

namespace CCE.Infrastructure;

/// <summary>
/// Production <see cref="ISystemClock"/> implementation returning real UTC time.
/// Tests supply a fake that can be advanced explicitly.
/// </summary>
public sealed class SystemClock : ISystemClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
```

- [ ] **Step 5: Write `backend/src/CCE.Infrastructure/DependencyInjection.cs`**

```csharp
using CCE.Domain.Common;
using Microsoft.Extensions.DependencyInjection;

namespace CCE.Infrastructure;

/// <summary>
/// Extension methods to register the Infrastructure layer's services on the DI container.
/// Phase 06 expands this to wire EF Core DbContext + Redis + OIDC/JWT + Serilog. Foundation
/// wires only the clock abstraction so Application layer handlers written in Phase 07 have
/// everything they need to resolve via DI.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ISystemClock, SystemClock>();
        return services;
    }
}
```

- [ ] **Step 6: Build**

```bash
dotnet build backend/src/CCE.Infrastructure/CCE.Infrastructure.csproj --nologo -c Debug
```
Expected: `Build succeeded.`.

- [ ] **Step 7: Commit**

```bash
git add backend/src/CCE.Infrastructure backend/Directory.Packages.props backend/CCE.sln
git -c commit.gpgsign=false commit -m "feat(phase-03): add CCE.Infrastructure project with SystemClock (EF/Redis/OIDC land in phase 06)"
```

---

## Task 3.7: Create `CCE.Api.External` — minimal web API scaffold

**Files:**
- Create: `backend/src/CCE.Api.External/CCE.Api.External.csproj`
- Create: `backend/src/CCE.Api.External/Program.cs`
- Create: `backend/src/CCE.Api.External/appsettings.json`
- Create: `backend/src/CCE.Api.External/appsettings.Development.json`
- Create: `backend/src/CCE.Api.External/Properties/launchSettings.json`

**Rationale:** External API serves public visitors and registered users. Foundation's version is a minimal ASP.NET Core host that boots, responds to `/` with `"CCE.Api.External — Foundation"`, and exits cleanly. Real endpoints land in Phase 07–08. Port 5001 per plan conventions.

- [ ] **Step 1: Create project + references**

```bash
dotnet new web -n CCE.Api.External -o backend/src/CCE.Api.External --framework net8.0 --force
dotnet sln backend/CCE.sln add backend/src/CCE.Api.External/CCE.Api.External.csproj
dotnet add backend/src/CCE.Api.External/CCE.Api.External.csproj reference backend/src/CCE.Application/CCE.Application.csproj
dotnet add backend/src/CCE.Api.External/CCE.Api.External.csproj reference backend/src/CCE.Infrastructure/CCE.Infrastructure.csproj
```

- [ ] **Step 2: Overwrite `backend/src/CCE.Api.External/CCE.Api.External.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <UserSecretsId>cce-api-external-dev</UserSecretsId>
  </PropertyGroup>

  <ItemGroup>
    <!-- No package references in Foundation — Phase 08 adds Swashbuckle, Serilog, JwtBearer, etc. -->
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\CCE.Application\CCE.Application.csproj" />
    <ProjectReference Include="..\CCE.Infrastructure\CCE.Infrastructure.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Overwrite `backend/src/CCE.Api.External/Program.cs`**

```csharp
using CCE.Application;
using CCE.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure();

var app = builder.Build();

app.MapGet("/", () => "CCE.Api.External — Foundation");

app.Run();

// Expose Program for WebApplicationFactory in integration tests
public partial class Program;
```

- [ ] **Step 4: Overwrite `backend/src/CCE.Api.External/appsettings.json`**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

- [ ] **Step 5: Overwrite `backend/src/CCE.Api.External/appsettings.Development.json`**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  }
}
```

- [ ] **Step 6: Overwrite `backend/src/CCE.Api.External/Properties/launchSettings.json`**

```json
{
  "profiles": {
    "CCE.Api.External": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://localhost:5001",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

- [ ] **Step 7: Build**

```bash
dotnet build backend/src/CCE.Api.External/CCE.Api.External.csproj --nologo -c Debug
```
Expected: `Build succeeded.`.

- [ ] **Step 8: Smoke-test the API boots + responds**

Run:
```bash
# Run in background, wait for it to bind, curl, kill
dotnet run --project backend/src/CCE.Api.External --no-build --urls http://localhost:5001 > /tmp/api-external.log 2>&1 &
API_PID=$!
for i in $(seq 1 15); do
  if curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5001/ 2>/dev/null | grep -q 200; then
    break
  fi
  sleep 1
done
RESPONSE=$(curl -s http://localhost:5001/)
echo "Response: $RESPONSE"
kill $API_PID 2>/dev/null; wait $API_PID 2>/dev/null
[ "$RESPONSE" = "CCE.Api.External — Foundation" ] && echo "SMOKE OK" || { echo "SMOKE FAILED"; cat /tmp/api-external.log; exit 1; }
```
Expected: prints `Response: CCE.Api.External — Foundation` and `SMOKE OK`.

- [ ] **Step 9: Commit**

```bash
git add backend/src/CCE.Api.External backend/CCE.sln
git -c commit.gpgsign=false commit -m "feat(phase-03): add CCE.Api.External project (port 5001, root endpoint returns foundation banner)"
```

---

## Task 3.8: Create `CCE.Api.Internal` — mirror of External on port 5002

**Files:**
- Create: `backend/src/CCE.Api.Internal/CCE.Api.Internal.csproj`
- Create: `backend/src/CCE.Api.Internal/Program.cs`
- Create: `backend/src/CCE.Api.Internal/appsettings.json`
- Create: `backend/src/CCE.Api.Internal/appsettings.Development.json`
- Create: `backend/src/CCE.Api.Internal/Properties/launchSettings.json`

**Rationale:** Internal API mirrors External but binds 5002 and will (Phase 08) require OIDC auth via the Keycloak `cce-internal` realm. Foundation's Internal is structurally identical to External.

- [ ] **Step 1: Create project + references**

```bash
dotnet new web -n CCE.Api.Internal -o backend/src/CCE.Api.Internal --framework net8.0 --force
dotnet sln backend/CCE.sln add backend/src/CCE.Api.Internal/CCE.Api.Internal.csproj
dotnet add backend/src/CCE.Api.Internal/CCE.Api.Internal.csproj reference backend/src/CCE.Application/CCE.Application.csproj
dotnet add backend/src/CCE.Api.Internal/CCE.Api.Internal.csproj reference backend/src/CCE.Infrastructure/CCE.Infrastructure.csproj
```

- [ ] **Step 2: Overwrite `backend/src/CCE.Api.Internal/CCE.Api.Internal.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <UserSecretsId>cce-api-internal-dev</UserSecretsId>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\CCE.Application\CCE.Application.csproj" />
    <ProjectReference Include="..\CCE.Infrastructure\CCE.Infrastructure.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Overwrite `backend/src/CCE.Api.Internal/Program.cs`**

```csharp
using CCE.Application;
using CCE.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure();

var app = builder.Build();

app.MapGet("/", () => "CCE.Api.Internal — Foundation");

app.Run();

public partial class Program;
```

- [ ] **Step 4: Overwrite `backend/src/CCE.Api.Internal/appsettings.json`**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

- [ ] **Step 5: Overwrite `backend/src/CCE.Api.Internal/appsettings.Development.json`**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  }
}
```

- [ ] **Step 6: Overwrite `backend/src/CCE.Api.Internal/Properties/launchSettings.json`**

```json
{
  "profiles": {
    "CCE.Api.Internal": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://localhost:5002",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

- [ ] **Step 7: Build + smoke-test on port 5002**

Run:
```bash
dotnet build backend/src/CCE.Api.Internal/CCE.Api.Internal.csproj --nologo -c Debug
dotnet run --project backend/src/CCE.Api.Internal --no-build --urls http://localhost:5002 > /tmp/api-internal.log 2>&1 &
API_PID=$!
for i in $(seq 1 15); do
  if curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5002/ 2>/dev/null | grep -q 200; then
    break
  fi
  sleep 1
done
RESPONSE=$(curl -s http://localhost:5002/)
echo "Response: $RESPONSE"
kill $API_PID 2>/dev/null; wait $API_PID 2>/dev/null
[ "$RESPONSE" = "CCE.Api.Internal — Foundation" ] && echo "SMOKE OK" || { echo "SMOKE FAILED"; cat /tmp/api-internal.log; exit 1; }
```
Expected: `Response: CCE.Api.Internal — Foundation` + `SMOKE OK`.

- [ ] **Step 8: Commit**

```bash
git add backend/src/CCE.Api.Internal backend/CCE.sln
git -c commit.gpgsign=false commit -m "feat(phase-03): add CCE.Api.Internal project (port 5002, mirrors External structure)"
```

---

## Task 3.9: Create `CCE.Integration` — empty placeholder

**Files:**
- Create: `backend/src/CCE.Integration/CCE.Integration.csproj`
- Create: `backend/src/CCE.Integration/.gitkeep`

**Rationale:** Integration gateway for KAPSARC, Email, SMS, SIEM, iCal lands in sub-project 8. Foundation ships a compilable empty project so the solution has the container ready and the CI `dotnet build` graph is stable.

- [ ] **Step 1: Create project**

```bash
dotnet new classlib -n CCE.Integration -o backend/src/CCE.Integration --framework net8.0 --force
rm -f backend/src/CCE.Integration/Class1.cs
touch backend/src/CCE.Integration/.gitkeep
dotnet sln backend/CCE.sln add backend/src/CCE.Integration/CCE.Integration.csproj
```

- [ ] **Step 2: Overwrite `backend/src/CCE.Integration/CCE.Integration.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <!-- Empty placeholder — real integrations land in sub-project 8 -->
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Build**

```bash
dotnet build backend/src/CCE.Integration/CCE.Integration.csproj --nologo -c Debug
```
Expected: `Build succeeded.` (empty projects build cleanly).

- [ ] **Step 4: Commit**

```bash
git add backend/src/CCE.Integration backend/CCE.sln
git -c commit.gpgsign=false commit -m "feat(phase-03): add CCE.Integration empty placeholder project (KAPSARC/Email/SMS/SIEM land in sub-project 8)"
```

---

## Task 3.10: Create 4 test projects + one green xUnit test

**Files:**
- Create: `backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj`
- Create: `backend/tests/CCE.Domain.Tests/Common/EntityTests.cs`
- Create: `backend/tests/CCE.Domain.Tests/GlobalUsings.cs`
- Create: `backend/tests/CCE.Application.Tests/CCE.Application.Tests.csproj`
- Create: `backend/tests/CCE.Application.Tests/GlobalUsings.cs`
- Create: `backend/tests/CCE.Application.Tests/.gitkeep`
- Create: `backend/tests/CCE.Infrastructure.Tests/CCE.Infrastructure.Tests.csproj`
- Create: `backend/tests/CCE.Infrastructure.Tests/GlobalUsings.cs`
- Create: `backend/tests/CCE.Infrastructure.Tests/.gitkeep`
- Create: `backend/tests/CCE.Api.IntegrationTests/CCE.Api.IntegrationTests.csproj`
- Create: `backend/tests/CCE.Api.IntegrationTests/GlobalUsings.cs`
- Create: `backend/tests/CCE.Api.IntegrationTests/.gitkeep`

**Rationale:** Four test projects covering the four production layers. Foundation seeds xUnit + FluentAssertions + NSubstitute + coverlet in all four, but only Domain.Tests has a green test (proving base class equality semantics). Other three are stubs — real tests land in Phases 05–08 under strict TDD.

This task is the most file-heavy in the phase. Grouped as one task because the project structure is parallel — splitting into 4 tasks would produce 4 nearly-identical sets of steps.

- [ ] **Step 1: Create Domain.Tests project**

```bash
dotnet new xunit -n CCE.Domain.Tests -o backend/tests/CCE.Domain.Tests --framework net8.0 --force
rm -f backend/tests/CCE.Domain.Tests/UnitTest1.cs
dotnet sln backend/CCE.sln add backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj
dotnet add backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj reference backend/src/CCE.Domain/CCE.Domain.csproj
```

- [ ] **Step 2: Overwrite `backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="xunit.analyzers" />
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="NSubstitute" />
    <PackageReference Include="NSubstitute.Analyzers.CSharp" />
    <PackageReference Include="coverlet.collector" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\CCE.Domain\CCE.Domain.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Write `backend/tests/CCE.Domain.Tests/GlobalUsings.cs`**

```csharp
global using FluentAssertions;
global using NSubstitute;
global using Xunit;
```

- [ ] **Step 4: Write `backend/tests/CCE.Domain.Tests/Common/EntityTests.cs`**

This is Foundation's *single green test*. It exercises `Entity<TId>` equality to prove the xUnit pipeline runs end-to-end.

```csharp
using CCE.Domain.Common;

namespace CCE.Domain.Tests.Common;

public class EntityTests
{
    [Fact]
    public void Two_entities_with_same_id_are_equal()
    {
        var a = new TestEntity(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var b = new TestEntity(Guid.Parse("11111111-1111-1111-1111-111111111111"));

        a.Should().Be(b);
        (a == b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Two_entities_with_different_ids_are_not_equal()
    {
        var a = new TestEntity(Guid.NewGuid());
        var b = new TestEntity(Guid.NewGuid());

        a.Should().NotBe(b);
        (a != b).Should().BeTrue();
    }

    private sealed class TestEntity : Entity<Guid>
    {
        public TestEntity(Guid id) : base(id) { }
    }
}
```

- [ ] **Step 5: Build + run Domain.Tests**

```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo -c Debug
```
Expected: `Passed!  - Failed: 0, Passed: 2, Skipped: 0` (the two Fact methods).

- [ ] **Step 6: Create Application.Tests (stub)**

```bash
dotnet new xunit -n CCE.Application.Tests -o backend/tests/CCE.Application.Tests --framework net8.0 --force
rm -f backend/tests/CCE.Application.Tests/UnitTest1.cs
touch backend/tests/CCE.Application.Tests/.gitkeep
dotnet sln backend/CCE.sln add backend/tests/CCE.Application.Tests/CCE.Application.Tests.csproj
dotnet add backend/tests/CCE.Application.Tests/CCE.Application.Tests.csproj reference backend/src/CCE.Application/CCE.Application.csproj
```

Overwrite `backend/tests/CCE.Application.Tests/CCE.Application.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="xunit.analyzers" />
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="NSubstitute" />
    <PackageReference Include="NSubstitute.Analyzers.CSharp" />
    <PackageReference Include="coverlet.collector" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\CCE.Application\CCE.Application.csproj" />
  </ItemGroup>

</Project>
```

Write `backend/tests/CCE.Application.Tests/GlobalUsings.cs`:

```csharp
global using FluentAssertions;
global using NSubstitute;
global using Xunit;
```

- [ ] **Step 7: Create Infrastructure.Tests (stub)**

```bash
dotnet new xunit -n CCE.Infrastructure.Tests -o backend/tests/CCE.Infrastructure.Tests --framework net8.0 --force
rm -f backend/tests/CCE.Infrastructure.Tests/UnitTest1.cs
touch backend/tests/CCE.Infrastructure.Tests/.gitkeep
dotnet sln backend/CCE.sln add backend/tests/CCE.Infrastructure.Tests/CCE.Infrastructure.Tests.csproj
dotnet add backend/tests/CCE.Infrastructure.Tests/CCE.Infrastructure.Tests.csproj reference backend/src/CCE.Infrastructure/CCE.Infrastructure.csproj
```

Overwrite `backend/tests/CCE.Infrastructure.Tests/CCE.Infrastructure.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="xunit.analyzers" />
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="NSubstitute" />
    <PackageReference Include="NSubstitute.Analyzers.CSharp" />
    <PackageReference Include="coverlet.collector" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\CCE.Infrastructure\CCE.Infrastructure.csproj" />
  </ItemGroup>

</Project>
```

Write `backend/tests/CCE.Infrastructure.Tests/GlobalUsings.cs`:

```csharp
global using FluentAssertions;
global using NSubstitute;
global using Xunit;
```

- [ ] **Step 8: Create Api.IntegrationTests (stub)**

```bash
dotnet new xunit -n CCE.Api.IntegrationTests -o backend/tests/CCE.Api.IntegrationTests --framework net8.0 --force
rm -f backend/tests/CCE.Api.IntegrationTests/UnitTest1.cs
touch backend/tests/CCE.Api.IntegrationTests/.gitkeep
dotnet sln backend/CCE.sln add backend/tests/CCE.Api.IntegrationTests/CCE.Api.IntegrationTests.csproj
dotnet add backend/tests/CCE.Api.IntegrationTests/CCE.Api.IntegrationTests.csproj reference backend/src/CCE.Api.External/CCE.Api.External.csproj
dotnet add backend/tests/CCE.Api.IntegrationTests/CCE.Api.IntegrationTests.csproj reference backend/src/CCE.Api.Internal/CCE.Api.Internal.csproj
```

Overwrite `backend/tests/CCE.Api.IntegrationTests/CCE.Api.IntegrationTests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="xunit.analyzers" />
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="NSubstitute" />
    <PackageReference Include="NSubstitute.Analyzers.CSharp" />
    <PackageReference Include="coverlet.collector" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
    <PackageReference Include="Testcontainers" />
    <PackageReference Include="Testcontainers.MsSql" />
    <PackageReference Include="Testcontainers.Redis" />
    <PackageReference Include="Testcontainers.Keycloak" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\CCE.Api.External\CCE.Api.External.csproj" />
    <ProjectReference Include="..\..\src\CCE.Api.Internal\CCE.Api.Internal.csproj" />
  </ItemGroup>

</Project>
```

Write `backend/tests/CCE.Api.IntegrationTests/GlobalUsings.cs`:

```csharp
global using FluentAssertions;
global using Microsoft.AspNetCore.Mvc.Testing;
global using NSubstitute;
global using Xunit;
```

- [ ] **Step 9: Build the full solution and run all tests**

```bash
dotnet build backend/CCE.sln --nologo -c Debug
dotnet test backend/CCE.sln --nologo -c Debug --no-build
```
Expected:
- Build: `Build succeeded.` with 0 errors across all 10 projects.
- Test: two tests pass (from `EntityTests`); other three test projects have zero tests and report `No test matches the given testcase filter` or `Passed! - Failed: 0, Passed: 0` — that's expected for now.

- [ ] **Step 10: Verify `dotnet sln list` shows all 10 projects**

```bash
dotnet sln backend/CCE.sln list
```
Expected: lists 10 projects — 6 `src/` + 4 `tests/`.

- [ ] **Step 11: Add `backend/.gitignore` for dotnet artifacts inside backend/**

The root `.gitignore` from Phase 00 Task 0.3 already excludes `bin/`, `obj/`, and `artifacts/` so this is redundant — verify:

```bash
git check-ignore -v backend/src/CCE.Domain/bin/Debug/net8.0/CCE.Domain.dll || echo "not ignored"
```
Expected: prints a line like `.gitignore:<line>:bin/ backend/src/CCE.Domain/bin/Debug/net8.0/CCE.Domain.dll` — the match comes from the root `.gitignore`.

If the file prints `not ignored`, add `bin/` and `obj/` patterns to the root `.gitignore` before committing (they should already be there from Phase 00).

- [ ] **Step 12: Commit**

```bash
git add backend/tests backend/CCE.sln
git -c commit.gpgsign=false commit -m "feat(phase-03): add 4 test projects (Domain.Tests green with 2 Entity equality tests; Application/Infrastructure/Api.Integration as TDD-ready stubs)"
```

---

## Phase 03 — completion checklist

- [ ] `backend/CCE.sln` contains 10 projects (6 `src/` + 4 `tests/`).
- [ ] `backend/Directory.Build.props` enforces nullable, warnings-as-errors, analyzers, LangVersion 12.
- [ ] `backend/Directory.Packages.props` pins every Foundation-phase package version (CPM enabled).
- [ ] `CCE.Domain` depends on nothing and exposes `Entity<TId>`, `AggregateRoot<TId>`, `ValueObject`, `IDomainEvent`, `ISystemClock`.
- [ ] `CCE.Application` depends on Domain, registers MediatR + FluentValidation assembly scanning, exposes `ICceDbContext` contract.
- [ ] `CCE.Infrastructure` depends on Application + Domain, registers `ISystemClock → SystemClock` singleton.
- [ ] `CCE.Api.External` binds `http://localhost:5001/` and returns `"CCE.Api.External — Foundation"`.
- [ ] `CCE.Api.Internal` binds `http://localhost:5002/` and returns `"CCE.Api.Internal — Foundation"`.
- [ ] `CCE.Integration` is an empty placeholder project that compiles.
- [ ] `CCE.Domain.Tests` has 2 green tests; other three test projects build cleanly with no tests yet.
- [ ] `dotnet build backend/CCE.sln` succeeds with 0 errors / 0 warnings (warnings-as-errors enforced).
- [ ] `dotnet test backend/CCE.sln` reports 2 passed, 0 failed across the solution.
- [ ] `git log --oneline | head -12` shows ~10 new Phase-03 commits (one per task).
- [ ] `git status` shows clean tree.

**If all boxes ticked, phase 03 is complete. Proceed to phase 04 (permissions source generator).**
