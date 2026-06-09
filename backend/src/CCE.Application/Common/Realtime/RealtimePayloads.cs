namespace CCE.Application.Common.Realtime;

// Minimal realtime payloads (ids + a little context). Clients refetch full state when needed — mirrors
// the existing reply/vote payload style.

/// <summary>A post was published in a community/topic.</summary>
public sealed record NewPostRealtime(
    System.Guid PostId, System.Guid CommunityId, System.Guid TopicId, string Title);

/// <summary>A post or reply was moderated (e.g. soft-deleted).</summary>
public sealed record PostModeratedRealtime(
    System.Guid PostId, System.Guid? ReplyId, string Action);

/// <summary>Moderation-room event: content was acted on by a moderator.</summary>
public sealed record ContentModeratedRealtime(
    string ContentType, System.Guid ContentId, System.Guid PostId, System.Guid ModeratorId, string Action);

/// <summary>Viewer count for a post changed (presence).</summary>
public sealed record PresenceChangedRealtime(System.Guid PostId, int Viewers);

/// <summary>A user started/stopped typing on a post.</summary>
public sealed record TypingChangedRealtime(System.Guid PostId, System.Guid UserId, bool IsTyping);
