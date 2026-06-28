namespace CCE.Application.Common.Messaging.IntegrationEvents;

/// <summary>
/// Published when an Event is scheduled. Captured by the EF outbox atomically with the
/// schedule transaction. <c>CCE.Worker</c>'s <c>ContentNotificationConsumer</c> fans out
/// to newsletter subscribers.
/// </summary>
public sealed record EventScheduledIntegrationEvent(
    System.Guid EventId,
    System.Guid TopicId,
    System.DateTimeOffset StartsOn,
    System.DateTimeOffset EndsOn,
    System.DateTimeOffset OccurredOn);
