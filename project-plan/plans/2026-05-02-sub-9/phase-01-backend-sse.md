# Phase 01 — Backend SSE (Sub-9)

> Parent: [`../2026-05-02-sub-9.md`](../2026-05-02-sub-9.md) · Spec: [`../../specs/2026-05-02-sub-9-design.md`](../../specs/2026-05-02-sub-9-design.md) §3 (data contracts), §11 (backend details)

**Phase goal:** Reshape the backend so `POST /api/assistant/query` accepts `{messages[], locale}` and responds with `text/event-stream`. The `ISmartAssistantClient` becomes `IAsyncEnumerable<SseEvent>`-shaped; the stub fake-streams ~8 chunks over ~1.2s with 1 resource + 1 map-node citation pulled from seeded data. Frontend `AssistantApiService.query` gets wired to the SSE client. Phase 02 starts the UI work.

**Tasks:** 5
**Working directory:** `/Users/m/CCE/`
**Preconditions:** Phase 00 closed (commit `7406479` or later); 85 suites · 453 frontend tests + 428 backend `Application.Tests` passing.

---

## Task 1.1: Reshape `ISmartAssistantClient` to streaming

**Files:**
- Modify: `backend/src/CCE.Application/Assistant/ISmartAssistantClient.cs` — replace single `AskAsync` with streaming `StreamAsync`.
- Delete: `backend/src/CCE.Application/Assistant/SmartAssistantReplyDto.cs` (no longer used after the reshape).
- Modify: `backend/src/CCE.Application/Assistant/Commands/AskAssistant/AskAssistantCommand.cs` — accept `IReadOnlyList<ChatMessage>` instead of single `Question`.
- Modify: `backend/src/CCE.Application/Assistant/Commands/AskAssistant/AskAssistantCommandHandler.cs` — return `IAsyncEnumerable<SseEvent>` (not `Task<>`).
- Modify: `backend/src/CCE.Application/Assistant/Commands/AskAssistant/AskAssistantCommandValidator.cs` — validate `Messages.Count > 0` and last-message-is-user.
- Create: `backend/src/CCE.Application/Assistant/ChatMessage.cs` — DTO record.

**Final state of `ChatMessage.cs`:**
```cs
namespace CCE.Application.Assistant;

public sealed record ChatMessage(string Role, string Content);
```

**Final state of `ISmartAssistantClient.cs`:**
```cs
namespace CCE.Application.Assistant;

/// <summary>
/// Abstraction over the smart-assistant LLM backend. Streams typed
/// SseEvent records (text chunks, citations, done, error) as they're
/// produced. Production LLM provider is a future swap-in; the stub
/// in CCE.Infrastructure fake-streams placeholder text.
/// </summary>
public interface ISmartAssistantClient
{
    IAsyncEnumerable<SseEvent> StreamAsync(
        IReadOnlyList<ChatMessage> messages,
        string locale,
        CancellationToken ct);
}
```

**Final state of `AskAssistantCommand.cs`:**
```cs
using MediatR;

namespace CCE.Application.Assistant.Commands.AskAssistant;

/// <summary>
/// Streaming command. The handler does NOT use MediatR's IRequest&lt;TResponse&gt;
/// pattern (which is single-response Task-shaped). Instead the endpoint
/// constructs the stream directly via ISmartAssistantClient and writes it
/// through SseWriter — this keeps MediatR out of the streaming hot path.
/// We keep the command + validator as a typed boundary for inputs.
/// </summary>
public sealed record AskAssistantCommand(
    IReadOnlyList<ChatMessage> Messages,
    string Locale);
```

**Final state of `AskAssistantCommandValidator.cs`:**
```cs
using FluentValidation;

namespace CCE.Application.Assistant.Commands.AskAssistant;

public sealed class AskAssistantCommandValidator : AbstractValidator<AskAssistantCommand>
{
    public AskAssistantCommandValidator()
    {
        RuleFor(x => x.Messages).NotEmpty().WithMessage("messages must contain at least one entry.");
        RuleFor(x => x.Messages).Must(m => m.Count <= 50)
            .WithMessage("messages must contain no more than 50 entries.");
        RuleFor(x => x.Messages).Must(m => m.Count == 0 || m[^1].Role == "user")
            .WithMessage("the last message must have role 'user'.");
        RuleForEach(x => x.Messages).ChildRules(child =>
        {
            child.RuleFor(m => m.Role).Must(r => r == "user" || r == "assistant")
                .WithMessage("role must be 'user' or 'assistant'.");
            child.RuleFor(m => m.Content).NotEmpty().MaximumLength(4000);
        });
        RuleFor(x => x.Locale).Must(l => l == "ar" || l == "en")
            .WithMessage("locale must be 'ar' or 'en'.");
    }
}
```

**Delete `AskAssistantCommandHandler.cs`** — the new endpoint calls `ISmartAssistantClient.StreamAsync` directly. (Validator still runs via the existing FluentValidation pipeline-behavior on the validator alone, but if MediatR's pipeline requires a handler for any registered IRequest, deleting the IRequest discriminator from the command obviates the need for a handler.)

**Delete `SmartAssistantReplyDto.cs`** — replaced by `SseEvent` events.

**Existing tests to update:**
- `backend/tests/CCE.Application.Tests/Assistant/AskAssistantCommandHandlerTests.cs` — delete (no longer applicable; handler is gone).
- `backend/tests/CCE.Application.Tests/Assistant/AskAssistantCommandValidatorTests.cs` — rewrite to assert against the new shape (messages array, last-is-user, role values).

**New validator tests** (replace existing):
```cs
using CCE.Application.Assistant;
using CCE.Application.Assistant.Commands.AskAssistant;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace CCE.Application.Tests.Assistant;

public class AskAssistantCommandValidatorTests
{
    private readonly AskAssistantCommandValidator _sut = new();

    [Fact]
    public void Empty_messages_is_invalid()
    {
        var cmd = new AskAssistantCommand(new List<ChatMessage>(), "en");
        _sut.TestValidate(cmd).ShouldHaveValidationErrorFor(c => c.Messages);
    }

    [Fact]
    public void Single_user_message_is_valid()
    {
        var cmd = new AskAssistantCommand(
            new List<ChatMessage> { new("user", "What is CCE?") }, "en");
        _sut.TestValidate(cmd).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Last_message_must_be_user()
    {
        var cmd = new AskAssistantCommand(
            new List<ChatMessage>
            {
                new("user", "hi"),
                new("assistant", "hello"),
            }, "en");
        _sut.TestValidate(cmd).ShouldHaveValidationErrorFor(c => c.Messages);
    }

    [Fact]
    public void Invalid_role_is_invalid()
    {
        var cmd = new AskAssistantCommand(
            new List<ChatMessage> { new("system", "hi") }, "en");
        var result = _sut.TestValidate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Locale_must_be_ar_or_en()
    {
        var cmd = new AskAssistantCommand(
            new List<ChatMessage> { new("user", "hi") }, "fr");
        _sut.TestValidate(cmd).ShouldHaveValidationErrorFor(c => c.Locale);
    }

    [Fact]
    public void Content_max_length_4000_is_enforced()
    {
        var cmd = new AskAssistantCommand(
            new List<ChatMessage> { new("user", new string('x', 4001)) }, "en");
        var result = _sut.TestValidate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Max_50_messages_is_enforced()
    {
        var msgs = Enumerable.Range(0, 51)
            .Select(i => new ChatMessage(i % 2 == 0 ? "user" : "assistant", $"m{i}"))
            .ToList();
        // Make sure last is user
        msgs[^1] = new ChatMessage("user", "last");
        var cmd = new AskAssistantCommand(msgs, "en");
        _sut.TestValidate(cmd).ShouldHaveValidationErrorFor(c => c.Messages);
    }
}
```

- [ ] **Step 1: Delete the old test files** + the old handler/dto:
  ```bash
  cd /Users/m/CCE && rm \
    backend/src/CCE.Application/Assistant/SmartAssistantReplyDto.cs \
    backend/src/CCE.Application/Assistant/Commands/AskAssistant/AskAssistantCommandHandler.cs \
    backend/tests/CCE.Application.Tests/Assistant/AskAssistantCommandHandlerTests.cs
  ```

- [ ] **Step 2: Create / edit the production files** with the contents above (`ChatMessage.cs`, `ISmartAssistantClient.cs`, `AskAssistantCommand.cs`, `AskAssistantCommandValidator.cs`).

- [ ] **Step 3: Replace the validator spec** with the new content above.

- [ ] **Step 4: Update the existing infrastructure stub** (`backend/src/CCE.Infrastructure/Assistant/SmartAssistantClient.cs`) to compile — Phase 01 Task 1.2 fills in the fake-streamer. For now it returns an empty async-enumerable so the project compiles:
  ```cs
  using CCE.Application.Assistant;
  using System.Runtime.CompilerServices;

  namespace CCE.Infrastructure.Assistant;

  public sealed class SmartAssistantClient : ISmartAssistantClient
  {
  #pragma warning disable CS1998 // async without await
      public async IAsyncEnumerable<SseEvent> StreamAsync(
          IReadOnlyList<ChatMessage> messages,
          string locale,
          [EnumeratorCancellation] CancellationToken ct)
  #pragma warning restore CS1998
      {
          // Phase 01 Task 1.2 fills in the fake-streamer.
          yield break;
      }
  }
  ```

- [ ] **Step 5: Update `/api/assistant/query` endpoint** (`backend/src/CCE.Api.External/Endpoints/AssistantEndpoints.cs`) to accept the new request shape and call the stream client directly, writing events via `SseWriter`. Don't worry about validation pipeline yet — Task 1.3 wires that in:
  ```cs
  using CCE.Application.Assistant;
  using Microsoft.AspNetCore.Builder;
  using Microsoft.AspNetCore.Http;
  using Microsoft.AspNetCore.Routing;

  namespace CCE.Api.External.Endpoints;

  public static class AssistantEndpoints
  {
      public static IEndpointRouteBuilder MapAssistantEndpoints(this IEndpointRouteBuilder app)
      {
          var assistant = app.MapGroup("/api/assistant").WithTags("Assistant");

          // POST /api/assistant/query — streams text/event-stream
          assistant.MapPost("/query", async (
              AskAssistantRequest body,
              ISmartAssistantClient client,
              HttpResponse response,
              CancellationToken ct) =>
          {
              var messages = (body.Messages ?? Array.Empty<ChatMessageDto>())
                  .Select(m => new ChatMessage(m.Role ?? "", m.Content ?? ""))
                  .ToList();
              var stream = client.StreamAsync(messages, body.Locale ?? "en", ct);
              await SseWriter.WriteAsync(response, stream, ct).ConfigureAwait(false);
          })
          .AllowAnonymous()
          .WithName("AskAssistant");

          return app;
      }
  }

  public sealed record AskAssistantRequest(IReadOnlyList<ChatMessageDto> Messages, string Locale);
  public sealed record ChatMessageDto(string Role, string Content);
  ```

- [ ] **Step 6: Run build**:
  ```bash
  cd backend && dotnet build
  ```
  Expected: success.

- [ ] **Step 7: Run tests** for the validator:
  ```bash
  cd backend && dotnet test tests/CCE.Application.Tests/ --filter FullyQualifiedName~AskAssistantCommandValidatorTests
  ```
  Expected: 7 passing.

- [ ] **Step 8: Commit:**
  ```bash
  git -c commit.gpgsign=false commit -m "feat(assistant): reshape command + endpoint for streaming

  ISmartAssistantClient becomes IAsyncEnumerable<SseEvent>-shaped.
  AskAssistantCommand carries IReadOnlyList<ChatMessage>; validator
  enforces non-empty + max 50 + last-is-user + role-in-{user,assistant}
  + content max 4000 + locale in {ar,en}. /api/assistant/query writes
  SSE via SseWriter. Stub returns empty stream — Task 1.2 fake-streams.
  Sub-9 Phase 01 Task 1.1."
  ```

---

## Task 1.2: Fake-streaming `SmartAssistantClient` stub

**Files:**
- Modify: `backend/src/CCE.Infrastructure/Assistant/SmartAssistantClient.cs` — replace empty stub with fake-streamer.

**Behaviour:**
- Build a placeholder reply text that incorporates the user's most recent message: `"This is a stub reply to: '<question>'. Real LLM coming in a later sub-project."`
- Split into ~8 chunks (split by spaces, group ~3 words per chunk; localized intro per locale).
- For each chunk: yield `TextEvent`; await `Task.Delay(150, ct)`.
- Halfway through chunks, query the DB for one random `Resource` and one random `KnowledgeMapNode`. For each found, yield a `CitationEvent` with the localized title.
- After all text + citations, yield `DoneEvent`.

**Final state:**
```cs
using CCE.Application.Assistant;
using CCE.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;

namespace CCE.Infrastructure.Assistant;

/// <summary>
/// Stub implementation of <see cref="ISmartAssistantClient"/>. Fake-streams
/// chunks of text + 1-2 citations from seeded data so the UI can exercise
/// the full streaming + citation flow without a real LLM. Real LLM
/// integration drops in by replacing this class.
/// </summary>
public sealed class SmartAssistantClient : ISmartAssistantClient
{
    private const int ChunkDelayMs = 150;
    private readonly ICceDbContext _db;

    public SmartAssistantClient(ICceDbContext db)
    {
        _db = db;
    }

    public async IAsyncEnumerable<SseEvent> StreamAsync(
        IReadOnlyList<ChatMessage> messages,
        string locale,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var lastUser = messages.LastOrDefault(m => m.Role == "user")?.Content ?? string.Empty;
        var (intro, body, outro) = locale == "ar"
            ? ("هذا رد تجريبي على", lastUser, "سيأتي مساعد LLM حقيقي في مشروع فرعي لاحق.")
            : ("This is a stub reply to:", lastUser, "Real LLM coming in a later sub-project.");

        var chunks = BuildChunks(intro, body, outro);
        var citationIndex = chunks.Count / 2;

        for (var i = 0; i < chunks.Count; i++)
        {
            await Task.Delay(ChunkDelayMs, ct).ConfigureAwait(false);
            yield return new TextEvent(chunks[i]);

            if (i == citationIndex)
            {
                await foreach (var c in EmitCitations(locale, ct).ConfigureAwait(false))
                {
                    yield return c;
                }
            }
        }

        yield return new DoneEvent();
    }

    private static List<string> BuildChunks(string intro, string body, string outro)
    {
        // 8 chunks, ~3 words each. Easy to demo without overengineering.
        var quoted = $"\"{body.Length > 80 ? body[..80] + "…" : body}\". ";
        var words = ($"{intro} {quoted}{outro}").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var chunks = new List<string>();
        for (var i = 0; i < words.Length; i += 3)
        {
            var slice = words.Skip(i).Take(3);
            chunks.Add(string.Join(' ', slice) + (i + 3 < words.Length ? " " : string.Empty));
        }
        return chunks;
    }

    private async IAsyncEnumerable<SseEvent> EmitCitations(
        string locale,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var resourceCount = await _db.Resources
            .CountAsyncEither(ct).ConfigureAwait(false);
        if (resourceCount > 0)
        {
            var resource = await _db.Resources
                .OrderBy(r => r.Id)
                .FirstAsyncEither(ct).ConfigureAwait(false);
            yield return new CitationEvent(new CitationDto(
                Id: resource.Id.ToString(),
                Kind: "resource",
                Title: locale == "ar" ? resource.TitleAr : resource.TitleEn,
                Href: $"/knowledge-center/resources/{resource.Id}",
                SourceText: null));
        }

        var nodeCount = await _db.KnowledgeMapNodes
            .CountAsyncEither(ct).ConfigureAwait(false);
        if (nodeCount > 0)
        {
            var node = await _db.KnowledgeMapNodes
                .OrderBy(n => n.Id)
                .FirstAsyncEither(ct).ConfigureAwait(false);
            yield return new CitationEvent(new CitationDto(
                Id: node.Id.ToString(),
                Kind: "map-node",
                Title: locale == "ar" ? node.NameAr : node.NameEn,
                Href: $"/knowledge-maps/{node.MapId}?node={node.Id}",
                SourceText: null));
        }
    }
}
```

(`CountAsyncEither` and `FirstAsyncEither` are existing project extensions on `ICceDbContext`'s queryables — double-check by grep; if they don't exist, swap for plain `CountAsync` / `FirstAsync` from `Microsoft.EntityFrameworkCore`.)

- [ ] **Step 1: Verify db-extensions** with `grep -rn "CountAsyncEither\|FirstAsyncEither" backend/src/CCE.Application/`. If absent, replace with `CountAsync` / `FirstAsync` (importing `Microsoft.EntityFrameworkCore`).

- [ ] **Step 2: Replace the stub** with the streaming implementation above (adjusting Async helpers per Step 1).

- [ ] **Step 3: Build**:
  ```bash
  cd backend && dotnet build
  ```

- [ ] **Step 4: Manual smoke (optional)** — start the API, `curl -N -H "Content-Type: application/json" -d '{"messages":[{"role":"user","content":"hi"}],"locale":"en"}' http://localhost:5001/api/assistant/query` should stream events.

- [ ] **Step 5: Commit:**
  ```bash
  git -c commit.gpgsign=false commit -m "feat(assistant): fake-streaming stub with seeded-data citations

  Yields ~8 text chunks @ 150ms each. Halfway through, queries the DB
  for one Resource + one KnowledgeMapNode and emits CitationEvents
  pointing at /knowledge-center/resources/<id> and
  /knowledge-maps/<id>?node=<id>. Localized title per request locale.
  Honours CancellationToken via Task.Delay(ms, ct). Real LLM drops in
  by replacing this class. Sub-9 Phase 01 Task 1.2."
  ```

---

## Task 1.3: Backend integration test

**Files:**
- Create: `backend/tests/CCE.Api.IntegrationTests/Assistant/AssistantSseStreamTests.cs`.

**Test:** assert the endpoint returns `Content-Type: text/event-stream`, that the body parses to ≥5 `text` events + ≥1 `citation` event + 1 `done` event in order.

```cs
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CCE.Api.IntegrationTests.Assistant;

public class AssistantSseStreamTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AssistantSseStreamTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Query_streams_text_citation_and_done_events_in_order()
    {
        using var client = _factory.CreateClient();
        var body = new
        {
            messages = new[] { new { role = "user", content = "What is CCE?" } },
            locale = "en",
        };
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/assistant/query");
        req.Content = JsonContent.Create(body);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        using var res = await client.SendAsync(req, HttpCompletionOption.ResponseContentRead, default);
        res.EnsureSuccessStatusCode();
        res.Content.Headers.ContentType?.MediaType.Should().Be("text/event-stream");

        var raw = await res.Content.ReadAsStringAsync();
        var events = raw.Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
            .Select(frame => frame.Replace("data: ", string.Empty))
            .Select(json => JsonDocument.Parse(json).RootElement)
            .ToList();

        var types = events.Select(e => e.GetProperty("type").GetString()).ToList();
        types.Count(t => t == "text").Should().BeGreaterOrEqualTo(5);
        types.Count(t => t == "citation").Should().BeGreaterOrEqualTo(1);
        types.Last().Should().Be("done");
    }
}
```

- [ ] **Step 1: Create the test** with the contents above.

- [ ] **Step 2: Run it**:
  ```bash
  cd backend && dotnet test tests/CCE.Api.IntegrationTests/ --filter FullyQualifiedName~AssistantSseStreamTests
  ```

- [ ] **Step 3: Commit:**
  ```bash
  git -c commit.gpgsign=false commit -m "test(assistant): integration test for SSE stream shape

  Asserts /api/assistant/query returns text/event-stream and emits
  >=5 text events + >=1 citation event + a final done event in order.
  Sub-9 Phase 01 Task 1.3."
  ```

---

## Task 1.4: Frontend `AssistantApiService.query` wires to `openSseStream`

**Files:**
- Modify: `frontend/apps/web-portal/src/app/features/assistant/assistant-api.service.ts`.
- Modify: `frontend/apps/web-portal/src/app/features/assistant/assistant-api.service.spec.ts`.

**Final state of service:**
```ts
import { Injectable } from '@angular/core';
import type { AssistantQueryRequest, SseEvent } from './assistant.types';
import { openSseStream } from './lib/sse-client';

@Injectable({ providedIn: 'root' })
export class AssistantApiService {
  query(req: AssistantQueryRequest, signal: AbortSignal): AsyncIterable<SseEvent> {
    return openSseStream('/api/assistant/query', req, signal);
  }
}
```

**Test** (replace placeholder):
```ts
import { TestBed } from '@angular/core/testing';
import { AssistantApiService } from './assistant-api.service';

describe('AssistantApiService', () => {
  let originalFetch: typeof globalThis.fetch;
  beforeEach(() => { originalFetch = globalThis.fetch; });
  afterEach(() => { globalThis.fetch = originalFetch; });

  it('query passes the request body to openSseStream', async () => {
    const fetchMock = jest.fn().mockResolvedValue({
      ok: true, status: 200,
      body: { getReader: () => ({
        read: jest.fn().mockResolvedValueOnce({ value: undefined, done: true }),
        releaseLock: jest.fn(),
      }) },
    });
    globalThis.fetch = fetchMock as unknown as typeof globalThis.fetch;

    TestBed.configureTestingModule({});
    const sut = TestBed.inject(AssistantApiService);
    const it = sut.query(
      { messages: [{ role: 'user', content: 'hi' }], locale: 'en' },
      new AbortController().signal,
    );
    for await (const _ of it) void _;

    expect(fetchMock).toHaveBeenCalledWith('/api/assistant/query', expect.objectContaining({
      method: 'POST',
      body: JSON.stringify({ messages: [{ role: 'user', content: 'hi' }], locale: 'en' }),
    }));
  });
});
```

- [ ] **Step 1: Replace files** with the contents above.
- [ ] **Step 2: Run tests:**
  ```bash
  cd frontend && ./node_modules/.bin/nx test web-portal --watch=false --testPathPattern=assistant-api.service.spec
  ```
  Expected: 1 passing.
- [ ] **Step 3: Commit:**
  ```bash
  git -c commit.gpgsign=false commit -m "feat(assistant): wire AssistantApiService.query to openSseStream

  Sub-9 Phase 01 Task 1.4."
  ```

---

## Task 1.5: Phase 01 close-out

- [ ] Full `nx test web-portal --watch=false` passes.
- [ ] Full `dotnet test` passes.
- [ ] `nx run web-portal:lint` clean.
- [ ] `nx build web-portal` succeeds.
- [ ] Manual smoke: visit `/assistant` (still empty UI from Phase 00) — no regression. The endpoint can be probed with `curl -N`.

**Phase 01 done when:**
- Backend test count grows by ~7 (validator) + ~1 (integration) = ~8.
- Frontend test count: -2 (placeholder service tests removed) + 1 (real service test) = ~452.
- 4 commits land on `main`.
- `ISmartAssistantClient` is fully streaming-shaped; the stub fake-streams from seeded data.
