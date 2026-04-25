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
}
