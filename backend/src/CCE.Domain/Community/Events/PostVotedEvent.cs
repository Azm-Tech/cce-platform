using CCE.Domain.Common;

namespace CCE.Domain.Community.Events;

/// <summary>
/// Raised on the <see cref="Post"/> aggregate when a user casts, changes, or retracts a vote.
/// Translated to a <c>VoteCreatedIntegrationEvent</c> by a bridge handler and relayed to the Worker
/// for Redis hot-counter updates and debounced realtime fan-out.
/// </summary>
public sealed record PostVotedEvent(
    System.Guid PostId,
    System.Guid UserId,
    int Direction,        // +1 = up, -1 = down, 0 = retract
    int UpvoteCount,
    int DownvoteCount,
    double Score,
    System.DateTimeOffset OccurredOn) : IDomainEvent;
