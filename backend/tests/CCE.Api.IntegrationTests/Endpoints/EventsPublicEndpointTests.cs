using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CCE.Api.IntegrationTests.Endpoints;

public class EventsPublicEndpointTests : IClassFixture<WebApplicationFactory<CCE.Api.External.Program>>
{
    private readonly WebApplicationFactory<CCE.Api.External.Program> _factory;

    public EventsPublicEndpointTests(WebApplicationFactory<CCE.Api.External.Program> factory) => _factory = factory;

    [Fact]
    public async Task List_returns_200_with_paged_result_shape()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync(new Uri("/api/events?page=1&pageSize=20", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body).RootElement;
        doc.GetProperty("items").ValueKind.Should().Be(JsonValueKind.Array);
        doc.GetProperty("page").GetInt32().Should().Be(1);
        doc.GetProperty("pageSize").GetInt32().Should().Be(20);
        doc.GetProperty("total").GetInt64().Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetById_unknown_returns_404()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync(new Uri($"/api/events/{System.Guid.NewGuid()}", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetIcs_unknown_returns_404()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync(new Uri($"/api/events/{System.Guid.NewGuid()}.ics", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
