using CCE.Application.Community;
using Microsoft.AspNetCore.SignalR;

namespace CCE.Infrastructure.Notifications;

/// <summary>SignalR implementation: broadcasts to the <c>post:{id}</c> group on the notifications hub.</summary>
public sealed class CommunityRealtimePublisher : ICommunityRealtimePublisher
{
    private readonly IHubContext<NotificationsHub> _hub;

    public CommunityRealtimePublisher(IHubContext<NotificationsHub> hub) => _hub = hub;

    public Task PublishToPostAsync(Guid postId, string eventName, object payload, CancellationToken ct)
        => _hub.Clients.Group($"post:{postId}").SendAsync(eventName, payload, ct);
}
