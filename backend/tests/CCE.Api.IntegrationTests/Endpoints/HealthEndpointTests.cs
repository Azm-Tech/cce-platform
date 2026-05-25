using System.Net;
using System.Text.Json;
using CCE.Api.External;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CCE.Api.IntegrationTests.Endpoints;

public class HealthEndpointTests : IClassFixture<WebApplicationFactory<CCE.Api.External.Program>>
{
    private readonly WebApplicationFactory<CCE.Api.External.Program> _factory;

    public HealthEndpointTests(WebApplicationFactory<CCE.Api.External.Program> factory) => _factory = factory;

    [Fact]
    public async Task Returns_ok_status_with_locale_from_accept_language()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AcceptLanguage.Clear();
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en");

        var resp = await client.GetAsync(new Uri("/health", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body).RootElement;
        doc.GetProperty("status").GetString().Should().Be("ok");
        doc.GetProperty("locale").GetString().Should().Be("en");
        doc.GetProperty("version").GetString().Should().NotBeNullOrWhiteSpace();
    }
}
