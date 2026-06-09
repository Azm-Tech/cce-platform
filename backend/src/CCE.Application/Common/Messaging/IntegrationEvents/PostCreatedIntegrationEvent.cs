namespace CCE.Application.Common.Messaging.IntegrationEvents;

/// <summary>
/// Raised when a post transitions from Draft to Published. Carried over the bus via
/// <see cref="IIntegrationEventPublisher"/> and captured into the EF outbox atomically
/// with the aggregate save. Triggers feed fan-out, ranking rebuild, and community/topic
/// real-time pushes in the Worker.
/// </summary>
public sealed record PostCreatedIntegrationEvent(
    System.Guid PostId,
    System.Guid CommunityId,
    System.Guid TopicId,
    System.Guid AuthorId,
    System.DateTimeOffset PublishedOn,
    bool IsExpert,
    string Locale);
