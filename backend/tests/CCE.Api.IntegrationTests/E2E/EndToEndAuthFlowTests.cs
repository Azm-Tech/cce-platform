using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CCE.Api.Internal;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CCE.Api.IntegrationTests.E2E;

public class EndToEndAuthFlowTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EndToEndAuthFlowTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private static async Task<string> AcquireUserTokenAsync()
    {
        // Use the realm's master admin credentials (Phase 02 Task 2.4 pattern)
        // to get an admin-API token, then create a TEMPORARY service-account token via cce-admin-cms.
        // For real user-flow testing in higher phases, we'd use authorization code flow.
        // For Foundation E2E, we rely on the cce-admin-cms service-account token + verifying the API
        // accepts it — proves JWKS validation works end-to-end.
        using var http = new HttpClient { BaseAddress = new Uri("http://localhost:8080") };

        using var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", "cce-admin-cms"),
            new KeyValuePair<string, string>("client_secret", "dev-internal-secret-change-me")
        });
        var resp = await http.PostAsync(new Uri("/realms/cce-internal/protocol/openid-connect/token", UriKind.Relative), form);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<TokenResponse>();
        return json!.AccessToken;
    }

    [Fact]
    public async Task Anonymous_health_returns_200()
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync(new Uri("/health", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Authenticated_endpoint_accepts_keycloak_token()
    {
        var token = await AcquireUserTokenAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await client.GetAsync(new Uri("/auth/echo", UriKind.Relative));

        // Service-account token may not pass SuperAdmin policy — but the bearer is validated.
        // Acceptable outcomes: 200 (token + claims OK) or 403 (token validated but missing SuperAdmin).
        resp.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Health_ready_returns_200_when_dependencies_up()
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync(new Uri("/health/ready", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Anonymous_auth_endpoint_returns_401()
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync(new Uri("/auth/echo", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record TokenResponse(
        [property: System.Text.Json.Serialization.JsonPropertyName("access_token")] string AccessToken);
}
