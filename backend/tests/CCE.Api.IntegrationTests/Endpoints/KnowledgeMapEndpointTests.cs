using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CCE.Api.IntegrationTests.Endpoints;

public class KnowledgeMapEndpointTests : IClassFixture<WebApplicationFactory<CCE.Api.External.Program>>
{
    private readonly WebApplicationFactory<CCE.Api.External.Program> _factory;

    public KnowledgeMapEndpointTests(WebApplicationFactory<CCE.Api.External.Program> factory) => _factory = factory;

    [Fact]
    public async Task List_returns_200()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync(new Uri("/api/knowledge-maps", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_unknown_returns_404()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync(new Uri($"/api/knowledge-maps/{System.Guid.NewGuid()}", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListNodes_returns_200()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync(new Uri($"/api/knowledge-maps/{System.Guid.NewGuid()}/nodes", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListEdges_returns_200()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync(new Uri($"/api/knowledge-maps/{System.Guid.NewGuid()}/edges", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
