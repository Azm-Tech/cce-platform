namespace CCE.Application.Common.Messaging.IntegrationEvents;

/// <summary>
/// Raised when a user submits a join request to a private community. Captured by the EF
/// outbox in the same transaction as the join-request row. Triggers moderator notifications
/// in the Worker.
/// </summary>
public sealed record CommunityJoinRequestedIntegrationEvent(
    System.Guid RequestId,
    System.Guid CommunityId,
    System.Guid UserId,
    System.DateTimeOffset RequestedOn);
