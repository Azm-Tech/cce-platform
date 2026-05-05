using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CCE.Api.IntegrationTests.Endpoints;

public class AssistantEndpointTests : IClassFixture<CceTestWebApplicationFactory<CCE.Api.External.Program>>
{
    private readonly CceTestWebApplicationFactory<CCE.Api.External.Program> _factory;

    public AssistantEndpointTests(CceTestWebApplicationFactory<CCE.Api.External.Program> factory)
        => _factory = factory;

    [Fact]
    public async Task Post_query_is_publicly_reachable()
    {
        using var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync(
            "/api/assistant/query",
            new
            {
                messages = new[] { new { role = "user", content = "What is CCE?" } },
                locale = "en",
            });

        resp.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        resp.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Post_query_streams_text_and_done_events_in_order()
    {
        using var client = _factory.CreateClient();
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/assistant/query");
        req.Content = JsonContent.Create(new
        {
            messages = new[] { new { role = "user", content = "What is CCE?" } },
            locale = "en",
        });
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseContentRead);
        resp.EnsureSuccessStatusCode();
        resp.Content.Headers.ContentType?.MediaType.Should().Be("text/event-stream");

        var raw = await resp.Content.ReadAsStringAsync();
        var events = raw.Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
            .Select(frame => frame.Replace("data: ", string.Empty, StringComparison.Ordinal))
            .Select(json => JsonDocument.Parse(json).RootElement)
            .ToList();

        var types = events.Select(e => e.GetProperty("type").GetString()).ToList();
        types.Count(t => t == "text").Should().BeGreaterThanOrEqualTo(5);
        types.Last().Should().Be("done");
    }
}
