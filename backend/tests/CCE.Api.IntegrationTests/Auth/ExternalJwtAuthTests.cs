using System.Net;
using System.Net.Http.Headers;
using CCE.Api.External;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CCE.Api.IntegrationTests.Auth;

public class ExternalJwtAuthTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ExternalJwtAuthTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Returns_401_without_token()
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync(new Uri("/auth/echo", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Returns_401_with_garbage_token()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-real-jwt");

        var resp = await client.GetAsync(new Uri("/auth/echo", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
