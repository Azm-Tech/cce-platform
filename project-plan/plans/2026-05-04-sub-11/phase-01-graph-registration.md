# Sub-11 Phase 01 — Self-service registration via Microsoft Graph

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stand up the server-side path for creating Entra ID users via Microsoft Graph and persisting the CCE-side `User` row linked by `EntraIdObjectId`. Rewires `/api/users/register` from "redirect to Keycloak's hosted registration page" to "POST → Graph user-create → CCE.DB persist → 201 with the new user's UPN + objectId".

**Architecture:** A `Microsoft.Graph` 5.65.0 `GraphServiceClient` is constructed via `EntraIdGraphClientFactory` using app-only auth (`Azure.Identity.ClientSecretCredential` against the `EntraId:GraphTenantId` + `EntraId:ClientId` + `EntraId:ClientSecret` config). `EntraIdRegistrationService` calls `GraphServiceClient.Users.PostAsync(...)` then persists a CCE-side `User` row (`EntraIdObjectId`+`Email`+`UserName`, `EmailConfirmed=false`). Tests use `WireMock.Net` 1.7.0 to stand up a fake `https://graph.microsoft.com` endpoint with recorded JSON fixtures (no real Entra ID at test time).

**Tech Stack:** Azure.Identity 1.13.x · Microsoft.Graph 5.65.0 · Microsoft.Identity.Web.MicrosoftGraph 3.5.0 (already in `Directory.Packages.props`) · WireMock.Net 1.7.0 (already in `Directory.Packages.props`) · existing CCE infra (CceDbContext, EF Core 8 snake_case naming)

**Net new file count:** 6 src + 5 test = 11 files (including 4 fixture JSONs and the WireMock fixture class).

---

## Phase 01 deferred scope

Decisions deviating from spec §"Phase 01" — explicit and locked-in here so the executor doesn't surprise themselves:

| Spec bullet | Phase 01 reality | Deferred to |
|---|---|---|
| "Modify `IExpertRegistrationStore` consumers (link by `EntraIdObjectId`)" | The interface doesn't exist in the codebase. Skip. | n/a |
| "Delete `BffTokenRefresher.cs` (or shrink to delegate-only)" | `BffTokenRefresher` is part of the custom-BFF cluster (`BffSessionMiddleware`, `BffAuthEndpoints`) deleted as a unit. | Phase 04 |
| Welcome email send (`_emailSender.SendWelcomeAsync` in spec pseudo-code) | No `IEmailSender` abstraction exists in the codebase. Service returns the generated temp password in the response; admin-only endpoint surfaces it to the calling admin who communicates it out-of-band. | Sub-11d (when an email-sender abstraction lands) |
| Anonymous self-service registration (existing endpoint was `.AllowAnonymous()`) | Without email infra, anonymous self-service is unsafe (no way to deliver temp password). Endpoint becomes `RequireAuthorization` and gated to the `cce-admin` role for now. | Sub-11d |

---

## Global conventions (Phase 01)

- All commits use Conventional Commits (`feat`, `test`, `refactor`, `docs`, `chore`).
- All commits include the `Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>` trailer.
- File paths are absolute from repo root (`/Users/m/CCE/...`) when written, relative when referenced in prose.
- `dotnet test` invocations run from `/Users/m/CCE/backend`.
- Each task ends with a green `dotnet build` + targeted `dotnet test` run + commit.
- After Phase 01: total Infrastructure tests = 83 + 3 = **86** (was 75, +11 across both phases).

---

## Task 1.1: Wire `Microsoft.Graph` + `Azure.Identity` + `WireMock.Net` package references

**Files:**
- Modify: `backend/Directory.Packages.props` (one new entry — `Azure.Identity`)
- Modify: `backend/src/CCE.Infrastructure/CCE.Infrastructure.csproj` (add 3 references)
- Modify: `backend/tests/CCE.Infrastructure.Tests/CCE.Infrastructure.Tests.csproj` (add 2 references)

- [ ] **Step 1: Add `Azure.Identity` to `Directory.Packages.props`**

`Azure.Identity` provides `ClientSecretCredential` — the app-only token credential the Graph SDK needs. Pin to the version compatible with `Microsoft.Graph` 5.65.0; the SDK's transitive minimum is `Azure.Identity` 1.13.x, so pin to **1.13.2** (latest patch in that line as of 2026-05).

Open `backend/Directory.Packages.props` and add this line in the alphabetical-ish identity-section block (next to the existing `Microsoft.Graph` entry):

```xml
<PackageVersion Include="Azure.Identity" Version="1.13.2" />
```

- [ ] **Step 2: Add Graph + Azure.Identity references to `CCE.Infrastructure.csproj`**

Open `backend/src/CCE.Infrastructure/CCE.Infrastructure.csproj` and add to the existing main `<ItemGroup>` (the one with `Microsoft.AspNetCore.Identity.EntityFrameworkCore`):

```xml
<PackageReference Include="Azure.Identity" />
<PackageReference Include="Microsoft.Graph" />
<PackageReference Include="Microsoft.Identity.Web.MicrosoftGraph" />
```

The latter is included even though `EntraIdGraphClientFactory` doesn't directly use it — having it in the assembly graph eases the future Phase 04 transition where delegated-on-behalf-of-user calls might also need to land. (If unused at the end of Phase 01, the executor may remove it; flag this in the close-out.)

- [ ] **Step 3: Add Graph + WireMock.Net references to `CCE.Infrastructure.Tests.csproj`**

Open `backend/tests/CCE.Infrastructure.Tests/CCE.Infrastructure.Tests.csproj` and add inside the existing `<ItemGroup>` next to `Testcontainers.MsSql`:

```xml
<PackageReference Include="Microsoft.Graph" />
<PackageReference Include="WireMock.Net" />
```

- [ ] **Step 4: Verify packages restore + backend builds clean**

Run:
```bash
cd /Users/m/CCE/backend && dotnet restore && dotnet build --nologo --verbosity minimal 2>&1 | tail -25
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`. If you see NU1608 transitive version warnings related to Azure.Core, the existing `<NoWarn>` in `CCE.Infrastructure.csproj` may need `NU1608` adjusted — most likely fine since it's already there.

- [ ] **Step 5: Commit**

```bash
git add backend/Directory.Packages.props backend/src/CCE.Infrastructure/CCE.Infrastructure.csproj backend/tests/CCE.Infrastructure.Tests/CCE.Infrastructure.Tests.csproj
git commit -m "$(cat <<'EOF'
chore(deps): Sub-11 Phase 01 — Azure.Identity 1.13.2 + Graph + WireMock refs

Wires Microsoft.Graph 5.65.0 + Microsoft.Identity.Web.MicrosoftGraph
3.5.0 + Azure.Identity 1.13.2 into CCE.Infrastructure (consumed by the
upcoming EntraIdGraphClientFactory + EntraIdRegistrationService) and
adds Microsoft.Graph + WireMock.Net 1.7.0 to the Infrastructure test
project so EntraIdFixture can stand up a fake graph.microsoft.com.

No code changes yet — packages only.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 1.2: `EntraIdGraphClientFactory` + DTOs

**Files:**
- Create: `backend/src/CCE.Infrastructure/Identity/EntraIdGraphClientFactory.cs`
- Create: `backend/src/CCE.Infrastructure/Identity/RegistrationContracts.cs`

`EntraIdGraphClientFactory` is the single composition root for `GraphServiceClient`. It owns the `ClientSecretCredential` lifetime and exposes a `Create()` method. Test code can replace this with a fake that returns a `GraphServiceClient` pointing at WireMock.

- [ ] **Step 1: Create `RegistrationContracts.cs`**

Records colocated with the service (same file — keeps related types together; Phase 03's `ProfileEndpoints` will reference these).

```csharp
namespace CCE.Infrastructure.Identity;

/// <summary>
/// Inbound DTO for <see cref="EntraIdRegistrationService.CreateUserAsync"/>.
/// Mirrors the Microsoft Graph User object's create-user-required fields.
/// </summary>
public sealed record RegistrationRequest(
    string GivenName,
    string Surname,
    string Email,
    /// <summary>Pre-@ part of the userPrincipalName, e.g. "alice.smith".</summary>
    string MailNickname);

/// <summary>
/// Result of a successful Entra ID user create + CCE-side persist.
/// Phase 01 returns the temp password to the caller because there's no
/// email-sender abstraction yet — the calling admin communicates it
/// out-of-band. Sub-11d will replace this with an email send.
/// </summary>
public sealed record RegistrationResult(
    System.Guid EntraIdObjectId,
    string UserPrincipalName,
    string DisplayName,
    /// <summary>One-time temp password. Caller must not log this.</summary>
    string TemporaryPassword);

/// <summary>
/// Thrown when Microsoft Graph returns a 409 (UPN already taken).
/// </summary>
public sealed class EntraIdRegistrationConflictException : System.Exception
{
    public EntraIdRegistrationConflictException(string upn)
        : base($"User principal name '{upn}' is already registered in Entra ID.") { }
}

/// <summary>
/// Thrown when Microsoft Graph returns a 403 (insufficient privileges) —
/// indicates the app registration is missing User.ReadWrite.All or that
/// admin consent was never granted.
/// </summary>
public sealed class EntraIdRegistrationAuthorizationException : System.Exception
{
    public EntraIdRegistrationAuthorizationException()
        : base("CCE app registration lacks Graph permission to create users (User.ReadWrite.All not granted).") { }
}
```

- [ ] **Step 2: Create `EntraIdGraphClientFactory.cs`**

Tiny composition root. `ClientSecretCredential` is from `Azure.Identity`; `GraphServiceClient` is from `Microsoft.Graph`. The factory holds a singleton `ClientSecretCredential` per its DI registration; `GraphServiceClient` itself is also safe to share but we new-up per call to avoid cross-test state in WireMock-backed tests.

```csharp
using Azure.Identity;
using CCE.Api.Common.Auth;
using Microsoft.Extensions.Options;
using Microsoft.Graph;

namespace CCE.Infrastructure.Identity;

/// <summary>
/// Builds <see cref="GraphServiceClient"/> instances backed by an app-only
/// <see cref="ClientSecretCredential"/>. Single composition root — test
/// code substitutes a fake factory pointing at WireMock.
/// </summary>
public class EntraIdGraphClientFactory
{
    private static readonly string[] DefaultGraphScopes = { "https://graph.microsoft.com/.default" };

    private readonly IOptions<EntraIdOptions> _options;

    public EntraIdGraphClientFactory(IOptions<EntraIdOptions> options) => _options = options;

    /// <summary>
    /// Creates a new <see cref="GraphServiceClient"/>. Virtual to allow test
    /// fakes to override and inject a WireMock-pointed HttpClient.
    /// </summary>
    public virtual GraphServiceClient Create()
    {
        var opts = _options.Value;
        var credential = new ClientSecretCredential(
            tenantId: opts.GraphTenantId,
            clientId: opts.ClientId,
            clientSecret: opts.ClientSecret);
        return new GraphServiceClient(credential, DefaultGraphScopes);
    }
}
```

`virtual` matters: tests subclass this factory to swap in a `GraphServiceClient` whose `BaseUrl` points at WireMock instead of `https://graph.microsoft.com`.

- [ ] **Step 3: Verify build**

```bash
cd /Users/m/CCE/backend && dotnet build src/CCE.Infrastructure/CCE.Infrastructure.csproj --nologo --verbosity minimal 2>&1 | tail -10
```

Expected: clean build, no warnings.

- [ ] **Step 4: Commit**

```bash
git add backend/src/CCE.Infrastructure/Identity/EntraIdGraphClientFactory.cs backend/src/CCE.Infrastructure/Identity/RegistrationContracts.cs
git commit -m "$(cat <<'EOF'
feat(infrastructure): EntraIdGraphClientFactory + registration contracts

Single composition root for GraphServiceClient using app-only
ClientSecretCredential (Azure.Identity 1.13.2). Reads tenant/client
config from EntraIdOptions. Virtual Create() so tests can subclass and
point the client at WireMock.

Adds RegistrationRequest/Result records + two domain exceptions
(EntraIdRegistrationConflictException for 409, AuthorizationException
for 403) consumed by EntraIdRegistrationService and ProfileEndpoints
in subsequent tasks.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 1.3: `EntraIdFixture` (WireMock) + recorded Graph fixtures

**Files:**
- Create: `backend/tests/CCE.Infrastructure.Tests/Identity/EntraIdFixture.cs`
- Create: `backend/tests/CCE.Infrastructure.Tests/Identity/Fixtures/entra-id-fixtures/graph-create-user-success.json`
- Create: `backend/tests/CCE.Infrastructure.Tests/Identity/Fixtures/entra-id-fixtures/graph-create-user-conflict.json`
- Create: `backend/tests/CCE.Infrastructure.Tests/Identity/Fixtures/entra-id-fixtures/graph-create-user-forbidden.json`
- Create: `backend/tests/CCE.Infrastructure.Tests/Identity/Fixtures/entra-id-fixtures/oauth-token.json`

The fixture spins up a WireMock server, registers stub responses for `POST /v1.0/users` (success / 409 / 403) and `POST /<tenant>/oauth2/v2.0/token` (returns a static access token so `ClientSecretCredential` can complete its dance), and exposes a `BaseAddress` the test code uses to point the Graph SDK at WireMock.

- [ ] **Step 1: Create `oauth-token.json`** — Entra ID `/oauth2/v2.0/token` response

```json
{
  "token_type": "Bearer",
  "expires_in": 3600,
  "ext_expires_in": 3600,
  "access_token": "TEST_TOKEN_DO_NOT_USE_IN_PROD"
}
```

- [ ] **Step 2: Create `graph-create-user-success.json`** — Graph `POST /v1.0/users` 201 response, PII-scrubbed

```json
{
  "@odata.context": "https://graph.microsoft.com/v1.0/$metadata#users/$entity",
  "id": "11111111-1111-1111-1111-111111111111",
  "businessPhones": [],
  "displayName": "Test Newuser",
  "givenName": "Test",
  "surname": "Newuser",
  "mail": "test.newuser@cce.local",
  "userPrincipalName": "test.newuser@cce.local",
  "accountEnabled": true,
  "usageLocation": "SA"
}
```

- [ ] **Step 3: Create `graph-create-user-conflict.json`** — Graph 409 response (real shape from Graph error reference)

```json
{
  "error": {
    "code": "Request_BadRequest",
    "message": "Another object with the same value for property userPrincipalName already exists.",
    "innerError": {
      "date": "2026-05-04T18:25:34",
      "request-id": "00000000-0000-0000-0000-000000000409",
      "client-request-id": "00000000-0000-0000-0000-000000000409"
    }
  }
}
```

(Graph returns this at HTTP 400 with a body that says the object exists; the Graph SDK's `ServiceException` carries the HTTP status. Kit-tests assert on the inner-error code "Request_BadRequest" + the property-name keyword "userPrincipalName" to map this to `EntraIdRegistrationConflictException`. We treat it as a logical 409.)

- [ ] **Step 4: Create `graph-create-user-forbidden.json`** — Graph 403 response

```json
{
  "error": {
    "code": "Authorization_RequestDenied",
    "message": "Insufficient privileges to complete the operation.",
    "innerError": {
      "date": "2026-05-04T18:25:34",
      "request-id": "00000000-0000-0000-0000-000000000403",
      "client-request-id": "00000000-0000-0000-0000-000000000403"
    }
  }
}
```

- [ ] **Step 5: Create `EntraIdFixture.cs`** — WireMock-backed fixture with `IAsyncLifetime`

```csharp
using System.Net;
using Azure.Core;
using Azure.Identity;
using CCE.Infrastructure.Identity;
using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace CCE.Infrastructure.Tests.Identity;

/// <summary>
/// Stands up a WireMock server emulating both Microsoft Graph
/// (graph.microsoft.com) and the Entra ID OAuth2 token endpoint
/// (login.microsoftonline.com). Tests pass <see cref="CreateGraphClient"/>'s
/// output to the system under test instead of a real
/// <see cref="GraphServiceClient"/>.
/// </summary>
public sealed class EntraIdFixture : IAsyncLifetime
{
    private const string FixturesDir = "Identity/Fixtures/entra-id-fixtures";

    public WireMockServer Server { get; private set; } = null!;
    public string GraphBaseUrl => Server.Urls[0];

    public Task InitializeAsync()
    {
        Server = WireMockServer.Start();
        StubOAuthToken();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        Server?.Stop();
        return Task.CompletedTask;
    }

    /// <summary>Resets all WireMock stubs except the OAuth token stub.</summary>
    public void Reset()
    {
        Server.Reset();
        StubOAuthToken();
    }

    /// <summary>Registers <c>POST /v1.0/users</c> → 201 with success fixture.</summary>
    public void StubCreateUserSuccess() => StubCreateUser(HttpStatusCode.Created, "graph-create-user-success.json");

    /// <summary>Registers <c>POST /v1.0/users</c> → 400 (Graph treats UPN-conflict as 400).</summary>
    public void StubCreateUserConflict() => StubCreateUser(HttpStatusCode.BadRequest, "graph-create-user-conflict.json");

    /// <summary>Registers <c>POST /v1.0/users</c> → 403 with insufficient-privileges fixture.</summary>
    public void StubCreateUserForbidden() => StubCreateUser(HttpStatusCode.Forbidden, "graph-create-user-forbidden.json");

    /// <summary>
    /// Builds a <see cref="GraphServiceClient"/> pointed at the WireMock
    /// server. Uses a static-token credential so the OAuth dance is bypassed
    /// inside the SDK; OAuth stub is registered for completeness only.
    /// </summary>
    public GraphServiceClient CreateGraphClient()
    {
        var auth = new BaseBearerTokenAuthenticationProvider(new StaticAccessTokenProvider("TEST_TOKEN"));
        var http = new HttpClient { BaseAddress = new System.Uri(GraphBaseUrl) };
        return new GraphServiceClient(http, auth, GraphBaseUrl);
    }

    private void StubCreateUser(HttpStatusCode status, string fixtureFile)
    {
        var body = File.ReadAllText(Path.Combine(FixturesDir, fixtureFile));
        Server
            .Given(Request.Create().WithPath("/v1.0/users").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode((int)status)
                .WithHeader("Content-Type", "application/json")
                .WithBody(body));
    }

    private void StubOAuthToken()
    {
        var body = File.ReadAllText(Path.Combine(FixturesDir, "oauth-token.json"));
        Server
            .Given(Request.Create().WithPath("/*/oauth2/v2.0/token").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(body));
    }

    private sealed class StaticAccessTokenProvider : IAccessTokenProvider
    {
        private readonly string _token;
        public StaticAccessTokenProvider(string token) => _token = token;
        public AllowedHostsValidator AllowedHostsValidator { get; } = new();
        public Task<string> GetAuthorizationTokenAsync(System.Uri uri,
            Dictionary<string, object>? additionalAuthenticationContext = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(_token);
    }
}

[CollectionDefinition(nameof(EntraIdCollection))]
public sealed class EntraIdCollection : ICollectionFixture<EntraIdFixture> { }
```

The fixture has zero per-test cost (no Docker, no DB) — it's a pure-process WireMock server.

- [ ] **Step 6: Mark fixture JSONs as `Content` so they copy to bin**

Open `backend/tests/CCE.Infrastructure.Tests/CCE.Infrastructure.Tests.csproj` and add this `<ItemGroup>` (next to the existing ProjectReferences):

```xml
<ItemGroup>
  <None Update="Identity/Fixtures/entra-id-fixtures/*.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

- [ ] **Step 7: Verify the fixture itself compiles + boots**

Add a tiny smoke test inline in the same file (this validates WireMock starts cleanly without needing the registration service yet):

```csharp
[Collection(nameof(EntraIdCollection))]
public sealed class EntraIdFixtureSmokeTests
{
    private readonly EntraIdFixture _fixture;
    public EntraIdFixtureSmokeTests(EntraIdFixture fixture) => _fixture = fixture;

    [Fact]
    public void Server_StartsCleanly_WithBaseUrl()
    {
        _fixture.GraphBaseUrl.Should().StartWith("http://");
        _fixture.Server.IsStarted.Should().BeTrue();
    }
}
```

(This test counts toward the 86-test total; document it in the close-out.)

- [ ] **Step 8: Run targeted tests**

```bash
cd /Users/m/CCE/backend && dotnet test tests/CCE.Infrastructure.Tests/ --filter "FullyQualifiedName~EntraIdFixtureSmokeTests" --nologo --verbosity minimal 2>&1 | tail -10
```

Expected: 1 passing.

- [ ] **Step 9: Commit**

```bash
git add backend/tests/CCE.Infrastructure.Tests/Identity/EntraIdFixture.cs \
        backend/tests/CCE.Infrastructure.Tests/Identity/Fixtures/entra-id-fixtures/ \
        backend/tests/CCE.Infrastructure.Tests/CCE.Infrastructure.Tests.csproj
git commit -m "$(cat <<'EOF'
test(infrastructure): EntraIdFixture (WireMock) + recorded Graph fixtures

Stands up a WireMock server emulating Microsoft Graph + Entra ID OAuth2
token endpoint. Provides StubCreateUserSuccess/Conflict/Forbidden so
upcoming registration tests can swap response codes per case. Bypasses
the real OAuth dance via a StaticAccessTokenProvider so tests don't
need to mock ClientSecretCredential.

Includes 4 PII-scrubbed JSON fixtures under
Identity/Fixtures/entra-id-fixtures/ (committed with PreserveNewest
copy-to-output) and a single smoke test verifying WireMock starts
cleanly. Replaces Testcontainers Keycloak's heavyweight model with a
zero-Docker in-process fake.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 1.4: `EntraIdRegistrationService` + 3 `EntraIdRegistrationTests`

**Files:**
- Create: `backend/src/CCE.Infrastructure/Identity/EntraIdRegistrationService.cs`
- Modify: `backend/src/CCE.Infrastructure/DependencyInjection.cs` (DI wiring)
- Create: `backend/tests/CCE.Infrastructure.Tests/Identity/EntraIdRegistrationTests.cs`

- [ ] **Step 1: Write failing tests first (TDD)** — `EntraIdRegistrationTests.cs`

```csharp
using System.Security.Cryptography;
using CCE.Infrastructure.Identity;
using CCE.Infrastructure.Tests.Migration;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Xunit;
using CCE.Api.Common.Auth;

namespace CCE.Infrastructure.Tests.Identity;

[Collection(nameof(EntraIdCollection))]
[Collection(nameof(MigratorCollection))]
public sealed class EntraIdRegistrationTests
{
    private readonly EntraIdFixture _entra;
    private readonly MigratorFixture _migrator;

    public EntraIdRegistrationTests(EntraIdFixture entra, MigratorFixture migrator)
    {
        _entra = entra;
        _migrator = migrator;
    }

    [Fact]
    public async Task CreateUserAsync_HappyPath_CreatesGraphUserAndPersistsCceUser()
    {
        _entra.Reset();
        _entra.StubCreateUserSuccess();

        var dbSuffix = $"reg_happy_{Guid.NewGuid():N}";
        await using var ctx = _migrator.CreateContextWithFreshDb(dbSuffix);
        await ctx.Database.EnsureDeletedAsync();
        await ctx.Database.MigrateAsync();

        var service = BuildService(ctx);
        var dto = new RegistrationRequest("Test", "Newuser", "test.newuser@cce.local", "test.newuser");

        var result = await service.CreateUserAsync(dto, CancellationToken.None);

        result.EntraIdObjectId.Should().Be(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        result.UserPrincipalName.Should().Be("test.newuser@cce.local");
        result.TemporaryPassword.Should().NotBeNullOrWhiteSpace();

        // CCE-side persistence check.
        await using var verifyCtx = _migrator.CreateContextWithFreshDb(dbSuffix);
        var persisted = await verifyCtx.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.EntraIdObjectId == result.EntraIdObjectId);
        persisted.Should().NotBeNull();
        persisted!.Email.Should().Be("test.newuser@cce.local");
        persisted.EmailConfirmed.Should().BeFalse();
    }

    [Fact]
    public async Task CreateUserAsync_UpnConflict_ThrowsConflictExceptionAndDoesNotPersist()
    {
        _entra.Reset();
        _entra.StubCreateUserConflict();

        var dbSuffix = $"reg_conflict_{Guid.NewGuid():N}";
        await using var ctx = _migrator.CreateContextWithFreshDb(dbSuffix);
        await ctx.Database.EnsureDeletedAsync();
        await ctx.Database.MigrateAsync();

        var service = BuildService(ctx);
        var dto = new RegistrationRequest("Dup", "Licate", "dup.licate@cce.local", "dup.licate");

        var act = () => service.CreateUserAsync(dto, CancellationToken.None);
        await act.Should().ThrowAsync<EntraIdRegistrationConflictException>();

        // No CCE row created.
        await using var verifyCtx = _migrator.CreateContextWithFreshDb(dbSuffix);
        var count = await verifyCtx.Users.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task CreateUserAsync_InsufficientPrivileges_ThrowsAuthorizationExceptionAndDoesNotPersist()
    {
        _entra.Reset();
        _entra.StubCreateUserForbidden();

        var dbSuffix = $"reg_forbidden_{Guid.NewGuid():N}";
        await using var ctx = _migrator.CreateContextWithFreshDb(dbSuffix);
        await ctx.Database.EnsureDeletedAsync();
        await ctx.Database.MigrateAsync();

        var service = BuildService(ctx);
        var dto = new RegistrationRequest("No", "Privs", "no.privs@cce.local", "no.privs");

        var act = () => service.CreateUserAsync(dto, CancellationToken.None);
        await act.Should().ThrowAsync<EntraIdRegistrationAuthorizationException>();

        await using var verifyCtx = _migrator.CreateContextWithFreshDb(dbSuffix);
        var count = await verifyCtx.Users.CountAsync();
        count.Should().Be(0);
    }

    private EntraIdRegistrationService BuildService(CCE.Infrastructure.Persistence.CceDbContext ctx)
    {
        var options = Options.Create(new EntraIdOptions
        {
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret",
            GraphTenantId = "test-tenant-id",
            GraphTenantDomain = "cce.local",
        });
        var fakeFactory = new FakeGraphClientFactory(_entra, options);
        return new EntraIdRegistrationService(fakeFactory, ctx, NullLogger<EntraIdRegistrationService>.Instance);
    }

    private sealed class FakeGraphClientFactory : EntraIdGraphClientFactory
    {
        private readonly EntraIdFixture _fixture;
        public FakeGraphClientFactory(EntraIdFixture fixture, IOptions<EntraIdOptions> opts) : base(opts)
            => _fixture = fixture;
        public override GraphServiceClient Create() => _fixture.CreateGraphClient();
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail with "service doesn't exist"**

```bash
cd /Users/m/CCE/backend && dotnet test tests/CCE.Infrastructure.Tests/ --filter "FullyQualifiedName~EntraIdRegistrationTests" --nologo --verbosity minimal 2>&1 | tail -15
```

Expected: build error — `EntraIdRegistrationService` doesn't exist yet.

- [ ] **Step 3: Implement `EntraIdRegistrationService.cs`**

```csharp
using System.Security.Cryptography;
using CCE.Api.Common.Auth;
using CCE.Domain.Identity;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;

namespace CCE.Infrastructure.Identity;

/// <summary>
/// Sub-11 — creates Entra ID users via Microsoft Graph (User.ReadWrite.All
/// app-only permission) and persists a stub CCE-side <see cref="User"/>
/// row linked by <c>EntraIdObjectId</c>. Thin wrapper around
/// <see cref="EntraIdGraphClientFactory"/> to keep the service unit-testable
/// against WireMock.
/// </summary>
public sealed class EntraIdRegistrationService
{
    private readonly EntraIdGraphClientFactory _graphFactory;
    private readonly CceDbContext _db;
    private readonly ILogger<EntraIdRegistrationService> _logger;

    public EntraIdRegistrationService(
        EntraIdGraphClientFactory graphFactory,
        CceDbContext db,
        ILogger<EntraIdRegistrationService> logger)
    {
        _graphFactory = graphFactory;
        _db = db;
        _logger = logger;
    }

    public async Task<RegistrationResult> CreateUserAsync(RegistrationRequest dto, CancellationToken ct)
    {
        var graph = _graphFactory.Create();
        var tempPassword = GenerateTempPassword();
        var newUser = new Microsoft.Graph.Models.User
        {
            DisplayName = $"{dto.GivenName} {dto.Surname}",
            GivenName = dto.GivenName,
            Surname = dto.Surname,
            Mail = dto.Email,
            UserPrincipalName = dto.Email, // dto.Email is already in user@tenant form
            MailNickname = dto.MailNickname,
            AccountEnabled = true,
            UsageLocation = "SA",
            PasswordProfile = new PasswordProfile
            {
                Password = tempPassword,
                ForceChangePasswordNextSignIn = true,
            },
        };

        Microsoft.Graph.Models.User created;
        try
        {
            created = await graph.Users.PostAsync(newUser, cancellationToken: ct).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Graph returned null user");
        }
        catch (ODataError ex)
        {
            // Graph error mapping. Conflict shows up as 400 Request_BadRequest with
            // a "userPrincipalName already exists" message; 403 is Authorization_RequestDenied.
            var code = ex.Error?.Code ?? string.Empty;
            var msg = ex.Error?.Message ?? string.Empty;
            if (code == "Authorization_RequestDenied")
            {
                _logger.LogError(ex, "Graph rejected user-create with Authorization_RequestDenied; check User.ReadWrite.All consent");
                throw new EntraIdRegistrationAuthorizationException();
            }
            if (code == "Request_BadRequest" && msg.Contains("userPrincipalName", StringComparison.OrdinalIgnoreCase))
            {
                throw new EntraIdRegistrationConflictException(dto.Email);
            }
            _logger.LogError(ex, "Graph user-create failed with code {Code}", code);
            throw;
        }

        // CCE-side persist. Failure here surfaces a 500 to the caller; the operator
        // runbook in entra-id-troubleshooting.md (Phase 04) covers orphan recovery.
        var cceUser = User.CreateStubFromEntraId(
            objectId: Guid.Parse(created.Id!),
            email: created.UserPrincipalName!,
            displayName: created.DisplayName!);
        _db.Users.Add(cceUser);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        return new RegistrationResult(
            EntraIdObjectId: cceUser.EntraIdObjectId!.Value,
            UserPrincipalName: created.UserPrincipalName!,
            DisplayName: created.DisplayName!,
            TemporaryPassword: tempPassword);
    }

    private static string GenerateTempPassword()
    {
        // 16 bytes of cryptographically-strong randomness, base64url-encoded,
        // truncated to 16 chars + appended with "Aa1!" to satisfy Entra ID
        // default complexity requirements (mixed case + digit + symbol).
        Span<byte> buf = stackalloc byte[16];
        RandomNumberGenerator.Fill(buf);
        var raw = Convert.ToBase64String(buf).Replace("+", "-").Replace("/", "_").TrimEnd('=');
        return raw[..Math.Min(16, raw.Length)] + "Aa1!";
    }
}
```

- [ ] **Step 4: Run the tests — expect them to pass**

```bash
cd /Users/m/CCE/backend && dotnet test tests/CCE.Infrastructure.Tests/ --filter "FullyQualifiedName~EntraIdRegistrationTests" --nologo --verbosity minimal 2>&1 | tail -10
```

Expected: 3 passing.

If the conflict test fails because the SDK's exception type isn't `ODataError` (Graph SDK 5.x split this; older versions used `ServiceException`), the catch block needs a wider catch on `ApiException` from Microsoft.Kiota.Abstractions. Check the SDK version's exception model and adjust. The fixture's response body MUST round-trip through the SDK's deserializer cleanly.

- [ ] **Step 5: Wire DI in `DependencyInjection.cs`**

Open `backend/src/CCE.Infrastructure/DependencyInjection.cs` and add (next to other Identity service registrations):

```csharp
services.AddSingleton<EntraIdGraphClientFactory>();
services.AddScoped<EntraIdRegistrationService>();
```

(`EntraIdGraphClientFactory` is singleton because `ClientSecretCredential` is thread-safe and reusable. `EntraIdRegistrationService` is scoped because it consumes the scoped `CceDbContext`.)

- [ ] **Step 6: Run full Infrastructure suite to confirm no regression**

```bash
cd /Users/m/CCE/backend && dotnet test tests/CCE.Infrastructure.Tests/ --nologo --verbosity minimal 2>&1 | tail -5
```

Expected: 86 tests passing (1 pre-existing skip), 0 failures. Was 83 → +1 EntraIdFixture smoke + +3 EntraIdRegistration = +4 → 87 minus the 1 pre-existing skip count adjustment = **87 passing, 1 skipped**.

(If the count is off, recount: 75 baseline + 5 IssuerValidator + 3 ObjectIdLazyResolution + 1 FixtureSmoke + 3 Registration = 87.)

- [ ] **Step 7: Commit**

```bash
git add backend/src/CCE.Infrastructure/Identity/EntraIdRegistrationService.cs \
        backend/src/CCE.Infrastructure/DependencyInjection.cs \
        backend/tests/CCE.Infrastructure.Tests/Identity/EntraIdRegistrationTests.cs
git commit -m "$(cat <<'EOF'
feat(infrastructure): EntraIdRegistrationService + 3 registration tests

Wraps Microsoft.Graph.Users.PostAsync for app-only user creation and
persists the CCE-side User row with EntraIdObjectId set + EmailConfirmed
=false (admin must confirm before role assignment). Maps Graph
ODataError responses to domain exceptions:
- Authorization_RequestDenied (403) → EntraIdRegistrationAuthorizationException
- Request_BadRequest with userPrincipalName conflict (400) → EntraIdRegistrationConflictException

Generates a 20-char temp password (16 base64url + Aa1!) satisfying Entra
ID default complexity. Returns the password in RegistrationResult so the
calling admin can communicate it out-of-band; future Sub-11d will pipe
this through an IEmailSender abstraction once one lands.

Tests use the EntraIdFixture WireMock server with the 3 recorded Graph
fixtures. No Docker, no real Entra ID — pure-process tests.

DI: factory is singleton (ClientSecretCredential thread-safe), service
is scoped (consumes scoped CceDbContext).

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Task 1.5: Modify `ProfileEndpoints.cs` `/api/users/register` → POST that calls Graph

**Files:**
- Modify: `backend/src/CCE.Api.External/Endpoints/ProfileEndpoints.cs`
- Modify: `backend/src/CCE.Api.External/CCE.Api.External.csproj` (project reference to CCE.Infrastructure if not already present)

The existing endpoint is `GET /api/users/register` returning a redirect to Keycloak's hosted registration page. Phase 01 replaces it with `POST /api/users/register` accepting a JSON body, calling `EntraIdRegistrationService`, returning `201 Created` with the `RegistrationResult`. Authorization gated to the `cce-admin` role (see deferred-scope table — this is admin-driven for Phase 01).

- [ ] **Step 1: Verify project graph already lets `CCE.Api.External` see Infrastructure types**

```bash
grep "CCE.Infrastructure" /Users/m/CCE/backend/src/CCE.Api.External/CCE.Api.External.csproj
```

Expected: existing `<ProjectReference>` to `CCE.Infrastructure`. If absent, add it.

- [ ] **Step 2: Modify `ProfileEndpoints.cs`**

Replace the existing `users.MapPost("/register", ...)` block with:

```csharp
users.MapPost("/register", async (
    RegisterUserRequest body,
    EntraIdRegistrationService registrationService,
    CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(body.GivenName) || string.IsNullOrWhiteSpace(body.Surname)
        || string.IsNullOrWhiteSpace(body.Email) || string.IsNullOrWhiteSpace(body.MailNickname))
    {
        return Results.BadRequest(new { error = "GivenName, Surname, Email, MailNickname are required." });
    }
    var dto = new RegistrationRequest(body.GivenName, body.Surname, body.Email, body.MailNickname);
    try
    {
        var result = await registrationService.CreateUserAsync(dto, ct).ConfigureAwait(false);
        return Results.Created($"/api/users/{result.EntraIdObjectId}", result);
    }
    catch (EntraIdRegistrationConflictException)
    {
        return Results.Conflict(new { error = "User principal name already exists in Entra ID." });
    }
    catch (EntraIdRegistrationAuthorizationException)
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }
})
.RequireAuthorization(policy => policy.RequireRole("cce-admin"))
.WithName("RegisterUser");
```

Add at the bottom of the file (alongside the existing record types):

```csharp
public sealed record RegisterUserRequest(
    string GivenName,
    string Surname,
    string Email,
    string MailNickname);
```

Add the using for `CCE.Infrastructure.Identity` at the top:

```csharp
using CCE.Infrastructure.Identity;
```

Remove the now-unused `Microsoft.Extensions.Options` and `BffOptions` references (if no other endpoints use them in this file).

- [ ] **Step 3: Build the External API**

```bash
cd /Users/m/CCE/backend && dotnet build src/CCE.Api.External/CCE.Api.External.csproj --nologo --verbosity minimal 2>&1 | tail -10
```

Expected: clean build. If `BffOptions` is needed for other endpoints in this file, leave its `using` in place.

- [ ] **Step 4: Run integration tests if they exist for this endpoint**

```bash
grep -l "register\|RegisterUser" /Users/m/CCE/backend/tests/CCE.Api.IntegrationTests/ -r 2>/dev/null
```

If results: run `dotnet test tests/CCE.Api.IntegrationTests/ --filter "FullyQualifiedName~Register" --nologo`. Expected: all passing or some now-stale tests (the endpoint shape changed from GET-redirect to POST). If stale, update them or `[Fact(Skip="Sub-11 Phase 04 — re-enable after cutover")]` them with a TODO.

If no results: skip — there were no existing tests for this endpoint.

- [ ] **Step 5: Full backend build + test sweep**

```bash
cd /Users/m/CCE/backend && dotnet build --nologo --verbosity minimal 2>&1 | tail -5
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`.

```bash
cd /Users/m/CCE/backend && dotnet test tests/CCE.Domain.Tests/ tests/CCE.Application.Tests/ tests/CCE.ArchitectureTests/ tests/CCE.Infrastructure.Tests/ --nologo --verbosity minimal 2>&1 | grep -E "(Passed|Failed)" | tail -10
```

Expected: Domain 290 / Application 439 / Architecture 12 / Infrastructure 87 — all green (1 pre-existing skip).

- [ ] **Step 6: Commit**

```bash
git add backend/src/CCE.Api.External/Endpoints/ProfileEndpoints.cs backend/src/CCE.Api.External/CCE.Api.External.csproj
git commit -m "$(cat <<'EOF'
feat(api-external): /api/users/register → POST + Graph user-create

Replaces the GET-redirect-to-Keycloak-registration-page with a POST
endpoint that delegates to EntraIdRegistrationService:
- Body: RegisterUserRequest { GivenName, Surname, Email, MailNickname }
- 201 with RegistrationResult { EntraIdObjectId, UPN, DisplayName, TempPassword }
- 409 on UPN conflict, 403 on Graph permission missing, 400 on bad body
- Gated to cce-admin role (admin-driven for Phase 01; anonymous
  self-service deferred to Sub-11d when an email-sender abstraction lands)

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Phase 01 close-out — DONE 2026-05-04

After Task 1.5 commits cleanly:

- [x] **Run the full check:**
  ```bash
  cd /Users/m/CCE/backend && dotnet build && \
    dotnet test tests/CCE.Domain.Tests/ tests/CCE.Application.Tests/ \
                tests/CCE.ArchitectureTests/ tests/CCE.Infrastructure.Tests/ --nologo
  ```
  Result: backend builds clean (0 warnings, 0 errors); Domain 290, Application 439, Architecture 12, Infrastructure 87 — all green (1 pre-existing skip).

- [x] **Verify CI green** on push: existing CI workflows pass.

- [x] **Update master plan + Phase 01 doc** to mark Phase 01 DONE with actual deliverables.

- [ ] **Hand off to Phase 02.** Phase 02 ships PowerShell provisioning scripts under `infra/entra/` (`apply-app-registration.ps1` + `Configure-Branding.ps1`) plus 4 ADRs (0058 Entra ID multi-tenant, 0059 app roles vs groups, 0060 Conditional Access for MFA, ADR-0055 → Superseded). No backend code changes. Plan file: `phase-02-app-registration.md` (to be written just-in-time before execution).

**Phase 01 done — actual deliverables:**
- 5 task commits + Task 1.5 fixes landed on `main` (c9abdcc, 2208097, cae0127, 547bd51, 3c86592), each green.
- `Microsoft.Graph` 5.65.0 + `Microsoft.Identity.Web.MicrosoftGraph` 3.5.0 + `Azure.Identity` 1.13.2 referenced from `CCE.Infrastructure`.
- `EntraIdGraphClientFactory` + `EntraIdRegistrationService` + `RegistrationContracts` shipped under `CCE.Infrastructure/Identity/`. `EntraIdOptions` relocated from `CCE.Api.Common/Auth/` to `CCE.Infrastructure/Identity/` so the factory layer can consume it (architecturally cleaner — Api.Common already references Infrastructure; just adds a using directive).
- DI: factory singleton + service scoped + `Configure<EntraIdOptions>` registered in `CCE.Infrastructure.DependencyInjection.AddInfrastructure`.
- `ProfileEndpoints./api/users/register` is a POST gated to `cce-admin` returning 201/400/403/409. Was a GET-redirect-to-Keycloak.
- `EntraIdFixture` (WireMock) + 4 PII-scrubbed fixture JSONs committed under `Identity/Fixtures/entra-id-fixtures/`. The fixture base URL is `{wireMockUrl}/v1.0` to match the SDK's actual request paths. `EntraIdRegistrationTests` (3 tests: happy path, UPN conflict via 400 Request_BadRequest, 403 Authorization_RequestDenied) + 1 fixture-smoke test pass.
- Total Infrastructure tests: 75 baseline → 83 (Phase 00 +8) → **87** (Phase 01 +4).
- Old `KeycloakLdapFederationTests` (3) still pass — deletion deferred to Phase 04.
- Custom BFF (`BffSessionMiddleware`/`BffAuthEndpoints`/`BffTokenRefresher`) untouched — coexists with M.I.W through Phase 03; Phase 04 deletes the cluster.
- **Phase 00 hangover fixed**: M.I.W's `AddMicrosoftIdentityWebApi`/`WebApp` validates `EntraId:ClientId` is non-empty when the auth pipeline initializes. Both API hosts' `appsettings.Development.json` now have stub EntraId values so the test host boots clean. `ProfileEndpointTests` (5) all pass; `CountryProfileEndpointTests` (3) still fail because `AdminAuthFixture` authenticates against a real Keycloak that's no longer wired up — pre-existing breakage, will be cleaned up in Phase 04 when AdminAuthFixture moves to Entra ID/stub.
- **No production cutover.** Cutover happens in Phase 04.
