namespace CCE.Api.Common.Auth;

public sealed class BffOptions
{
    public const string SectionName = "Bff";

    /// <summary>Keycloak realm (e.g., <c>cce-public</c>).</summary>
    public string KeycloakRealm { get; set; } = string.Empty;

    /// <summary>Keycloak client id (e.g., <c>cce-public-portal</c>).</summary>
    public string KeycloakClientId { get; set; } = string.Empty;

    /// <summary>Keycloak client secret (dev-only — prod uses public-client + PKCE without secret).</summary>
    public string KeycloakClientSecret { get; set; } = string.Empty;

    /// <summary>Cookie domain attribute. Default <c>localhost</c>.</summary>
    public string CookieDomain { get; set; } = "localhost";

    /// <summary>Session cookie lifetime in minutes (sliding). Default 30.</summary>
    public int SessionLifetimeMinutes { get; set; } = 30;

    /// <summary>Keycloak base URL. Default <c>http://localhost:8080</c>.</summary>
    public string KeycloakBaseUrl { get; set; } = "http://localhost:8080";
}
