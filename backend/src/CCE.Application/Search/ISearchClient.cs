using CCE.Application.Common.Pagination;

namespace CCE.Application.Search;

public interface ISearchClient
{
    /// <summary>Idempotent: ensures the index exists for the given <paramref name="type"/>.</summary>
    Task EnsureIndexAsync(SearchableType type, CancellationToken ct);

    /// <summary>Upsert a document into the index for <paramref name="type"/>.</summary>
    Task UpsertAsync<TDoc>(SearchableType type, TDoc doc, CancellationToken ct) where TDoc : class;

    /// <summary>Remove a document by id.</summary>
    Task DeleteAsync(SearchableType type, System.Guid id, CancellationToken ct);

    /// <summary>
    /// Search across one type (or all when <paramref name="type"/> is null).
    /// Returns paged hits with score + excerpts.
    /// NOTE: CommunityPosts and CommunityReplies are excluded from the "all" path —
    /// use <see cref="SearchCommunityPostsAsync"/> for community search.
    /// </summary>
    Task<PagedResult<SearchHitDto>> SearchAsync(
        string query,
        SearchableType? type,
        int page,
        int pageSize,
        CancellationToken ct);

    /// <summary>
    /// Search the community_posts and community_replies indexes concurrently.
    /// Returns raw ranked hits that the caller merges, filters by visibility, and hydrates.
    /// </summary>
    Task<CommunityRawSearchResult> SearchCommunityPostsAsync(string query, int limit, CancellationToken ct);

    /// <summary>Batch-upsert multiple documents in a single Meilisearch round-trip.</summary>
    Task UpsertBatchAsync<TDoc>(SearchableType type, System.Collections.Generic.IEnumerable<TDoc> docs, CancellationToken ct) where TDoc : class;
}
