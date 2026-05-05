using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CCE.Api.IntegrationTests.OpenApi;

public class SwaggerEndpointTests
    : IClassFixture<CceTestWebApplicationFactory<CCE.Api.External.Program>>,
      IClassFixture<CceTestWebApplicationFactory<CCE.Api.Internal.Program>>
{
    private readonly CceTestWebApplicationFactory<CCE.Api.External.Program> _externalFactory;
    private readonly CceTestWebApplicationFactory<CCE.Api.Internal.Program> _internalFactory;

    public SwaggerEndpointTests(
        CceTestWebApplicationFactory<CCE.Api.External.Program> externalFactory,
        CceTestWebApplicationFactory<CCE.Api.Internal.Program> internalFactory)
    {
        _externalFactory = externalFactory;
        _internalFactory = internalFactory;
    }

    [Fact]
    public async Task External_swagger_json_is_served_and_well_formed()
    {
        var client = _externalFactory.CreateClient();

        var resp = await client.GetAsync(new Uri("/swagger/external/v1/swagger.json", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body).RootElement;
        doc.GetProperty("openapi").GetString().Should().StartWith("3.");
        doc.GetProperty("info").GetProperty("title").GetString().Should().Be("CCE External API");
    }

    [Fact]
    public async Task Internal_swagger_json_is_served_and_well_formed()
    {
        var client = _internalFactory.CreateClient();

        var resp = await client.GetAsync(new Uri("/swagger/internal/v1/swagger.json", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body).RootElement;
        doc.GetProperty("openapi").GetString().Should().StartWith("3.");
        doc.GetProperty("info").GetProperty("title").GetString().Should().Be("CCE Internal API");
    }
}
