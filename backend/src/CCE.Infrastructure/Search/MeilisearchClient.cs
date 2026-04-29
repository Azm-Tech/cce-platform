using CCE.Application.Common.Pagination;
using CCE.Application.Search;
using Meilisearch;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NugetMeili = Meilisearch.MeilisearchClient;

namespace CCE.Infrastructure.Search;

public sealed class MeilisearchClient : ISearchClient
{
    private readonly NugetMeili _client;
    private readonly ILogger<MeilisearchClient> _logger;

    public MeilisearchClient(IOptions<CceInfrastructureOptions> opts, ILogger<MeilisearchClient> logger)
    {
        var o = opts.Value;
        _client = new NugetMeili(o.MeilisearchUrl, string.IsNullOrEmpty(o.MeilisearchMasterKey) ? null! : o.MeilisearchMasterKey);
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

        var types = type is { } t ? new[] { t } : System.Enum.GetValues<SearchableType>();
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
            catch (MeilisearchApiError ex)
            {
                _logger.LogWarning(ex, "Meilisearch search failed for {IndexUid}; skipping.", IndexUid(st));
            }
        }
        return new PagedResult<SearchHitDto>(allHits, page, pageSize, totalAcross);
    }

    private static string IndexUid(SearchableType type) => type switch
    {
        SearchableType.News => "news",
        SearchableType.Events => "events",
        SearchableType.Resources => "resources",
        SearchableType.Pages => "pages",
        SearchableType.KnowledgeMaps => "knowledge_maps",
        _ => throw new System.ArgumentOutOfRangeException(nameof(type)),
    };

    private static string Excerpt(string? content)
    {
        if (string.IsNullOrEmpty(content)) return string.Empty;
        return content.Length <= 200 ? content : content[..200] + "...";
    }

}
