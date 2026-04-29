using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CCE.Api.IntegrationTests.Endpoints;

public class AssistantEndpointTests : IClassFixture<WebApplicationFactory<CCE.Api.External.Program>>
{
    private readonly WebApplicationFactory<CCE.Api.External.Program> _factory;

    public AssistantEndpointTests(WebApplicationFactory<CCE.Api.External.Program> factory)
        => _factory = factory;

    [Fact]
    public async Task Post_query_is_publicly_reachable()
    {
        using var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync(
            "/api/assistant/query",
            new { question = "What is CCE?", locale = "en" });

        resp.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        resp.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Post_query_returns_200_with_reply()
    {
        using var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync(
            "/api/assistant/query",
            new { question = "What is CCE?", locale = "en" });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<ReplyBody>();
        body!.Reply.Should().Contain("sub-project 8");
    }

    [Fact]
    public async Task Post_query_with_invalid_locale_returns_400()
    {
        using var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync(
            "/api/assistant/query",
            new { question = "Hello", locale = "fr" });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_query_with_empty_question_returns_400()
    {
        using var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync(
            "/api/assistant/query",
            new { question = "", locale = "en" });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed record ReplyBody(string Reply);
}
