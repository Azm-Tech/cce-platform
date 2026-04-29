using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CCE.Api.IntegrationTests.Endpoints;

/// <summary>
/// Smoke-tests: all community write endpoints return 401 for anonymous requests.
/// </summary>
public class CommunityWriteEndpointTests : IClassFixture<WebApplicationFactory<CCE.Api.External.Program>>
{
    private readonly WebApplicationFactory<CCE.Api.External.Program> _factory;

    public CommunityWriteEndpointTests(WebApplicationFactory<CCE.Api.External.Program> factory)
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
    public async Task FollowTopic_anonymous_returns_401()
    {
        using var client = AnonClient();
        var resp = await client.PostAsync(
            new Uri($"/api/me/follows/topics/{System.Guid.NewGuid()}", UriKind.Relative), null);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UnfollowTopic_anonymous_returns_401()
    {
        using var client = AnonClient();
        var resp = await client.DeleteAsync(
            new Uri($"/api/me/follows/topics/{System.Guid.NewGuid()}", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task FollowUser_anonymous_returns_401()
    {
        using var client = AnonClient();
        var resp = await client.PostAsync(
            new Uri($"/api/me/follows/users/{System.Guid.NewGuid()}", UriKind.Relative), null);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UnfollowUser_anonymous_returns_401()
    {
        using var client = AnonClient();
        var resp = await client.DeleteAsync(
            new Uri($"/api/me/follows/users/{System.Guid.NewGuid()}", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task FollowPost_anonymous_returns_401()
    {
        using var client = AnonClient();
        var resp = await client.PostAsync(
            new Uri($"/api/me/follows/posts/{System.Guid.NewGuid()}", UriKind.Relative), null);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UnfollowPost_anonymous_returns_401()
    {
        using var client = AnonClient();
        var resp = await client.DeleteAsync(
            new Uri($"/api/me/follows/posts/{System.Guid.NewGuid()}", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
