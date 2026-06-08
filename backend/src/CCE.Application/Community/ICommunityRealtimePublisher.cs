namespace CCE.Application.Community;

/// <summary>
/// Pushes live community events to clients subscribed to a post's SignalR group (<c>post:{id}</c>):
/// vote-count changes, new replies, and poll-result changes (§11). Best-effort presence push —
/// not a stored notification.
/// </summary>
public interface ICommunityRealtimePublisher
{
    Task PublishToPostAsync(Guid postId, string eventName, object payload, CancellationToken ct);
}
