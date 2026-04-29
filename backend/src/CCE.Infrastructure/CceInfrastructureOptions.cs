namespace CCE.Infrastructure;

/// <summary>
/// Strongly-typed options for the Infrastructure layer. Bound from <c>appsettings.json</c>
/// section <c>"Infrastructure"</c> (or env vars <c>Infrastructure__SqlConnectionString</c> etc.).
/// </summary>
public sealed class CceInfrastructureOptions
{
    public const string SectionName = "Infrastructure";

    /// <summary>SQL Server connection string. Required.</summary>
    public string SqlConnectionString { get; init; } = string.Empty;

    /// <summary>Redis connection string (e.g., <c>localhost:6379</c>). Required.</summary>
    public string RedisConnectionString { get; init; } = string.Empty;

    /// <summary>Root directory for dev-mode file uploads. Created on first save if missing.</summary>
    public string LocalUploadsRoot { get; init; } = "./backend/uploads/";

    /// <summary>ClamAV daemon hostname. Default <c>localhost</c>.</summary>
    public string ClamAvHost { get; init; } = "localhost";

    /// <summary>ClamAV daemon TCP port. Default 3310.</summary>
    public int ClamAvPort { get; init; } = 3310;

    /// <summary>
    /// Allowed MIME types for asset uploads. Defaults to a curated PDF/image/video/zip whitelist.
    /// </summary>
    public IReadOnlyList<string> AllowedAssetMimeTypes { get; init; } =
        new[] { "application/pdf", "image/png", "image/jpeg", "image/svg+xml", "video/mp4", "application/zip" };

    /// <summary>Meilisearch HTTP base URL. Default <c>http://localhost:7700</c>.</summary>
    public string MeilisearchUrl { get; init; } = "http://localhost:7700";

    /// <summary>Meilisearch master key. Required for indexing/search; optional for health probes.</summary>
    public string MeilisearchMasterKey { get; init; } = string.Empty;

    /// <summary>Output-cache TTL in seconds for anonymous reads. Default 60.</summary>
    public int OutputCacheTtlSeconds { get; init; } = 60;
}
