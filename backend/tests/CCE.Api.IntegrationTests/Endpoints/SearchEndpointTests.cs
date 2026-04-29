using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CCE.Api.IntegrationTests.Endpoints;

public class SearchEndpointTests : IClassFixture<WebApplicationFactory<CCE.Api.External.Program>>
{
    private readonly WebApplicationFactory<CCE.Api.External.Program> _factory;

    public SearchEndpointTests(WebApplicationFactory<CCE.Api.External.Program> factory) => _factory = factory;

    [Fact]
    public async Task Search_endpoint_is_publicly_reachable()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync(new Uri("/api/search?q=carbon&page=1&pageSize=10", UriKind.Relative));

        // Acceptable: 200 (Meili hit), 503 (Meili unreachable from test env).
        // NOT acceptable: 401/403 (the endpoint must be anonymous-OK).
        resp.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        resp.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Search_with_empty_q_returns_400()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync(new Uri("/api/search?q=", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
