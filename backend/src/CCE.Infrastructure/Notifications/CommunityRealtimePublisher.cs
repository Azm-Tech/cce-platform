using CCE.Application.Common.Realtime;
using CCE.Application.Community;
using Microsoft.AspNetCore.SignalR;

namespace CCE.Infrastructure.Notifications;

/// <summary>
/// SignalR implementation: broadcasts to the post/community/topic/moderation rooms on the notifications
/// hub. With the Redis backplane wired (<c>AddCceSignalR</c>) these reach clients on any process.
/// </summary>
public sealed class CommunityRealtimePublisher : ICommunityRealtimePublisher
{
    private readonly IHubContext<NotificationsHub> _hub;

    public CommunityRealtimePublisher(IHubContext<NotificationsHub> hub) => _hub = hub;

    public Task PublishToPostAsync(Guid postId, string eventName, object payload, CancellationToken ct)
        => _hub.Clients.Group(RealtimeGroups.Post(postId)).SendAsync(eventName, payload, ct);

    public Task PublishToCommunityAsync(Guid communityId, string eventName, object payload, CancellationToken ct)
        => _hub.Clients.Group(RealtimeGroups.Community(communityId)).SendAsync(eventName, payload, ct);

    public Task PublishToTopicAsync(Guid topicId, string eventName, object payload, CancellationToken ct)
        => _hub.Clients.Group(RealtimeGroups.Topic(topicId)).SendAsync(eventName, payload, ct);

    public Task PublishToModeratorsAsync(string eventName, object payload, CancellationToken ct)
        => _hub.Clients.Group(RealtimeGroups.Moderation).SendAsync(eventName, payload, ct);
}
