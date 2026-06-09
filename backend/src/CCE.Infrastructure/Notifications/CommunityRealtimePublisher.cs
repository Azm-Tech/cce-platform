using CCE.Application.Common.Realtime;
using CCE.Application.Community;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CCE.Infrastructure.Notifications;

/// <summary>
/// SignalR implementation: broadcasts to the post/community/topic/moderation rooms on the notifications
/// hub. With the Redis backplane wired (<c>AddCceSignalR</c>) these reach clients on any process.
///
/// Best-effort: a <see cref="RedisException"/> is caught and logged as a warning so the API stays up
/// when Redis is unavailable (normal for local dev without a running Redis instance).
/// </summary>
public sealed class CommunityRealtimePublisher : ICommunityRealtimePublisher
{
    private readonly IHubContext<NotificationsHub> _hub;
    private readonly ILogger<CommunityRealtimePublisher> _logger;

    public CommunityRealtimePublisher(IHubContext<NotificationsHub> hub, ILogger<CommunityRealtimePublisher> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task PublishToPostAsync(Guid postId, string eventName, object payload, CancellationToken ct)
    {
        try
        {
            await _hub.Clients.Group(RealtimeGroups.Post(postId)).SendAsync(eventName, payload, ct).ConfigureAwait(false);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for realtime publish to post {PostId} ({Event}); skipping.", postId, eventName);
        }
    }

    public async Task PublishToCommunityAsync(Guid communityId, string eventName, object payload, CancellationToken ct)
    {
        try
        {
            await _hub.Clients.Group(RealtimeGroups.Community(communityId)).SendAsync(eventName, payload, ct).ConfigureAwait(false);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for realtime publish to community {CommunityId} ({Event}); skipping.", communityId, eventName);
        }
    }

    public async Task PublishToTopicAsync(Guid topicId, string eventName, object payload, CancellationToken ct)
    {
        try
        {
            await _hub.Clients.Group(RealtimeGroups.Topic(topicId)).SendAsync(eventName, payload, ct).ConfigureAwait(false);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for realtime publish to topic {TopicId} ({Event}); skipping.", topicId, eventName);
        }
    }

    public async Task PublishToModeratorsAsync(string eventName, object payload, CancellationToken ct)
    {
        try
        {
            await _hub.Clients.Group(RealtimeGroups.Moderation).SendAsync(eventName, payload, ct).ConfigureAwait(false);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable for realtime publish to moderators ({Event}); skipping.", eventName);
        }
    }
}
