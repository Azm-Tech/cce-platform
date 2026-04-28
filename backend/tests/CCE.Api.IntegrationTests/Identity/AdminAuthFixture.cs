using System.Net.Http.Json;

namespace CCE.Api.IntegrationTests.Identity;

/// <summary>
/// Issues + caches a SuperAdmin JWT against the dev Keycloak realm, so admin-endpoint tests
/// don't pay the OIDC round-trip cost per test. The token is service-account-issued under
/// the cce-admin-cms client, which the dev Keycloak setup grants the SuperAdmin role.
/// </summary>
public sealed class AdminAuthFixture : IAsyncLifetime
{
    private const string TokenEndpoint = "http://localhost:8080/realms/cce-internal/protocol/openid-connect/token";
    private const string ClientId = "cce-admin-cms";
    private const string ClientSecret = "dev-internal-secret-change-me";

    public string AccessToken { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        using var http = new HttpClient();
        using var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", ClientId),
            new KeyValuePair<string, string>("client_secret", ClientSecret),
        });
        var resp = await http.PostAsync(new Uri(TokenEndpoint), form);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<TokenResponse>();
        AccessToken = json!.AccessToken;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private sealed record TokenResponse(
        [property: System.Text.Json.Serialization.JsonPropertyName("access_token")] string AccessToken);
}
