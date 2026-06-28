using System.Net;
using CCE.Api.External;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CCE.Api.IntegrationTests.Endpoints;

public class NotificationsEndpointTests : IClassFixture<WebApplicationFactory<CCE.Api.External.Program>>
{
    private readonly WebApplicationFactory<CCE.Api.External.Program> _factory;

    public NotificationsEndpointTests(WebApplicationFactory<CCE.Api.External.Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ListNotifications_anonymous_returns_401()
    {
        using var client = _factory.CreateClient();

        var resp = await client.GetAsync(new Uri("/api/me/notifications", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UnreadCount_anonymous_returns_401()
    {
        using var client = _factory.CreateClient();

        var resp = await client.GetAsync(new Uri("/api/me/notifications/unread-count", UriKind.Relative));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MarkRead_anonymous_returns_401()
    {
        using var client = _factory.CreateClient();

        var resp = await client.PostAsync(
            new Uri($"/api/me/notifications/{System.Guid.NewGuid()}/mark-read", UriKind.Relative),
            null);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MarkAllRead_anonymous_returns_401()
    {
        using var client = _factory.CreateClient();

        var resp = await client.PostAsync(
            new Uri("/api/me/notifications/mark-all-read", UriKind.Relative),
            null);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
