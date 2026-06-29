namespace CCE.Application.Common.Realtime;

// Minimal realtime payloads (ids + a little context). Clients refetch full state when needed — mirrors
// the existing reply/vote payload style.

/// <summary>
/// Outer wrapper for every server→client push. Gives the client an eventId for dedup,
/// a timestamp for ordering, and a stable nesting shape so payload schemas can evolve
/// independently of the envelope. eventId is a random GUID (not monotonic) — order by
/// OccurredOn; use EventId only to drop duplicates after a reconnect.
/// </summary>
public sealed record RealtimeEnvelope(
    System.Guid EventId,
    System.DateTimeOffset OccurredOn,
    object Payload)
{
    public static RealtimeEnvelope Wrap(object payload) =>
        new(System.Guid.NewGuid(), System.DateTimeOffset.UtcNow, payload);
}

/// <summary>A post was published in a community/topic.</summary>
public sealed record NewPostRealtime(
    System.Guid PostId, System.Guid CommunityId, System.Guid TopicId, string Title);

/// <summary>A post or reply was moderated (e.g. soft-deleted).</summary>
public sealed record PostModeratedRealtime(
    System.Guid PostId, System.Guid? ReplyId, string Action);

/// <summary>Moderation-room event: content was acted on by a moderator.</summary>
public sealed record ContentModeratedRealtime(
    string ContentType, System.Guid ContentId, System.Guid PostId, System.Guid ModeratorId, string Action);

/// <summary>
/// Moderation-room event: the AI pipeline flagged or rejected content (no human moderator).
/// <paramref name="Status"/> is the <c>ModerationStatus</c> name ("Flagged" | "Rejected").
/// </summary>
public sealed record ContentFlaggedRealtime(
    string ContentType, System.Guid ContentId, string Status, string? Category, string? Reason);

/// <summary>Viewer count for a post changed (presence).</summary>
public sealed record PresenceChangedRealtime(System.Guid PostId, int Viewers);

/// <summary>A user started/stopped typing on a post.</summary>
public sealed record TypingChangedRealtime(System.Guid PostId, System.Guid UserId, bool IsTyping);
