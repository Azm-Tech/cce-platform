using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CCE.Api.IntegrationTests.Endpoints;

public class CountriesPublicEndpointTests : IClassFixture<WebApplicationFactory<CCE.Api.External.Program>>
{
    private readonly WebApplicationFactory<CCE.Api.External.Program> _factory;

    public CountriesPublicEndpointTests(WebApplicationFactory<CCE.Api.External.Program> factory) => _factory = factory;

    [Fact]
    public async Task List_returns_200()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync(new Uri("/api/countries", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProfile_unknown_country_returns_404()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync(new Uri($"/api/countries/{System.Guid.NewGuid()}/profile", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_with_search_returns_200()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync(new Uri("/api/countries?search=United", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
