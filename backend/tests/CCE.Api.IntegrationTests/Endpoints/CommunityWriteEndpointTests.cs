using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CCE.Api.IntegrationTests.Endpoints;

/// <summary>
/// Smoke-tests: all community write endpoints return 401 for anonymous requests.
/// </summary>
public class CommunityWriteEndpointTests : IClassFixture<CceTestWebApplicationFactory<CCE.Api.External.Program>>
{
    private readonly CceTestWebApplicationFactory<CCE.Api.External.Program> _factory;

    public CommunityWriteEndpointTests(CceTestWebApplicationFactory<CCE.Api.External.Program> factory)
        => _factory = factory;

    private HttpClient AnonClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false
    });

    [Fact]
    public async Task CreatePost_anonymous_returns_401()
    {
        using var client = AnonClient();
        var resp = await client.PostAsJsonAsync(
            new Uri("/api/community/posts", UriKind.Relative),
            new { topicId = System.Guid.NewGuid(), content = "hello", locale = "en", isAnswerable = false });
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateReply_anonymous_returns_401()
    {
        using var client = AnonClient();
        var postId = System.Guid.NewGuid();
        var resp = await client.PostAsJsonAsync(
            new Uri($"/api/community/posts/{postId}/replies", UriKind.Relative),
            new { content = "reply", locale = "en" });
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RatePost_anonymous_returns_401()
    {
        using var client = AnonClient();
        var postId = System.Guid.NewGuid();
        var resp = await client.PostAsJsonAsync(
            new Uri($"/api/community/posts/{postId}/rate", UriKind.Relative),
            new { stars = 4 });
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MarkAnswer_anonymous_returns_401()
    {
        using var client = AnonClient();
        var postId = System.Guid.NewGuid();
        var resp = await client.PostAsJsonAsync(
            new Uri($"/api/community/posts/{postId}/mark-answer", UriKind.Relative),
            new { replyId = System.Guid.NewGuid() });
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task EditReply_anonymous_returns_401()
    {
        using var client = AnonClient();
        var replyId = System.Guid.NewGuid();
        var resp = await client.PutAsJsonAsync(
            new Uri($"/api/community/replies/{replyId}", UriKind.Relative),
            new { content = "edited" });
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SetTopicFollow_anonymous_returns_401()
    {
        using var client = AnonClient();
        var resp = await client.PutAsJsonAsync(
            new Uri($"/api/me/follows/topics/{System.Guid.NewGuid()}", UriKind.Relative),
            new { status = "Followed" });
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SetUserFollow_anonymous_returns_401()
    {
        using var client = AnonClient();
        var resp = await client.PutAsJsonAsync(
            new Uri($"/api/me/follows/users/{System.Guid.NewGuid()}", UriKind.Relative),
            new { status = "Followed" });
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SetPostFollow_anonymous_returns_401()
    {
        using var client = AnonClient();
        var resp = await client.PutAsJsonAsync(
            new Uri($"/api/me/follows/posts/{System.Guid.NewGuid()}", UriKind.Relative),
            new { status = "Unfollowed" });
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SetCommunityFollow_anonymous_returns_401()
    {
        using var client = AnonClient();
        var resp = await client.PutAsJsonAsync(
            new Uri($"/api/community/communities/{System.Guid.NewGuid()}/follow", UriKind.Relative),
            new { status = "Followed" });
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
