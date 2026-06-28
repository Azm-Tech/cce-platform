namespace CCE.Infrastructure.Search;

internal sealed class CommunityReplyDocument
{
    public string  Id         { get; set; } = string.Empty;
    // PostId is stored for retrieval but excluded from Meilisearch's searchable attributes
    // (configured in EnsureIndexAsync). A raw GUID string is not meaningful to full-text search.
    public string  PostId     { get; set; } = string.Empty;
    public string? ContentAr  { get; set; } // set when PostReply.Locale == "ar"
    public string? ContentEn  { get; set; } // set when PostReply.Locale == "en"
    public string? AuthorName { get; set; } // FirstName + LastName, fallback UserName
}
