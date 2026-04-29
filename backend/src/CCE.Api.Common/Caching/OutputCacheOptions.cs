namespace CCE.Api.Common.Caching;

public sealed class OutputCacheOptions
{
    public const string SectionName = "OutputCache";

    /// <summary>Route prefixes eligible for output caching. Default: a curated public-read list.</summary>
    public IReadOnlyList<string> WhitelistPrefixes { get; init; } = new[]
    {
        "/api/news",
        "/api/events",
        "/api/pages",
        "/api/resources",
        "/api/homepage-sections",
        "/api/topics",
        "/api/categories",
        "/api/countries",
    };
}
