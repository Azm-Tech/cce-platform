namespace CCE.Application.Common.Messaging.IntegrationEvents;

/// <summary>
/// Published when a Resource transitions to published. Captured by the EF outbox atomically
/// with the publish transaction. <c>CCE.Worker</c>'s <c>ContentNotificationConsumer</c> fans out
/// to newsletter subscribers.
/// </summary>
public sealed record ResourcePublishedIntegrationEvent(
    System.Guid ResourceId,
    System.Guid CategoryId,
    System.Guid? CountryId,
    System.Guid UploadedById,
    System.DateTimeOffset PublishedOn);
