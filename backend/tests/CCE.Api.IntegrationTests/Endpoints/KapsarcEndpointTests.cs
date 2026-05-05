using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CCE.Api.IntegrationTests.Endpoints;

public class KapsarcEndpointTests : IClassFixture<CceTestWebApplicationFactory<CCE.Api.External.Program>>
{
    private readonly CceTestWebApplicationFactory<CCE.Api.External.Program> _factory;

    public KapsarcEndpointTests(CceTestWebApplicationFactory<CCE.Api.External.Program> factory)
        => _factory = factory;

    [Fact]
    public async Task GetSnapshot_is_publicly_reachable()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync(
            new Uri($"/api/kapsarc/snapshots/{System.Guid.NewGuid()}", UriKind.Relative));

        resp.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        resp.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetSnapshot_unknown_country_returns_404()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync(
            new Uri($"/api/kapsarc/snapshots/{System.Guid.NewGuid()}", UriKind.Relative));

        // Table is empty in dev/test — 404 is the expected response per spec.
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
