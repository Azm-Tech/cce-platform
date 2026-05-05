using System.Net;
using System.Net.Http.Json;

namespace CCE.Api.IntegrationTests.Endpoints;

public class InteractiveCityEndpointTests : IClassFixture<CceTestWebApplicationFactory<CCE.Api.External.Program>>
{
    private readonly CceTestWebApplicationFactory<CCE.Api.External.Program> _factory;

    public InteractiveCityEndpointTests(CceTestWebApplicationFactory<CCE.Api.External.Program> factory)
        => _factory = factory;

    private HttpClient AnonClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false
    });

    [Fact]
    public async Task ListTechnologies_anonymous_returns_200()
    {
        using var client = AnonClient();
        var resp = await client.GetAsync(new Uri("/api/interactive-city/technologies", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RunScenario_anonymous_returns_200()
    {
        using var client = AnonClient();
        var resp = await client.PostAsJsonAsync(
            new Uri("/api/interactive-city/scenarios/run", UriKind.Relative),
            new { cityType = 0, targetYear = 2040, configurationJson = "{\"technologyIds\":[]}" });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SaveScenario_anonymous_returns_401()
    {
        using var client = AnonClient();
        var resp = await client.PostAsJsonAsync(
            new Uri("/api/me/interactive-city/scenarios", UriKind.Relative),
            new { nameAr = "سيناريو", nameEn = "Scenario", cityType = 0, targetYear = 2040,
                  configurationJson = "{\"technologyIds\":[]}" });
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListMyScenarios_anonymous_returns_401()
    {
        using var client = AnonClient();
        var resp = await client.GetAsync(new Uri("/api/me/interactive-city/scenarios", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteMyScenario_anonymous_returns_401()
    {
        using var client = AnonClient();
        var resp = await client.DeleteAsync(
            new Uri($"/api/me/interactive-city/scenarios/{System.Guid.NewGuid()}", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
