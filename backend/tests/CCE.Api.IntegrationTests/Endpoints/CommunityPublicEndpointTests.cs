using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CCE.Api.IntegrationTests.Endpoints;

public class CommunityPublicEndpointTests : IClassFixture<WebApplicationFactory<CCE.Api.External.Program>>
{
    private readonly WebApplicationFactory<CCE.Api.External.Program> _factory;

    public CommunityPublicEndpointTests(WebApplicationFactory<CCE.Api.External.Program> factory)
        => _factory = factory;

    [Fact]
    public async Task GetTopicBySlug_unknown_slug_returns_404()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync(new Uri("/api/community/topics/no-such-slug-xyz-99999", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTopicBySlug_returns_200_or_404_for_known_slug()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync(new Uri("/api/community/topics/energy", UriKind.Relative));
        resp.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListPostsInTopic_returns_200_for_any_guid()
    {
        using var client = _factory.CreateClient();
        var id = System.Guid.NewGuid();
        var resp = await client.GetAsync(new Uri($"/api/community/topics/{id}/posts", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPostById_unknown_id_returns_404()
    {
        using var client = _factory.CreateClient();
        var id = System.Guid.NewGuid();
        var resp = await client.GetAsync(new Uri($"/api/community/posts/{id}", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListPostReplies_returns_200_for_any_guid()
    {
        using var client = _factory.CreateClient();
        var id = System.Guid.NewGuid();
        var resp = await client.GetAsync(new Uri($"/api/community/posts/{id}/replies", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMyFollows_unauthenticated_returns_401()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var resp = await client.GetAsync(new Uri("/api/me/follows", UriKind.Relative));
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
