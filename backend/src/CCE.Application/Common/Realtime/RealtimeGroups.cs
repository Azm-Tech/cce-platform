namespace CCE.Application.Common.Realtime;

/// <summary>
/// Single source of truth for SignalR group ("room") names, shared by the hub, the publishers, and the
/// handlers that broadcast. Keeps the wire contract consistent (like <c>CacheRegions</c> for the cache).
/// </summary>
public static class RealtimeGroups
{
    /// <summary>Global room joined by moderators on connect; receives <c>ContentModerated</c>.</summary>
    public const string Moderation = "moderation";

    /// <summary>Per-user room (auto-joined on connect) for personal notifications.</summary>
    public static string User(string userId) => $"user:{userId}";

    /// <summary>Per-post room for live reply/vote/poll/presence/typing events.</summary>
    public static string Post(System.Guid postId) => $"post:{postId}";

    /// <summary>Per-community room for community-feed events (new post, moderation).</summary>
    public static string Community(System.Guid communityId) => $"community:{communityId}";

    /// <summary>Per-topic room for topic-feed events (new post).</summary>
    public static string Topic(System.Guid topicId) => $"topic:{topicId}";
}
