namespace CCE.Infrastructure.Search;

/// <summary>Document shape stored in Meilisearch indexes. Phase 2 indexer creates these.</summary>
public sealed class SearchableDocument
{
    public string Id { get; set; } = string.Empty;
    public string? TitleAr { get; set; }
    public string? TitleEn { get; set; }
    public string? ContentAr { get; set; }
    public string? ContentEn { get; set; }
}
