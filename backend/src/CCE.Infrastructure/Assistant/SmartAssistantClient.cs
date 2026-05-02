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
        var quoted = $"\"{(body.Length > 80 ? body[..80] + "…" : body)}\". ";
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
        var resourceCount = await _db.Resources.CountAsync(ct).ConfigureAwait(false);
        if (resourceCount > 0)
        {
            var resource = await _db.Resources
                .OrderBy(r => r.Id)
                .FirstAsync(ct).ConfigureAwait(false);
            yield return new CitationEvent(new CitationDto(
                Id: resource.Id.ToString(),
                Kind: "resource",
                Title: locale == "ar" ? resource.TitleAr : resource.TitleEn,
                Href: $"/knowledge-center/resources/{resource.Id}",
                SourceText: null));
        }

        var nodeCount = await _db.KnowledgeMapNodes.CountAsync(ct).ConfigureAwait(false);
        if (nodeCount > 0)
        {
            var node = await _db.KnowledgeMapNodes
                .OrderBy(n => n.Id)
                .FirstAsync(ct).ConfigureAwait(false);
            yield return new CitationEvent(new CitationDto(
                Id: node.Id.ToString(),
                Kind: "map-node",
                Title: locale == "ar" ? node.NameAr : node.NameEn,
                Href: $"/knowledge-maps/{node.MapId}?node={node.Id}",
                SourceText: null));
        }
    }
}
