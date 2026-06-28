using System.Linq;
using CCE.Application.Common.Pagination;
using CCE.Application.Search;
using Meilisearch;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prometheus;
using NugetMeili = Meilisearch.MeilisearchClient;

namespace CCE.Infrastructure.Search;

public sealed class MeilisearchClient : ISearchClient, System.IDisposable
{
    private readonly NugetMeili _client;
    private readonly System.Net.Http.HttpClient _httpClient;
    private readonly ILogger<MeilisearchClient> _logger;

    public MeilisearchClient(IOptions<CceInfrastructureOptions> opts, ILogger<MeilisearchClient> logger)
    {
        var o = opts.Value;
        // SDK v0.15.5 serializes RankingScoreThreshold (non-nullable double, default 0.0) on every
        // search request. Meilisearch ≤1.7 does not recognise the field and returns 400. We strip it
        // via a delegating handler so the SDK and server versions stay decoupled.
        // The SDK's HttpClient overload (.ctor(HttpClient, string)) requires the caller to set
        // BaseAddress and the Authorization header on the provided client.
        // CA2000: HttpClient takes ownership of its DelegatingHandler and disposes it on Dispose().
        // The _httpClient field is disposed by this class's own Dispose() method.
#pragma warning disable CA2000
        _httpClient = new System.Net.Http.HttpClient(
            new StripUnknownSearchFieldsHandler { InnerHandler = new System.Net.Http.HttpClientHandler() })
        {
            BaseAddress = new System.Uri(o.MeilisearchUrl),
        };
#pragma warning restore CA2000
        if (!string.IsNullOrEmpty(o.MeilisearchMasterKey))
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", o.MeilisearchMasterKey);
        _client = new NugetMeili(_httpClient);
        _logger = logger;
    }

    public async Task EnsureIndexAsync(SearchableType type, CancellationToken ct)
    {
        var indexUid = IndexUid(type);
        try
        {
            await _client.GetIndexAsync(indexUid, ct).ConfigureAwait(false);
        }
        catch (MeilisearchApiError)
        {
            await _client.CreateIndexAsync(indexUid, "id", ct).ConfigureAwait(false);
        }

        // community_replies stores postId for retrieval but it must not be full-text searched —
        // a raw GUID string is meaningless to users and wastes search capacity.
        if (type == SearchableType.CommunityReplies)
        {
            var index = _client.Index(indexUid);
            await index.UpdateSearchableAttributesAsync(
                new[] { "contentAr", "contentEn", "authorName" }, ct).ConfigureAwait(false);
        }
    }

    public async Task UpsertAsync<TDoc>(SearchableType type, TDoc doc, CancellationToken ct) where TDoc : class
    {
        var index = _client.Index(IndexUid(type));
        await index.AddDocumentsAsync(new[] { doc }, "id", ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(SearchableType type, System.Guid id, CancellationToken ct)
    {
        var index = _client.Index(IndexUid(type));
        await index.DeleteOneDocumentAsync(id.ToString(), ct).ConfigureAwait(false);
    }

    private static readonly Counter MeiliFailuresTotal = Metrics
        .CreateCounter(
            "community_search_meili_failures",
            "Meilisearch index query failures during community search, labeled by index.",
            new CounterConfiguration { LabelNames = new[] { "index" } });

    // CommunityPosts and CommunityReplies are intentionally excluded from global cross-content search.
    // They are served by SearchCommunityPostsAsync via the /feed?q= endpoint instead.
    private static readonly SearchableType[] GlobalSearchTypes =
    [
        SearchableType.News, SearchableType.Events, SearchableType.Resources,
        SearchableType.Pages, SearchableType.KnowledgeMaps,
    ];

    public async Task<PagedResult<SearchHitDto>> SearchAsync(
        string query,
        SearchableType? type,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        page = System.Math.Max(1, page);
        pageSize = System.Math.Clamp(pageSize, 1, 100);
        var offset = (page - 1) * pageSize;

        var types = type is { } t ? new[] { t } : GlobalSearchTypes;
        var allHits = new System.Collections.Generic.List<SearchHitDto>();
        long totalAcross = 0;

        foreach (var st in types)
        {
            var index = _client.Index(IndexUid(st));
            var sq = new SearchQuery
            {
                Limit = pageSize,
                Offset = offset,
                AttributesToHighlight = new[] { "titleAr", "titleEn" },
                ShowMatchesPosition = true,
            };
            try
            {
                var raw = await index.SearchAsync<SearchableDocument>(query, sq, ct).ConfigureAwait(false);
                if (raw is SearchResult<SearchableDocument> result)
                {
                    totalAcross += result.EstimatedTotalHits;
                    foreach (var hit in result.Hits)
                    {
                        allHits.Add(new SearchHitDto(
                            System.Guid.TryParse(hit.Id, out var g) ? g : System.Guid.Empty,
                            st,
                            hit.TitleAr ?? string.Empty,
                            hit.TitleEn ?? string.Empty,
                            Excerpt(hit.ContentAr),
                            Excerpt(hit.ContentEn),
                            Score: 0));
                    }
                }
            }
            // CA1031: SDK has no common base type for MeilisearchApiError, MeilisearchCommunicationError,
            // MeilisearchTimeoutError; widening to Exception mirrors SearchIndexSafeAsync.
#pragma warning disable CA1031
            catch (System.Exception ex)
#pragma warning restore CA1031
            {
                _logger.LogWarning(ex, "Meilisearch search failed for {IndexUid}; skipping.", IndexUid(st));
            }
        }
        return new PagedResult<SearchHitDto>(allHits, page, pageSize, totalAcross);
    }

    private static string IndexUid(SearchableType type) => type switch
    {
        SearchableType.News             => "news",
        SearchableType.Events           => "events",
        SearchableType.Resources        => "resources",
        SearchableType.Pages            => "pages",
        SearchableType.KnowledgeMaps    => "knowledge_maps",
        SearchableType.CommunityPosts   => "community_posts",
        SearchableType.CommunityReplies => "community_replies",
        _ => throw new System.ArgumentOutOfRangeException(nameof(type)),
    };

    public async Task UpsertBatchAsync<TDoc>(
        SearchableType type,
        System.Collections.Generic.IEnumerable<TDoc> docs,
        CancellationToken ct) where TDoc : class
    {
        var index = _client.Index(IndexUid(type));
        await index.AddDocumentsAsync(docs, "id", ct).ConfigureAwait(false);
    }

    public async Task<CommunityRawSearchResult> SearchCommunityPostsAsync(
        string query, int limit, CancellationToken ct)
    {
        var postsIndex   = _client.Index(IndexUid(SearchableType.CommunityPosts));
        var repliesIndex = _client.Index(IndexUid(SearchableType.CommunityReplies));

        var postsSq = new SearchQuery
        {
            Limit = limit,
            AttributesToHighlight = new[] { "titleAr", "titleEn", "contentAr", "contentEn", "authorName", "tagNamesAr", "tagNamesEn" },
        };
        var repliesSq = new SearchQuery
        {
            Limit = limit,
            AttributesToHighlight = new[] { "contentAr", "contentEn", "authorName" },
        };

        var postsTask   = SearchIndexSafeAsync<CommunityPostHitDocument>(postsIndex, query, postsSq, ct);
        var repliesTask = SearchIndexSafeAsync<CommunityReplyHitDocument>(repliesIndex, query, repliesSq, ct);

        await System.Threading.Tasks.Task.WhenAll(postsTask, repliesTask).ConfigureAwait(false);

        var postHits = (await postsTask)
            .Select((hit, rank) => new CommunityPostHit(
                System.Guid.TryParse(hit.Id, out var g) ? g : System.Guid.Empty,
                ResolveTitle(hit.Formatted?.TitleAr, hit.Formatted?.TitleEn, hit.TitleAr, hit.TitleEn),
                ResolveHighlightedExcerpt(hit.Formatted?.ContentAr, hit.Formatted?.ContentEn, hit.ContentAr, hit.ContentEn),
                MeiliRank: rank))
            .Where(h => h.PostId != System.Guid.Empty)
            .ToList();

        var replyHits = (await repliesTask)
            .Select((hit, rank) => new CommunityReplyHit(
                System.Guid.TryParse(hit.Id, out var rId) ? rId : System.Guid.Empty,
                System.Guid.TryParse(hit.PostId, out var pId) ? pId : System.Guid.Empty,
                ResolveHighlightedExcerpt(hit.Formatted?.ContentAr, hit.Formatted?.ContentEn, hit.ContentAr, hit.ContentEn),
                MeiliRank: rank))
            .Where(h => h.ReplyId != System.Guid.Empty && h.PostId != System.Guid.Empty)
            .ToList();

        return new CommunityRawSearchResult(postHits, replyHits);
    }

    // CA1031: any Meilisearch failure (API error, connection refused, timeout) must degrade gracefully
    // to an empty result set so the /feed endpoint keeps serving even when Meilisearch is unavailable.
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Meilisearch SDK has no common base exception type across MeilisearchApiError, " +
                        "MeilisearchCommunicationError and MeilisearchTimeoutError. Catching Exception " +
                        "here is intentional: search failures must never break the feed endpoint.")]
    private async System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<T>> SearchIndexSafeAsync<T>(
        Meilisearch.Index index, string query, SearchQuery sq, CancellationToken ct)
        where T : class
    {
        try
        {
            var raw = await index.SearchAsync<T>(query, sq, ct).ConfigureAwait(false);
            return raw is SearchResult<T> result ? result.Hits.ToList() : System.Array.Empty<T>();
        }
        catch (System.Exception ex)
        {
            MeiliFailuresTotal.WithLabels(index.Uid).Inc();
            _logger.LogWarning(ex, "Community Meilisearch search failed on index {Uid}; returning empty.", index.Uid);
            return System.Array.Empty<T>();
        }
    }

    private static string? ResolveTitle(
        string? formattedAr, string? formattedEn, string? rawAr, string? rawEn)
    {
        // Prefer the highlighted (formatted) version; fall back to raw.
        if (!string.IsNullOrEmpty(formattedAr)) return formattedAr;
        if (!string.IsNullOrEmpty(formattedEn)) return formattedEn;
        return null;
    }

    private static string? ResolveHighlightedExcerpt(
        string? formattedAr, string? formattedEn, string? rawAr, string? rawEn)
    {
        var content = !string.IsNullOrEmpty(formattedAr) ? formattedAr
                    : !string.IsNullOrEmpty(formattedEn) ? formattedEn
                    : rawAr ?? rawEn;
        return string.IsNullOrEmpty(content) ? null
            : content.Length <= 300 ? content : content[..300] + "...";
    }

    private static string Excerpt(string? content)
    {
        if (string.IsNullOrEmpty(content)) return string.Empty;
        return content.Length <= 200 ? content : content[..200] + "...";
    }

    public void Dispose() => _httpClient.Dispose();
}

