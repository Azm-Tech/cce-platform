using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CCE.Api.IntegrationTests.Endpoints;

public class ProfileEndpointTests : IClassFixture<CceTestWebApplicationFactory<CCE.Api.External.Program>>
{
    private readonly CceTestWebApplicationFactory<CCE.Api.External.Program> _factory;

    public ProfileEndpointTests(CceTestWebApplicationFactory<CCE.Api.External.Program> factory) => _factory = factory;

    [Fact]
    public async Task Register_anonymous_returns_401()
    {
        // Sub-11 Phase 01: registration is now an admin-only POST that calls
        // EntraIdRegistrationService against Microsoft Graph. Was a Keycloak
        // redirect; that test was deleted with the old behavior. This test
        // covers the unauth path; happy-path coverage lives in
        // EntraIdRegistrationTests against WireMock.
        using var client = _factory.CreateClient();
        using var content = new System.Net.Http.StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        var resp = await client.PostAsync(new Uri("/api/users/register", UriKind.Relative), content);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMe_anonymous_returns_401()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync(new Uri("/api/me", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PutMe_anonymous_returns_401()
    {
        using var client = _factory.CreateClient();
        using var content = new System.Net.Http.StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        var resp = await client.PutAsync(new Uri("/api/me", UriKind.Relative), content);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Submit_anonymous_returns_401()
    {
        using var client = _factory.CreateClient();
        using var content = new System.Net.Http.StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        var resp = await client.PostAsync(new Uri("/api/users/expert-request", UriKind.Relative), content);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Status_anonymous_returns_401()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync(new Uri("/api/me/expert-status", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
