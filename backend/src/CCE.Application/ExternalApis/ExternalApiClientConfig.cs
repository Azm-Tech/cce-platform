namespace CCE.Application.ExternalApis;

/// <summary>
/// Per-client configuration used by <c>AddExternalApiClient&lt;TClient&gt;</c>.
/// Bound from <c>ExternalApis:{ApiName}</c> in appsettings.
/// </summary>
public sealed class ExternalApiClientConfig
{
    public string BaseUrl { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; } = 30;
    public ExternalApiAuthConfig Auth { get; init; } = new();
}
