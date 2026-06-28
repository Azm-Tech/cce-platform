namespace CCE.Application.Search;

/// <summary>A post hit from the community_posts Meilisearch index.</summary>
public sealed record CommunityPostHit(
    System.Guid PostId,
    string? HighlightedTitle, // locale-resolved: whichever of Ar/En was non-null, with <em> wrapping
    string? ExcerptContent,   // locale-resolved content excerpt with <em> wrapping
    int MeiliRank);           // 0-based position in Meilisearch result list (lower = more relevant)

/// <summary>A reply hit from the community_replies Meilisearch index.</summary>
public sealed record CommunityReplyHit(
    System.Guid ReplyId,
    System.Guid PostId,       // parent post — used to surface the post in results
    string? Excerpt,          // locale-resolved highlighted reply fragment with <em> wrapping
    int MeiliRank);

/// <summary>Combined result from searching both community indexes concurrently.</summary>
public sealed record CommunityRawSearchResult(
    System.Collections.Generic.IReadOnlyList<CommunityPostHit> PostHits,
    System.Collections.Generic.IReadOnlyList<CommunityReplyHit> ReplyHits);
