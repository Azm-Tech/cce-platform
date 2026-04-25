using System.Net;
using System.Text.Json;
using CCE.Api.External;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CCE.Api.IntegrationTests.OpenApi;

public class SwaggerEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SwaggerEndpointTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Swagger_json_is_served_and_well_formed()
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync(new Uri("/swagger/v1/swagger.json", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body).RootElement;
        doc.GetProperty("openapi").GetString().Should().StartWith("3.");
        doc.GetProperty("info").GetProperty("title").GetString().Should().NotBeNullOrWhiteSpace();
    }
}
