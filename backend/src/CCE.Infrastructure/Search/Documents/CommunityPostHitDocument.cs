namespace CCE.Infrastructure.Search;

// Used ONLY for deserializing Meilisearch search responses — separate from the upsert
// document types to avoid sending _formatted back to the index.
internal sealed class CommunityPostHitDocument
{
    public string  Id         { get; set; } = string.Empty;
    public string? TitleAr    { get; set; }
    public string? TitleEn    { get; set; }
    public string? ContentAr  { get; set; }
    public string? ContentEn  { get; set; }
    public string? AuthorName { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("_formatted")]
    public CommunityPostHitDocument? Formatted { get; set; }
}
