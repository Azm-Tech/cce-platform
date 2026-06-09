namespace CCE.Application.Common.Messaging.IntegrationEvents;

/// <summary>
/// Raised when a user upvotes, downvotes, or retracts a vote on a post. Captured by the
/// EF outbox in the same transaction as the vote row + denormalized counter update.
/// Triggers Redis hot-counter bumps and debounced SignalR pushes in the Worker.
/// </summary>
public sealed record VoteCreatedIntegrationEvent(
    System.Guid PostId,
    System.Guid UserId,
    int Direction,        // +1 = up, -1 = down, 0 = retract
    int UpvoteCount,
    int DownvoteCount,
    double Score);
