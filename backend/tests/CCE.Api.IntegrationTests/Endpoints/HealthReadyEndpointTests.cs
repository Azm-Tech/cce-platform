using System.Collections.Generic;
using System.Net;
using CCE.Api.External;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace CCE.Api.IntegrationTests.Endpoints;

public class HealthReadyEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthReadyEndpointTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Returns_200_when_all_dependencies_healthy()
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync(new Uri("/health/ready", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Returns_503_when_a_dependency_fails()
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Infrastructure:RedisConnectionString", "localhost:1");
        });
        var client = factory.CreateClient();

        var resp = await client.GetAsync(new Uri("/health/ready", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }
}
