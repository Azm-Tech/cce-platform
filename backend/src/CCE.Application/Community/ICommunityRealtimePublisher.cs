namespace CCE.Application.Community;

/// <summary>
/// Pushes live community events to clients subscribed to a post's SignalR group (<c>post:{id}</c>):
/// vote-count changes, new replies, and poll-result changes (§11). Best-effort presence push —
/// not a stored notification.
/// </summary>
public interface ICommunityRealtimePublisher
{
    /// <summary>Broadcast to the <c>post:{id}</c> room.</summary>
    Task PublishToPostAsync(Guid postId, string eventName, object payload, CancellationToken ct);

    /// <summary>Broadcast to the <c>community:{id}</c> room.</summary>
    Task PublishToCommunityAsync(Guid communityId, string eventName, object payload, CancellationToken ct);

    /// <summary>Broadcast to the <c>topic:{id}</c> room.</summary>
    Task PublishToTopicAsync(Guid topicId, string eventName, object payload, CancellationToken ct);

    /// <summary>Broadcast to the global <c>moderation</c> room (moderators only).</summary>
    Task PublishToModeratorsAsync(string eventName, object payload, CancellationToken ct);
}
