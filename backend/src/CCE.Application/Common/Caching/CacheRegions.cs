namespace CCE.Application.Common.Caching;

/// <summary>
/// Single source of truth for output-cache "regions" — named groups of cached entries (the cache
/// "tables": resources, feed, posts, …). Plain strings only (no Redis/EF dependency) so the Application
/// layer, the Api.Common middleware, and the Infrastructure invalidator can all share the same scheme.
///
/// <para>Redis layout: each cached response lives at <c>out:&lt;path&gt;?&lt;query&gt;|lang=…</c>; every key is
/// also indexed into a per-region SET <c>out:tag:&lt;region&gt;</c> so a whole region can be cleared by
/// set membership — no <c>KEYS</c>/<c>SCAN</c> against the shared Redis.</para>
/// </summary>
public static class CacheRegions
{
    /// <summary>Prefix for every output-cache entry key (matches the middleware).</summary>
    public const string KeyPrefix = "out:";

    public const string Resources = "resources";
    public const string Feed = "feed";
    public const string Posts = "posts";
    public const string News = "news";
    public const string Events = "events";
    public const string Topics = "topics";
    public const string Categories = "categories";
    public const string Countries = "countries";
    public const string Pages = "pages";
    public const string Homepage = "homepage";

    /// <summary>All known regions — used by the status listing and the flush-all operation.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        Resources, Feed, Posts, News, Events, Topics, Categories, Countries, Pages, Homepage,
    ];

    /// <summary>Redis SET key that indexes the live entry keys for a region.</summary>
    public static string TagSetKey(string region) => $"{KeyPrefix}tag:{region}";

    // Ordered route-prefix → region map. First match wins.
    private static readonly (string Prefix, string Region)[] PrefixMap =
    [
        ("/api/resources", Resources),
        ("/api/feed", Feed),
        ("/api/community", Posts),
        ("/api/news", News),
        ("/api/events", Events),
        ("/api/topics", Topics),
        ("/api/categories", Categories),
        ("/api/countries", Countries),
        ("/api/pages", Pages),
        ("/api/homepage-sections", Homepage),
    ];

    /// <summary>Maps a request path to its cache region, or null when the path belongs to no region.</summary>
    public static string? ResolveRegion(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        foreach (var (prefix, region) in PrefixMap)
        {
            if (path.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                return region;
        }
        return null;
    }

    /// <summary>True when <paramref name="region"/> is one of the known regions.</summary>
    public static bool IsKnownRegion(string region) =>
        All.Contains(region, System.StringComparer.OrdinalIgnoreCase);
}
