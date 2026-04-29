using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CCE.Api.IntegrationTests.Endpoints;

public class NewsPublicEndpointTests : IClassFixture<WebApplicationFactory<CCE.Api.External.Program>>
{
    private readonly WebApplicationFactory<CCE.Api.External.Program> _factory;

    public NewsPublicEndpointTests(WebApplicationFactory<CCE.Api.External.Program> factory) => _factory = factory;

    [Fact]
    public async Task List_returns_200()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync(new Uri("/api/news?page=1&pageSize=20", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetBySlug_unknown_returns_404()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync(new Uri("/api/news/no-such-slug-xyz", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
