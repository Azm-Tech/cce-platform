namespace CCE.Application.ExternalApis;

/// <summary>
/// Authentication configuration for an external API client.
/// Only the fields relevant to <see cref="ExternalApiAuthType"/> need to be populated.
/// </summary>
public sealed class ExternalApiAuthConfig
{
    public ExternalApiAuthType Type { get; init; } = ExternalApiAuthType.None;

    // ApiKey
    public string KeyName { get; init; } = string.Empty;
    public string KeyLocation { get; init; } = "Header";
    public string Value { get; init; } = string.Empty;

    // Bearer
    public string Token { get; init; } = string.Empty;

    // Basic & OAuth2 shared
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;

    // OAuth2
    public string TokenUrl { get; init; } = string.Empty;
    public string Scope { get; init; } = string.Empty;
    public bool AutoRefresh { get; init; } = true;
}
