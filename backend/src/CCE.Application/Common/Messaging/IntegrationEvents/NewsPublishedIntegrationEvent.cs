namespace CCE.Application.Common.Messaging.IntegrationEvents;

/// <summary>
/// Published when a News article transitions to published. Captured by the EF outbox atomically
/// with the publish transaction. <c>CCE.Worker</c>'s <c>ContentNotificationConsumer</c> fans out
/// to newsletter subscribers.
/// </summary>
public sealed record NewsPublishedIntegrationEvent(
    System.Guid NewsId,
    System.Guid TopicId,
    System.Guid AuthorId,
    System.DateTimeOffset PublishedOn);
