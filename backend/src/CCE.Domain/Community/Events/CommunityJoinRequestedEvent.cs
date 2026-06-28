using CCE.Domain.Common;

namespace CCE.Domain.Community.Events;

/// <summary>
/// Raised on the <see cref="Community"/> aggregate when a user submits a join request to a private
/// community. Translated to a <c>CommunityJoinRequestedIntegrationEvent</c> by a bridge handler and
/// relayed to the Worker for moderator notifications. Carries the real persisted join-request id.
/// </summary>
public sealed record CommunityJoinRequestedEvent(
    System.Guid RequestId,
    System.Guid CommunityId,
    System.Guid UserId,
    System.DateTimeOffset OccurredOn) : IDomainEvent;
