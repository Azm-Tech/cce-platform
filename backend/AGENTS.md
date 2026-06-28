# AGENTS.md — CCE Backend

## Build Discipline

- **Every warning is an error.** `Directory.Build.props` sets `TreatWarningsAsErrors=true` + `AnalysisMode=AllEnabledByDefault`.
- A curated `NoWarn` list exists for false positives (CS1591, CA2007, CA1724, CA1873, etc.). Do not add to it without a short comment.
- Build artifacts go to `artifacts/bin/<ProjectName>/` and `artifacts/obj/<ProjectName>/`.

## Essential Commands

```bash
# Full build (must pass before any commit)
dotnet build CCE.sln

# Run a single test project
dotnet test tests/CCE.Domain.Tests

# Run a single test method
dotnet test tests/CCE.Domain.Tests --filter "FullyQualifiedName~FakeSystemClockTests"

# Run all tests
dotnet test CCE.sln

# EF migrations (use Infrastructure as startup — it has Design package)
$env:CCE_DESIGN_SQL_CONN = "Server=db52197.public.databaseasp.net;Database=db52197;User Id=db52197;Password=3Mm!x5#Y?rR9;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"
dotnet ef database update --project src/CCE.Infrastructure --startup-project src/CCE.Infrastructure

# Seed demo data
dotnet run --project src/CCE.Seeder -- --demo
```

## Architecture

Two ASP.NET Core APIs sharing the same solution:

| API | Port | Swagger | Auth |
|---|---|---|---|
| **CCE.Api.External** | 5001 | `/swagger/external/index.html` | Public (dev shim when `Auth:DevMode=true`) |
| **CCE.Api.Internal** | 5002 | `/swagger/internal/index.html` | Admin/CMS |

Both use Minimal APIs + MediatR. No controllers.

## Auth & Permissions

- **Permissions are code-generated from `permissions.yaml`.** Edit the YAML, then rebuild `CCE.Domain` — a Roslyn source generator (`CCE.Domain.SourceGenerators/PermissionsGenerator.cs`) emits `CCE.Domain.Permissions` and `CCE.Domain.RolePermissionMap`.
- Known roles: `cce-admin`, `cce-editor`, `cce-reviewer`, `cce-expert`, `cce-user`, `Anonymous`.
- Dev mode (`Auth:DevMode=true` in `appsettings.Development.json`) enables `/dev/sign-in`, `/dev/whoami`, `/dev/sign-out` endpoints and swaps JWT for a test handler.

## Testing

- **Integration tests** use `CceTestWebApplicationFactory<TProgram>` which replaces the real JwtBearer scheme with `TestAuthHandler` so no live IdP is needed.
- **No SQL Server required for unit tests** — most Application-layer tests mock `ICceDbContext` with NSubstitute.

## Database & Seeding

- EF Core with SQL Server + `snake_case` naming convention (`EFCore.NamingConventions`).
- Connection string lives in `appsettings.Development.json` under `Infrastructure:SqlConnectionString`.
- Seeder console app (`CCE.Seeder`) is the canonical way to apply migrations and seed. Seeders are idempotent and ordered by `ISeeder.Order`:
  1. `RolesAndPermissionsSeeder`
  2. `ReferenceDataSeeder`
  3. `KnowledgeMapSeeder`
  4. `DemoDataSeeder` (skipped unless `--demo` flag)

## Redis

- **Optional at dev time.** The output-cache middleware catches `RedisException` and bypasses the cache with a warning. The API starts cleanly without Redis.
- Connection string: `localhost:6379` by default in dev settings.

## Swagger Quirk

Swagger UI routes are **not** at `/swagger/index.html`. They use a tag prefix:
- External: `/swagger/external/index.html`
- Internal: `/swagger/internal/index.html`

Both now include a JWT Bearer security definition (`Bearer` scheme).

## EF Query Pattern

Use `ToListAsyncEither()` and `CountAsyncEither()` from `PaginationExtensions` instead of raw `ToListAsync()` / `CountAsync()`. This dispatches to EF async when the queryable is `IAsyncEnumerable<T>` and falls back to synchronous `ToList()` / `Count()` for in-memory test queryables.

## Gotchas

- **Port locks:** If `dotnet run` fails with "address already in use", kill all `CCE.Api.*.exe` and `dotnet.exe` processes, then rebuild.
- **File locks during build:** The API EXEs hold DLLs open. Stop running APIs before rebuilding.
- **Source generators:** `CCE.Domain.SourceGenerators` targets `netstandard2.0` and uses Roslyn 4.8. Do not upgrade Roslyn packages beyond what the .NET 8 SDK ships.
- **No `ConnectionStrings:Default` section.** The connection string is under `Infrastructure:SqlConnectionString`, not the conventional `ConnectionStrings` section.
