using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CCE.Api.IntegrationTests.Endpoints;

public class ResourcesPublicEndpointTests : IClassFixture<WebApplicationFactory<CCE.Api.External.Program>>
{
    private readonly WebApplicationFactory<CCE.Api.External.Program> _factory;

    public ResourcesPublicEndpointTests(WebApplicationFactory<CCE.Api.External.Program> factory) => _factory = factory;

    [Fact]
    public async Task List_returns_200()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync(new Uri("/api/resources?page=1&pageSize=20", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_unknown_returns_404()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync(new Uri($"/api/resources/{System.Guid.NewGuid()}", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Download_unknown_returns_404()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync(new Uri($"/api/resources/{System.Guid.NewGuid()}/download", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
