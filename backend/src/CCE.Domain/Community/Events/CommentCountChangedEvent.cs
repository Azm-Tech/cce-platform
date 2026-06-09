using CCE.Domain.Common;

namespace CCE.Domain.Community.Events;

/// <summary>
/// Raised when the <see cref="Post.CommentsCount"/> changes (reply created or deleted).
/// Bridge handlers (e.g., CommentCountChangedBusPublisher) can fan this onto the bus for
/// real-time updates.
/// </summary>
public sealed record CommentCountChangedEvent(
    System.Guid PostId,
    int CommentsCount,
    System.DateTimeOffset OccurredOn) : IDomainEvent;
