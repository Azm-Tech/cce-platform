namespace CCE.Api.Common.Auth;

/// <summary>Keycloak token endpoint response shape.</summary>
public sealed record BffTokenResponse(
    [property: System.Text.Json.Serialization.JsonPropertyName("access_token")] string AccessToken,
    [property: System.Text.Json.Serialization.JsonPropertyName("refresh_token")] string RefreshToken,
    [property: System.Text.Json.Serialization.JsonPropertyName("expires_in")] int ExpiresIn);
