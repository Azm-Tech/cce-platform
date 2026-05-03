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
        "في", "من", "على", "عن", "هذا", "هذه", "ذلك", "تلك",
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
            .Select(r => new ResourceCandidate(r.Id, isAr ? r.TitleAr : r.TitleEn))
            .ToListAsync(ct).ConfigureAwait(false);
        var bestResource = ScoreTopOne(resources, queryTokens, c => c.Title);
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
            .Select(n => new MapNodeCandidate(n.Id, n.MapId, isAr ? n.NameAr : n.NameEn))
            .ToListAsync(ct).ConfigureAwait(false);
        var bestNode = ScoreTopOne(nodes, queryTokens, c => c.Title);
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

    private sealed record ResourceCandidate(Guid Id, string Title);
    private sealed record MapNodeCandidate(Guid Id, Guid MapId, string Title);

    private static T? ScoreTopOne<T>(
        IEnumerable<T> rows,
        HashSet<string> queryTokens,
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

    private static HashSet<string> Tokenize(string? text)
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
