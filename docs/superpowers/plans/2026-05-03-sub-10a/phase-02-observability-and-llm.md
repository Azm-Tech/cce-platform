# Phase 02 — Observability + real LLM (Sub-10a)

> Parent: [`../2026-05-03-sub-10a.md`](../2026-05-03-sub-10a.md) · Spec: [`../../specs/2026-05-03-sub-10a-design.md`](../../specs/2026-05-03-sub-10a-design.md) §3 (data contracts), §5 (components)

**Phase goal:** Fill the Phase 00 skeletons with real behaviour. Wire `UseCceSerilog` into both APIs (console JSON + rolling-file + optional Sentry sink, all reading from the existing `Serilog:*` config + `SENTRY_DSN` env-var). Wire `UseCcePrometheus` into both APIs with `/metrics` + the two custom assistant counters. Implement `CitationSearch` (RAG-lite token-overlap scoring against `Resources` + `KnowledgeMapNodes`). Implement `AnthropicSmartAssistantClient` (the existing `Anthropic.SDK` 5.0.0 streaming API) and flip `AssistantClientFactory` to honour `Assistant:Provider` + `ANTHROPIC_API_KEY` env-var.

**Tasks:** 5
**Working directory:** `/Users/m/CCE/`
**Preconditions:** Phase 01 closed (commit `53c8c9d` or later); all four production images build and pass smoke probes; backend `dotnet test tests/CCE.Application.Tests/` passes (429).

---

## Task 2.1: Wire `UseCceSerilog` into both API hosts

**Files:**
- Modify: `backend/src/CCE.Api.Common/Observability/LoggingExtensions.cs` — replace the no-op skeleton with real wiring.
- Modify: `backend/src/CCE.Api.Common/CCE.Api.Common.csproj` — `<PackageReference>` for Serilog.AspNetCore + Serilog.Sinks.Console + Serilog.Sinks.File + Serilog.Formatting.Compact + Sentry.Serilog.
- Modify: `backend/src/CCE.Api.External/Program.cs` — call `builder.Host.UseCceSerilog()`.
- Modify: `backend/src/CCE.Api.Internal/Program.cs` — same.

**Implementation:** the existing `CorrelationIdMiddleware` uses `_logger.BeginScope()` to attach `CorrelationId` to the logging context. With `Serilog.AspNetCore`'s default request-logging configured + `Enrichers.FromLogContext()`, the correlation id flows into every event automatically. No custom enricher needed.

**Final state of `LoggingExtensions.cs`:**

```cs
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Sentry;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace CCE.Api.Common.Observability;

/// <summary>
/// Host-level Serilog wiring. Console JSON-compact for kubectl/docker logs,
/// rolling-file (daily) for on-host inspection, optional Sentry sink for
/// warning+ events when SENTRY_DSN is set. Correlation-id flows from the
/// existing CorrelationIdMiddleware via BeginScope + FromLogContext.
/// </summary>
public static class LoggingExtensions
{
    public static IHostBuilder UseCceSerilog(this IHostBuilder builder)
    {
        return builder.UseSerilog((ctx, services, cfg) =>
        {
            var minLevel = ParseLevel(ctx.Configuration["Serilog:MinimumLevel"])
                ?? LogEventLevel.Information;
            var fileEnabled = ctx.Configuration.GetValue<bool>("Serilog:FileSink:Enabled");
            var filePath = ctx.Configuration["Serilog:FileSink:Path"] ?? "logs/cce-.log";
            var retainedDays = ctx.Configuration.GetValue<int?>("Serilog:FileSink:RetainedDays") ?? 7;
            var sentryDsn = ctx.Configuration["SENTRY_DSN"]
                         ?? Environment.GetEnvironmentVariable("SENTRY_DSN");

            cfg
                .MinimumLevel.Is(minLevel)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("app", ctx.HostingEnvironment.ApplicationName)
                .Enrich.WithProperty("env", ctx.HostingEnvironment.EnvironmentName)
                .WriteTo.Console(new CompactJsonFormatter());

            if (fileEnabled)
            {
                cfg.WriteTo.File(
                    formatter: new CompactJsonFormatter(),
                    path: filePath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: retainedDays);
            }

            if (!string.IsNullOrWhiteSpace(sentryDsn))
            {
                cfg.WriteTo.Sentry(o =>
                {
                    o.Dsn = sentryDsn;
                    o.MinimumEventLevel = LogEventLevel.Warning;
                    o.MinimumBreadcrumbLevel = LogEventLevel.Information;
                });
            }
        });
    }

    private static LogEventLevel? ParseLevel(string? value)
        => Enum.TryParse<LogEventLevel>(value, ignoreCase: true, out var lvl) ? lvl : null;
}
```

**`CCE.Api.Common.csproj`** additions (inside the existing `<ItemGroup>` for package refs):
```xml
<PackageReference Include="Serilog.AspNetCore" />
<PackageReference Include="Serilog.Sinks.Console" />
<PackageReference Include="Serilog.Sinks.File" />
<PackageReference Include="Serilog.Formatting.Compact" />
<PackageReference Include="Sentry.Serilog" />
```

**`Program.cs` modification (both APIs):** add `builder.Host.UseCceSerilog();` immediately after `var builder = WebApplication.CreateBuilder(args);` plus `using CCE.Api.Common.Observability;` at the top.

Also add `app.UseSerilogRequestLogging();` immediately after `app.UseMiddleware<CorrelationIdMiddleware>();` so per-request log lines include the response status, elapsed time, etc., enriched with the correlation id from `BeginScope`.

- [ ] **Step 1:** Add the Serilog `<PackageReference>` lines to `CCE.Api.Common.csproj`.

- [ ] **Step 2:** Replace `LoggingExtensions.cs` with the implementation above.

- [ ] **Step 3:** Modify both APIs' `Program.cs`:
  - Add `using CCE.Api.Common.Observability;` near other usings.
  - Add `builder.Host.UseCceSerilog();` after `WebApplication.CreateBuilder`.
  - Add `app.UseSerilogRequestLogging();` after the correlation-id middleware (which preserves the `BeginScope` so request log includes `CorrelationId`).

- [ ] **Step 4:** Build:
  ```bash
  cd backend && dotnet build
  ```
  Expected: success.

- [ ] **Step 5:** Smoke-run `Api.External` and verify a structured JSON log on stdout:
  ```bash
  cd backend && (Keycloak__Authority=http://localhost:8080/realms/cce \
    Keycloak__Audience=cce-api Keycloak__RequireHttpsMetadata=false \
    Infrastructure__SqlConnectionString="Server=localhost;Database=CCE;Integrated Security=True" \
    Infrastructure__RedisConnectionString="localhost:6379" \
    Serilog__FileSink__Enabled=false \
    timeout 5 dotnet run --project src/CCE.Api.External --no-launch-profile 2>&1 | head -5)
  ```
  Expected: each log line is a `{"@t":"...","@l":"Information",...}` JSON event. (No need to actually serve traffic — startup logs are enough to confirm the sink is wired.)

- [ ] **Step 6:** Commit:
  ```bash
  git add backend/src/CCE.Api.Common/CCE.Api.Common.csproj \
          backend/src/CCE.Api.Common/Observability/LoggingExtensions.cs \
          backend/src/CCE.Api.External/Program.cs \
          backend/src/CCE.Api.Internal/Program.cs
  git -c commit.gpgsign=false commit -m "feat(api-common): wire Serilog into both API hosts

  UseCceSerilog: console (JSON-compact), rolling-file daily with
  RetainedDays retention (when Serilog:FileSink:Enabled), optional
  Sentry sink for warning+ events when SENTRY_DSN env-var is set.
  Microsoft.AspNetCore + EFCore noise filtered to Warning+ by default.

  app.UseSerilogRequestLogging() picks up CorrelationId from the
  existing middleware's BeginScope so per-request log lines carry
  it automatically.

  Sub-10a Phase 02 Task 2.1.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 2.2: Wire `UseCcePrometheus` into both API hosts + custom counters

**Files:**
- Modify: `backend/src/CCE.Api.Common/Observability/PrometheusExtensions.cs`.
- Modify: `backend/src/CCE.Api.Common/CCE.Api.Common.csproj` — `<PackageReference>` for prometheus-net + prometheus-net.AspNetCore.
- Modify: both `Program.cs` files — call `app.UseCcePrometheus()`.

**Final state of `PrometheusExtensions.cs`:**

```cs
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Prometheus;

namespace CCE.Api.Common.Observability;

/// <summary>
/// Prometheus middleware + /metrics endpoint. Custom assistant counters
/// (cce_assistant_streams_total{provider} and
/// cce_assistant_citations_total{kind}) are exposed for the LLM
/// observability story.
/// </summary>
public static class PrometheusExtensions
{
    public static readonly Counter AssistantStreamsTotal = Metrics
        .CreateCounter(
            "cce_assistant_streams_total",
            "Total assistant stream requests, labeled by provider.",
            new CounterConfiguration { LabelNames = new[] { "provider" } });

    public static readonly Counter AssistantCitationsTotal = Metrics
        .CreateCounter(
            "cce_assistant_citations_total",
            "Total citations emitted by the assistant, labeled by kind.",
            new CounterConfiguration { LabelNames = new[] { "kind" } });

    public static WebApplication UseCcePrometheus(this WebApplication app)
    {
        app.UseHttpMetrics();
        app.MapMetrics("/metrics").AllowAnonymous();
        return app;
    }
}
```

**`Program.cs` modifications (both APIs):** add `app.UseCcePrometheus();` after `app.UseAuthentication() / UseAuthorization()` block (so `/metrics` requests pass through auth as anonymous).

- [ ] **Step 1:** Add `<PackageReference Include="prometheus-net" />` and `<PackageReference Include="prometheus-net.AspNetCore" />` to `CCE.Api.Common.csproj`.

- [ ] **Step 2:** Replace `PrometheusExtensions.cs` with the implementation above.

- [ ] **Step 3:** Modify both `Program.cs` files — add `app.UseCcePrometheus();` after the auth middleware lines.

- [ ] **Step 4:** Build:
  ```bash
  cd backend && dotnet build
  ```

- [ ] **Step 5:** Smoke-run + probe `/metrics`:
  ```bash
  Keycloak__Authority=http://localhost:8080/realms/cce \
  Keycloak__Audience=cce-api Keycloak__RequireHttpsMetadata=false \
  Infrastructure__SqlConnectionString="Server=localhost;Database=CCE;Integrated Security=True" \
  Infrastructure__RedisConnectionString="localhost:6379" \
  ASPNETCORE_URLS=http://localhost:18090 \
  dotnet run --project backend/src/CCE.Api.External --no-launch-profile &
  PID=$!
  for i in $(seq 1 15); do
    sleep 2
    if curl -fsS http://localhost:18090/metrics | head -3 | grep -q '^# HELP'; then
      echo "PASS"; break
    fi
    [ $i -eq 15 ] && echo "FAIL"
  done
  kill $PID 2>/dev/null; wait $PID 2>/dev/null
  ```
  Expected: `PASS`.

- [ ] **Step 6:** Commit:
  ```bash
  git add backend/src/CCE.Api.Common/CCE.Api.Common.csproj \
          backend/src/CCE.Api.Common/Observability/PrometheusExtensions.cs \
          backend/src/CCE.Api.External/Program.cs \
          backend/src/CCE.Api.Internal/Program.cs
  git -c commit.gpgsign=false commit -m "feat(api-common): wire Prometheus into both API hosts

  /metrics endpoint via prometheus-net.AspNetCore exposes default
  http_request_duration_seconds histogram + 2 custom counters:
    cce_assistant_streams_total{provider}
    cce_assistant_citations_total{kind}

  Endpoint is AllowAnonymous so Prometheus scraping doesn't need to
  carry auth credentials. Sub-10a Phase 02 Task 2.2.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 2.3: Implement `CitationSearch` (RAG-lite)

**Files:**
- Create: `backend/src/CCE.Infrastructure/Assistant/CitationSearch.cs`.
- Create: `backend/src/CCE.Application/Assistant/ICitationSearch.cs`.
- Create: `backend/tests/CCE.Application.Tests/Assistant/CitationSearchTests.cs`.

**Algorithm:** token-overlap (Jaccard) scoring.
1. Tokenize the user question + assistant reply by lowercasing and splitting on non-word chars; drop tokens shorter than 3 chars; drop a small stopword list.
2. For each row in `Resources` and `KnowledgeMapNodes`, tokenize the locale-appropriate title field (`TitleEn`/`TitleAr` for resources, `NameEn`/`NameAr` for nodes) the same way.
3. Score = `|query_tokens ∩ title_tokens| / |query_tokens ∪ title_tokens|`. Skip rows with score 0.
4. Return the top 1 of each kind, sorted by score descending.

**`ICitationSearch.cs`** (interface lives in Application so the stub can implement a no-op alternative without depending on Infrastructure):

```cs
namespace CCE.Application.Assistant;

public interface ICitationSearch
{
    Task<IReadOnlyList<CitationDto>> FindCitationsAsync(
        string userQuestion,
        string assistantReply,
        string locale,
        CancellationToken ct);
}
```

**`CitationSearch.cs`** (production implementation):

```cs
using CCE.Application.Assistant;
using CCE.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Assistant;

/// <summary>
/// RAG-lite citation source. Token-overlap (Jaccard) scoring against
/// Resources and KnowledgeMapNodes. Returns up to 1 of each kind.
/// </summary>
public sealed class CitationSearch : ICitationSearch
{
    private static readonly HashSet<string> Stopwords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "and", "for", "with", "from", "about", "what", "how", "why",
        "this", "that", "these", "those", "are", "was", "were", "have", "has",
        "في", "من", "على", "عن", "هذا", "هذه", "ذلك", "تلك", // Arabic stopwords
    };

    private readonly ICceDbContext _db;

    public CitationSearch(ICceDbContext db) => _db = db;

    public async Task<IReadOnlyList<CitationDto>> FindCitationsAsync(
        string userQuestion, string assistantReply, string locale, CancellationToken ct)
    {
        var queryTokens = Tokenize($"{userQuestion} {assistantReply}");
        if (queryTokens.Count == 0) return Array.Empty<CitationDto>();

        var isAr = string.Equals(locale, "ar", StringComparison.OrdinalIgnoreCase);

        var results = new List<CitationDto>(2);

        // Resources (top 1)
        var resources = await _db.Resources
            .Select(r => new { r.Id, Title = isAr ? r.TitleAr : r.TitleEn })
            .ToListAsync(ct).ConfigureAwait(false);
        var bestResource = ScoreTopOne(resources, queryTokens, r => r.Title);
        if (bestResource is not null)
        {
            results.Add(new CitationDto(
                Id: bestResource.Id.ToString(),
                Kind: "resource",
                Title: bestResource.Title,
                Href: $"/knowledge-center/resources/{bestResource.Id}",
                SourceText: null));
        }

        // Knowledge-map nodes (top 1)
        var nodes = await _db.KnowledgeMapNodes
            .Select(n => new { n.Id, n.MapId, Title = isAr ? n.NameAr : n.NameEn })
            .ToListAsync(ct).ConfigureAwait(false);
        var bestNode = ScoreTopOne(nodes, queryTokens, n => n.Title);
        if (bestNode is not null)
        {
            results.Add(new CitationDto(
                Id: bestNode.Id.ToString(),
                Kind: "map-node",
                Title: bestNode.Title,
                Href: $"/knowledge-maps/{bestNode.MapId}?node={bestNode.Id}",
                SourceText: null));
        }

        return results;
    }

    private static T? ScoreTopOne<T>(
        IEnumerable<T> rows,
        IReadOnlySet<string> queryTokens,
        Func<T, string> titleSelector) where T : class
    {
        T? best = null;
        double bestScore = 0.0;
        foreach (var row in rows)
        {
            var rowTokens = Tokenize(titleSelector(row));
            if (rowTokens.Count == 0) continue;
            var intersection = queryTokens.Intersect(rowTokens).Count();
            if (intersection == 0) continue;
            var union = queryTokens.Union(rowTokens).Count();
            var score = (double)intersection / union;
            if (score > bestScore)
            {
                bestScore = score;
                best = row;
            }
        }
        return best;
    }

    private static IReadOnlySet<string> Tokenize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new HashSet<string>();
        var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var current = new System.Text.StringBuilder();
        foreach (var ch in text)
        {
            if (char.IsLetterOrDigit(ch))
            {
                current.Append(char.ToLowerInvariant(ch));
            }
            else if (current.Length > 0)
            {
                Add(tokens, current.ToString());
                current.Clear();
            }
        }
        if (current.Length > 0) Add(tokens, current.ToString());
        return tokens;
    }

    private static void Add(HashSet<string> tokens, string token)
    {
        if (token.Length < 3) return;
        if (Stopwords.Contains(token)) return;
        tokens.Add(token);
    }
}
```

**`CitationSearchTests.cs`** — uses an in-memory `DbContext` from the existing test infrastructure:

```cs
using CCE.Application.Common.Interfaces;
using CCE.Domain.Content;
using CCE.Domain.KnowledgeMaps;
using CCE.Infrastructure.Assistant;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.Tests.Assistant;

public class CitationSearchTests : IDisposable
{
    private readonly CceDbContext _db;
    private readonly CitationSearch _sut;

    public CitationSearchTests()
    {
        var opts = new DbContextOptionsBuilder<CceDbContext>()
            .UseInMemoryDatabase($"citation-search-{Guid.NewGuid()}")
            .Options;
        _db = new CceDbContext(opts);
        _sut = new CitationSearch(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Returns_empty_when_no_query_tokens()
    {
        var result = await _sut.FindCitationsAsync("", "", "en", default);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Returns_empty_when_no_rows_match()
    {
        // No resources or nodes seeded
        var result = await _sut.FindCitationsAsync("solar panels", "", "en", default);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Picks_resource_with_highest_token_overlap()
    {
        SeedResource(Guid.NewGuid(), "Solar Panel Installation Guide", "Solar Panel Installation Guide");
        SeedResource(Guid.NewGuid(), "Wind Turbine Maintenance", "Wind Turbine Maintenance");
        await _db.SaveChangesAsync();

        var result = await _sut.FindCitationsAsync("How do I install solar panels?", "", "en", default);

        result.Should().ContainSingle(c => c.Kind == "resource");
        var resource = result.Single(c => c.Kind == "resource");
        resource.Title.Should().Contain("Solar");
    }

    [Fact]
    public async Task Picks_map_node_with_highest_token_overlap()
    {
        SeedMapNode(Guid.NewGuid(), Guid.NewGuid(), "Carbon Capture", "احتجاز الكربون");
        SeedMapNode(Guid.NewGuid(), Guid.NewGuid(), "Renewable Energy", "طاقة متجددة");
        await _db.SaveChangesAsync();

        var result = await _sut.FindCitationsAsync("Tell me about carbon capture", "", "en", default);

        result.Should().ContainSingle(c => c.Kind == "map-node");
        result.Single(c => c.Kind == "map-node").Title.Should().Be("Carbon Capture");
    }

    [Fact]
    public async Task Locale_ar_uses_Arabic_title_fields()
    {
        var nodeId = Guid.NewGuid();
        SeedMapNode(nodeId, Guid.NewGuid(), "Carbon Capture", "احتجاز الكربون");
        await _db.SaveChangesAsync();

        var result = await _sut.FindCitationsAsync("احتجاز الكربون", "", "ar", default);

        result.Single(c => c.Kind == "map-node").Title.Should().Be("احتجاز الكربون");
    }

    [Fact]
    public async Task Returns_at_most_one_of_each_kind()
    {
        for (int i = 0; i < 5; i++)
        {
            SeedResource(Guid.NewGuid(), $"Solar Resource {i}", $"Solar Resource {i}");
            SeedMapNode(Guid.NewGuid(), Guid.NewGuid(), $"Solar Node {i}", $"Solar Node {i}");
        }
        await _db.SaveChangesAsync();

        var result = await _sut.FindCitationsAsync("solar", "", "en", default);
        result.Count(c => c.Kind == "resource").Should().Be(1);
        result.Count(c => c.Kind == "map-node").Should().Be(1);
    }

    private void SeedResource(Guid id, string titleEn, string titleAr)
    {
        // The Resource aggregate has private setters; use reflection-based shape
        // mirroring the existing seeders. Implementer should use the right
        // factory call once the ctor signature is read at execution time.
        // (See backend/src/CCE.Seeder/Seeders/ for the current pattern.)
        // For the InMemory test we can use a minimal set of fields the
        // CitationSearch query selects: Id + TitleEn + TitleAr.
        var resource = (Resource)System.Runtime.Serialization.FormatterServices
            .GetUninitializedObject(typeof(Resource));
        SetProp(resource, "Id", id);
        SetProp(resource, "TitleEn", titleEn);
        SetProp(resource, "TitleAr", titleAr);
        _db.Resources.Add(resource);
    }

    private void SeedMapNode(Guid id, Guid mapId, string nameEn, string nameAr)
    {
        var node = (KnowledgeMapNode)System.Runtime.Serialization.FormatterServices
            .GetUninitializedObject(typeof(KnowledgeMapNode));
        SetProp(node, "Id", id);
        SetProp(node, "MapId", mapId);
        SetProp(node, "NameEn", nameEn);
        SetProp(node, "NameAr", nameAr);
        _db.KnowledgeMapNodes.Add(node);
    }

    private static void SetProp<T>(T obj, string name, object value)
    {
        typeof(T).GetProperty(name)!.SetValue(obj, value);
    }
}
```

(Implementation note: the test uses uninitialized-object reflection to bypass the aggregate constructors. If `CCE.Application.Tests` already has a `Resource`-builder helper, prefer that. The test only relies on the `Id` + title properties the query projects, so deeper invariants are skipped.)

- [ ] **Step 1:** Create `ICitationSearch.cs` in `CCE.Application/Assistant/`.

- [ ] **Step 2:** Create `CitationSearch.cs` in `CCE.Infrastructure/Assistant/`.

- [ ] **Step 3:** Register the service in `Infrastructure/DependencyInjection.cs`:
  ```cs
  services.AddScoped<ICitationSearch, CitationSearch>();
  ```
  Place this near the existing assistant client registration.

- [ ] **Step 4:** Create the test file. The test uses `EF Core InMemory` provider — verify `Microsoft.EntityFrameworkCore.InMemory` is in `Directory.Packages.props` (it is, line 50) and add a `<PackageReference>` to `CCE.Application.Tests.csproj` if not already present.

- [ ] **Step 5:** Run tests:
  ```bash
  cd backend && dotnet test tests/CCE.Application.Tests/ --filter FullyQualifiedName~CitationSearchTests
  ```
  Expected: 6 passing.

- [ ] **Step 6:** Commit:
  ```bash
  git add backend/src/CCE.Application/Assistant/ICitationSearch.cs \
          backend/src/CCE.Infrastructure/Assistant/CitationSearch.cs \
          backend/src/CCE.Infrastructure/DependencyInjection.cs \
          backend/tests/CCE.Application.Tests/Assistant/CitationSearchTests.cs \
          backend/tests/CCE.Application.Tests/CCE.Application.Tests.csproj
  git -c commit.gpgsign=false commit -m "feat(infrastructure): RAG-lite CitationSearch

  Token-overlap (Jaccard) scoring against Resources and
  KnowledgeMapNodes. Returns up to 1 of each kind, locale-aware
  (TitleEn/Ar, NameEn/Ar). Tokenizer drops <3-char tokens and a
  small EN+AR stopword list. AnthropicSmartAssistantClient (Task 2.4)
  calls this after the LLM stream completes to attach citations.

  6 unit tests passing against EF Core InMemory.
  Sub-10a Phase 02 Task 2.3.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 2.4: Implement `AnthropicSmartAssistantClient` + tests

**Files:**
- Create: `backend/src/CCE.Infrastructure/Assistant/AnthropicSmartAssistantClient.cs`.
- Create: `backend/tests/CCE.Infrastructure.Tests/Assistant/AnthropicSmartAssistantClientTests.cs`.
- Modify: `backend/src/CCE.Infrastructure/CCE.Infrastructure.csproj` — `<PackageReference Include="Anthropic.SDK" />`.

**Anthropic.SDK 5.0.0 surface (verify at execution time):** the SDK exposes `AnthropicClient` constructed with `new APIAuthentication(apiKey)`, and `client.Messages.GetClaudeMessageAsync(...)` for non-streaming or `client.Messages.GetClaudeMessageAsyncEnumerable(...)` for streaming. The streaming method yields `MessageResponse` objects whose `Delta?.Text` carries chunks.

**Implementation pseudocode (adjust to match the SDK at execution time):**

```cs
using System.Runtime.CompilerServices;
using Anthropic.SDK;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;
using CCE.Application.Assistant;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prometheus;

namespace CCE.Infrastructure.Assistant;

public sealed class AnthropicSmartAssistantClient : ISmartAssistantClient
{
    private static readonly Counter StreamsCounter = Metrics.CreateCounter(
        "cce_assistant_streams_total", "Stream calls.",
        new CounterConfiguration { LabelNames = new[] { "provider" } });
    private static readonly Counter CitationsCounter = Metrics.CreateCounter(
        "cce_assistant_citations_total", "Citations emitted.",
        new CounterConfiguration { LabelNames = new[] { "kind" } });

    private readonly AnthropicClient _client;
    private readonly AnthropicOptions _options;
    private readonly ICitationSearch _citationSearch;
    private readonly ILogger<AnthropicSmartAssistantClient> _logger;

    public AnthropicSmartAssistantClient(
        AnthropicClient client,
        IOptions<AnthropicOptions> options,
        ICitationSearch citationSearch,
        ILogger<AnthropicSmartAssistantClient> logger)
    {
        _client = client;
        _options = options.Value;
        _citationSearch = citationSearch;
        _logger = logger;
    }

    public async IAsyncEnumerable<SseEvent> StreamAsync(
        IReadOnlyList<ChatMessage> messages,
        string locale,
        [EnumeratorCancellation] CancellationToken ct)
    {
        StreamsCounter.WithLabels("anthropic").Inc();

        var lastUser = messages.LastOrDefault(m => m.Role == "user")?.Content ?? "";
        var sdkMessages = messages.Select(m => new Message
        {
            Role = m.Role == "assistant" ? RoleType.Assistant : RoleType.User,
            Content = new List<ContentBase> { new TextContent { Text = m.Content } },
        }).ToList();

        var systemPrompt =
            $"You are the CCE Knowledge Center assistant. Answer in {(locale == "ar" ? "Arabic" : "English")}. " +
            "Be concise (2-4 sentences). Topics relate to circular carbon economy.";

        var parameters = new MessageParameters
        {
            Messages = sdkMessages,
            Model = _options.Model,
            MaxTokens = _options.MaxTokens,
            Temperature = (decimal)_options.Temperature,
            System = new List<SystemMessage> { new() { Text = systemPrompt } },
            Stream = true,
        };

        var assistantText = new System.Text.StringBuilder();
        var streamFailed = false;

        IAsyncEnumerable<MessageResponse> sdkStream;
        try
        {
            sdkStream = _client.Messages.StreamClaudeMessageAsync(parameters, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Anthropic stream open failed");
            yield return new ErrorEvent(new ErrorPayload("server"));
            yield break;
        }

        await foreach (var resp in WithErrorHandling(sdkStream, ct))
        {
            if (resp is null) { streamFailed = true; break; }
            var text = resp.Delta?.Text;
            if (!string.IsNullOrEmpty(text))
            {
                assistantText.Append(text);
                yield return new TextEvent(text);
            }
        }

        if (streamFailed)
        {
            yield return new ErrorEvent(new ErrorPayload("server"));
            yield break;
        }

        // After the model finishes, attach RAG-lite citations.
        IReadOnlyList<CitationDto> citations;
        try
        {
            citations = await _citationSearch.FindCitationsAsync(
                lastUser, assistantText.ToString(), locale, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Citation search failed; continuing without citations");
            citations = Array.Empty<CitationDto>();
        }

        foreach (var c in citations)
        {
            CitationsCounter.WithLabels(c.Kind).Inc();
            yield return new CitationEvent(c);
        }

        yield return new DoneEvent();
    }

    /// <summary>
    /// Wraps the SDK stream so an exception mid-stream becomes a single
    /// null sentinel instead of bubbling — lets the outer iterator yield
    /// an ErrorEvent before terminating.
    /// </summary>
    private async IAsyncEnumerable<MessageResponse?> WithErrorHandling(
        IAsyncEnumerable<MessageResponse> source,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var iterator = source.GetAsyncEnumerator(ct);
        try
        {
            while (true)
            {
                MessageResponse? next = null;
                bool failed = false;
                try
                {
                    if (!await iterator.MoveNextAsync().ConfigureAwait(false)) yield break;
                    next = iterator.Current;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Anthropic stream error mid-flight");
                    failed = true;
                }

                if (failed) { yield return null; yield break; }
                yield return next;
            }
        }
        finally
        {
            await iterator.DisposeAsync().ConfigureAwait(false);
        }
    }
}
```

(SDK class names like `Message`, `RoleType`, `ContentBase`, `TextContent`, `MessageParameters`, `MessageResponse`, `StreamClaudeMessageAsync` are best-guess for `Anthropic.SDK 5.0.0`. The implementer should `dotnet build` after writing the file and adjust based on compile errors.)

**Test file** uses a mocked `AnthropicClient` — but the SDK doesn't make `Messages` virtual, so we wrap it behind an interface for testability:

Actually, simpler approach: wrap the SDK's stream method behind an internal interface `IAnthropicStreamProvider` that the prod code injects, and pass a fake one in tests. Add this to the same file:

```cs
public interface IAnthropicStreamProvider
{
    IAsyncEnumerable<MessageResponse> StreamClaudeMessageAsync(
        MessageParameters parameters,
        CancellationToken ct);
}

internal sealed class AnthropicStreamProvider : IAnthropicStreamProvider
{
    private readonly AnthropicClient _client;
    public AnthropicStreamProvider(AnthropicClient client) => _client = client;
    public IAsyncEnumerable<MessageResponse> StreamClaudeMessageAsync(
        MessageParameters parameters, CancellationToken ct)
        => _client.Messages.StreamClaudeMessageAsync(parameters, ct);
}
```

The `AnthropicSmartAssistantClient` constructor takes `IAnthropicStreamProvider` instead of the raw client. DI registers `IAnthropicStreamProvider → AnthropicStreamProvider` and the underlying `AnthropicClient` as a singleton.

**`AnthropicSmartAssistantClientTests.cs`** (mocks the stream provider + citation search; only tests the orchestration, not Anthropic itself):

```cs
using CCE.Application.Assistant;
using CCE.Infrastructure.Assistant;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CCE.Infrastructure.Tests.Assistant;

public class AnthropicSmartAssistantClientTests
{
    [Fact]
    public async Task Yields_text_chunks_then_citations_then_done()
    {
        var provider = new FakeStreamProvider(yields:
            FakeYield(text: "Hello "),
            FakeYield(text: "world"));
        var citationSearch = new FakeCitationSearch(citations:
            new CitationDto("r1", "resource", "Some Resource", "/x", null));
        var sut = new AnthropicSmartAssistantClient(
            provider, OptionsFor(new AnthropicOptions()), citationSearch, NullLogger<AnthropicSmartAssistantClient>.Instance);

        var events = new List<SseEvent>();
        await foreach (var e in sut.StreamAsync(
            new[] { new ChatMessage("user", "hi") }, "en", default))
        {
            events.Add(e);
        }

        events.OfType<TextEvent>().Select(t => t.Content).Should().Equal("Hello ", "world");
        events.OfType<CitationEvent>().Should().ContainSingle();
        events.Last().Should().BeOfType<DoneEvent>();
    }

    [Fact]
    public async Task Stream_open_failure_yields_ErrorEvent()
    {
        var provider = new FakeStreamProvider(throwOnOpen: true);
        var sut = new AnthropicSmartAssistantClient(
            provider, OptionsFor(new AnthropicOptions()),
            new FakeCitationSearch(), NullLogger<AnthropicSmartAssistantClient>.Instance);

        var events = new List<SseEvent>();
        await foreach (var e in sut.StreamAsync(
            new[] { new ChatMessage("user", "hi") }, "en", default))
        {
            events.Add(e);
        }
        events.Should().ContainSingle().Which.Should().BeOfType<ErrorEvent>();
    }

    [Fact]
    public async Task Mid_stream_exception_yields_partial_text_then_ErrorEvent()
    {
        var provider = new FakeStreamProvider(yields:
            FakeYield(text: "Partial"),
            FakeYield(throwAfter: true));
        var sut = new AnthropicSmartAssistantClient(
            provider, OptionsFor(new AnthropicOptions()),
            new FakeCitationSearch(), NullLogger<AnthropicSmartAssistantClient>.Instance);

        var events = new List<SseEvent>();
        await foreach (var e in sut.StreamAsync(
            new[] { new ChatMessage("user", "hi") }, "en", default))
        {
            events.Add(e);
        }
        events.OfType<TextEvent>().Single().Content.Should().Be("Partial");
        events.Last().Should().BeOfType<ErrorEvent>();
    }

    [Fact]
    public async Task Citation_search_failure_continues_without_citations()
    {
        var provider = new FakeStreamProvider(yields: FakeYield(text: "ok"));
        var citationSearch = new FakeCitationSearch(throwOnSearch: true);
        var sut = new AnthropicSmartAssistantClient(
            provider, OptionsFor(new AnthropicOptions()),
            citationSearch, NullLogger<AnthropicSmartAssistantClient>.Instance);

        var events = new List<SseEvent>();
        await foreach (var e in sut.StreamAsync(
            new[] { new ChatMessage("user", "hi") }, "en", default))
        {
            events.Add(e);
        }
        events.OfType<CitationEvent>().Should().BeEmpty();
        events.Last().Should().BeOfType<DoneEvent>();
    }

    private static IOptions<T> OptionsFor<T>(T value) where T : class, new()
        => Microsoft.Extensions.Options.Options.Create(value);

    // Test fakes — in the same file or a separate Fakes/ folder. Simplified.
}
```

(Test fake classes for `IAnthropicStreamProvider` and `ICitationSearch` are written inline; concrete shape depends on SDK type names which the implementer reads from the `dotnet build` output at execution time.)

- [ ] **Step 1:** Add `<PackageReference Include="Anthropic.SDK" />` to `CCE.Infrastructure.csproj`.

- [ ] **Step 2:** Create `AnthropicSmartAssistantClient.cs` with the implementation. Build:
  ```bash
  cd backend && dotnet build src/CCE.Infrastructure/
  ```
  The build will fail with SDK type mismatches; adjust class/method/property names per the actual `Anthropic.SDK` 5.0.0 surface until it compiles.

- [ ] **Step 3:** Create the test file. Run:
  ```bash
  cd backend && dotnet test tests/CCE.Infrastructure.Tests/ --filter FullyQualifiedName~AnthropicSmartAssistantClientTests
  ```
  Expected: 4 passing.

- [ ] **Step 4:** Commit:
  ```bash
  git add backend/src/CCE.Infrastructure/CCE.Infrastructure.csproj \
          backend/src/CCE.Infrastructure/Assistant/AnthropicSmartAssistantClient.cs \
          backend/tests/CCE.Infrastructure.Tests/Assistant/AnthropicSmartAssistantClientTests.cs
  git -c commit.gpgsign=false commit -m "feat(infrastructure): AnthropicSmartAssistantClient

  Implements ISmartAssistantClient against Anthropic.SDK 5.0.0
  streaming. Yields TextEvent per content_block_delta, then queries
  CitationSearch for RAG-lite citations and yields CitationEvents,
  then DoneEvent. Stream-open failure yields ErrorEvent('server');
  mid-stream exception yields partial text + ErrorEvent. Citation
  search failure is logged Warning and the stream completes without
  citations. Increments cce_assistant_streams_total{provider=anthropic}
  and cce_assistant_citations_total{kind=...}.

  IAnthropicStreamProvider abstraction wraps the SDK so unit tests
  can mock the streaming behaviour. 4 tests passing.

  Sub-10a Phase 02 Task 2.4.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Task 2.5: Flip `AssistantClientFactory` to honour config + tests

**Files:**
- Modify: `backend/src/CCE.Infrastructure/Assistant/AssistantClientFactory.cs` — replace the always-stub Phase 00 implementation.
- Create: `backend/tests/CCE.Application.Tests/Assistant/AssistantClientFactoryTests.cs`.

**Final state of `AssistantClientFactory.cs`:**

```cs
using Anthropic.SDK;
using CCE.Application.Assistant;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CCE.Infrastructure.Assistant;

public static class AssistantClientFactory
{
    public const string AnthropicApiKeyEnvVar = "ANTHROPIC_API_KEY";

    public static IServiceCollection AddCceAssistantClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = configuration["Assistant:Provider"]?.Trim().ToLowerInvariant() ?? "stub";
        var apiKey = Environment.GetEnvironmentVariable(AnthropicApiKeyEnvVar)
                  ?? configuration[AnthropicApiKeyEnvVar];

        services.Configure<AnthropicOptions>(configuration.GetSection("Assistant:Anthropic"));

        if (provider == "anthropic" && !string.IsNullOrWhiteSpace(apiKey))
        {
            services.TryAddSingleton(sp => new AnthropicClient(apiKey));
            services.AddScoped<IAnthropicStreamProvider, AnthropicStreamProvider>();
            services.AddScoped<ISmartAssistantClient, AnthropicSmartAssistantClient>();
            return services;
        }

        if (provider == "anthropic" && string.IsNullOrWhiteSpace(apiKey))
        {
            // Log at startup. We can't resolve ILogger here directly; the host's
            // bootstrap logger picks up the warning when the service collection
            // is built. Use a temporary console-bridge factory.
            using var lf = LoggerFactory.Create(b => b.AddConsole());
            lf.CreateLogger(nameof(AssistantClientFactory))
                .LogWarning(
                    "Assistant:Provider is 'anthropic' but {EnvVar} is not set. " +
                    "Falling back to the stub assistant client.",
                    AnthropicApiKeyEnvVar);
        }

        services.AddScoped<ISmartAssistantClient, SmartAssistantClient>();
        return services;
    }
}
```

**Test file** uses `IServiceCollection` + `BuildServiceProvider()` to verify which implementation is registered:

```cs
using CCE.Application.Assistant;
using CCE.Infrastructure.Assistant;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CCE.Application.Tests.Assistant;

public class AssistantClientFactoryTests
{
    [Fact]
    public void Provider_stub_registers_stub_client()
    {
        var services = new ServiceCollection();
        var config = BuildConfig(("Assistant:Provider", "stub"));

        services.AddCceAssistantClient(config);

        services.AddSingleton<CCE.Application.Common.Interfaces.ICceDbContext>(_ => null!);
        services.AddSingleton<CCE.Domain.Common.ISystemClock>(_ => null!);
        // The stub takes ICceDbContext + ISystemClock + ILogger; we register
        // null-stubs because the tests verify only the registration, not behaviour.

        // Just check the type is registered.
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ISmartAssistantClient));
        descriptor.Should().NotBeNull();
        descriptor!.ImplementationType.Should().Be(typeof(SmartAssistantClient));
    }

    [Fact]
    public void Provider_anthropic_with_key_registers_Anthropic_client()
    {
        Environment.SetEnvironmentVariable(AssistantClientFactory.AnthropicApiKeyEnvVar, "sk-test");
        try
        {
            var services = new ServiceCollection();
            var config = BuildConfig(("Assistant:Provider", "anthropic"));

            services.AddCceAssistantClient(config);

            var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ISmartAssistantClient));
            descriptor!.ImplementationType.Should().Be(typeof(AnthropicSmartAssistantClient));
        }
        finally
        {
            Environment.SetEnvironmentVariable(AssistantClientFactory.AnthropicApiKeyEnvVar, null);
        }
    }

    [Fact]
    public void Provider_anthropic_without_key_falls_back_to_stub()
    {
        Environment.SetEnvironmentVariable(AssistantClientFactory.AnthropicApiKeyEnvVar, null);

        var services = new ServiceCollection();
        var config = BuildConfig(("Assistant:Provider", "anthropic"));

        services.AddCceAssistantClient(config);

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ISmartAssistantClient));
        descriptor!.ImplementationType.Should().Be(typeof(SmartAssistantClient));
    }

    [Fact]
    public void Default_provider_is_stub()
    {
        var services = new ServiceCollection();
        var config = BuildConfig();

        services.AddCceAssistantClient(config);

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ISmartAssistantClient));
        descriptor!.ImplementationType.Should().Be(typeof(SmartAssistantClient));
    }

    private static IConfiguration BuildConfig(params (string Key, string Value)[] entries)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(entries.Select(e =>
                new KeyValuePair<string, string?>(e.Key, e.Value)))
            .Build();
}
```

- [ ] **Step 1:** Replace `AssistantClientFactory.cs` with the implementation above.

- [ ] **Step 2:** Create the test file.

- [ ] **Step 3:** Run tests:
  ```bash
  cd backend && dotnet test tests/CCE.Application.Tests/ --filter FullyQualifiedName~AssistantClientFactoryTests
  ```
  Expected: 4 passing.

- [ ] **Step 4:** Run full backend suite to confirm nothing else broke:
  ```bash
  cd backend && dotnet test tests/CCE.Application.Tests/ tests/CCE.Infrastructure.Tests/
  ```
  Expected: all green; total grows by ~14 (6 CitationSearch + 4 AnthropicSmartAssistantClient + 4 factory).

- [ ] **Step 5:** Commit:
  ```bash
  git add backend/src/CCE.Infrastructure/Assistant/AssistantClientFactory.cs \
          backend/tests/CCE.Application.Tests/Assistant/AssistantClientFactoryTests.cs
  git -c commit.gpgsign=false commit -m "feat(infrastructure): wire AssistantClientFactory to provider config

  Reads Assistant:Provider config + ANTHROPIC_API_KEY env-var.
  - 'stub' (default) → SmartAssistantClient
  - 'anthropic' + key → AnthropicSmartAssistantClient + AnthropicClient
    + IAnthropicStreamProvider
  - 'anthropic' without key → falls back to stub with a startup
    Warning log (using a one-shot LoggerFactory, since IConfiguration
    extensions can't resolve the host's logger).

  4 unit tests cover the four resolution paths.
  Sub-10a Phase 02 Task 2.5.

  Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
  ```

---

## Phase 02 close-out

- [ ] All five tasks committed; backend tests green (target ~443 application + 4 infrastructure tests, +14 from Phase 00's 429 baseline).
- [ ] `dotnet build` clean.
- [ ] Smoke-run `Api.External` with `ASSISTANT_PROVIDER=anthropic` + a real `ANTHROPIC_API_KEY` and watch a real reply stream from `/api/assistant/query`. (Optional — depends on whether the operator has an API key handy.)

**Phase 02 done when:**
- Both APIs emit JSON-compact Serilog logs to stdout.
- Both APIs expose `/metrics` with default histograms + the two custom counters.
- `CitationSearch` exists and tests pass.
- `AnthropicSmartAssistantClient` exists and tests pass.
- `AssistantClientFactory` honours `Assistant:Provider` + `ANTHROPIC_API_KEY`.
- 5 commits on `main`.
- Phase 03 plan to be written next: Lighthouse + axe-core CI gates + ADRs + tag.
