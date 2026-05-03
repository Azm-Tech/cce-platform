# Phase 00 — Cross-cutting (Sub-10a)

> Parent: [`../2026-05-03-sub-10a.md`](../2026-05-03-sub-10a.md) · Spec: [`../../specs/2026-05-03-sub-10a-design.md`](../../specs/2026-05-03-sub-10a-design.md) §3 (data contracts), §5 (components)

**Phase goal:** Lay every foundation the next three phases need, without changing user-visible behaviour. Add the `Anthropic.SDK` package; create empty `LoggingExtensions` + `PrometheusExtensions` skeletons under a new `CCE.Api.Common.Observability` namespace; create `AnthropicOptions` + `AssistantClientFactory` skeletons under `CCE.Infrastructure.Assistant`; add `Assistant:` + `Serilog:` config sections to both APIs' `appsettings.json`; register `AssistantClientFactory` in `Infrastructure/DependencyInjection.cs` returning the existing stub. Phase 01 starts the Docker work; Phase 02 fills these skeletons with real behaviour.

**Tasks:** 5
**Working directory:** `/Users/m/CCE/`
**Preconditions:**
- Sub-9 closed (`web-portal-v0.4.0` tag exists; main at the post-Sub-9 commit or later).
- Frontend baseline: 90 suites · 502 tests passing; lint + build clean.
- Backend `dotnet test tests/CCE.Application.Tests/` passes.

---

## Task 0.1: Add `Anthropic.SDK` to centralized package management

**Files:**
- Modify: `backend/Directory.Packages.props` — add a `<PackageVersion>` entry for `Anthropic.SDK`.

**Final state:** the file has all existing `<PackageVersion>` entries plus one new one inside the appropriate `<ItemGroup>`. The new entry goes near the existing observability-related lines (Serilog/Sentry):

```xml
    <PackageVersion Include="Anthropic.SDK" Version="5.0.0" />
```

(Place this in the `<ItemGroup Label="Observability (Serilog + Sentry + Prometheus)">` block as the SDK is conceptually adjacent to the assistant feature; if a more apt block exists at edit time, use it.)

- [ ] **Step 1:** Read the current `Directory.Packages.props` to find an appropriate `<ItemGroup>` for the new entry.

- [ ] **Step 2:** Add the new line. Verify XML is still well-formed:
  ```bash
  cd backend && dotnet restore 2>&1 | tail -5
  ```
  Expected: no errors. (Restore won't actually fetch the package because no project references it yet — but the props file must parse.)

- [ ] **Step 3:** Commit:
  ```bash
  git add backend/Directory.Packages.props
  git -c commit.gpgsign=false commit -m "chore(deps): add Anthropic.SDK 5.0.0 to centralized package management

  Phase 02 will add a PackageReference from CCE.Infrastructure to wire
  the production LLM client. Sub-10a Phase 00 Task 0.1.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 0.2: `LoggingExtensions` skeleton

**Files:**
- Create: `backend/src/CCE.Api.Common/Observability/LoggingExtensions.cs`.

**Why a skeleton in Phase 00:** the actual Serilog wiring lands in Phase 02 (Task 2.1). Phase 00 creates the extension method as a no-op pass-through so Phase 01's Dockerfiles can compile against a stable surface. Two callers (`CCE.Api.External/Program.cs` + `CCE.Api.Internal/Program.cs`) won't be modified to call this until Phase 02.

**Final state of file:**

```cs
using Microsoft.Extensions.Hosting;

namespace CCE.Api.Common.Observability;

/// <summary>
/// Host-level Serilog wiring for both APIs. Phase 02 fills in the
/// console + rolling-file + Sentry sinks plus correlation-id /
/// locale / user-id enrichers. Phase 00 is a no-op pass-through so
/// Dockerfiles can compile against the stable surface.
/// </summary>
public static class LoggingExtensions
{
    public static IHostBuilder UseCceSerilog(this IHostBuilder builder)
    {
        // Phase 02 Task 2.1: wire UseSerilog with sinks + enrichers.
        return builder;
    }
}
```

- [ ] **Step 1:** Create the file with the contents above.

- [ ] **Step 2:** Build:
  ```bash
  cd backend && dotnet build src/CCE.Api.Common/CCE.Api.Common.csproj
  ```
  Expected: success.

- [ ] **Step 3:** Commit:
  ```bash
  git add backend/src/CCE.Api.Common/Observability/LoggingExtensions.cs
  git -c commit.gpgsign=false commit -m "feat(api-common): LoggingExtensions skeleton

  No-op host extension. Phase 02 Task 2.1 fills in Serilog console JSON
  + rolling-file + Sentry sinks plus correlation-id / locale / user-id
  enrichers. Sub-10a Phase 00 Task 0.2.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 0.3: `PrometheusExtensions` skeleton

**Files:**
- Create: `backend/src/CCE.Api.Common/Observability/PrometheusExtensions.cs`.

**Final state of file:**

```cs
using Microsoft.AspNetCore.Builder;

namespace CCE.Api.Common.Observability;

/// <summary>
/// Prometheus middleware + /metrics endpoint for both APIs. Phase 02
/// fills in UseHttpMetrics() + MapMetrics("/metrics") plus the two
/// custom counters (cce_assistant_streams_total{provider} and
/// cce_assistant_citations_total{kind}). Phase 00 is a no-op
/// pass-through so Dockerfiles can compile against the stable surface.
/// </summary>
public static class PrometheusExtensions
{
    public static WebApplication UseCcePrometheus(this WebApplication app)
    {
        // Phase 02 Task 2.2: wire UseHttpMetrics() + MapMetrics() + custom counters.
        return app;
    }
}
```

- [ ] **Step 1:** Create the file with the contents above.

- [ ] **Step 2:** Build:
  ```bash
  cd backend && dotnet build src/CCE.Api.Common/CCE.Api.Common.csproj
  ```
  Expected: success.

- [ ] **Step 3:** Commit:
  ```bash
  git add backend/src/CCE.Api.Common/Observability/PrometheusExtensions.cs
  git -c commit.gpgsign=false commit -m "feat(api-common): PrometheusExtensions skeleton

  No-op WebApplication extension. Phase 02 Task 2.2 fills in
  UseHttpMetrics() + MapMetrics() + the two custom assistant counters.
  Sub-10a Phase 00 Task 0.3.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 0.4: Assistant config + factory skeletons

**Files:**
- Create: `backend/src/CCE.Infrastructure/Assistant/AnthropicOptions.cs`.
- Create: `backend/src/CCE.Infrastructure/Assistant/AssistantClientFactory.cs`.
- Modify: `backend/src/CCE.Infrastructure/DependencyInjection.cs` — register the factory; have it always return the stub for now.
- Modify: `backend/src/CCE.Api.External/appsettings.json` — add `Assistant:Provider` + `Assistant:Anthropic:*` + `Serilog:*` sections (defaults).
- Modify: `backend/src/CCE.Api.Internal/appsettings.json` — same additions.

**Final state of `AnthropicOptions.cs`:**
```cs
namespace CCE.Infrastructure.Assistant;

/// <summary>
/// Configuration shape for the Anthropic LLM client. Bound from
/// Configuration["Assistant:Anthropic"]; missing keys fall back to
/// the defaults declared here.
/// </summary>
public sealed record AnthropicOptions
{
    public string Model { get; init; } = "claude-sonnet-4-5-20250929";
    public int MaxTokens { get; init; } = 1024;
    public double Temperature { get; init; } = 0.3;
}
```

**Final state of `AssistantClientFactory.cs`** (Phase 00 stub — always returns the existing stub):
```cs
using CCE.Application.Assistant;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CCE.Infrastructure.Assistant;

/// <summary>
/// Picks the assistant client implementation based on configuration.
/// Phase 02 Task 2.4 flips this to honour Assistant:Provider.
/// </summary>
public static class AssistantClientFactory
{
    public static IServiceCollection AddCceAssistantClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Phase 02 Task 2.4: read Assistant:Provider + ANTHROPIC_API_KEY
        // and register AnthropicSmartAssistantClient when both are set.
        // Phase 00: always register the stub.
        services.AddScoped<ISmartAssistantClient, SmartAssistantClient>();
        return services;
    }
}
```

(Note: this references `SmartAssistantClient` which currently lives in `CCE.Infrastructure/Assistant/SmartAssistantClient.cs` — same namespace, so the `using` statement above isn't needed; if there's a namespace mismatch at edit time, add the appropriate `using`.)

**`Infrastructure/DependencyInjection.cs` modification:** locate the existing line that registers `ISmartAssistantClient` directly and replace it with a call to the new factory. Search for `services.AddScoped<ISmartAssistantClient, SmartAssistantClient>();` (line ~120 per existing repo state) and replace with `services.AddCceAssistantClient(configuration);`. The factory call **must** appear before any other code that might resolve `ISmartAssistantClient`.

**`appsettings.json` additions** (apply identically to both APIs — `External` and `Internal`):

```json
{
  "Assistant": {
    "Provider": "stub",
    "Anthropic": {
      "Model": "claude-sonnet-4-5-20250929",
      "MaxTokens": 1024,
      "Temperature": 0.3
    }
  },
  "Serilog": {
    "MinimumLevel": "Information",
    "FileSink": {
      "Enabled": true,
      "Path": "logs/cce-.log",
      "RetainedDays": 7
    }
  }
}
```

Merge into the existing root object. Do not remove existing keys.

- [ ] **Step 1:** Create `AnthropicOptions.cs` with the contents above.

- [ ] **Step 2:** Create `AssistantClientFactory.cs` with the contents above.

- [ ] **Step 3:** Modify `Infrastructure/DependencyInjection.cs` to call the factory:
  ```bash
  cd /Users/m/CCE && grep -n "ISmartAssistantClient" backend/src/CCE.Infrastructure/DependencyInjection.cs
  ```
  Replace the `services.AddScoped<ISmartAssistantClient, SmartAssistantClient>();` line with `services.AddCceAssistantClient(configuration);`. Add `using CCE.Infrastructure.Assistant;` to the top of the file if not already present.

- [ ] **Step 4:** Add the new config blocks to both `appsettings.json` files. Validate JSON:
  ```bash
  python3 -m json.tool < backend/src/CCE.Api.External/appsettings.json > /dev/null
  python3 -m json.tool < backend/src/CCE.Api.Internal/appsettings.json > /dev/null
  ```
  Expected: no errors.

- [ ] **Step 5:** Build:
  ```bash
  cd backend && dotnet build
  ```
  Expected: success. The stub is still wired; no behavioural change.

- [ ] **Step 6:** Run application tests to ensure nothing broke:
  ```bash
  cd backend && dotnet test tests/CCE.Application.Tests/
  ```
  Expected: 429 passing.

- [ ] **Step 7:** Commit:
  ```bash
  git add backend/src/CCE.Infrastructure/Assistant/AnthropicOptions.cs \
          backend/src/CCE.Infrastructure/Assistant/AssistantClientFactory.cs \
          backend/src/CCE.Infrastructure/DependencyInjection.cs \
          backend/src/CCE.Api.External/appsettings.json \
          backend/src/CCE.Api.Internal/appsettings.json
  git -c commit.gpgsign=false commit -m "feat(infrastructure): AssistantClientFactory + Sub-10a config sections

  Routes ISmartAssistantClient registration through a new factory
  that Phase 02 Task 2.4 will flip to honour Assistant:Provider.
  Phase 00 always registers the existing stub. Adds AnthropicOptions
  config record + Assistant: + Serilog: sections to both APIs'
  appsettings.json (defaults Provider=stub, FileSink.Enabled=true).

  429 application tests still passing. Sub-10a Phase 00 Task 0.4.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 0.5: Document Phase 00 env-vars

**Files:**
- Create: `docs/superpowers/plans/2026-05-03-sub-10a/env-vars.md`.

**Purpose:** every environment variable Sub-10a introduces gets documented in one place that Phase 01's Dockerfiles + docker-compose, Phase 02's wiring code, and Phase 03's CI workflows all reference.

**Final state of file:**

```markdown
# Sub-10a — Environment variables

| Variable | Used by | Maps to / effect |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | both APIs | `Production` in prod images |
| `ASPNETCORE_URLS` | both APIs | `http://+:8080` in prod images |
| `ConnectionStrings__Default` | both APIs | SQL Server connection |
| `ConnectionStrings__Redis` | both APIs | Redis connection (cache) |
| `ASSISTANT_PROVIDER` | `Api.External` | overrides `Assistant:Provider`. Values: `stub` (default) or `anthropic`. |
| `ANTHROPIC_API_KEY` | `Api.External` | required when `ASSISTANT_PROVIDER=anthropic`. Absent → factory falls back to stub with a warn log. |
| `SENTRY_DSN` | both APIs | optional. Absent → Sentry sink is a no-op. |
| `LOG_LEVEL` | both APIs | overrides `Serilog:MinimumLevel`. Default `Information`. |

**Never committed:** `ANTHROPIC_API_KEY`, `SENTRY_DSN`. These are supplied at runtime via the deploy environment (Sub-10b will document the secret-supply mechanism).

**Defaults:** every variable has a safe default. CI runs without any of them set.
```

- [ ] **Step 1:** Create the file with the contents above.

- [ ] **Step 2:** Commit:
  ```bash
  git add docs/superpowers/plans/2026-05-03-sub-10a/env-vars.md
  git -c commit.gpgsign=false commit -m "docs(sub-10a): document Phase 00 env-vars

  Single reference table for every env-var Sub-10a introduces. Phase 01
  Dockerfiles, Phase 02 wiring, and Phase 03 CI workflows all link
  back to this file. Sub-10a Phase 00 Task 0.5.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Phase 00 close-out

After Task 0.5 commits cleanly:

- [ ] **Run the full check:**
  ```bash
  cd backend && dotnet build && dotnet test tests/CCE.Application.Tests/
  cd /Users/m/CCE/frontend && ./node_modules/.bin/nx test web-portal --watch=false \
                && ./node_modules/.bin/nx run web-portal:lint \
                && ./node_modules/.bin/nx build web-portal
  ```
  Expected: backend build success + 429 tests passing; frontend 502 tests + lint clean + build success.

- [ ] **Smoke-check that the new factory still resolves the stub:** start `Api.External` locally and confirm `/api/assistant/query` still emits the existing `[STUB]` placeholder. (No env-vars set → factory returns `SmartAssistantClient`.)

- [ ] **Hand off to Phase 01.** Phase 01 writes the four production Dockerfiles + nginx config + `docker-compose.prod.yml` + the new `docker-build` CI job. Plan file: `phase-01-docker-images.md` (to be written when we're ready to start it).

**Phase 00 done when:**
- 5 commits land on `main`, each green.
- `Anthropic.SDK` is in `Directory.Packages.props` but no project references it yet.
- `LoggingExtensions.UseCceSerilog` and `PrometheusExtensions.UseCcePrometheus` exist as no-op pass-throughs.
- `AssistantClientFactory.AddCceAssistantClient` is wired into `Infrastructure/DependencyInjection.cs` returning the existing stub.
- `appsettings.json` (both APIs) includes `Assistant:` and `Serilog:` sections with safe defaults.
- `docs/superpowers/plans/2026-05-03-sub-10a/env-vars.md` documents every Sub-10a env-var.
- Test counts unchanged (frontend 502; backend application 429).
