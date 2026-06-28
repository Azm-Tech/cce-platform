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
    public string LocalUploadsRoot { get; init; } = "./backend/";

    /// <summary>ClamAV daemon hostname. Default <c>localhost</c>.</summary>
    public string ClamAvHost { get; init; } = "localhost";

    /// <summary>ClamAV daemon TCP port. Default 3310.</summary>
    public int ClamAvPort { get; init; } = 3310;

    /// <summary>
    /// Allowed MIME types for asset uploads. Defaults to a curated PDF/image/video/zip whitelist.
    /// </summary>
    public IReadOnlyList<string> AllowedAssetMimeTypes { get; init; } =
        new[] { "application/pdf", "image/png", "image/jpeg", "image/svg+xml", "video/mp4", "application/zip" };

    /// <summary>Root directory for media file storage. When under wwwroot/, files are also served as static content.</summary>
    public string MediaUploadsRoot { get; init; } = "./wwwroot/media/";

    /// <summary>S3-compatible object store endpoint (Supabase / MinIO / R2). Example: <c>https://xxx.supabase.co/storage/v1/s3</c>.</summary>
    public string S3EndpointUrl { get; init; } = string.Empty;

    /// <summary>
    /// Public CDN/storage base URL used to build file URLs returned to clients.
    /// For Supabase: <c>https://&lt;project&gt;.supabase.co/storage/v1/object/public</c>.
    /// The bucket name and storage key are appended automatically: <c>{S3PublicBaseUrl}/{bucket}/{key}</c>.
    /// </summary>
    public string S3PublicBaseUrl { get; init; } = string.Empty;

    /// <summary>S3 access key ID.</summary>
    public string S3AccessKey { get; init; } = string.Empty;

    /// <summary>S3 secret access key.</summary>
    public string S3SecretKey { get; init; } = string.Empty;

    /// <summary>S3 bucket name for all asset/media uploads.</summary>
    public string S3BucketName { get; init; } = "uploads";

    /// <summary>Meilisearch HTTP base URL. Default <c>http://localhost:7700</c>.</summary>
    public string MeilisearchUrl { get; init; } = "http://localhost:7700";

    /// <summary>Meilisearch master key. Required for indexing/search; optional for health probes.</summary>
    public string MeilisearchMasterKey { get; init; } = string.Empty;

    /// <summary>Output-cache TTL in seconds for anonymous reads. Default 60.</summary>
    public int OutputCacheTtlSeconds { get; init; } = 60;

    /// <summary>
    /// Max concurrent newsletter dispatch tasks per content-publish event.
    /// Tune upward if subscriber lists are large and bus throughput allows it. Default 10.
    /// </summary>
    public int NewsletterFanOutConcurrency { get; init; } = 10;

    /// <summary>
    /// FollowerCount above which an author is treated as a celebrity and personal feed fan-out is
    /// skipped. Their posts are merged dynamically at read time instead. Default 10 000.
    /// Override via <c>Infrastructure:CelebrityFollowerThreshold</c> in appsettings without redeploy.
    /// </summary>
    public int CelebrityFollowerThreshold { get; init; } = 10_000;
}
